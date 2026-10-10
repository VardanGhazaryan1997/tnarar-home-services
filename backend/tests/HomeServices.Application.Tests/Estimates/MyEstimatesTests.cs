using HomeServices.Application.Errors;
using HomeServices.Application.Estimates;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Estimates;
using HomeServices.Domain.Localization;

namespace HomeServices.Application.Tests.Estimates;

public class MyEstimatesTests
{
    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly FakeCurrentLanguage _language = new("en");
    private readonly Guid _userId = Guid.NewGuid();
    private readonly WorkItem _painting;
    private readonly WorkItem _toilet;

    public MyEstimatesTests()
    {
        var category = Category.Create("renovation", LocalizedText.Empty.With("hy", "x"), 1);
        _db.Categories.Add(category);
        _painting = Item(category, "wall-painting", WorkUnit.SquareMeter, WorkSurface.Wall, new PriceRange(1000, 1500, 2000));
        _toilet = Item(category, "toilet", WorkUnit.Piece, WorkSurface.None, new PriceRange(10000, 15000, 20000));
        _db.SaveChanges();
    }

    private WorkItem Item(Category category, string slug, WorkUnit unit, WorkSurface surface, PriceRange price)
    {
        var item = WorkItem.Create(category.Id, slug, LocalizedText.Empty.With("hy", slug).With("en", slug), unit, surface, 1);
        item.SetPrice(price);
        _db.WorkItems.Add(item);
        return item;
    }

    private FakeCurrentUser Me => new(_userId);

    private SaveMyEstimateHandler Saver(FakeCurrentUser? user = null) => new(_db, user ?? Me, _language, TimeProvider.System);

    private SaveMyEstimate Flat(Guid? id = null, string title = "My flat") => new(
        id,
        title,
        null,
        OldBuilding: false,
        [
            new EstimateRoomInput("Bedroom", RoomType.Bedroom, 4m, 3m, null, 2.5m, [new OpeningInput(OpeningKind.Door, 1m, 2m)], [new LineInput(_painting.Id)]),
            new EstimateRoomInput("Bath", RoomType.Bathroom, null, null, 4m, null, Lines: [new LineInput(_toilet.Id, 1m), new LineInput(_painting.Id, 10m)]),
        ]);

    [Fact]
    public async Task An_estimate_is_saved_with_its_rooms_and_prices()
    {
        var saved = await Saver().HandleAsync(Flat(), CancellationToken.None);

        saved.Title.ShouldBe("My flat");
        saved.Rooms.Select(r => r.Name).ShouldBe(new[] { "Bedroom", "Bath" });
        saved.Rooms[0].Openings.Single().Kind.ShouldBe(OpeningKind.Door);
        saved.Rooms[1].Height.ShouldBe(RoomSize.DefaultHeight);
        saved.Rooms[1].Lines.Select(l => l.WorkItemId).ShouldBe(new[] { _toilet.Id, _painting.Id });
        // Bedroom walls: 14 m × 2.5 m − a 2 m² door = 33 m² × 1,000–2,000.
        saved.Measurement.Rooms[0].Lines.Single().Quantity.ShouldBe(33m);
        saved.Measurement.Rooms[0].TotalMin.ShouldBe(33000);
        saved.Measurement.TotalTypical.ShouldBe(49500 + 15000 + 15000);
        saved.ShareToken.ShouldBeNull();

        var list = await new GetMyEstimatesHandler(_db, Me, _language).HandleAsync(new GetMyEstimates(), CancellationToken.None);
        list.Single().ShouldSatisfyAllConditions(
            e => e.Id.ShouldBe(saved.Id),
            e => e.RoomCount.ShouldBe(2),
            e => e.TotalTypical.ShouldBe(saved.Measurement.TotalTypical),
            e => e.IsShared.ShouldBeFalse());
    }

    [Fact]
    public async Task Saving_again_replaces_everything()
    {
        var saved = await Saver().HandleAsync(Flat(), CancellationToken.None);

        var changed = await Saver().HandleAsync(
            new SaveMyEstimate(saved.Id, "Renamed", null, OldBuilding: true, [new EstimateRoomInput("Kitchen", RoomType.Kitchen, null, null, 9m, null)]),
            CancellationToken.None);

        changed.Id.ShouldBe(saved.Id);
        changed.Title.ShouldBe("Renamed");
        changed.OldBuilding.ShouldBeTrue();
        changed.Rooms.Single().Name.ShouldBe("Kitchen");
        _db.Set<EstimateRoom>().Count().ShouldBe(1);
        _db.Set<EstimateLine>().Count().ShouldBe(0);
    }

