using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Files;
using HomeServices.Application.Partners;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Partners;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Partners;

public class PublicPartnersTests
{
    private readonly PartnerTestData _data = new();

    private FileDtoFactory Files => new(_data.Storage, Options.Create(new FileSettings()), _data.Clock);

    private Task<PagedResult<PublicPartnerCardDto>> SearchAsync(SearchPartners query) =>
        new SearchPartnersHandler(_data.Db, new FakeCurrentLanguage("ru"), Files).HandleAsync(query, CancellationToken.None);

    private Task<PublicPartnerDto> GetAsync(string slug) =>
        new GetPublicPartnerHandler(_data.Db, new FakeCurrentLanguage("ru"), Files).HandleAsync(new GetPublicPartner(slug), CancellationToken.None);

    private PartnerProfile Approved(string name, Action<PartnerProfile>? configure = null, DateTimeOffset? approvedAt = null) =>
        _data.GivenProfile(name, PartnerStatus.Approved, approvedAt: approvedAt, configure: configure);

    [Fact]
    public async Task Only_approved_partners_with_active_accounts_are_public()
    {
        var approved = Approved("Visible");
        _data.GivenProfile("Draft", PartnerStatus.Draft);
        _data.GivenProfile("Under review");
        _data.GivenProfile("Rejected", PartnerStatus.Rejected);
        _data.GivenProfile("Suspended", PartnerStatus.Suspended);
        var blocked = Approved("Blocked owner");
        (await _data.Db.Users.SingleAsync(u => u.Id == blocked.UserId)).Block();
        await _data.Db.SaveChangesAsync();

        var page = await SearchAsync(new SearchPartners());

        page.TotalCount.ShouldBe(1);
        page.Items.ShouldHaveSingleItem().Slug.ShouldBe(approved.Slug);
        page.Items[0].Id.ShouldBe(approved.Id);
        (await Should.ThrowAsync<NotFoundException>(() => GetAsync(blocked.Slug!))).Code.ShouldBe("partner.not_found");
    }

    [Fact]
    public async Task A_card_shows_the_basics_a_cover_and_no_documents()
    {
        var partner = Approved("Aram Plumbing", p =>
        {
            p.UpdateDetails(PartnerType.Specialist, "Aram Plumbing", new string('a', 300), 12, null);
            p.SetAreas([(_data.Yerevan.Id, _data.Kentron.Id), (_data.Masis.Id, null)]);
        });

        var card = (await SearchAsync(new SearchPartners())).Items.Single();

        card.Slug.ShouldStartWith("aram-plumbing-");
        card.DisplayName.ShouldBe("Aram Plumbing");
        card.Type.ShouldBe("Specialist");
        card.YearsOfExperience.ShouldBe(12);
        card.AboutExcerpt.Length.ShouldBe(SearchPartnersHandler.ExcerptLength + 1);
        card.AboutExcerpt.ShouldEndWith("…");
        card.Categories.ShouldBe(new[] { new PublicCategoryDto("plumbing", "Սանտեխնիկա"), new PublicCategoryDto("heating", "Ջեռուցում") });
        card.Cities.ShouldBe(new[] { "Երևան", "Մասիս" });
        card.WorkExampleCount.ShouldBe(1);
        card.Cover.ShouldNotBeNull();
        card.Cover.Kind.ShouldBe("Image");
        var workFile = partner.Media.Single(m => m.Kind == PartnerMediaKind.WorkExample).FileId;
        card.Cover.Url.ShouldContain((await _data.Db.Files.SingleAsync(f => f.Id == workFile)).Key);
        card.Avatar.ShouldBeNull();
    }

