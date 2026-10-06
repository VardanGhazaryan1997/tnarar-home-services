using HomeServices.Application.Catalog;
using HomeServices.Application.Errors;
using HomeServices.Application.Partners;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Tests.Partners;

public class PartnerPricesTests
{
    private readonly PartnerTestData _data = new();
    private readonly Category _leaks;
    private readonly Category _pipes;
    private readonly WorkItem _leakRepair;
    private readonly WorkItem _jointRepair;
    private readonly WorkItem _pipePoint;
    private readonly WorkItem _boilerService;

    public PartnerPricesTests()
    {
        _leaks = Category.Create("leaks", PartnerTestData.Text("Արտահոսքեր"), 1, _data.Plumbing.Id);
        _pipes = Category.Create("pipes", PartnerTestData.Text("Խողովակներ"), 2, _data.Plumbing.Id);
        var hiddenSub = Category.Create("hidden-sub", PartnerTestData.Text("Թաքնված"), 3, _data.Plumbing.Id);
        hiddenSub.Deactivate();
        Db.Categories.AddRange(_leaks, _pipes, hiddenSub);

        _leakRepair = Item(_leaks, "leak-repair", 1, new PriceRange(8000, 15000, 25000));
        _jointRepair = Item(_leaks, "joint-repair", 2);
        _pipePoint = Item(_pipes, "pipe-point", 1, new PriceRange(8000, 12000, 18000));
        _boilerService = Item(_data.Boilers, "boiler-service", 1);
        var hiddenItem = Item(_pipes, "hidden-item", 2);
        hiddenItem.Deactivate();
        Item(hiddenSub, "in-hidden-category", 1);
        Db.SaveChanges();
    }

    private InMemoryAppDbContext Db => _data.Db;

    private WorkItem Item(Category category, string slug, int sortOrder, PriceRange? price = null)
    {
        var item = WorkItem.Create(category.Id, slug, PartnerTestData.Text(slug), WorkUnit.Piece, WorkSurface.None, sortOrder);
        item.SetPrice(price);
        Db.WorkItems.Add(item);
        return item;
    }

    private async Task GivenMyProfile(params Guid[] services)
    {
        Db.ChangeTracker.Clear();
        await new SaveMyPartnerProfileHandler(Db, _data.Me, _data.Dtos).HandleAsync(
            _data.ValidSave() with { CategoryIds = services.Length > 0 ? services : [_data.Plumbing.Id] },
            CancellationToken.None);
    }

    private Task<MyPriceListDto> GetAsync() =>
        new GetMyPricesHandler(Db, _data.Me, new FakeCurrentLanguage("hy")).HandleAsync(new GetMyPrices(), CancellationToken.None);

    private async Task<MyPriceListDto> SaveAsync(params MyPriceInput[] prices)
    {
        Db.ChangeTracker.Clear();
        return await new SaveMyPricesHandler(Db, _data.Me, new FakeCurrentLanguage("hy"))
            .HandleAsync(new SaveMyPrices(prices), CancellationToken.None);
    }

