using HomeServices.Application.Estimates;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Estimates;
using HomeServices.Domain.Localization;

namespace HomeServices.Application.Tests.Estimates;

public class MeasureEstimateTests
{
    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly WorkItem _plastering;
    private readonly WorkItem _skirting;
    private readonly WorkItem _sockets;

    public MeasureEstimateTests()
    {
        var category = Category.Create("plastering", LocalizedText.Empty.With("hy", "Սվաղ"), 1);
        _db.Categories.Add(category);
        _plastering = Item(category, "wall-plastering", WorkUnit.SquareMeter, WorkSurface.Wall, "Wall plastering");
        _skirting = Item(category, "skirting", WorkUnit.RunningMeter, WorkSurface.Floor, "Skirting");
        _sockets = Item(category, "sockets", WorkUnit.Point, WorkSurface.None, "Sockets");
        var hidden = Item(category, "hidden", WorkUnit.Piece, WorkSurface.None, "Hidden");
        hidden.Deactivate();
        _db.SaveChanges();
    }

    private WorkItem Item(Category category, string slug, WorkUnit unit, WorkSurface surface, string english)
    {
        var item = WorkItem.Create(category.Id, slug, LocalizedText.Empty.With("hy", slug).With("en", english), unit, surface, 1);
        _db.WorkItems.Add(item);
        return item;
    }

    private Task<EstimateMeasurementDto> MeasureAsync(params RoomInput[] rooms) =>
        new MeasureEstimateHandler(_db, new FakeCurrentLanguage("en")).HandleAsync(new MeasureEstimate(rooms), CancellationToken.None);

    [Fact]
    public async Task Measures_each_room_and_the_quantity_of_its_work()
    {
        var result = await MeasureAsync(new RoomInput(
            RoomType.Bedroom,
            4m,
            3m,
            null,
            null,
            [new OpeningInput(OpeningKind.Door, 0.9m, 2.1m), new OpeningInput(OpeningKind.Window, 1.5m, 1.4m)],
            [new LineInput(_plastering.Id), new LineInput(_skirting.Id, 15m), new LineInput(_sockets.Id)]));

        var room = result.Rooms.ShouldHaveSingleItem();
        room.FloorArea.ShouldBe(12m);
        room.WallArea.ShouldBe(33.81m); // default height 2.7 m
        room.SkirtingLength.ShouldBe(13.1m);

        var plastering = room.Lines[0];
        plastering.Name.ShouldBe("Wall plastering");
        (plastering.MeasuredQuantity, plastering.Quantity, plastering.NeedsQuantity).ShouldBe(((decimal?)33.81m, (decimal?)33.81m, false));
        var skirting = room.Lines[1];
        (skirting.MeasuredQuantity, skirting.Quantity).ShouldBe(((decimal?)13.1m, (decimal?)15m)); // the customer's own quantity wins
        var sockets = room.Lines[2];
        (sockets.MeasuredQuantity, sockets.Quantity, sockets.NeedsQuantity).ShouldBe(((decimal?)null, (decimal?)null, true));
        sockets.Unit.ShouldBe(WorkUnit.Point);
    }

    [Fact]
    public async Task Rooms_given_by_area_and_without_work_are_measured_too()
    {
        var result = await MeasureAsync(new RoomInput(RoomType.Bathroom, null, null, 4m, 2.5m), new RoomInput(RoomType.Hallway, 2m, 1m, null, 2.5m));

        result.Rooms.Select(r => r.FloorArea).ShouldBe(new[] { 4m, 2m });
        result.Rooms[0].WallArea.ShouldBe(20m);
        result.Rooms.ShouldAllBe(r => r.Lines.Count == 0);
    }

    [Fact]
    public async Task Unknown_or_hidden_work_and_impossible_sizes_are_refused()
    {
        var hidden = _db.WorkItems.Single(w => w.Slug == "hidden");

        (await Should.ThrowAsync<DomainException>(() => MeasureAsync(new RoomInput(RoomType.Other, 4m, 3m, null, null, null, [new LineInput(hidden.Id)]))))
            .Code.ShouldBe("estimate.work_item_not_found");
        (await Should.ThrowAsync<DomainException>(() => MeasureAsync(new RoomInput(RoomType.Other, 4m, null, null, null))))
            .Code.ShouldBe("estimate.size_invalid");
    }

    [Fact]
    public async Task The_validator_checks_rooms_and_lines()
    {
        var validator = new MeasureEstimateValidator();
        var line = new LineInput(Guid.NewGuid());

        (await validator.ValidateAsync(new MeasureEstimate([]))).Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "rooms.count_invalid" });
        (await validator.ValidateAsync(new MeasureEstimate([new RoomInput((RoomType)99, 1m, 1m, null, null, null, [line, line])])))
            .Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "room.type_invalid", "room.duplicate_lines" }, ignoreOrder: true);
        (await validator.ValidateAsync(new MeasureEstimate([new RoomInput(RoomType.Other, 1m, 1m, null, null, null, [new LineInput(Guid.NewGuid(), -1m)])])))
            .Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "line.quantity_invalid" });
    }
}
