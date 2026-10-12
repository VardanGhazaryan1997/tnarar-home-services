using HomeServices.Application.Abstractions;
using HomeServices.Application.Catalog;
using HomeServices.Application.Messaging;
using HomeServices.Application.Stores;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Demo;

/// <summary>
/// Adds three clearly fictional building-materials stores with branches and products linked to the catalog materials
/// (staging and local development only), so material prices, the comparison pages and "where to buy" have something to
/// show. Prices are the material's usual price times the pack size, nudged up or down per store. Does nothing when any
/// demo store already exists. Returns how many stores were created.
/// </summary>
public sealed record SeedDemoStores : ICommand<int>;

public sealed class SeedDemoStoresHandler(IAppDbContext db, IOptions<StorePricingSettings> settings, TimeProvider clock)
    : ICommandHandler<SeedDemoStores, int>
{
    public async Task<int> HandleAsync(SeedDemoStores command, CancellationToken cancellationToken)
    {
        var slugs = DemoStores.All.Select(s => s.Slug).ToList();
        if (await db.Stores.AnyAsync(s => slugs.Contains(s.Slug), cancellationToken))
        {
            return 0;
        }

        var materialSlugs = DemoStores.Products.Select(p => p.MaterialSlug).Distinct().ToList();
        var materials = await db.Materials.Where(m => materialSlugs.Contains(m.Slug)).ToDictionaryAsync(m => m.Slug, cancellationToken);
        var cities = await db.Cities.AsNoTracking().Where(c => c.IsActive).ToDictionaryAsync(c => c.Slug, c => c.Id, cancellationToken);
        var now = clock.GetUtcNow();
        var today = settings.Value.Today(now);
        var touched = new HashSet<Guid>();
        var created = 0;

        foreach (var (demo, storeIndex) in DemoStores.All.Select((demo, index) => (demo, index)))
        {
            var description = LocalizedText.Empty.With("hy", demo.DescriptionHy).With("en", demo.DescriptionEn);
            var store = Store.Create(demo.Slug, demo.Name, description, website: null, demo.Phone, logoFileId: null, storeIndex + 1);
            store.SetBranches(demo.Branches.Where(b => cities.ContainsKey(b.City)).Select(b =>
                new BranchInput(b.Name, cities[b.City], DistrictId: null, b.Address, demo.Phone, "Mon–Sat 9:00–19:00")));
            db.Stores.Add(store);

            foreach (var (product, productIndex) in DemoStores.Products.Select((product, index) => (product, index)))
            {
                // Every product is sold by two of the three stores, so each material has a market price.
                if (productIndex % 3 == storeIndex || !materials.TryGetValue(product.MaterialSlug, out var material))
                {
                    continue;
                }

                var price = DemoStores.PackPrice(material, product, storeIndex);
                var onSale = productIndex % 11 == 3;
                var item = StoreProduct.Create(
                    store.Id,
                    product.Name,
                    product.Brand,
                    $"{demo.CodePrefix}-{productIndex + 1:000}",
                    product.PackSize,
                    product.PackLabel,
                    price,
                    onSale ? MarketPrices.Tidy(price * 0.9) : null,
                    onSale ? today.AddDays(20) : null,
                    inStock: !(storeIndex == 2 && productIndex == 6),
                    photoFileId: null,
                    now);
                item.LinkTo(material.Id, product.Grade);
                db.StoreProducts.Add(item);
                touched.Add(material.Id);
            }

            created++;
        }

        await db.SaveChangesAsync(cancellationToken);
        await MaterialMarketPrices.RecalculateAsync(db, settings.Value, clock, touched, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return created;
    }
}

/// <summary>The demo stores and their products: invented names (never real shops), the same for every run.</summary>
internal static class DemoStores
{
    public sealed record Demo(
        string Slug, string Name, string CodePrefix, string Phone, string DescriptionHy, string DescriptionEn, Branch[] Branches);

    public sealed record Branch(string Name, string City, string Address);

    public sealed record Product(string MaterialSlug, MaterialGrade Grade, string Name, string Brand, decimal PackSize, string PackLabel);

    public static readonly Demo[] All =
    [
        new("demo-building-market", "Demo Building Market", "DBM", "+374 99 000 201",
            "Դեմո խանութ՝ փորձարկման համար։ Իրական խանութ չէ։",
            "A demo store for testing. Not a real shop.",
            [new("Demo Center", "yerevan", "1 Demo Street"), new("Demo North", "yerevan", "2 Sample Avenue"), new("Demo Gyumri", "gyumri", "3 Test Street")]),
        new("demo-stroy", "Demo Stroy", "DST", "+374 99 000 202",
            "Դեմո խանութ՝ փորձարկման համար։ Իրական խանութ չէ։",
            "A demo store for testing. Not a real shop.",
            [new("Demo Stroy Main", "yerevan", "4 Sample Street"), new("Demo Stroy South", "yerevan", "5 Test Avenue")]),
        new("demo-home-center", "Demo Home Center", "DHC", "+374 99 000 203",
            "Դեմո խանութ՝ փորձարկման համար։ Իրական խանութ չէ։",
            "A demo store for testing. Not a real shop.",
            [new("Demo Home", "yerevan", "6 Demo Avenue")]),
    ];

    /// <summary>22 products; the store whose index equals the product's index modulo 3 doesn't sell it (44 in all).</summary>
    public static readonly Product[] Products =
    [
        new("finishing-putty", MaterialGrade.Standard, "Finishing putty 25 kg", "DemoMix", 25m, "25 kg bag"),
        new("finishing-putty", MaterialGrade.Premium, "Premium finishing putty 20 kg", "DemoMix Pro", 20m, "20 kg bag"),
        new("base-putty", MaterialGrade.Standard, "Base putty 25 kg", "DemoMix", 25m, "25 kg bag"),
        new("gypsum-plaster", MaterialGrade.Standard, "Gypsum plaster 30 kg", "SampleBuild", 30m, "30 kg bag"),
        new("gypsum-plaster", MaterialGrade.Budget, "Economy gypsum plaster 30 kg", "SampleBuild Eco", 30m, "30 kg bag"),
        new("tile-adhesive", MaterialGrade.Standard, "Tile adhesive 25 kg", "TestFix", 25m, "25 kg bag"),
        new("tile-adhesive", MaterialGrade.Premium, "Flexible tile adhesive 25 kg", "TestFix Flex", 25m, "25 kg bag"),
        new("tile-grout", MaterialGrade.Standard, "Tile grout 2 kg", "TestFix", 2m, "2 kg bag"),
        new("primer", MaterialGrade.Standard, "Deep-penetration primer 10 L", "DemoCoat", 10m, "10 L can"),
        new("primer", MaterialGrade.Budget, "Economy primer 5 L", "DemoCoat Eco", 5m, "5 L can"),
        new("interior-paint", MaterialGrade.Standard, "Interior wall paint 9 L", "DemoCoat", 9m, "9 L bucket"),
        new("interior-paint", MaterialGrade.Premium, "Premium washable wall paint 9 L", "DemoCoat Pro", 9m, "9 L bucket"),
        new("interior-paint", MaterialGrade.Budget, "Economy wall paint 15 L", "DemoCoat Eco", 15m, "15 L bucket"),
        new("screed-mix", MaterialGrade.Standard, "Screed mix 50 kg", "SampleBuild", 50m, "50 kg bag"),
        new("self-levelling-mix", MaterialGrade.Standard, "Self-levelling floor 25 kg", "SampleBuild", 25m, "25 kg bag"),
        new("waterproofing-mix", MaterialGrade.Standard, "Waterproofing compound 20 kg", "TestSeal", 20m, "20 kg set"),
        new("silicone-sealant", MaterialGrade.Standard, "Silicone sealant 280 ml", "TestSeal", 1m, "tube"),
        new("wall-tiles", MaterialGrade.Standard, "Wall tiles 30×60", "Demo Ceramics", 1.44m, "box 1.44 m²"),
        new("laminate", MaterialGrade.Standard, "Laminate 8 mm", "Demo Floor", 2.22m, "pack 2.22 m²"),
        new("drywall-sheet", MaterialGrade.Standard, "Drywall sheet 1.2×2.5 m", "SampleBoard", 3m, "sheet 3 m²"),
        new("metal-profile", MaterialGrade.Standard, "Metal profile 3 m", "SampleBoard", 3m, "3 m profile"),
        new("corner-bead", MaterialGrade.Standard, "Plaster corner bead 3 m", "SampleBoard", 3m, "3 m profile"),
    ];

    /// <summary>The price of one pack: the material's usual price for the grade times the pack size, 90–112% depending on the store and product.</summary>
    public static int PackPrice(Material material, Product product, int storeIndex)
    {
        var unit = product.Grade switch
        {
            MaterialGrade.Budget => material.PriceBudget,
            MaterialGrade.Premium => material.PricePremium,
            _ => material.PriceStandard,
        };
        var factor = 0.9 + (DemoPrice.Next(Guid.Empty, $"{product.Name}/{storeIndex}", 7) * 0.22);
        return Math.Max(MarketPrices.Tidy(unit * (double)product.PackSize * factor), 10);
    }
}
