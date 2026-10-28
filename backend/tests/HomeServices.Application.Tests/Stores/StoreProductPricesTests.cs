using HomeServices.Application.Errors;
using HomeServices.Application.Files;
using HomeServices.Application.Stores;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Stores;

/// <summary>R7: staff save many products' prices at once.</summary>
public class StoreProductPricesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 11);

    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly FakeClock _clock = new(Now);
    private readonly IOptions<StorePricingSettings> _settings = Options.Create(new StorePricingSettings());
    private readonly Store _market = Store.Create("demo-market", "Demo Market", LocalizedText.Empty, null, null, null, 1);
    private readonly Store _stroy = Store.Create("demo-stroy", "Demo Stroy", LocalizedText.Empty, null, null, null, 2);
    private readonly Material _putty = Material.Create("putty", LocalizedText.Empty.With("hy", "Շպակլ").With("en", "Putty"), MaterialUnit.Kilogram, 100, 200, 300, 1, Now);
    private readonly StoreProduct _bag;
    private readonly StoreProduct _bucket;
    private readonly StoreProduct _elsewhere;

    public StoreProductPricesTests()
    {
        _db.Stores.AddRange(_market, _stroy);
        _db.Materials.Add(_putty);
        _bag = Product(_market, "Putty bag", 5000);
        _bucket = Product(_market, "Putty bucket", 9000);
        _elsewhere = Product(_stroy, "Stroy putty", 4000);
        _db.SaveChanges();
    }

    private StoreProduct Product(Store store, string name, int price)
    {
        var product = StoreProduct.Create(store.Id, name, null, null, 25m, null, price, null, null, true, null, Now.AddDays(-10));
        product.LinkTo(_putty.Id, MaterialGrade.Standard);
        _db.StoreProducts.Add(product);
        return product;
    }

    private Task<IReadOnlyList<AdminStoreProductDto>> SaveAsync(Store store, params StoreProductPriceInput[] items) =>
        new SaveStoreProductPricesHandler(_db, new FileDtoFactory(new FakeFileStorage(), Options.Create(new FileSettings()), _clock), new FakeCurrentLanguage("en"), _settings, _clock)
            .HandleAsync(new SaveStoreProductPrices(store.Id, items), CancellationToken.None);

    private StoreProduct Stored(Guid id) => _db.StoreProducts.AsNoTracking().Single(p => p.Id == id);

    private int HistoryRows(Guid id) => _db.StoreProductPrices.AsNoTracking().Count(r => r.StoreProductId == id);

    [Fact]
    public async Task Several_prices_are_saved_at_once_with_history_only_for_the_changed_ones()
    {
        var saved = await SaveAsync(
            _market,
            new StoreProductPriceInput(_bag.Id, 4500, 4000, Today.AddDays(5), true),
            new StoreProductPriceInput(_bucket.Id, 9000, null, null, false));

        saved.Select(p => (p.Name, p.Price, p.SalePrice, p.InStock)).ShouldBe(new[] { ("Putty bag", 4500, (int?)4000, true), ("Putty bucket", 9000, (int?)null, false) });
        (Stored(_bag.Id).PriceUpdatedAt, Stored(_bucket.Id).PriceUpdatedAt).ShouldBe((Now, Now.AddDays(-10)));
        (HistoryRows(_bag.Id), HistoryRows(_bucket.Id)).ShouldBe((1, 0));
    }

    [Fact]
    public async Task Confirming_an_unchanged_price_marks_it_checked_today()
    {
        await SaveAsync(_market, new StoreProductPriceInput(_bucket.Id, 9000, null, null, true, Confirm: true));

        Stored(_bucket.Id).PriceUpdatedAt.ShouldBe(Now);
        HistoryRows(_bucket.Id).ShouldBe(0);
    }

    [Fact]
    public async Task One_invalid_sale_saves_nothing_and_names_the_product()
    {
        var error = await Should.ThrowAsync<DomainException>(() => SaveAsync(
            _market,
            new StoreProductPriceInput(_bag.Id, 4500, null, null, true),
            new StoreProductPriceInput(_bucket.Id, 9000, 9500, Today, true)));

        error.Code.ShouldBe("store_product.sale_invalid");
        error.Message.ShouldStartWith("Putty bucket: ");
        Stored(_bag.Id).Price.ShouldBe(5000);
        HistoryRows(_bag.Id).ShouldBe(0);
    }

    [Fact]
    public async Task Another_stores_product_is_not_found()
    {
        (await Should.ThrowAsync<NotFoundException>(() => SaveAsync(
            _market,
            new StoreProductPriceInput(_bag.Id, 4500, null, null, true),
            new StoreProductPriceInput(_elsewhere.Id, 3000, null, null, true))))
            .Code.ShouldBe("store_product.not_found");
        Stored(_elsewhere.Id).Price.ShouldBe(4000);
    }

    [Fact]
    public async Task The_validator_wants_1_to_100_different_products()
    {
        var validator = new SaveStoreProductPricesValidator();
        var item = new StoreProductPriceInput(_bag.Id, 1, null, null, true);

        (await validator.ValidateAsync(new SaveStoreProductPrices(_market.Id, []))).Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "items.count_invalid" });
        (await validator.ValidateAsync(new SaveStoreProductPrices(_market.Id, [item, item]))).Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "items.duplicate" });
        (await validator.ValidateAsync(new SaveStoreProductPrices(_market.Id, Enumerable.Range(0, 101).Select(_ => item with { ProductId = Guid.NewGuid() }).ToList())))
            .Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "items.count_invalid" });
    }
}
