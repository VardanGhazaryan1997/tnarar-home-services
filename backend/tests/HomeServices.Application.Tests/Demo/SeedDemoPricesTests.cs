using HomeServices.Application.Catalog;
using HomeServices.Application.Demo;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Demo;

public class SeedDemoPricesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 9, 0, 0, TimeSpan.Zero);
    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly List<WorkItem> _items = [];

    public SeedDemoPricesTests()
    {
        var text = LocalizedText.Empty.With("hy", "x");
        var plumbing = Category.Create("plumbing", text, 1);
        var cleaning = Category.Create("cleaning", text, 2);
        var leaks = Category.Create("plumbing-leaks", text, 1, plumbing.Id);
        var regular = Category.Create("cleaning-regular", text, 1, cleaning.Id);
        _db.Categories.AddRange(plumbing, cleaning, leaks, regular);
        for (var i = 0; i < 10; i++)
        {
            _items.Add(Item(leaks, $"leak-{i}", 10_000 + (i * 1_000)));
            _items.Add(Item(regular, $"clean-{i}", 200 + (i * 10)));
        }

        _items.Add(Item(leaks, "unpriced", null));
        var yerevanRegion = Region.Create("yerevan", text, 1);
        var yerevan = City.Create("yerevan", text, 1, yerevanRegion.Id);
        yerevan.AddDistrict("kentron", text, 1);
        _db.Regions.Add(yerevanRegion);
        _db.Cities.Add(yerevan);
        _db.SaveChanges();
    }

    private WorkItem Item(Category category, string slug, int? typical)
    {
        var item = WorkItem.Create(category.Id, slug, LocalizedText.Empty.With("hy", slug), WorkUnit.Piece, WorkSurface.None, 1);
        if (typical is { } price)
        {
            item.SetPrice(new PriceRange(price / 2, price, price * 2));
        }

        _db.WorkItems.Add(item);
        return item;
    }

    private Task<int> SeedPricesAsync(int minPartners = 2) =>
        new SeedDemoPricesHandler(_db, Options.Create(new PricingSettings { MinPartners = minPartners }), new FakeClock(Now))
            .HandleAsync(new SeedDemoPrices(), CancellationToken.None);

    private async Task SeedPartnersAsync() =>
        await new SeedDemoPartnersHandler(_db, new FakeFileStorage(), new FakeClock(Now)).HandleAsync(new SeedDemoPartners(), CancellationToken.None);

    [Fact]
    public async Task Demo_partners_get_prices_around_the_market_price_for_their_services()
    {
        await SeedPartnersAsync();

        var partners = await SeedPricesAsync();

        partners.ShouldBeGreaterThan(1);
        var prices = await _db.PartnerPrices.ToListAsync();
        prices.ShouldNotBeEmpty();
        prices.Select(p => p.PartnerProfileId).Distinct().Count().ShouldBe(partners);
        var unpriced = _items.Single(i => i.Slug == "unpriced");
        prices.ShouldNotContain(p => p.WorkItemId == unpriced.Id);
        foreach (var price in prices)
        {
            var typical = _items.Single(i => i.Id == price.WorkItemId).PriceTypical!.Value;
            price.PriceFrom.ShouldBeInRange((int)(typical * 0.75), (int)(typical * 1.4));
            (price.PriceTo ?? price.PriceFrom).ShouldBeGreaterThanOrEqualTo(price.PriceFrom);
        }

        prices.ShouldContain(p => p.PriceTo != null);
    }

    [Fact]
    public async Task Market_ranges_are_recalculated_from_the_demo_prices()
    {
        await SeedPartnersAsync();

        // The test catalog is small, so few work items get prices from two demo partners: one is enough here.
        await SeedPricesAsync(minPartners: 1);

        _db.ChangeTracker.Clear();
        (await _db.WorkItems.CountAsync(w => w.MarketSource == MarketPriceSource.Partners)).ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Running_again_leaves_existing_prices_alone()
    {
        await SeedPartnersAsync();
        await SeedPricesAsync();
        var count = await _db.PartnerPrices.CountAsync();

        (await SeedPricesAsync()).ShouldBe(0);
        (await _db.PartnerPrices.CountAsync()).ShouldBe(count);
    }

    [Fact]
    public async Task Without_demo_partners_nothing_happens() => (await SeedPricesAsync()).ShouldBe(0);

    [Fact]
    public void The_same_partner_and_work_always_get_the_same_price()
    {
        var partner = Guid.NewGuid();

        DemoPriceFor(partner).ShouldBe(DemoPriceFor(partner));
        DemoPrice.Next(partner, "leak-1", 1).ShouldBeInRange(0, 0.9999);
    }

    private static (int, int?, bool) DemoPriceFor(Guid partner) => DemoPrice.For(partner, "leak-1", 10_000, company: false);
}
