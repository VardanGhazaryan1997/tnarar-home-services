using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Files;
using HomeServices.Application.Stores;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Stores;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Stores;

public class PublicStoresTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 11);

    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly FakeClock _clock = new(Now);
    private readonly IOptions<StorePricingSettings> _settings = Options.Create(new StorePricingSettings());
    private readonly ICurrentLanguage _language = new FakeCurrentLanguage("en");
    private readonly Region _yerevanRegion = Region.Create("yerevan", Text("Երևան", "Yerevan"), 1);
    private readonly Region _shirak = Region.Create("shirak", Text("Շիրակ", "Shirak"), 2);
    private readonly City _yerevan;
    private readonly City _gyumri;
    private readonly Store _alpha = Store.Create("alpha", "Alpha", Text("Նկարագրություն", "About Alpha"), "https://alpha.example", "+374 10 000001", null, 1);
    private readonly Store _beta = Store.Create("beta", "Beta", LocalizedText.Empty, null, null, null, 2);
    private readonly Store _hidden = Store.Create("hidden", "Hidden", LocalizedText.Empty, null, null, null, 3);
    private readonly Material _putty = Material.Create("putty", Text("Շպաքլ", "Putty"), MaterialUnit.Kilogram, 100, 160, 260, 1, Now);
    private readonly Material _paint = Material.Create("paint", Text("Ներկ", "Paint"), MaterialUnit.Liter, 500, 800, 1500, 2, Now);

    public PublicStoresTests()
    {
        _db.Regions.AddRange(_yerevanRegion, _shirak);
        _db.SaveChanges();
        _yerevan = City.Create("yerevan", Text("Երևան", "Yerevan"), 1, _yerevanRegion.Id);
        _yerevan.AddDistrict("kentron", Text("Կենտրոն", "Kentron"), 1);
        _gyumri = City.Create("gyumri", Text("Գյումրի", "Gyumri"), 2, _shirak.Id);
        _db.Cities.AddRange(_yerevan, _gyumri);
        _hidden.Deactivate();
        _db.Stores.AddRange(_alpha, _beta, _hidden);
        _db.Materials.AddRange(_putty, _paint);
        _db.SaveChanges();

        _alpha.SetBranches(
        [
            new BranchInput("Komitas", _yerevan.Id, _yerevan.Districts.Single().Id, "Komitas 1", "+374 10 111111", "Mon–Sat 9–19"),
            new BranchInput("Shirak branch", _gyumri.Id, null, "Garegin 3", null, null),
        ]);
        _beta.SetBranches([new BranchInput("Gyumri", _gyumri.Id, null, "Abovyan 9", null, null)]);
        _db.SaveChanges();
    }

    private static LocalizedText Text(string hy, string en) => LocalizedText.Empty.With("hy", hy).With("en", en);

    private FileDtoFactory Files => new(new FakeFileStorage(), Options.Create(new FileSettings()), _clock);

    private StoreProduct Add(
        Store store,
        Material material,
        MaterialGrade grade,
        int unit,
        bool inStock = true,
        int? sale = null,
        DateOnly? saleEnds = null,
        DateTimeOffset? updatedAt = null,
        bool active = true,
        string? name = null)
    {
        var product = StoreProduct.Create(store.Id, name ?? $"{material.Slug} {unit}", "Brand", null, 25m, "25 kg bag", unit * 25, sale, saleEnds, inStock, null, updatedAt ?? Now);
        product.LinkTo(material.Id, grade);
        if (!active)
        {
            product.Deactivate();
        }

        _db.StoreProducts.Add(product);
        _db.SaveChanges();
        return product;
    }

    private Task<IReadOnlyList<PublicStoreDto>> StoresAsync(string? region = null) =>
        new GetPublicStoresHandler(_db, Files, _language).HandleAsync(new GetPublicStores(region), CancellationToken.None);

    private Task<PublicStoreDto> StoreAsync(string slug) =>
        new GetPublicStoreHandler(_db, Files, _language, _settings, _clock).HandleAsync(new GetPublicStore(slug), CancellationToken.None);

    private Task<IReadOnlyList<PublicMaterialDto>> MaterialsAsync(string? search = null, string? region = null) =>
        new GetPublicMaterialsHandler(_db, _language, _settings, _clock).HandleAsync(new GetPublicMaterials(search, region), CancellationToken.None);

    private Task<PublicMaterialDetailDto> MaterialAsync(string slug, string? region = null, MaterialGrade? grade = null, bool inStockOnly = false) =>
        new GetPublicMaterialHandler(_db, Files, _language, _settings, _clock).HandleAsync(new GetPublicMaterial(slug, region, grade, inStockOnly), CancellationToken.None);

    [Fact]
    public async Task Stores_are_listed_in_order_with_their_branches_in_the_request_language_and_withunavailablehidden_ones()
    {
        var stores = await StoresAsync();

        stores.Select(s => s.Slug).ToArray().ShouldBe(new[] { "alpha", "beta" });
        var alpha = stores[0];
        alpha.Name.ShouldBe("Alpha");
        alpha.Description.ShouldBe("About Alpha");
        alpha.Website.ShouldBe("https://alpha.example");
        alpha.Phone.ShouldBe("+374 10 000001");
        alpha.LogoUrl.ShouldBeNull();
        alpha.Materials.ShouldBeNull();
        alpha.Branches.Select(b => b.Name).ToArray().ShouldBe(new[] { "Komitas", "Shirak branch" });
        var komitas = alpha.Branches[0];
        (komitas.City, komitas.District, komitas.Address, komitas.Phone, komitas.OpeningHours).ShouldBe(("Yerevan", "Kentron", "Komitas 1", "+374 10 111111", "Mon–Sat 9–19"));
        alpha.Branches[1].District.ShouldBeNull();
        stores[1].Description.ShouldBeNull();
    }

    [Fact]
    public async Task A_region_keeps_only_the_stores_and_branches_there()
    {
        var shirak = await StoresAsync("Shirak");
        shirak.Select(s => s.Slug).ToArray().ShouldBe(new[] { "alpha", "beta" });
        shirak[0].Branches.Select(b => b.City).ToArray().ShouldBe(new[] { "Gyumri" });

        var yerevan = await StoresAsync("yerevan");
        yerevan.Select(s => s.Slug).ToArray().ShouldBe(new[] { "alpha" });

        (await StoresAsync("nowhere")).ShouldBeEmpty();
        (await StoresAsync(" ")).Count.ShouldBe(2);
    }

    [Fact]
    public async Task A_store_shows_its_current_linked_products_grouped_by_material()
    {
        Add(_alpha, _putty, MaterialGrade.Premium, 300);
        Add(_alpha, _putty, MaterialGrade.Standard, 200);
        Add(_alpha, _putty, MaterialGrade.Standard, 180);
        Add(_alpha, _paint, MaterialGrade.Standard, 900);
        Add(_alpha, _paint, MaterialGrade.Budget, 100, active: false); // hidden product
        Add(_alpha, _paint, MaterialGrade.Budget, 100, updatedAt: Now.AddDays(-91)); // price not checked for too long
        Add(_beta, _paint, MaterialGrade.Standard, 700); // another store's
        var unlinked = StoreProduct.Create(_alpha.Id, "Unlinked", null, null, 1m, null, 1000, null, null, true, null, Now);
        _db.StoreProducts.Add(unlinked);
        await _db.SaveChangesAsync();

        var store = await StoreAsync("alpha");

        store.Materials!.Select(m => m.Slug).ToArray().ShouldBe(new[] { "putty", "paint" });
        var putty = store.Materials![0];
        (putty.Name, putty.Unit).ShouldBe(("Putty", MaterialUnit.Kilogram));
        putty.Products.Select(p => (p.Grade, p.UnitPrice)).ToArray().ShouldBe(new[] { (MaterialGrade.Standard, 180m), (MaterialGrade.Standard, 200m), (MaterialGrade.Premium, 300m) });
        store.Materials![1].Products.Single().UnitPrice.ShouldBe(900m);
        store.Branches.Count.ShouldBe(2);
    }

    [Fact]
    public async Task A_sale_shows_only_until_its_last_day()
    {
        Add(_alpha, _putty, MaterialGrade.Standard, 200, sale: 4000, saleEnds: Today, name: "On sale");
        Add(_alpha, _putty, MaterialGrade.Standard, 210, sale: 4000, saleEnds: Today.AddDays(-1), name: "Sale over");

        var products = (await StoreAsync("alpha")).Materials!.Single().Products;

        var onSale = products.Single(p => p.Name == "On sale");
        onSale.Price.ShouldBe(5000);
        onSale.SalePrice.ShouldBe(4000);
        onSale.SaleEndsOn.ShouldBe(Today);
        onSale.CurrentPrice.ShouldBe(4000);
        onSale.UnitPrice.ShouldBe(160m);
        var over = products.Single(p => p.Name == "Sale over");
        over.Price.ShouldBe(5250);
        over.SalePrice.ShouldBeNull();
        over.SaleEndsOn.ShouldBeNull();
        over.CurrentPrice.ShouldBe(5250);
        over.UnitPrice.ShouldBe(210m);
    }

    [Fact]
    public async Task A_hidden_or_unknown_store_is_not_found()
    {
        (await Should.ThrowAsync<NotFoundException>(() => StoreAsync("hidden"))).Code.ShouldBe("store.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => StoreAsync("nope"))).Code.ShouldBe("store.not_found");
        (await StoreAsync(" ALPHA ")).Slug.ShouldBe("alpha");
    }

    [Fact]
    public async Task The_material_list_shows_usual_prices_store_counts_and_the_cheapest_price_in_stock()
    {
        Add(_alpha, _putty, MaterialGrade.Standard, 200);
        Add(_beta, _putty, MaterialGrade.Standard, 300);
        Add(_beta, _putty, MaterialGrade.Budget, 120, inStock: false);
        Add(_hidden, _putty, MaterialGrade.Budget, 50); // hidden store
        var material = _db.Materials.Single(m => m.Id == _putty.Id);
        material.UpdateMarket(null, 250, null, 2, Now);
        await _db.SaveChangesAsync();

        var list = await MaterialsAsync();

        list.Select(m => m.Slug).ToArray().ShouldBe(new[] { "putty", "paint" });
        var putty = list[0];
        (putty.Name, putty.Unit, putty.StoreCount).ShouldBe(("Putty", MaterialUnit.Kilogram, 2));
        putty.LowestUnitPrice.ShouldBe(200m);
        (putty.Prices.Budget.Price, putty.Prices.Budget.Source).ShouldBe((100, MaterialPriceSource.Staff));
        (putty.Prices.Standard.Price, putty.Prices.Standard.Source).ShouldBe((250, MaterialPriceSource.Stores));
        (putty.Prices.Premium.Price, putty.Prices.Premium.Source).ShouldBe((260, MaterialPriceSource.Staff));
        list[1].StoreCount.ShouldBe(0);
        list[1].LowestUnitPrice.ShouldBeNull();
    }

    [Fact]
    public async Task The_material_list_can_be_searched_and_limited_to_a_region()
    {
        Add(_alpha, _putty, MaterialGrade.Standard, 200);
        Add(_beta, _putty, MaterialGrade.Standard, 300);
        _paint.Deactivate();
        _db.Materials.Add(Material.Create("tile-glue", Text("Սոսինձ", "Tile glue"), MaterialUnit.Kilogram, 100, 150, 200, 3, Now));
        await _db.SaveChangesAsync();

        (await MaterialsAsync()).Select(m => m.Slug).ToArray().ShouldBe(new[] { "putty", "tile-glue" }); // the hidden paint is left out
        (await MaterialsAsync(" GLUE ")).Select(m => m.Slug).ToArray().ShouldBe(new[] { "tile-glue" });
        (await MaterialsAsync("putty")).Single().Slug.ShouldBe("putty");
        (await MaterialsAsync("Շպաքլ")).ShouldBeEmpty(); // names are searched in the request language

        var inYerevan = (await MaterialsAsync(region: "yerevan")).First();
        inYerevan.StoreCount.ShouldBe(1);
        inYerevan.LowestUnitPrice.ShouldBe(200m);
        var inShirak = (await MaterialsAsync(region: "shirak")).First();
        inShirak.StoreCount.ShouldBe(2);
        inShirak.LowestUnitPrice.ShouldBe(200m);
        (await MaterialsAsync(region: "nowhere")).First().StoreCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_materials_offers_list_in_stock_first_and_then_cheapest_per_unit()
    {
        var unavailable = Add(_alpha, _putty, MaterialGrade.Standard, 100, inStock: false);
        var dear = Add(_alpha, _putty, MaterialGrade.Premium, 400);
        var cheap = Add(_beta, _putty, MaterialGrade.Standard, 200);
        Add(_beta, _paint, MaterialGrade.Standard, 800); // another material
        Add(_beta, _putty, MaterialGrade.Budget, 90, updatedAt: Now.AddDays(-100)); // stale
        Add(_hidden, _putty, MaterialGrade.Budget, 80); // hidden store

        var material = await MaterialAsync("putty");

        (material.Name, material.Unit, material.StoreCount).ShouldBe(("Putty", MaterialUnit.Kilogram, 2));
        material.Prices.Standard.Price.ShouldBe(160);
        material.Offers.Select(o => o.Product.Id).ToArray().ShouldBe(new[] { cheap.Id, dear.Id, unavailable.Id });
        var first = material.Offers[0];
        first.Store.Slug.ShouldBe("beta");
        first.Store.Name.ShouldBe("Beta");
        first.Store.Branches.Single().City.ShouldBe("Gyumri");
        (first.Product.Brand, first.Product.PackLabel, first.Product.PackSize, first.Product.Price, first.Product.UnitPrice).ShouldBe(("Brand", "25 kg bag", 25m, 5000, 200m));
        material.Offers[1].Store.Website.ShouldBe("https://alpha.example");
        material.Offers[2].Product.InStock.ShouldBeFalse();
    }

    [Fact]
    public async Task Offers_can_be_narrowed_by_grade_stock_and_region()
    {
        var standard = Add(_alpha, _putty, MaterialGrade.Standard, 200);
        var premium = Add(_beta, _putty, MaterialGrade.Premium, 400);
        var outOfStock = Add(_beta, _putty, MaterialGrade.Standard, 150, inStock: false);

        (await MaterialAsync("putty", grade: MaterialGrade.Premium)).Offers.Select(o => o.Product.Id).ToArray().ShouldBe(new[] { premium.Id });
        (await MaterialAsync("putty", inStockOnly: true)).Offers.Select(o => o.Product.Id).ToArray().ShouldBe(new[] { standard.Id, premium.Id });
        (await MaterialAsync("putty", region: "yerevan")).Offers.Select(o => o.Product.Id).ToArray().ShouldBe(new[] { standard.Id });
        var shirak = await MaterialAsync("putty", region: "shirak");
        shirak.Offers.Select(o => o.Product.Id).ToArray().ShouldBe(new[] { standard.Id, premium.Id, outOfStock.Id });
        shirak.Offers.Single(o => o.Store.Slug == "alpha").Store.Branches.Single().City.ShouldBe("Gyumri");
        (await MaterialAsync("putty", region: "nowhere")).Offers.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_hidden_or_unknown_material_is_not_found()
    {
        _paint.Deactivate();
        await _db.SaveChangesAsync();

        (await Should.ThrowAsync<NotFoundException>(() => MaterialAsync("paint"))).Code.ShouldBe("material.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => MaterialAsync("nope"))).Code.ShouldBe("material.not_found");
        (await MaterialAsync(" PUTTY ")).Offers.ShouldBeEmpty();
    }
}
