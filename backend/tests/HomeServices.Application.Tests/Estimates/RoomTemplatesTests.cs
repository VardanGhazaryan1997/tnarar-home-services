using HomeServices.Application.Estimates;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Estimates;
using HomeServices.Domain.Localization;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Tests.Estimates;

public class RoomTemplatesTests
{
    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly WorkItem _tiling;
    private readonly WorkItem _toilet;
    private readonly WorkItem _sockets;
    private readonly WorkItem _hidden;

    public RoomTemplatesTests()
    {
        var category = Category.Create("renovation", LocalizedText.Empty.With("hy", "x"), 1);
        _db.Categories.Add(category);
        _tiling = Item(category, "floor-tiling", WorkUnit.SquareMeter, WorkSurface.Floor, new PriceRange(5000, 6500, 9000));
        _toilet = Item(category, "toilet", WorkUnit.Piece, WorkSurface.None, new PriceRange(10000, 15000, 22000));
        _sockets = Item(category, "sockets", WorkUnit.Point, WorkSurface.None, new PriceRange(2500, 3500, 5000));
        _hidden = Item(category, "hidden", WorkUnit.Piece, WorkSurface.None, new PriceRange(1, 1, 1));
        _hidden.Deactivate();
        _db.RoomTemplates.AddRange(
            RoomTemplate.Create(RoomType.Bathroom, _tiling.Id, 1),
            RoomTemplate.Create(RoomType.Bathroom, _toilet.Id, 2, quantity: 1m),
            RoomTemplate.Create(RoomType.Bathroom, _hidden.Id, 3, quantity: 1m),
            RoomTemplate.Create(RoomType.Bedroom, _sockets.Id, 1, quantityPerSquareMeter: 0.25m));
        _db.SaveChanges();
    }

    private WorkItem Item(Category category, string slug, WorkUnit unit, WorkSurface surface, PriceRange price)
    {
        var item = WorkItem.Create(category.Id, slug, LocalizedText.Empty.With("hy", slug).With("en", slug.ToUpperInvariant()), unit, surface, 1);
        item.SetPrice(price);
        _db.WorkItems.Add(item);
        return item;
    }

    private Task<EstimateMeasurementDto> QuickAsync(RoomType type, decimal area, bool oldBuilding = false) =>
        new QuickEstimateHandler(_db, new FakeCurrentLanguage("en")).HandleAsync(new QuickEstimate(type, area, null, oldBuilding), CancellationToken.None);

    [Fact]
    public async Task Every_room_type_lists_its_usual_active_work_in_order()
    {
        var templates = await new GetRoomTemplatesHandler(_db, new FakeCurrentLanguage("en")).HandleAsync(new GetRoomTemplates(), CancellationToken.None);

        templates.Select(t => t.Type).ShouldBe(Enum.GetValues<RoomType>());
        var bathroom = templates.Single(t => t.Type == RoomType.Bathroom);
        bathroom.Items.Select(i => i.Slug).ShouldBe(new[] { "floor-tiling", "toilet" });
        bathroom.Items[0].Name.ShouldBe("FLOOR-TILING");
        bathroom.Items[1].Quantity.ShouldBe(1m);
        bathroom.Items[1].MarketTypical.ShouldBe(15000);
        templates.Single(t => t.Type == RoomType.Bedroom).Items.Single().QuantityPerSquareMeter.ShouldBe(0.25m);
        templates.Single(t => t.Type == RoomType.Garage).Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_quick_estimate_prices_the_usual_work_of_a_room()
    {
        var result = await QuickAsync(RoomType.Bathroom, 4m);

        var room = result.Rooms.Single();
        room.FloorArea.ShouldBe(4m);
        room.Lines.Select(l => (l.Name, l.Quantity)).ShouldBe(new[] { ("FLOOR-TILING", (decimal?)4m), ("TOILET", 1m) });
        (result.TotalMin, result.TotalTypical, result.TotalMax).ShouldBe((30000, 41000, 58000));
    }

    [Fact]
    public async Task Counts_per_square_metre_round_up()
    {
        var result = await QuickAsync(RoomType.Bedroom, 13m);

        result.Rooms.Single().Lines.Single().Quantity.ShouldBe(4m); // 13 × 0.25 = 3.25 → 4 sockets
    }

    [Fact]
    public async Task Staff_replace_a_template()
    {
        var saved = await new SetRoomTemplateHandler(_db).HandleAsync(
            new SetRoomTemplate(RoomType.Bathroom, [new RoomTemplateInput(_toilet.Id, 2m), new RoomTemplateInput(_tiling.Id)]),
            CancellationToken.None);

        saved.Items.Select(i => i.WorkItemId).ShouldBe(new[] { _toilet.Id, _tiling.Id });
        var all = await new GetAdminRoomTemplatesHandler(_db).HandleAsync(new GetAdminRoomTemplates(), CancellationToken.None);
        all.Single(t => t.Type == RoomType.Bathroom).Items.Select(i => (i.WorkItemId, i.Quantity)).ShouldBe(new[] { (_toilet.Id, (decimal?)2m), (_tiling.Id, null) });
        (await _db.RoomTemplates.CountAsync(t => t.RoomType == RoomType.Bedroom)).ShouldBe(1);

        (await Should.ThrowAsync<DomainException>(() => new SetRoomTemplateHandler(_db).HandleAsync(
            new SetRoomTemplate(RoomType.Bathroom, [new RoomTemplateInput(Guid.NewGuid())]), CancellationToken.None)))
            .Code.ShouldBe("room_template.work_item_not_found");
    }

    [Fact]
    public void Template_counts_are_checked()
    {
        Should.Throw<DomainException>(() => RoomTemplate.Create(RoomType.Bedroom, Guid.NewGuid(), 1, 0m)).Code.ShouldBe("room_template.quantity_invalid");
        Should.Throw<DomainException>(() => RoomTemplate.Create(RoomType.Bedroom, Guid.NewGuid(), 1, 1m, 0.5m)).Code.ShouldBe("room_template.quantity_invalid");
        Should.Throw<DomainException>(() => RoomTemplate.Create((RoomType)99, Guid.NewGuid(), 1)).Code.ShouldBe("room_template.type_invalid");
        RoomTemplate.Create(RoomType.Bedroom, Guid.NewGuid(), 1, quantityPerSquareMeter: 0.25m).DefaultQuantity(2m).ShouldBe(1m);
        RoomTemplate.Create(RoomType.Bedroom, Guid.NewGuid(), 1).DefaultQuantity(20m).ShouldBeNull();
    }

    [Fact]
    public async Task Quick_estimates_are_validated()
    {
        var validator = new QuickEstimateValidator();

        (await validator.ValidateAsync(new QuickEstimate(RoomType.Kitchen, 0m))).Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "area.invalid" });
        (await validator.ValidateAsync(new QuickEstimate(RoomType.Kitchen, 10m, 1m))).Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "height.invalid" });
        (await validator.ValidateAsync(new QuickEstimate((RoomType)99, 10m))).Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "room.type_invalid" });
    }
}
