using HomeServices.Application.Estimates;
using HomeServices.Application.Files;
using HomeServices.Application.Catalog;
using HomeServices.Application.Stores;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Estimates;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Stores;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Stores;

public class MaterialMarketPricesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 11);

    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly FakeClock _clock = new(Now);
    private readonly StorePricingSettings _pricing = new();
    private readonly Store _a = Store.Create("store-a", "Store A", LocalizedText.Empty, null, null, null, 1);
    private readonly Store _b = Store.Create("store-b", "Store B", LocalizedText.Empty, null, null, null, 2);
    private readonly Store _c = Store.Create("store-c", "Store C", LocalizedText.Empty, null, null, null, 3);
    private readonly Material _adhesive;

    public MaterialMarketPricesTests()
    {
        _db.Stores.AddRange(_a, _b, _c);
        _adhesive = Material.Create("tile-adhesive", LocalizedText.Empty.With("en", "Tile adhesive"), MaterialUnit.Kilogram, 100, 160, 260, 1, Now);
        _db.Materials.Add(_adhesive);
        _db.SaveChanges();
    }

    private IOptions<StorePricingSettings> Settings => Options.Create(_pricing);

    private FileDtoFactory Files => new(new FakeFileStorage(), Options.Create(new FileSettings()), _clock);

    /// <summary>A 25 kg pack priced so that one kg costs <paramref name="unit"/> AMD.</summary>
    private StoreProduct Add(
        Store store,
        MaterialGrade grade,
        int unit,
        bool inStock = true,
        int? sale = null,
        DateOnly? saleEnds = null,
        DateTimeOffset? updatedAt = null,
        Material? material = null,
        string? code = null)
    {
        var product = StoreProduct.Create(store.Id, "Adhesive", null, code, 25m, "25 kg bag", unit * 25, sale, saleEnds, inStock, null, updatedAt ?? Now);
        product.LinkTo((material ?? _adhesive).Id, grade);
        _db.StoreProducts.Add(product);
        _db.SaveChanges();
        return product;
    }

    private async Task<Material> RecalculateAsync()
    {
        await MaterialMarketPrices.RecalculateAsync(_db, _pricing, _clock, materialIds: null, CancellationToken.None);
        await _db.SaveChangesAsync();
        return _db.Materials.Single(m => m.Id == _adhesive.Id);
    }

    private static int?[] Market(Material material) => [material.MarketBudget, material.MarketStandard, material.MarketPremium];

    [Fact]
    public async Task A_grades_price_is_the_median_of_the_stores_per_unit_prices()
    {
        Add(_a, MaterialGrade.Standard, 200);
        Add(_b, MaterialGrade.Standard, 300);
        Add(_c, MaterialGrade.Standard, 800);
        Add(_a, MaterialGrade.Premium, 400);
        Add(_b, MaterialGrade.Premium, 500);

        var material = await RecalculateAsync();

        Market(material).ShouldBe(new int?[] { null, 300, 450 });
        (material.MarketStoreCount, material.MarketUpdatedAt).ShouldBe((3, Now));
        material.PriceFor(MaterialGrade.Budget).ShouldBe(100); // no store sells budget: the staff price stays
        material.PriceFor(MaterialGrade.Standard).ShouldBe(300);
        material.PriceFor(MaterialGrade.Premium).ShouldBe(450);
        material.PriceSourceFor(MaterialGrade.Budget).ShouldBe(MaterialPriceSource.Staff);
        material.PriceSourceFor(MaterialGrade.Standard).ShouldBe(MaterialPriceSource.Stores);
        material.StaffPriceFor(MaterialGrade.Standard).ShouldBe(160);
    }

    [Fact]
    public async Task A_store_counts_once_per_grade_with_the_median_of_its_products()
    {
        Add(_a, MaterialGrade.Standard, 100, code: "1");
        Add(_a, MaterialGrade.Standard, 110, code: "2");
        Add(_a, MaterialGrade.Standard, 900, code: "3");
        Add(_b, MaterialGrade.Standard, 200);

        var material = await RecalculateAsync();

        Market(material).ShouldBe(new int?[] { null, 160, null }); // medians 110 and 200 → 155 → 160
        material.MarketStoreCount.ShouldBe(2);
    }

    [Fact]
    public async Task A_grade_needs_the_minimum_number_of_stores()
    {
        Add(_a, MaterialGrade.Standard, 300);

        var material = await RecalculateAsync();

        Market(material).ShouldBe(new int?[] { null, null, null });
        material.MarketStoreCount.ShouldBe(1);
        material.PriceFor(MaterialGrade.Standard).ShouldBe(160);

        _pricing.MinStores = 1;
        Market(await RecalculateAsync()).ShouldBe(new int?[] { null, 300, null });
    }

    [Fact]
    public async Task Only_fresh_prices_count_and_a_price_goes_stale_after_the_fresh_period()
    {
        Add(_a, MaterialGrade.Standard, 300, updatedAt: Now.AddDays(-90));
        Add(_b, MaterialGrade.Standard, 500, updatedAt: Now.AddDays(-91));
        Add(_c, MaterialGrade.Standard, 700);

        Market(await RecalculateAsync()).ShouldBe(new int?[] { null, 500, null });
        _db.Materials.Single().MarketStoreCount.ShouldBe(2);

        _clock.Now = Now.AddDays(1); // the first price is now 91 days old
        var material = await RecalculateAsync();

        Market(material).ShouldBe(new int?[] { null, null, null });
        material.MarketStoreCount.ShouldBe(1);
    }

    [Fact]
    public async Task The_sale_price_counts_until_its_last_day()
    {
        Add(_a, MaterialGrade.Standard, 200, sale: 4000, saleEnds: Today); // 160 per kg while on sale
        Add(_b, MaterialGrade.Standard, 200);

        Market(await RecalculateAsync()).ShouldBe(new int?[] { null, 180, null });

        _clock.Now = Now.AddDays(1);
        Market(await RecalculateAsync()).ShouldBe(new int?[] { null, 200, null });
    }

    [Fact]
    public async Task Hidden_unavailable_and_unlinked_products_dont_count()
    {
        Add(_a, MaterialGrade.Standard, 300);
        Add(_b, MaterialGrade.Standard, 300, inStock: false);
        var hidden = Add(_c, MaterialGrade.Standard, 300);
        hidden.Deactivate();
        var unlinked = Add(_c, MaterialGrade.Standard, 300, code: "2");
        unlinked.Unlink();
        await _db.SaveChangesAsync();

        var material = await RecalculateAsync();

        Market(material).ShouldBe(new int?[] { null, null, null });
        material.MarketStoreCount.ShouldBe(1);

        _b.Deactivate();
        Add(_b, MaterialGrade.Standard, 300, code: "3");
        (await RecalculateAsync()).MarketStoreCount.ShouldBe(1); // a hidden store doesn't count either
    }

    [Fact]
    public async Task Locking_a_material_keeps_the_staff_prices_but_still_counts_stores()
    {
        Add(_a, MaterialGrade.Standard, 300);
        Add(_b, MaterialGrade.Standard, 500);
        _adhesive.LockPrice(true);

        var material = await RecalculateAsync();

        material.MarketStandard.ShouldBe(400);
        material.MarketStoreCount.ShouldBe(2);
        material.PriceFor(MaterialGrade.Standard).ShouldBe(160);
        material.PriceSourceFor(MaterialGrade.Standard).ShouldBe(MaterialPriceSource.Staff);
    }

    [Fact]
    public async Task Prices_keep_the_order_budget_standard_premium()
    {
        Add(_a, MaterialGrade.Budget, 500);
        Add(_b, MaterialGrade.Budget, 500);
        Add(_a, MaterialGrade.Standard, 300, code: "2");
        Add(_b, MaterialGrade.Standard, 300, code: "2");
        Add(_a, MaterialGrade.Premium, 400, code: "3");
        Add(_b, MaterialGrade.Premium, 400, code: "3");

        Market(await RecalculateAsync()).ShouldBe(new int?[] { 500, 500, 500 });
    }

    [Fact]
    public async Task Small_unit_prices_are_rounded_to_whole_drams_and_never_to_zero()
    {
        _pricing.MinStores = 1;
        var sand = Material.Create("sand", LocalizedText.Empty.With("en", "Sand"), MaterialUnit.Kilogram, 5, 8, 12, 2, Now);
        _db.Materials.Add(sand);
        await _db.SaveChangesAsync();
        var product = StoreProduct.Create(_a.Id, "Sand 40 kg", null, null, 40m, null, 560, null, null, true, null, Now); // 14 per kg
        product.LinkTo(sand.Id, MaterialGrade.Budget);
        var cheap = StoreProduct.Create(_a.Id, "Gravel", null, "g", 1000m, null, 1, null, null, true, null, Now); // 0.001 per kg
        cheap.LinkTo(sand.Id, MaterialGrade.Premium);
        _db.StoreProducts.AddRange(product, cheap);
        await _db.SaveChangesAsync();

        await MaterialMarketPrices.RecalculateAsync(_db, _pricing, _clock, [sand.Id], CancellationToken.None);

        Market(sand).ShouldBe(new int?[] { 14, null, 14 }); // premium 1 is raised to the budget price
    }

    [Fact]
    public async Task Materials_without_store_products_lose_their_market_prices()
    {
        Add(_a, MaterialGrade.Standard, 300);
        Add(_b, MaterialGrade.Standard, 300);
        (await RecalculateAsync()).MarketStandard.ShouldBe(300);

        _db.StoreProducts.RemoveRange(_db.StoreProducts.ToList());
        await _db.SaveChangesAsync();
        var material = await RecalculateAsync();

        (material.MarketStandard, material.MarketStoreCount).ShouldBe((null, 0));
    }

    [Fact]
    public async Task The_job_recalculates_every_material()
    {
        Add(_a, MaterialGrade.Standard, 300);
        Add(_b, MaterialGrade.Standard, 500);
        var other = Material.Create("other", LocalizedText.Empty.With("en", "Other"), MaterialUnit.Piece, 1, 2, 3, 2, Now);
        _db.Materials.Add(other);
        await _db.SaveChangesAsync();

        var result = await new RecalculateMaterialPricesHandler(_db, Settings, _clock).HandleAsync(new RecalculateMaterialPrices(), CancellationToken.None);

        result.ShouldBe(new MaterialPricesResult(2, 1));
        _db.Materials.Single(m => m.Id == other.Id).MarketUpdatedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task Changing_products_updates_the_material_prices_right_away()
    {
        var language = new FakeCurrentLanguage("en");
        CreateStoreProduct New(Store store, int price) =>
            new(store.Id, "Adhesive", null, null, 25m, null, price, null, null, true, null, _adhesive.Id, MaterialGrade.Standard);
        var create = new CreateStoreProductHandler(_db, Files, language, Settings, _clock);

        var first = await create.HandleAsync(New(_a, 5000), CancellationToken.None);
        _db.Materials.Single().MarketStandard.ShouldBeNull();
        var second = await create.HandleAsync(New(_b, 10000), CancellationToken.None);
        _db.Materials.Single().MarketStandard.ShouldBe(300); // 200 and 400 per kg

        await new UpdateStoreProductHandler(_db, Files, language, Settings, _clock).HandleAsync(
            new UpdateStoreProduct(_b.Id, second.Id, "Adhesive", null, null, 25m, null, 7500, null, null, true, null, _adhesive.Id, MaterialGrade.Standard),
            CancellationToken.None);
        _db.Materials.Single().MarketStandard.ShouldBe(250); // 200 and 300

        _clock.Now = Now.AddDays(100);
        await new ConfirmStoreProductPriceHandler(_db, Files, language, Settings, _clock).HandleAsync(new ConfirmStoreProductPrice(_b.Id, second.Id), CancellationToken.None);
        _db.Materials.Single().MarketStandard.ShouldBeNull(); // the first price is stale; the confirmed one is the only one left
        _db.Materials.Single().MarketStoreCount.ShouldBe(1);

        await new ConfirmStoreProductPriceHandler(_db, Files, language, Settings, _clock).HandleAsync(new ConfirmStoreProductPrice(_a.Id, first.Id), CancellationToken.None);
        _db.Materials.Single().MarketStandard.ShouldBe(250);

        await new DeleteStoreProductHandler(_db, Settings, _clock).HandleAsync(new DeleteStoreProduct(_a.Id, first.Id), CancellationToken.None);
        _db.Materials.Single().MarketStandard.ShouldBeNull();
    }

    [Fact]
    public async Task Relinking_a_product_updates_both_materials()
    {
        var language = new FakeCurrentLanguage("en");
        var other = Material.Create("grout", LocalizedText.Empty.With("en", "Grout"), MaterialUnit.Kilogram, 100, 160, 260, 2, Now);
        _db.Materials.Add(other);
        await _db.SaveChangesAsync();
        _pricing.MinStores = 1;
        var product = await new CreateStoreProductHandler(_db, Files, language, Settings, _clock).HandleAsync(
            new CreateStoreProduct(_a.Id, "Adhesive", null, null, 25m, null, 5000, null, null, true, null, _adhesive.Id, MaterialGrade.Standard), CancellationToken.None);
        _db.Materials.Single(m => m.Id == _adhesive.Id).MarketStandard.ShouldBe(200);

        await new UpdateStoreProductHandler(_db, Files, language, Settings, _clock).HandleAsync(
            new UpdateStoreProduct(_a.Id, product.Id, "Adhesive", null, null, 25m, null, 5000, null, null, true, null, other.Id, MaterialGrade.Standard), CancellationToken.None);

        _db.Materials.Single(m => m.Id == _adhesive.Id).MarketStandard.ShouldBeNull();
        _db.Materials.Single(m => m.Id == other.Id).MarketStandard.ShouldBe(200);
    }

    [Fact]
    public async Task Hiding_or_showing_a_store_updates_the_material_prices()
    {
        Add(_a, MaterialGrade.Standard, 300);
        Add(_b, MaterialGrade.Standard, 500);
        await RecalculateAsync();
        _db.Materials.Single().MarketStandard.ShouldBe(400);
        var update = new UpdateStoreHandler(_db, Files, Settings, _clock);

        await update.HandleAsync(new UpdateStore(_b.Id, "store-b", "Store B", null, null, null, null, 2, IsActive: false), CancellationToken.None);
        _db.Materials.Single().MarketStandard.ShouldBeNull();

        await update.HandleAsync(new UpdateStore(_b.Id, "store-b", "Store B", null, null, null, null, 2, IsActive: true), CancellationToken.None);
        _db.Materials.Single().MarketStandard.ShouldBe(400);
    }

    [Fact]
    public async Task Updating_a_material_can_lock_its_prices_and_shows_the_store_prices()
    {
        Add(_a, MaterialGrade.Standard, 300);
        Add(_b, MaterialGrade.Standard, 500);
        await RecalculateAsync();
        var handler = new UpdateMaterialHandler(_db, Settings, _clock);
        UpdateMaterial Command(bool isLocked) =>
            new(_adhesive.Id, "tile-adhesive", new Dictionary<string, string> { ["en"] = "Tile adhesive" }, MaterialUnit.Kilogram, 100, 160, 260, 1, IsPriceLocked: isLocked);

        var lockedDto = await handler.HandleAsync(Command(true), CancellationToken.None);

        (lockedDto.IsPriceLocked, lockedDto.MarketStandard, lockedDto.MarketStoreCount, lockedDto.PriceStandard).ShouldBe((true, 400, 2, 160));

        var unlocked = await handler.HandleAsync(Command(false), CancellationToken.None);

        (unlocked.IsPriceLocked, unlocked.MarketBudget, unlocked.MarketStandard, unlocked.MarketPremium).ShouldBe((false, null, 400, null));
    }

    [Fact]
    public async Task A_material_that_products_link_to_cant_be_deleted()
    {
        Add(_a, MaterialGrade.Standard, 300);

        (await Should.ThrowAsync<DomainException>(() => new DeleteMaterialHandler(_db).HandleAsync(new DeleteMaterial(_adhesive.Id), CancellationToken.None)))
            .Code.ShouldBe("material.in_use");
    }

    [Fact]
    public async Task Estimates_use_the_store_prices_unless_the_material_is_locked()
    {
        var category = Category.Create("tiling", LocalizedText.Empty.With("hy", "Սալիկ"), 1);
        _db.Categories.Add(category);
        var tiling = WorkItem.Create(category.Id, "floor-tiling", LocalizedText.Empty.With("hy", "tiling").With("en", "Floor tiling"), WorkUnit.SquareMeter, WorkSurface.Floor, 1);
        _db.WorkItems.Add(tiling);
        _db.WorkItemMaterials.Add(WorkItemMaterial.Create(tiling.Id, _adhesive.Id, 6m, 0, 1));
        await _db.SaveChangesAsync();
        Add(_a, MaterialGrade.Standard, 300);
        Add(_b, MaterialGrade.Standard, 500);
        await RecalculateAsync();

        async Task<EstimateMeasurementDto> MeasureAsync(MaterialGrade grade) =>
            await new MeasureEstimateHandler(_db, new FakeCurrentLanguage("en")).HandleAsync(
                new MeasureEstimate([new RoomInput(RoomType.Bathroom, null, null, 10m, null, null, [new LineInput(tiling.Id)])], Materials: grade),
                CancellationToken.None);

        var withStores = await MeasureAsync(MaterialGrade.Standard);
        withStores.Materials!.Single().UnitPrice.ShouldBe(400);
        (await MeasureAsync(MaterialGrade.Budget)).Materials!.Single().UnitPrice.ShouldBe(100);

        _adhesive.LockPrice(true);
        await _db.SaveChangesAsync();
        (await MeasureAsync(MaterialGrade.Standard)).Materials!.Single().UnitPrice.ShouldBe(160);
    }
}
