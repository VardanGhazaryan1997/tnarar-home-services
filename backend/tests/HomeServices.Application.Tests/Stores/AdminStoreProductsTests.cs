using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Files;
using HomeServices.Application.Stores;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Files;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Stores;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Stores;

public class AdminStoreProductsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 11);

    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly FakeClock _clock = new(Now);
    private readonly IOptions<StorePricingSettings> _settings = Options.Create(new StorePricingSettings());
    private readonly FakeCurrentLanguage _language = new("en");
    private readonly Store _market;
    private readonly Store _stroy;
    private readonly Material _putty;
    private readonly Material _paint;

    public AdminStoreProductsTests()
    {
        _market = Store.Create("demo-market", "Demo Market", LocalizedText.Empty, null, null, null, 1);
        _stroy = Store.Create("demo-stroy", "Demo Stroy", LocalizedText.Empty, null, null, null, 2);
        _db.Stores.AddRange(_market, _stroy);
        _putty = Material.Create("putty", LocalizedText.Empty.With("hy", "Շպակլ").With("en", "Putty"), MaterialUnit.Kilogram, 100, 200, 300, 1, Now);
        _paint = Material.Create("paint", LocalizedText.Empty.With("hy", "Ներկ").With("en", "Paint"), MaterialUnit.Liter, 1000, 2000, 3000, 2, Now);
        _db.Materials.AddRange(_putty, _paint);
        _db.SaveChanges();
    }

    private FileDtoFactory Files => new(new FakeFileStorage(), Options.Create(new FileSettings()), _clock);

    private Task<AdminStoreProductDto> CreateAsync(Store store, CreateStoreProduct? command = null) =>
        new CreateStoreProductHandler(_db, Files, _language, _settings, _clock).HandleAsync(command ?? Product(store), CancellationToken.None);

    private CreateStoreProduct Product(
        Store store,
        string name = "Finishing putty 25 kg",
        string? code = "P-25",
        int price = 5000,
        int? sale = null,
        DateOnly? saleEnds = null,
        Material? material = null,
        MaterialGrade? grade = MaterialGrade.Standard) =>
        new(store.Id, name, "Knauf", code, 25m, "25 kg bag", price, sale, saleEnds, true, null, (material ?? _putty).Id, grade);

    private Task<PagedResult<AdminStoreProductDto>> ListAsync(
        Store store, string? search = null, Guid? materialId = null, bool? linked = null, bool? stale = null, int page = 1, int pageSize = 25) =>
        new GetAdminStoreProductsHandler(_db, Files, _language, _settings, _clock)
            .HandleAsync(new GetAdminStoreProducts(store.Id, search, materialId, linked, stale, page, pageSize), CancellationToken.None);

    private static List<string> Names(PagedResult<AdminStoreProductDto> page) => page.Items.Select(p => p.Name).ToList();

    [Fact]
    public async Task A_product_is_linked_to_a_material_and_priced_per_unit()
    {
        var product = await CreateAsync(_market);

        (product.MaterialId, product.MaterialName, product.Unit, product.Grade).ShouldBe((_putty.Id, "Putty", MaterialUnit.Kilogram, MaterialGrade.Standard));
        (product.Name, product.Brand, product.Code, product.PackSize, product.PackLabel, product.Price).ShouldBe(("Finishing putty 25 kg", "Knauf", "P-25", 25m, "25 kg bag", 5000));
        (product.CurrentPrice, product.UnitPrice, product.SaleActive, product.InStock, product.IsActive, product.IsStale).ShouldBe((5000, 200m, false, true, true, false));
        product.PriceUpdatedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task A_product_can_be_created_without_a_material_and_isnt_compared()
    {
        var product = await CreateAsync(_market, Product(_market) with { MaterialId = null, Grade = null });

        (product.MaterialId, product.MaterialName, product.Unit, product.Grade, product.UnitPrice).ShouldBe((null, null, null, null, null));
    }

    [Fact]
    public async Task A_sale_price_counts_until_the_end_of_its_last_day_in_local_time()
    {
        var onSale = await CreateAsync(_market, Product(_market, sale: 4000, saleEnds: Today));
        onSale.SaleActive.ShouldBeTrue();
        (onSale.CurrentPrice, onSale.UnitPrice).ShouldBe((4000, 160m));

        // 21:00 UTC on the 11th is already the 12th in Armenia (UTC+4).
        _clock.Now = new DateTimeOffset(2026, 10, 11, 21, 0, 0, TimeSpan.Zero);
        var list = await ListAsync(_market);

        list.Items.Single().SaleActive.ShouldBeFalse();
        list.Items.Single().CurrentPrice.ShouldBe(5000);
    }

    [Fact]
    public async Task The_material_and_grade_are_checked()
    {
        (await Should.ThrowAsync<DomainException>(() => CreateAsync(_market, Product(_market) with { MaterialId = Guid.NewGuid() })))
            .Code.ShouldBe("store_product.material_not_found");
        (await Should.ThrowAsync<DomainException>(() => CreateAsync(_market, Product(_market, grade: null))))
            .Code.ShouldBe("store_product.grade_required");
        _db.StoreProducts.Count().ShouldBe(0);
    }

    [Fact]
    public async Task A_code_is_used_by_one_product_per_store()
    {
        await CreateAsync(_market);

        (await Should.ThrowAsync<ConflictException>(() => CreateAsync(_market, Product(_market, name: "Other"))))
            .Code.ShouldBe("store_product.code_taken");
        await CreateAsync(_stroy); // another store may use the code
        await CreateAsync(_market, Product(_market, code: null));
        await CreateAsync(_market, Product(_market, code: null, name: "No code either"));
        _db.StoreProducts.Count().ShouldBe(4);
    }

    [Fact]
    public async Task A_photo_must_be_an_uploaded_image()
    {
        (await Should.ThrowAsync<DomainException>(() => CreateAsync(_market, Product(_market) with { PhotoFileId = Guid.NewGuid() })))
            .Code.ShouldBe("store.file_invalid");
        var photo = StoredFile.Begin(FileOwnerType.Staff, "staff-1", "p.png", "image/png", 1000, Now);
        photo.MarkReady(1000, Now);
        _db.Files.Add(photo);
        await _db.SaveChangesAsync();

        var product = await CreateAsync(_market, Product(_market) with { PhotoFileId = photo.Id });

        product.PhotoUrl.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Updating_a_product_changes_the_link_and_checks_the_price_only_when_it_changes()
    {
        var created = await CreateAsync(_market);
        var handler = new UpdateStoreProductHandler(_db, Files, _language, _settings, _clock);
        _clock.Now = Now.AddDays(10);

        var sameprice = await handler.HandleAsync(
            new UpdateStoreProduct(_market.Id, created.Id, "Putty", null, "P-25", 25m, "25 kg bag", 5000, null, null, false, null, _paint.Id, MaterialGrade.Premium, false),
            CancellationToken.None);

        (sameprice.Name, sameprice.Brand, sameprice.MaterialId, sameprice.Grade, sameprice.InStock, sameprice.IsActive, sameprice.PriceUpdatedAt)
            .ShouldBe(("Putty", null, _paint.Id, MaterialGrade.Premium, false, false, Now));

        var repriced = await handler.HandleAsync(
            new UpdateStoreProduct(_market.Id, created.Id, "Putty", null, "P-25", 25m, "25 kg bag", 5400, null, null, false, null, null, null),
            CancellationToken.None);

        (repriced.MaterialId, repriced.Grade, repriced.UnitPrice, repriced.IsActive, repriced.PriceUpdatedAt).ShouldBe((null, null, null, true, Now.AddDays(10)));
        (await Should.ThrowAsync<NotFoundException>(() => handler.HandleAsync(
            new UpdateStoreProduct(_stroy.Id, created.Id, "X", null, null, 1m, null, 100, null, null, true, null, null, null), CancellationToken.None)))
            .Code.ShouldBe("store_product.not_found");
    }

    [Fact]
    public async Task A_code_can_stay_when_updating_but_not_clash()
    {
        var first = await CreateAsync(_market);
        var second = await CreateAsync(_market, Product(_market, code: "P-50", name: "50 kg"));
        var handler = new UpdateStoreProductHandler(_db, Files, _language, _settings, _clock);

        await handler.HandleAsync(new UpdateStoreProduct(_market.Id, first.Id, "Renamed", null, "P-25", 25m, null, 5000, null, null, true, null, null, null), CancellationToken.None);
        (await Should.ThrowAsync<ConflictException>(() => handler.HandleAsync(
            new UpdateStoreProduct(_market.Id, second.Id, "50 kg", null, "P-25", 25m, null, 5000, null, null, true, null, null, null), CancellationToken.None)))
            .Code.ShouldBe("store_product.code_taken");
    }

    [Fact]
    public async Task Confirming_a_price_marks_it_fresh()
    {
        var created = await CreateAsync(_market);
        _clock.Now = Now.AddDays(100);
        (await ListAsync(_market)).Items.Single().IsStale.ShouldBeTrue();

        var confirmed = await new ConfirmStoreProductPriceHandler(_db, Files, _language, _settings, _clock)
            .HandleAsync(new ConfirmStoreProductPrice(_market.Id, created.Id), CancellationToken.None);

        (confirmed.IsStale, confirmed.PriceUpdatedAt, confirmed.Price).ShouldBe((false, Now.AddDays(100), 5000));
    }

    [Fact]
    public async Task A_product_can_be_deleted()
    {
        var created = await CreateAsync(_market);

        await new DeleteStoreProductHandler(_db, _settings, _clock).HandleAsync(new DeleteStoreProduct(_market.Id, created.Id), CancellationToken.None);

        _db.StoreProducts.Count().ShouldBe(0);
        (await Should.ThrowAsync<NotFoundException>(() => new DeleteStoreProductHandler(_db, _settings, _clock).HandleAsync(new DeleteStoreProduct(_market.Id, created.Id), CancellationToken.None)))
            .Code.ShouldBe("store_product.not_found");
    }

    [Fact]
    public async Task The_product_list_filters_and_pages()
    {
        await CreateAsync(_market, Product(_market, name: "A putty", code: "A"));
        await CreateAsync(_market, Product(_market, name: "B paint", code: "B", material: _paint));
        await CreateAsync(_market, Product(_market, name: "C unlinked", code: "C") with { MaterialId = null, Grade = null });
        await CreateAsync(_stroy, Product(_stroy, name: "Other store", code: "A"));
        _db.StoreProducts.Single(p => p.Code == "B" && p.StoreId == _market.Id).ConfirmPrice(Now.AddDays(-100));
        await _db.SaveChangesAsync();

        Names(await ListAsync(_market)).ShouldBe(new[] { "A putty", "B paint", "C unlinked" });
        Names(await ListAsync(_market, linked: true)).ShouldBe(new[] { "A putty", "B paint" });
        Names(await ListAsync(_market, linked: false)).ShouldBe(new[] { "C unlinked" });
        Names(await ListAsync(_market, materialId: _paint.Id)).ShouldBe(new[] { "B paint" });
        Names(await ListAsync(_market, stale: true)).ShouldBe(new[] { "B paint" });
        Names(await ListAsync(_market, stale: false)).ShouldBe(new[] { "A putty", "C unlinked" });
        Names(await ListAsync(_market, search: "KNAUF")).Count.ShouldBe(3);
        Names(await ListAsync(_market, search: " unlinked ")).ShouldBe(new[] { "C unlinked" });
        Names(await ListAsync(_market, search: "b")).ShouldContain("B paint");

        var page = await ListAsync(_market, page: 2, pageSize: 2);
        Names(page).ShouldBe(new[] { "C unlinked" });
        (page.TotalCount, page.TotalPages).ShouldBe((3, 2));
    }

    [Fact]
    public async Task Products_are_listed_for_an_existing_store_only()
    {
        (await Should.ThrowAsync<NotFoundException>(() => new GetAdminStoreProductsHandler(_db, Files, _language, _settings, _clock)
            .HandleAsync(new GetAdminStoreProducts(Guid.NewGuid()), CancellationToken.None))).Code.ShouldBe("store.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => new CreateStoreProductHandler(_db, Files, _language, _settings, _clock)
            .HandleAsync(Product(_market) with { StoreId = Guid.NewGuid() }, CancellationToken.None))).Code.ShouldBe("store.not_found");
    }

    [Fact]
    public async Task A_material_lists_its_products_across_stores_by_grade_and_unit_price()
    {
        await CreateAsync(_market, Product(_market, name: "Premium", code: "1", price: 10000, grade: MaterialGrade.Premium));
        await CreateAsync(_market, Product(_market, name: "Standard dear", code: "2", price: 6000));
        await CreateAsync(_stroy, Product(_stroy, name: "Standard cheap", code: "1", price: 4000));
        await CreateAsync(_stroy, Product(_stroy, name: "Budget", code: "2", price: 2000, grade: MaterialGrade.Budget));
        await CreateAsync(_stroy, Product(_stroy, name: "Paint", code: "3", material: _paint));
        _stroy.Deactivate();
        await _db.SaveChangesAsync();

        var offers = await new GetMaterialStoreProductsHandler(_db, Files, _language, _settings, _clock)
            .HandleAsync(new GetMaterialStoreProducts(_putty.Id), CancellationToken.None);

        offers.Select(o => o.Product.Name).ToList().ShouldBe(new List<string> { "Budget", "Standard cheap", "Standard dear", "Premium" });
        offers.Select(o => (o.StoreName, o.StoreIsActive)).ToList().ShouldBe(new List<(string, bool)> { ("Demo Stroy", false), ("Demo Stroy", false), ("Demo Market", true), ("Demo Market", true) });
        (await Should.ThrowAsync<NotFoundException>(() => new GetMaterialStoreProductsHandler(_db, Files, _language, _settings, _clock)
            .HandleAsync(new GetMaterialStoreProducts(Guid.NewGuid()), CancellationToken.None))).Code.ShouldBe("material.not_found");
    }

    private static async Task<string[]> ErrorsAsync(CreateStoreProduct command) =>
        (await new CreateStoreProductValidator().ValidateAsync(command)).Errors.Select(e => e.ErrorCode).Order().ToArray();

    [Fact]
    public async Task Product_fields_are_validated()
    {
        var good = Product(_market);

        (await ErrorsAsync(good)).ShouldBeEmpty();
        (await ErrorsAsync(good with { Name = " " })).ShouldBe(new[] { "name.required" });
        (await ErrorsAsync(good with { Name = new string('x', StoreProduct.MaxNameLength + 1) })).ShouldBe(new[] { "name.too_long" });
        (await ErrorsAsync(good with { Brand = new string('x', StoreProduct.MaxBrandLength + 1) })).ShouldBe(new[] { "brand.too_long" });
        (await ErrorsAsync(good with { Code = new string('x', StoreProduct.MaxCodeLength + 1) })).ShouldBe(new[] { "code.too_long" });
        (await ErrorsAsync(good with { PackLabel = new string('x', StoreProduct.MaxPackLabelLength + 1) })).ShouldBe(new[] { "pack_label.too_long" });
        (await ErrorsAsync(good with { PackSize = 0m })).ShouldBe(new[] { "pack_size.invalid" });
        (await ErrorsAsync(good with { Price = 0 })).Contains("price.invalid").ShouldBeTrue();
        (await ErrorsAsync(good with { Price = Material.PriceLimit + 1 })).ShouldBe(new[] { "price.invalid" });
        (await ErrorsAsync(good with { SalePrice = 4000 })).ShouldBe(new[] { "sale.invalid" });
        (await ErrorsAsync(good with { SaleEndsOn = Today })).ShouldBe(new[] { "sale.invalid" });
        (await ErrorsAsync(good with { SalePrice = 5000, SaleEndsOn = Today })).ShouldBe(new[] { "sale.invalid" });
        (await ErrorsAsync(good with { SalePrice = 4000, SaleEndsOn = Today })).ShouldBeEmpty();
        (await ErrorsAsync(good with { Grade = null })).ShouldBe(new[] { "grade.required" });
        (await ErrorsAsync(good with { Grade = (MaterialGrade)99 })).ShouldBe(new[] { "grade.invalid" });
        (await ErrorsAsync(good with { MaterialId = null, Grade = null })).ShouldBeEmpty();
    }

    [Fact]
    public async Task The_page_is_validated()
    {
        var validator = new GetAdminStoreProductsValidator();

        (await validator.ValidateAsync(new GetAdminStoreProducts(Guid.NewGuid(), Page: 0))).Errors.Single().ErrorCode.ShouldBe("page.invalid");
        (await validator.ValidateAsync(new GetAdminStoreProducts(Guid.NewGuid(), PageSize: GetAdminStoreProducts.MaxPageSize + 1))).Errors.Single().ErrorCode.ShouldBe("page_size.invalid");
        (await validator.ValidateAsync(new GetAdminStoreProducts(Guid.NewGuid()))).IsValid.ShouldBeTrue();
    }
}
