using HomeServices.Application.Demo;
using HomeServices.Application.Stores;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Demo;

public class SeedDemoStoresTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 9, 0, 0, TimeSpan.Zero);
    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();

    public SeedDemoStoresTests()
    {
        var text = LocalizedText.Empty.With("hy", "x");
        var region = Region.Create("yerevan", text, 1);
        _db.Regions.Add(region);
        _db.Cities.Add(City.Create("yerevan", text, 1, region.Id));
        var order = 1;
        foreach (var slug in DemoStoresSlugs())
        {
            _db.Materials.Add(Material.Create(slug, text, MaterialUnit.Kilogram, 100, 200, 300, order++, Now));
        }

        _db.SaveChanges();
    }

    private static IEnumerable<string> DemoStoresSlugs() =>
    [
        "finishing-putty", "base-putty", "gypsum-plaster", "tile-adhesive", "tile-grout", "primer", "interior-paint", "screed-mix",
        "self-levelling-mix", "waterproofing-mix", "silicone-sealant", "wall-tiles", "laminate", "drywall-sheet", "metal-profile", "corner-bead",
    ];

    private Task<int> SeedAsync() =>
        new SeedDemoStoresHandler(_db, Options.Create(new StorePricingSettings()), new FakeClock(Now))
            .HandleAsync(new SeedDemoStores(), CancellationToken.None);

    [Fact]
    public async Task Adds_three_clearly_fictional_stores_with_branches()
    {
        (await SeedAsync()).ShouldBe(3);

        var stores = await _db.Stores.Include(s => s.Branches).OrderBy(s => s.SortOrder).ToListAsync();
        stores.Select(s => s.Name).ShouldBe(new[] { "Demo Building Market", "Demo Stroy", "Demo Home Center" });
        stores.ShouldAllBe(s => s.Website == null && s.IsActive);
        stores.Select(s => s.Branches.Count).ShouldBe(new[] { 2, 2, 1 }); // no Gyumri in this catalog
    }

    [Fact]
    public async Task Links_every_product_to_a_material_and_sells_each_in_two_stores()
    {
        await SeedAsync();

        var products = await _db.StoreProducts.ToListAsync();
        products.Count.ShouldBe(44);
        products.ShouldAllBe(p => p.MaterialId != null && p.Grade != null && p.Price > 0 && p.IsActive);
        products.GroupBy(p => p.Name).ShouldAllBe(g => g.Count() == 2);
        products.Select(p => (p.StoreId, p.Code)).Distinct().Count().ShouldBe(products.Count);
        products.Count(p => p.SalePrice != null).ShouldBeGreaterThan(0);
        products.Count(p => !p.InStock).ShouldBe(1);
    }

    [Fact]
    public async Task Gives_materials_a_market_price_from_the_demo_stores()
    {
        await SeedAsync();

        var putty = await _db.Materials.SingleAsync(m => m.Slug == "finishing-putty");
        putty.MarketStandard.ShouldNotBeNull();
        putty.MarketPremium.ShouldNotBeNull();
        putty.MarketStoreCount.ShouldBe(3);
    }

    [Fact]
    public async Task Runs_once()
    {
        await SeedAsync();
        var products = await _db.StoreProducts.CountAsync();

        (await SeedAsync()).ShouldBe(0);

        (await _db.Stores.CountAsync()).ShouldBe(3);
        (await _db.StoreProducts.CountAsync()).ShouldBe(products);
    }

    [Fact]
    public async Task Skips_products_whose_material_is_missing()
    {
        _db.Materials.RemoveRange(_db.Materials.Where(m => m.Slug != "primer"));
        await _db.SaveChangesAsync();

        await SeedAsync();

        (await _db.StoreProducts.CountAsync()).ShouldBe(4); // the two primer products, each in two stores
    }

    [Fact]
    public async Task Records_a_price_for_every_product_and_an_older_different_price_for_some()
    {
        await SeedAsync();

        var products = await _db.StoreProducts.ToListAsync();
        var rows = await _db.StoreProductPrices.ToListAsync();
        products.ShouldAllBe(p => rows.Any(r => r.StoreProductId == p.Id && r.Price == p.Price));
        var older = rows.Where(r => r.RecordedAt < Now.AddDays(-90)).ToList();
        older.Count.ShouldBeGreaterThan(0);
        older.ShouldAllBe(r => r.Price != products.Single(p => p.Id == r.StoreProductId).Price);
    }
}
