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

public class AdminStoresTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly FakeClock _clock = new(Now);
    private readonly IOptions<StorePricingSettings> _settings = Options.Create(new StorePricingSettings());

    private FileDtoFactory Files => new(new FakeFileStorage(), Options.Create(new FileSettings()), _clock);

    private static LocalizedText Text(string hy) => LocalizedText.Empty.With("hy", hy);

    private static CreateStore NewStore(string slug = "demo-market", string name = "Demo Market", Guid? logo = null, bool isActive = true) =>
        new(slug, name, new Dictionary<string, string> { ["en"] = "Building materials" }, "https://demo.example", "+374 10 000000", logo, 1, isActive);

    private Task<AdminStoreDto> CreateAsync(CreateStore command) =>
        new CreateStoreHandler(_db, Files, _settings, _clock).HandleAsync(command, CancellationToken.None);

    private async Task<City> GivenCityAsync(string slug, params string[] districts)
    {
        var city = City.Create(slug, Text(slug), 1);
        foreach (var district in districts)
        {
            city.AddDistrict(district, Text(district), 1);
        }

        _db.Cities.Add(city);
        await _db.SaveChangesAsync();
        return city;
    }

    private async Task<StoredFile> GivenImageAsync(bool ready = true)
    {
        var file = StoredFile.Begin(FileOwnerType.Staff, "staff-1", "logo.png", "image/png", 1000, Now);
        if (ready)
        {
            file.MarkReady(1000, Now);
        }

        _db.Files.Add(file);
        await _db.SaveChangesAsync();
        return file;
    }

    [Fact]
    public async Task A_store_is_created_with_its_fields_and_no_branches_or_products()
    {
        var store = await CreateAsync(NewStore(isActive: false));

        (store.Slug, store.Name, store.Website, store.Phone, store.IsActive).ShouldBe(("demo-market", "Demo Market", "https://demo.example", "+374 10 000000", false));
        store.Description["en"].ShouldBe("Building materials");
        (store.BranchCount, store.ProductCount, store.LinkedCount, store.StaleCount).ShouldBe((0, 0, 0, 0));
        store.Branches.ShouldNotBeNull().ShouldBeEmpty();
        _db.Stores.Count().ShouldBe(1);
    }

    [Fact]
    public async Task A_slug_is_used_by_one_store()
    {
        await CreateAsync(NewStore());

        var exception = await Should.ThrowAsync<ConflictException>(() => CreateAsync(NewStore(name: "Other")));
        exception.Code.ShouldBe("store.slug_taken");
    }

    [Fact]
    public async Task A_logo_must_be_an_uploaded_image()
    {
        (await Should.ThrowAsync<DomainException>(() => CreateAsync(NewStore(logo: Guid.NewGuid())))).Code.ShouldBe("store.file_invalid");
        var pending = await GivenImageAsync(ready: false);
        (await Should.ThrowAsync<DomainException>(() => CreateAsync(NewStore(logo: pending.Id)))).Code.ShouldBe("store.file_invalid");

        var logo = await GivenImageAsync();
        var store = await CreateAsync(NewStore(logo: logo.Id));

        store.LogoFileId.ShouldBe(logo.Id);
        store.LogoUrl.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task A_store_is_updated_and_can_be_hidden()
    {
        var created = await CreateAsync(NewStore());
        var other = await CreateAsync(NewStore("other", "Other"));

        var updated = await new UpdateStoreHandler(_db, Files, _settings, _clock).HandleAsync(
            new UpdateStore(created.Id, "demo-market-2", "Demo Market 2", null, null, null, null, 5, false), CancellationToken.None);

        (updated.Slug, updated.Name, updated.Website, updated.SortOrder, updated.IsActive).ShouldBe(("demo-market-2", "Demo Market 2", null, 5, false));
        updated.Description.ShouldBeEmpty();
        var clash = await Should.ThrowAsync<ConflictException>(() => new UpdateStoreHandler(_db, Files, _settings, _clock).HandleAsync(
            new UpdateStore(other.Id, "demo-market-2", "Other", null, null, null, null, 1), CancellationToken.None));
        clash.Code.ShouldBe("store.slug_taken");
        var missing = await Should.ThrowAsync<NotFoundException>(() => new UpdateStoreHandler(_db, Files, _settings, _clock).HandleAsync(
            new UpdateStore(Guid.NewGuid(), "x", "X", null, null, null, null, 1), CancellationToken.None));
        missing.Code.ShouldBe("store.not_found");
    }

    [Fact]
    public async Task The_list_counts_branches_products_linked_and_stale_prices()
    {
        var market = await CreateAsync(NewStore());
        await CreateAsync(NewStore("stroy", "Stroy", isActive: false));
        var yerevan = await GivenCityAsync("yerevan");
        await new SetStoreBranchesHandler(_db, Files, _settings, _clock).HandleAsync(
            new SetStoreBranches(market.Id, [new StoreBranchInput("Komitas", yerevan.Id, null, "Komitas 1")]), CancellationToken.None);
        var putty = Material.Create("putty", Text("putty"), MaterialUnit.Kilogram, 100, 200, 300, 1, Now);
        _db.Materials.Add(putty);
        var linked = StoreProduct.Create(market.Id, "Putty 25", null, null, 25m, null, 5000, null, null, true, null, Now.AddDays(-10));
        linked.LinkTo(putty.Id, MaterialGrade.Standard);
        _db.StoreProducts.AddRange(
            linked,
            StoreProduct.Create(market.Id, "Old price", null, null, 1m, null, 100, null, null, true, null, Now.AddDays(-100)));
        await _db.SaveChangesAsync();

        var list = await new GetAdminStoresHandler(_db, Files, _settings, _clock).HandleAsync(new GetAdminStores(), CancellationToken.None);

        list.Select(s => s.Slug).ShouldBe(new[] { "demo-market", "stroy" });
        (list[0].BranchCount, list[0].ProductCount, list[0].LinkedCount, list[0].StaleCount).ShouldBe((1, 2, 1, 1));
        (list[1].BranchCount, list[1].ProductCount, list[1].LinkedCount, list[1].StaleCount).ShouldBe((0, 0, 0, 0));
        list[0].Branches.ShouldBeNull();

        (await new GetAdminStoresHandler(_db, Files, _settings, _clock).HandleAsync(new GetAdminStores("STR"), CancellationToken.None))
            .Select(s => s.Slug).ShouldBe(new[] { "stroy" });
        (await new GetAdminStoresHandler(_db, Files, _settings, _clock).HandleAsync(new GetAdminStores(IsActive: true), CancellationToken.None))
            .Select(s => s.Slug).ShouldBe(new[] { "demo-market" });
    }

    [Fact]
    public async Task A_store_with_products_cant_be_deleted_but_an_empty_one_can()
    {
        var market = await CreateAsync(NewStore());
        var empty = await CreateAsync(NewStore("empty", "Empty"));
        var yerevan = await GivenCityAsync("yerevan");
        await new SetStoreBranchesHandler(_db, Files, _settings, _clock).HandleAsync(
            new SetStoreBranches(empty.Id, [new StoreBranchInput("Komitas", yerevan.Id, null, "Komitas 1")]), CancellationToken.None);
        _db.StoreProducts.Add(StoreProduct.Create(market.Id, "Putty", null, null, 1m, null, 100, null, null, true, null, Now));
        await _db.SaveChangesAsync();

        (await Should.ThrowAsync<DomainException>(() => new DeleteStoreHandler(_db).HandleAsync(new DeleteStore(market.Id), CancellationToken.None)))
            .Code.ShouldBe("store.has_products");
        await new DeleteStoreHandler(_db).HandleAsync(new DeleteStore(empty.Id), CancellationToken.None);

        _db.Stores.Select(s => s.Slug).ToList().ShouldBe(new[] { "demo-market" });
        _db.StoreBranches.Count().ShouldBe(0);
    }

    [Fact]
    public async Task Branches_are_replaced_as_a_whole()
    {
        var store = await CreateAsync(NewStore());
        var yerevan = await GivenCityAsync("yerevan", "kentron");
        var gyumri = await GivenCityAsync("gyumri");
        var handler = new SetStoreBranchesHandler(_db, Files, _settings, _clock);

        var first = await handler.HandleAsync(
            new SetStoreBranches(
                store.Id,
                [
                    new StoreBranchInput("Komitas", yerevan.Id, null, "Komitas 1", "+374 10 111111", "Mon–Sat 9–19"),
                    new StoreBranchInput("Center", yerevan.Id, yerevan.Districts.Single().Id, "Abovyan 5"),
                ]),
            CancellationToken.None);
        first.BranchCount.ShouldBe(2);
        first.Branches!.Select(b => b.Name).ShouldBe(new[] { "Komitas", "Center" });
        first.Branches![0].OpeningHours.ShouldBe("Mon–Sat 9–19");

        var second = await handler.HandleAsync(new SetStoreBranches(store.Id, [new StoreBranchInput("Shirak", gyumri.Id, null, "Garegin 3")]), CancellationToken.None);

        second.Branches!.Select(b => b.Name).ShouldBe(new[] { "Shirak" });
        _db.StoreBranches.Count().ShouldBe(1);
        (await new GetAdminStoreHandler(_db, Files, _settings, _clock).HandleAsync(new GetAdminStore(store.Id), CancellationToken.None))
            .Branches!.Select(b => b.Name).ShouldBe(new[] { "Shirak" });

        (await handler.HandleAsync(new SetStoreBranches(store.Id, []), CancellationToken.None)).BranchCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_branch_needs_an_existing_town_and_a_district_in_it()
    {
        var store = await CreateAsync(NewStore());
        var yerevan = await GivenCityAsync("yerevan", "kentron");
        var gyumri = await GivenCityAsync("gyumri");
        var handler = new SetStoreBranchesHandler(_db, Files, _settings, _clock);

        (await Should.ThrowAsync<DomainException>(() => handler.HandleAsync(
            new SetStoreBranches(store.Id, [new StoreBranchInput("A", Guid.NewGuid(), null, "Street 1")]), CancellationToken.None)))
            .Code.ShouldBe("store_branch.city_not_found");
        (await Should.ThrowAsync<DomainException>(() => handler.HandleAsync(
            new SetStoreBranches(store.Id, [new StoreBranchInput("A", gyumri.Id, yerevan.Districts.Single().Id, "Street 1")]), CancellationToken.None)))
            .Code.ShouldBe("store_branch.district_invalid");
        (await Should.ThrowAsync<DomainException>(() => handler.HandleAsync(
            new SetStoreBranches(store.Id, [new StoreBranchInput("A", yerevan.Id, Guid.NewGuid(), "Street 1")]), CancellationToken.None)))
            .Code.ShouldBe("store_branch.district_invalid");
        (await Should.ThrowAsync<NotFoundException>(() => handler.HandleAsync(
            new SetStoreBranches(Guid.NewGuid(), []), CancellationToken.None))).Code.ShouldBe("store.not_found");
    }

    private static async Task<string[]> ErrorsAsync(CreateStore command) =>
        (await new CreateStoreValidator().ValidateAsync(command)).Errors.Select(e => e.ErrorCode).ToArray();

    [Fact]
    public async Task Store_fields_are_validated()
    {
        (await ErrorsAsync(NewStore())).ShouldBeEmpty();
        (await ErrorsAsync(NewStore(slug: "Not a slug"))).ShouldBe(new[] { "slug.invalid" });
        (await ErrorsAsync(NewStore(name: " "))).ShouldBe(new[] { "name.required" });
        (await ErrorsAsync(NewStore(name: new string('x', Store.MaxNameLength + 1)))).ShouldBe(new[] { "name.too_long" });
        (await ErrorsAsync(NewStore() with { Website = "ftp://demo.example" })).ShouldBe(new[] { "website.invalid" });
        (await ErrorsAsync(NewStore() with { Website = "  " })).ShouldBeEmpty();
        (await ErrorsAsync(NewStore() with { Phone = new string('1', Store.MaxPhoneLength + 1) })).ShouldBe(new[] { "phone.too_long" });
        (await ErrorsAsync(NewStore() with { SortOrder = -1 })).ShouldBe(new[] { "sort_order.invalid" });
        (await ErrorsAsync(NewStore() with { Description = new Dictionary<string, string> { ["english!"] = "x" } })).ShouldBe(new[] { "description.language_invalid" });
        (await ErrorsAsync(NewStore() with { Description = new Dictionary<string, string> { ["en"] = new string('x', StoreRules.DescriptionMaxLength + 1) } })).ShouldBe(new[] { "description.too_long" });
    }

    [Fact]
    public async Task Branch_lists_are_validated()
    {
        var validator = new SetStoreBranchesValidator();
        var good = new StoreBranchInput("Komitas", Guid.NewGuid(), null, "Komitas 1");

        (await validator.ValidateAsync(new SetStoreBranches(Guid.NewGuid(), [good]))).IsValid.ShouldBeTrue();
        (await validator.ValidateAsync(new SetStoreBranches(Guid.NewGuid(), [good with { Name = " " }]))).Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "branch.invalid" });
        (await validator.ValidateAsync(new SetStoreBranches(Guid.NewGuid(), [good with { Address = "" }]))).Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "branch.invalid" });
        (await validator.ValidateAsync(new SetStoreBranches(Guid.NewGuid(), [good with { CityId = Guid.Empty }]))).Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "branch.invalid" });
        (await validator.ValidateAsync(new SetStoreBranches(Guid.NewGuid(), Enumerable.Repeat(good, Store.MaxBranches + 1).ToList()))).Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "branches.too_many" });
    }
}
