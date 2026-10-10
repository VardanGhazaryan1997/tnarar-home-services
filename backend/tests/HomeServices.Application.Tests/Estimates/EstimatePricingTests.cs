using HomeServices.Application.Estimates;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Estimates;
using HomeServices.Domain.Localization;

namespace HomeServices.Application.Tests.Estimates;

public class EstimatePricingTests
{
    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly WorkItem _painting;
    private readonly WorkItem _laminate;
    private readonly WorkItem _toilet;
    private readonly WorkItem _unpriced;

    public EstimatePricingTests()
    {
        var category = Category.Create("renovation", LocalizedText.Empty.With("hy", "x"), 1);
        _db.Categories.Add(category);
        _painting = Item(category, "wall-painting", WorkUnit.SquareMeter, WorkSurface.Wall, new PriceRange(1000, 1500, 2500));
        _laminate = Item(category, "laminate", WorkUnit.SquareMeter, WorkSurface.Floor, new PriceRange(1500, 2000, 3000));
        _toilet = Item(category, "toilet", WorkUnit.Piece, WorkSurface.None, new PriceRange(10000, 15000, 22000));
        _unpriced = Item(category, "unpriced", WorkUnit.SquareMeter, WorkSurface.Floor, null);
        _db.SaveChanges();
    }

    private WorkItem Item(Category category, string slug, WorkUnit unit, WorkSurface surface, PriceRange? price)
    {
        var item = WorkItem.Create(category.Id, slug, LocalizedText.Empty.With("hy", slug), unit, surface, 1);
        item.SetPrice(price);
        _db.WorkItems.Add(item);
        return item;
    }

    private Task<EstimateMeasurementDto> MeasureAsync(bool oldBuilding, params RoomInput[] rooms) =>
        new MeasureEstimateHandler(_db, new FakeCurrentLanguage("hy")).HandleAsync(new MeasureEstimate(rooms, oldBuilding), CancellationToken.None);

    [Fact]
    public async Task Lines_are_priced_from_their_quantity_and_the_market_range_and_add_up()
    {
        var result = await MeasureAsync(false, new RoomInput(
            RoomType.Bedroom, 4m, 3m, null, 2.5m, null,
            [new LineInput(_painting.Id), new LineInput(_laminate.Id), new LineInput(_toilet.Id, 1m), new LineInput(_unpriced.Id)]));

        var room = result.Rooms.Single();
        var painting = room.Lines[0]; // 35 m² of walls
        (painting.PriceMin, painting.PriceTypical, painting.PriceMax, painting.Factor).ShouldBe(((int?)35000, (int?)52500, (int?)87500, 1m));
        var laminate = room.Lines[1]; // 12 m² of floor
        (laminate.PriceMin, laminate.PriceTypical, laminate.PriceMax).ShouldBe(((int?)18000, (int?)24000, (int?)36000));
        room.Lines[2].PriceTypical.ShouldBe(15000);
        room.Lines[3].PriceTypical.ShouldBeNull();
        (room.TotalMin, room.TotalTypical, room.TotalMax).ShouldBe((63000, 91500, 145500));
        (result.TotalMin, result.TotalTypical, result.TotalMax, result.UnpricedLines).ShouldBe((63000, 91500, 145500, 1));
    }

    [Fact]
    public async Task Lines_without_a_quantity_are_left_out_of_the_totals()
    {
        var result = await MeasureAsync(false, new RoomInput(RoomType.Bathroom, null, null, 4m, null, null, [new LineInput(_toilet.Id)]));

        var toilet = result.Rooms.Single().Lines.Single();
        toilet.NeedsQuantity.ShouldBeTrue();
        toilet.PriceTypical.ShouldBeNull();
        (result.TotalTypical, result.UnpricedLines).ShouldBe((0, 1));
    }

    [Fact]
    public async Task Old_buildings_and_high_ceilings_cost_more()
    {
        var result = await MeasureAsync(
            true,
            new RoomInput(RoomType.LivingRoom, 4m, 3m, null, 3.2m, null, [new LineInput(_painting.Id), new LineInput(_laminate.Id)]));

        var room = result.Rooms.Single();
        room.Lines[0].Factor.ShouldBe(1.265m); // old building × high ceiling, on wall work
        room.Lines[1].Factor.ShouldBe(1.15m); // floor work: old building only
        room.Lines[1].PriceTypical.ShouldBe(27600); // 12 m² × 2,000 × 1.15
    }

    [Theory]
    [InlineData(10.5, 1234, 1, 13000)]
    [InlineData(0.5, 1000, 1, 500)]
    [InlineData(3, 150000, 1, 450000)]
    public void Amounts_are_rounded_to_tidy_numbers(double quantity, int unitPrice, double factor, int expected) =>
        EstimatePricing.Price((decimal)quantity, new PriceRange(unitPrice, unitPrice, unitPrice), (decimal)factor)!.Typical.ShouldBe(expected);

    [Fact]
    public void Work_without_a_market_price_has_no_price() => EstimatePricing.Price(10m, null, 1m).ShouldBeNull();
}
