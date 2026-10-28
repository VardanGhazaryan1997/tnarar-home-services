using HomeServices.Application.Stores;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Stores;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Stores;

public class WhereToBuyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly FakeClock _clock = new(Now);
    private readonly IOptions<StorePricingSettings> _settings = Options.Create(new StorePricingSettings());
    private readonly Region _yerevanRegion = Region.Create("yerevan", Text("Երևան", "Yerevan"), 1);
    private readonly Region _shirak = Region.Create("shirak", Text("Շիրակ", "Shirak"), 2);
    private readonly City _yerevan;
    private readonly City _gyumri;
    private readonly Store _alpha = Store.Create("alpha", "Alpha", LocalizedText.Empty, null, null, null, 1);
    private readonly Store _beta = Store.Create("beta", "Beta", LocalizedText.Empty, null, null, null, 2);
    private readonly Material _putty = Material.Create("putty", Text("Շպաքլ", "Putty"), MaterialUnit.Kilogram, 100, 160, 260, 1, Now);
    private readonly Material _paint = Material.Create("paint", Text("Ներկ", "Paint"), MaterialUnit.Liter, 500, 800, 1500, 2, Now);

    public WhereToBuyTests()
    {
        _db.Regions.AddRange(_yerevanRegion, _shirak);
        _db.SaveChanges();
        _yerevan = City.Create("yerevan", Text("Երևան", "Yerevan"), 1, _yerevanRegion.Id);
        _gyumri = City.Create("gyumri", Text("Գյումրի", "Gyumri"), 2, _shirak.Id);
        _db.Cities.AddRange(_yerevan, _gyumri);
        _db.Stores.AddRange(_alpha, _beta);
        _db.Materials.AddRange(_putty, _paint);
        _db.SaveChanges();
        _alpha.SetBranches([new BranchInput("Main", _yerevan.Id, null, "Main 1", null, null)]);
        _beta.SetBranches([new BranchInput("Main", _gyumri.Id, null, "Main 2", null, null)]);
        _db.SaveChanges();
    }

    private static LocalizedText Text(string hy, string en) => LocalizedText.Empty.With("hy", hy).With("en", en);

    private StoreProduct Add(
        Store store,
        Material material,
        decimal packSize,
        int price,
        MaterialGrade grade = MaterialGrade.Standard,
        bool inStock = true,
        int? sale = null,
        DateOnly? saleEnds = null,
        DateTimeOffset? updatedAt = null,
        string? name = null)
    {
        var product = StoreProduct.Create(store.Id, name ?? $"{material.Slug} {price}", "Brand", null, packSize, $"{packSize} pack", price, sale, saleEnds, inStock, null, updatedAt ?? Now);
        product.LinkTo(material.Id, grade);
        _db.StoreProducts.Add(product);
        _db.SaveChanges();
        return product;
    }

    private Task<WhereToBuyDto> AskAsync(MaterialGrade grade, string? region, params WhereToBuyNeed[] needs) =>
        new WhereToBuyHandler(_db, _settings, _clock).HandleAsync(new WhereToBuy(grade, needs, region), CancellationToken.None);

    [Fact]
    public async Task Each_material_gets_its_cheapest_offer_rounded_up_to_whole_packs()
    {
        Add(_alpha, _putty, 25m, 2500);
        Add(_beta, _putty, 25m, 2000);

        var result = await AskAsync(MaterialGrade.Standard, null, new WhereToBuyNeed(_putty.Id, 60m));

        var offer = result.Cheapest.Single();
        (offer.MaterialSlug, offer.StoreSlug, offer.Packs, offer.Cost, offer.PackSize).ShouldBe(("putty", "beta", 3, 6000, 25m));
        offer.PackLabel.ShouldBe("25 pack");
    }

    [Fact]
    public async Task Whole_packs_can_make_a_dearer_per_unit_product_the_cheaper_buy()
    {
        // 30 kg: two 25 kg bags (2 × 2000 = 4000) against one 30 kg bag (3900).
        Add(_alpha, _putty, 25m, 2000, name: "Bag 25");
        Add(_alpha, _putty, 30m, 3900, name: "Bag 30");

        var offer = (await AskAsync(MaterialGrade.Standard, null, new WhereToBuyNeed(_putty.Id, 30m))).Cheapest.Single();

        (offer.ProductName, offer.Packs, offer.Cost).ShouldBe(("Bag 30", 1, 3900));
    }

    [Fact]
    public async Task A_sale_price_counts_until_its_end_date()
    {
        Add(_alpha, _putty, 25m, 3000, sale: 1000, saleEnds: new DateOnly(2026, 10, 11));
        Add(_beta, _putty, 25m, 2000);

        var onTheLastDay = await AskAsync(MaterialGrade.Standard, null, new WhereToBuyNeed(_putty.Id, 25m));
        onTheLastDay.Cheapest.Single().StoreSlug.ShouldBe("alpha");
        onTheLastDay.Cheapest.Single().Cost.ShouldBe(1000);

        _clock.Now = _clock.Now.AddDays(1);
        var after = await AskAsync(MaterialGrade.Standard, null, new WhereToBuyNeed(_putty.Id, 25m));
        after.Cheapest.Single().StoreSlug.ShouldBe("beta");
    }

    [Fact]
    public async Task Stores_are_ranked_by_how_many_materials_they_have_and_then_by_total()
    {
        Add(_alpha, _putty, 25m, 2500);
        Add(_alpha, _paint, 10m, 8000);
        Add(_beta, _putty, 25m, 1000);

        var result = await AskAsync(MaterialGrade.Standard, null, new WhereToBuyNeed(_putty.Id, 50m), new WhereToBuyNeed(_paint.Id, 10m));

        result.Stores.Select(s => (s.StoreSlug, s.Total, s.Covered, s.Missing)).ToArray()
            .ShouldBe(new[] { ("alpha", 13000, 2, 0), ("beta", 2000, 1, 1) });
        result.Cheapest.Select(o => (o.MaterialSlug, o.StoreSlug, o.Cost)).ToArray()
            .ShouldBe(new[] { ("putty", "beta", 2000), ("paint", "alpha", 8000) });
    }

    [Fact]
    public async Task Each_store_lists_what_it_would_sell_in_the_request_order_adding_up_to_its_total()
    {
        Add(_alpha, _putty, 25m, 2500, name: "Alpha putty dear");
        Add(_alpha, _putty, 25m, 2400, name: "Alpha putty");
        Add(_alpha, _paint, 10m, 8000, name: "Alpha paint");
        Add(_beta, _putty, 25m, 1000, name: "Beta putty");

        var result = await AskAsync(MaterialGrade.Standard, null, new WhereToBuyNeed(_paint.Id, 10m), new WhereToBuyNeed(_putty.Id, 50m));

        var alpha = result.Stores.Single(s => s.StoreSlug == "alpha");
        alpha.Items.ShouldNotBeNull().Select(i => (i.MaterialSlug, i.ProductName, i.Packs, i.Cost)).ToArray()
            .ShouldBe(new[] { ("paint", "Alpha paint", 1, 8000), ("putty", "Alpha putty", 2, 4800) });
        alpha.Items.Sum(i => i.Cost).ShouldBe(alpha.Total);
        result.Stores.Single(s => s.StoreSlug == "beta").Items.ShouldNotBeNull().ShouldHaveSingleItem().ProductName.ShouldBe("Beta putty");
    }

    [Fact]
    public async Task Offers_of_another_grade_out_of_stock_stale_or_inactive_do_not_count()
    {
        Add(_alpha, _putty, 25m, 100, grade: MaterialGrade.Premium);
        Add(_alpha, _putty, 25m, 200, inStock: false);
        Add(_alpha, _putty, 25m, 300, updatedAt: Now.AddDays(-91));
        var inactive = Add(_beta, _putty, 25m, 400);
        inactive.Deactivate();
        _beta.Deactivate();
        _db.SaveChanges();
        Add(_alpha, _putty, 25m, 5000);

        var result = await AskAsync(MaterialGrade.Standard, null, new WhereToBuyNeed(_putty.Id, 25m));

        result.Cheapest.Single().Cost.ShouldBe(5000);
        result.Stores.Select(s => s.StoreSlug).ToArray().ShouldBe(new[] { "alpha" });
    }

    [Fact]
    public async Task A_region_keeps_only_stores_with_a_branch_there()
    {
        Add(_alpha, _putty, 25m, 2500);
        Add(_beta, _putty, 25m, 2000);

        var yerevan = await AskAsync(MaterialGrade.Standard, "yerevan", new WhereToBuyNeed(_putty.Id, 25m));
        yerevan.Stores.Select(s => s.StoreSlug).ToArray().ShouldBe(new[] { "alpha" });

        var nowhere = await AskAsync(MaterialGrade.Standard, "nowhere", new WhereToBuyNeed(_putty.Id, 25m));
        nowhere.Cheapest.ShouldBeEmpty();
        nowhere.Stores.ShouldBeEmpty();
    }

    [Fact]
    public async Task Nothing_is_returned_when_no_store_sells_the_materials()
    {
        (await AskAsync(MaterialGrade.Standard, null)).Cheapest.ShouldBeEmpty();

        var result = await AskAsync(MaterialGrade.Standard, null, new WhereToBuyNeed(_paint.Id, 5m));
        result.Cheapest.ShouldBeEmpty();
        result.Stores.ShouldBeEmpty();
    }

    [Fact]
    public void The_request_is_checked()
    {
        var validator = new WhereToBuyValidator();
        var id = Guid.NewGuid();

        validator.Validate(new WhereToBuy(MaterialGrade.Standard, [new WhereToBuyNeed(id, 5m)])).IsValid.ShouldBeTrue();
        validator.Validate(new WhereToBuy((MaterialGrade)9, [new WhereToBuyNeed(id, 5m)])).IsValid.ShouldBeFalse();
        validator.Validate(new WhereToBuy(MaterialGrade.Standard, [new WhereToBuyNeed(id, 0m)])).IsValid.ShouldBeFalse();
        validator.Validate(new WhereToBuy(MaterialGrade.Standard, [new WhereToBuyNeed(id, 5m), new WhereToBuyNeed(id, 6m)])).IsValid.ShouldBeFalse();
        validator.Validate(new WhereToBuy(MaterialGrade.Standard, Enumerable.Range(0, 101).Select(_ => new WhereToBuyNeed(Guid.NewGuid(), 1m)).ToList())).IsValid.ShouldBeFalse();
    }
}