    [Fact]
    public async Task A_category_includes_its_subcategories()
    {
        var plumber = Approved("Plumber", p => p.SetServices([_data.Plumbing.Id]));
        var boilerFitter = Approved("Boiler fitter", p => p.SetServices([_data.Boilers.Id]));

        (await SearchAsync(new SearchPartners(Category: "heating"))).Items.ShouldHaveSingleItem().Slug.ShouldBe(boilerFitter.Slug);
        (await SearchAsync(new SearchPartners(Category: "BOILERS"))).Items.ShouldHaveSingleItem().Slug.ShouldBe(boilerFitter.Slug);
        (await SearchAsync(new SearchPartners(Category: "plumbing"))).Items.ShouldHaveSingleItem().Slug.ShouldBe(plumber.Slug);
        (await SearchAsync(new SearchPartners(Category: "hidden"))).Items.ShouldBeEmpty();
        (await SearchAsync(new SearchPartners(Category: "unknown"))).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Partners_serving_a_whole_city_match_each_of_its_districts()
    {
        var wholeCity = Approved("Whole Yerevan", p => p.SetAreas([(_data.Yerevan.Id, null)]));
        var kentronOnly = Approved("Kentron only", p => p.SetAreas([(_data.Yerevan.Id, _data.Kentron.Id)]));
        var masis = Approved("Masis", p => p.SetAreas([(_data.Masis.Id, null)]));

        (await SearchAsync(new SearchPartners(City: "yerevan"))).Items.Select(i => i.Slug).ShouldBe(new[] { wholeCity.Slug, kentronOnly.Slug }, ignoreOrder: true);
        (await SearchAsync(new SearchPartners(City: "yerevan", District: "kentron"))).Items.Select(i => i.Slug).ShouldBe(new[] { wholeCity.Slug, kentronOnly.Slug }, ignoreOrder: true);
        (await SearchAsync(new SearchPartners(City: "masis"))).Items.ShouldHaveSingleItem().Slug.ShouldBe(masis.Slug);
        (await SearchAsync(new SearchPartners(City: "yerevan", District: "closed"))).Items.ShouldBeEmpty();
        (await SearchAsync(new SearchPartners(City: "closed-city"))).Items.ShouldBeEmpty();
        (await SearchAsync(new SearchPartners(City: "masis", District: "kentron"))).Items.ShouldBeEmpty();
        (await SearchAsync(new SearchPartners(City: "nowhere", District: "kentron"))).Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_district_partner_does_not_match_another_district()
    {
        var arabkir = _data.Yerevan.AddDistrict("arabkir", PartnerTestData.Text("Արաբկիր"), 3);
        _data.Db.Districts.Add(arabkir);
        await _data.Db.SaveChangesAsync();
        Approved("Kentron only", p => p.SetAreas([(_data.Yerevan.Id, _data.Kentron.Id)]));

        (await SearchAsync(new SearchPartners(City: "yerevan", District: "arabkir"))).Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Partners_are_filtered_by_type_and_searched_by_name_or_about()
    {
        var company = _data.GivenProfile("Best Build", PartnerStatus.Approved, PartnerType.Company);
        var tiler = Approved("Tiler", p => p.UpdateDetails(PartnerType.Specialist, "Tiler", "Bathrooms and kitchens with Italian tiles, 10 years.", null, null));

        (await SearchAsync(new SearchPartners(Type: PartnerType.Company))).Items.ShouldHaveSingleItem().Slug.ShouldBe(company.Slug);
        (await SearchAsync(new SearchPartners(Search: "BEST"))).Items.ShouldHaveSingleItem().Slug.ShouldBe(company.Slug);
        (await SearchAsync(new SearchPartners(Search: "italian"))).Items.ShouldHaveSingleItem().Slug.ShouldBe(tiler.Slug);
    }

    [Fact]
    public async Task Newest_approvals_come_first_and_the_list_is_paged()
    {
        var oldest = Approved("Oldest", approvedAt: PartnerTestData.Now.AddDays(-3));
        var middle = Approved("Middle", approvedAt: PartnerTestData.Now.AddDays(-2));
        var newest = Approved("Newest", approvedAt: PartnerTestData.Now.AddDays(-1));

        var first = await SearchAsync(new SearchPartners(PageSize: 2));
        var second = await SearchAsync(new SearchPartners(Page: 2, PageSize: 2));

        first.Items.Select(i => i.Slug).ShouldBe(new[] { newest.Slug, middle.Slug });
        second.Items.ShouldHaveSingleItem().Slug.ShouldBe(oldest.Slug);
        first.TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task A_public_profile_shows_work_areas_and_photos_but_not_documents()
    {
        var avatar = _data.GivenFile();
        var partner = Approved("Aram Plumbing", p =>
        {
            p.SetAreas([(_data.Yerevan.Id, _data.Kentron.Id), (_data.Masis.Id, null)]);
            p.UpdateDetails(PartnerType.Specialist, "Aram Plumbing", new string('a', 60), 7, avatar.Id);
        });

        var dto = await GetAsync(partner.Slug!.ToUpperInvariant());

        dto.Slug.ShouldBe(partner.Slug);
        dto.About.ShouldBe(new string('a', 60));
        dto.YearsOfExperience.ShouldBe(7);
        dto.MemberSince.ShouldBe(PartnerTestData.Now);
        dto.Avatar!.Url.ShouldContain(avatar.Key);
        dto.Areas.ShouldBe(new[]
        {
            new PublicAreaDto("yerevan", "Երևան", "kentron", "Կենտրոն"),
            new PublicAreaDto("masis", "Մասիս", null, null),
        });
        dto.WorkExamples.ShouldHaveSingleItem().Kind.ShouldBe("Image");
        dto.Categories.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Hidden_places_categories_and_unfinished_files_are_left_out()
    {
        var pending = _data.GivenFile(ready: false);
        var partner = Approved("Aram Plumbing", p =>
        {
            p.SetServices([_data.Plumbing.Id, _data.Hidden.Id]);
            p.SetAreas([(_data.Yerevan.Id, _data.ClosedDistrict.Id), (_data.ClosedCity.Id, null), (_data.Masis.Id, null)]);
            p.AddMedia(PartnerMediaKind.WorkExample, pending.Id, null);
        });

        var dto = await GetAsync(partner.Slug!);

        dto.Categories.ShouldBe(new[] { new PublicCategoryDto("plumbing", "Սանտեխնիկա") });
        dto.Areas.ShouldBe(new[] { new PublicAreaDto("masis", "Մասիս", null, null) });
        dto.WorkExamples.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Profiles_that_are_not_approved_are_404()
    {
        var underReview = _data.GivenProfile("Under review");

        (await Should.ThrowAsync<NotFoundException>(() => GetAsync(underReview.Slug!))).Code.ShouldBe("partner.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => GetAsync("no-such-partner"))).Code.ShouldBe("partner.not_found");
    }

    [Fact]
    public void Search_queries_are_validated()
    {
        var validator = new SearchPartnersValidator();

        validator.Validate(new SearchPartners(Category: "plumbing", City: "yerevan", District: "kentron")).IsValid.ShouldBeTrue();
        validator.Validate(new SearchPartners(District: "kentron", Type: (PartnerType)9, Search: new string('s', 101), Page: 0, PageSize: 51))
            .Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "district.needs_city", "type.invalid", "search.too_long", "page.invalid", "page_size.invalid" }, ignoreOrder: true);
    }
}