    [Fact]
    public async Task A_user_without_a_partner_profile_gets_404()
    {
        (await Should.ThrowAsync<NotFoundException>(() => GetAsync())).Code.ShouldBe("partner.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => SaveAsync())).Code.ShouldBe("partner.not_found");
    }

    [Fact]
    public async Task The_list_has_the_work_of_the_offered_services_in_catalog_order_with_market_ranges()
    {
        await GivenMyProfile();

        var list = await GetAsync();

        list.Items.Select(i => i.Slug).ShouldBe(new[] { "leak-repair", "joint-repair", "pipe-point" });
        var leak = list.Items[0];
        leak.Name.ShouldBe("leak-repair");
        leak.Unit.ShouldBe(WorkUnit.Piece);
        leak.CategoryId.ShouldBe(_leaks.Id);
        leak.CategoryName.ShouldBe("Արտահոսքեր");
        leak.MainCategoryId.ShouldBe(_data.Plumbing.Id);
        leak.MainCategoryName.ShouldBe("Սանտեխնիկա");
        (leak.MarketMin, leak.MarketTypical, leak.MarketMax).ShouldBe((8000, 15000, 25000));
        leak.PriceFrom.ShouldBeNull();
        list.Items[1].MarketTypical.ShouldBeNull();
        list.PricedCount.ShouldBe(0);
    }

    [Fact]
    public async Task Offering_a_subcategory_lists_only_its_work()
    {
        await GivenMyProfile(_data.Boilers.Id);

        (await GetAsync()).Items.Select(i => i.Slug).ShouldBe(new[] { "boiler-service" });
    }

    [Fact]
    public async Task Saves_single_prices_and_ranges_with_the_materials_flag()
    {
        await GivenMyProfile();

        var list = await SaveAsync(
            new MyPriceInput(_leakRepair.Id, 10000, 20000, IncludesMaterials: false),
            new MyPriceInput(_pipePoint.Id, 12000, 12000, IncludesMaterials: true));

        list.PricedCount.ShouldBe(2);
        var leak = list.Items.Single(i => i.WorkItemId == _leakRepair.Id);
        (leak.PriceFrom, leak.PriceTo, leak.IncludesMaterials).ShouldBe((10000, 20000, false));
        var pipe = list.Items.Single(i => i.WorkItemId == _pipePoint.Id);
        (pipe.PriceFrom, pipe.PriceTo, pipe.IncludesMaterials).ShouldBe((12000, (int?)null, true)); // equal ends = one price
        (await GetAsync()).PricedCount.ShouldBe(2);
    }

    [Fact]
    public async Task Saving_again_replaces_the_list()
    {
        await GivenMyProfile();
        await SaveAsync(new MyPriceInput(_leakRepair.Id, 10000, null, false), new MyPriceInput(_pipePoint.Id, 9000, null, false));

        var list = await SaveAsync(new MyPriceInput(_leakRepair.Id, 11000, 14000, true), new MyPriceInput(_jointRepair.Id, 5000, null, false));

        list.Items.Where(i => i.PriceFrom is not null).Select(i => (i.Slug, i.PriceFrom, i.PriceTo))
            .ShouldBe(new[] { ("leak-repair", (int?)11000, (int?)14000), ("joint-repair", 5000, null) });
        Db.ChangeTracker.Clear();
        (await Db.PartnerPrices.CountAsync()).ShouldBe(2);
    }

    [Fact]
    public async Task Work_outside_the_offered_services_cannot_be_priced()
    {
        await GivenMyProfile();

        (await Should.ThrowAsync<DomainException>(() => SaveAsync(new MyPriceInput(_boilerService.Id, 10000, null, false))))
            .Code.ShouldBe("partner_price.not_offered");
        (await Should.ThrowAsync<DomainException>(() => SaveAsync(new MyPriceInput(Guid.NewGuid(), 10000, null, false))))
            .Code.ShouldBe("partner_price.not_offered");
    }

    [Fact]
    public async Task Prices_of_services_no_longer_offered_are_kept_for_later()
    {
        await GivenMyProfile(_data.Plumbing.Id, _data.Heating.Id);
        await SaveAsync(new MyPriceInput(_leakRepair.Id, 10000, null, false), new MyPriceInput(_boilerService.Id, 15000, null, false));

        await GivenMyProfile(_data.Heating.Id);
        (await SaveAsync()).Items.Select(i => i.Slug).ShouldBe(new[] { "boiler-service" });

        await GivenMyProfile(_data.Plumbing.Id);
        (await GetAsync()).Items.Single(i => i.WorkItemId == _leakRepair.Id).PriceFrom.ShouldBe(10000);
    }

    [Fact]
    public async Task The_Back_Office_sees_how_many_partners_priced_an_item_and_cannot_delete_it()
    {
        await GivenMyProfile();
        await SaveAsync(new MyPriceInput(_leakRepair.Id, 10000, null, false));

        var items = await new GetAdminWorkItemsHandler(Db).HandleAsync(new GetAdminWorkItems(), CancellationToken.None);
        items.Single(i => i.Id == _leakRepair.Id).PartnerCount.ShouldBe(1);
        items.Single(i => i.Id == _pipePoint.Id).PartnerCount.ShouldBe(0);

        var hidden = await new SetWorkItemActiveHandler(Db).HandleAsync(new SetWorkItemActive(_leakRepair.Id, false), CancellationToken.None);
        hidden.PartnerCount.ShouldBe(1);

        (await Should.ThrowAsync<DomainException>(() =>
            new DeleteWorkItemHandler(Db).HandleAsync(new DeleteWorkItem(_leakRepair.Id), CancellationToken.None)))
            .Code.ShouldBe("work_item.has_prices");
    }

    [Fact]
    public async Task The_validator_rejects_duplicates_and_invalid_prices()
    {
        var validator = new SaveMyPricesValidator();
        var id = Guid.NewGuid();

        (await validator.ValidateAsync(new SaveMyPrices([new MyPriceInput(id, 1, null, false)]))).IsValid.ShouldBeTrue();
        (await validator.ValidateAsync(new SaveMyPrices([new MyPriceInput(id, 1, null, false), new MyPriceInput(id, 2, null, false)])))
            .Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "prices.duplicate" });
        (await validator.ValidateAsync(new SaveMyPrices([new MyPriceInput(id, 0, null, false)])))
            .Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "partner_price.invalid" });
        (await validator.ValidateAsync(new SaveMyPrices([new MyPriceInput(id, 500, 400, false)])))
            .Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "partner_price.invalid" });
        (await validator.ValidateAsync(new SaveMyPrices(null!))).Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "prices.required" });
    }
}