    [Fact]
    public async Task Others_estimates_are_not_found()
    {
        var saved = await Saver().HandleAsync(Flat(), CancellationToken.None);
        var stranger = new FakeCurrentUser(Guid.NewGuid());

        (await Should.ThrowAsync<NotFoundException>(() => new GetMyEstimateHandler(_db, stranger, _language).HandleAsync(new GetMyEstimate(saved.Id), CancellationToken.None)))
            .Code.ShouldBe("estimate.not_found");
        await Should.ThrowAsync<NotFoundException>(() => Saver(stranger).HandleAsync(Flat(saved.Id), CancellationToken.None));
        await Should.ThrowAsync<NotFoundException>(() => new DeleteMyEstimateHandler(_db, stranger).HandleAsync(new DeleteMyEstimate(saved.Id), CancellationToken.None));
        (await new GetMyEstimatesHandler(_db, stranger, _language).HandleAsync(new GetMyEstimates(), CancellationToken.None)).ShouldBeEmpty();
        await Should.ThrowAsync<UnauthorizedException>(() => Saver(new FakeCurrentUser(null)).HandleAsync(Flat(), CancellationToken.None));
        await Should.ThrowAsync<UnauthorizedException>(() => Saver(new FakeCurrentUser(_userId, isStaff: true)).HandleAsync(Flat(), CancellationToken.None));
    }

    [Fact]
    public async Task An_estimate_is_deleted()
    {
        var saved = await Saver().HandleAsync(Flat(), CancellationToken.None);

        (await new DeleteMyEstimateHandler(_db, Me).HandleAsync(new DeleteMyEstimate(saved.Id), CancellationToken.None)).ShouldBeTrue();

        _db.Estimates.Count().ShouldBe(0);
    }

    [Fact]
    public async Task A_shared_estimate_opens_by_its_link_until_sharing_stops()
    {
        var saved = await Saver().HandleAsync(Flat(), CancellationToken.None);
        var sharer = new ShareMyEstimateHandler(_db, Me, _language);

        var shared = await sharer.HandleAsync(new ShareMyEstimate(saved.Id, Share: true), CancellationToken.None);
        shared.ShareToken.ShouldNotBeNull();
        shared.ShareToken!.Length.ShouldBe(22);
        (await sharer.HandleAsync(new ShareMyEstimate(saved.Id, Share: true), CancellationToken.None)).ShareToken.ShouldBe(shared.ShareToken);

        var viewer = new GetSharedEstimateHandler(_db, _language);
        var seen = await viewer.HandleAsync(new GetSharedEstimate(shared.ShareToken), CancellationToken.None);
        seen.Title.ShouldBe("My flat");
        seen.Measurement.TotalTypical.ShouldBe(saved.Measurement.TotalTypical);

        await sharer.HandleAsync(new ShareMyEstimate(saved.Id, Share: false), CancellationToken.None);
        (await Should.ThrowAsync<NotFoundException>(() => viewer.HandleAsync(new GetSharedEstimate(shared.ShareToken), CancellationToken.None))).Code.ShouldBe("estimate.not_found");
        await Should.ThrowAsync<NotFoundException>(() => viewer.HandleAsync(new GetSharedEstimate(""), CancellationToken.None));
    }

    [Fact]
    public async Task Work_hidden_later_is_left_out_of_the_prices_but_kept()
    {
        var saved = await Saver().HandleAsync(Flat(), CancellationToken.None);
        _toilet.Deactivate();
        await _db.SaveChangesAsync();

        var loaded = await new GetMyEstimateHandler(_db, Me, _language).HandleAsync(new GetMyEstimate(saved.Id), CancellationToken.None);

        loaded.Rooms[1].Lines.Count.ShouldBe(2);
        loaded.Measurement.Rooms[1].Lines.Select(l => l.WorkItemId).ShouldBe(new[] { _painting.Id });
        (await Saver().HandleAsync(Flat(saved.Id), CancellationToken.None)).Rooms[1].Lines.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Unknown_work_and_places_are_refused()
    {
        var unknownWork = Flat() with { Rooms = [new EstimateRoomInput("Hall", RoomType.Hallway, null, null, 5m, null, Lines: [new LineInput(Guid.NewGuid())])] };
        (await Should.ThrowAsync<DomainException>(() => Saver().HandleAsync(unknownWork, CancellationToken.None))).Code.ShouldBe("estimate.work_item_not_found");
        (await Should.ThrowAsync<DomainException>(() => Saver().HandleAsync(Flat() with { CityId = Guid.NewGuid() }, CancellationToken.None))).Code.ShouldBe("estimate.city_not_found");
        _db.Estimates.Count().ShouldBe(0);
    }

    [Fact]
    public void Requests_are_validated()
    {
        var validator = new SaveMyEstimateValidator();
        validator.Validate(Flat()).IsValid.ShouldBeTrue();
        validator.Validate(Flat(title: " ")).Errors.Single().ErrorCode.ShouldBe("title.invalid");
        validator.Validate(Flat() with { Rooms = [new EstimateRoomInput("", RoomType.Bedroom, null, null, 5m, null)] }).Errors.Single().ErrorCode.ShouldBe("room.name_invalid");
        validator.Validate(Flat() with { Rooms = [new EstimateRoomInput("A", RoomType.Bedroom, null, null, 5m, null, Lines: [new LineInput(_toilet.Id), new LineInput(_toilet.Id)])] })
            .Errors.Single().ErrorCode.ShouldBe("room.duplicate_lines");
        validator.Validate(Flat() with { Rooms = [] }).IsValid.ShouldBeTrue();
    }
}
