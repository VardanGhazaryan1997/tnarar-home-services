using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Requests;
using HomeServices.Application.Tests.Partners;
using HomeServices.Domain;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Tests.Requests;

public class CustomerRequestsTests
{
    private readonly RequestTestData _data = new();

    private ServiceRequest Stored(Guid id) => _data.Db.ServiceRequests.Include(r => r.Recipients).Include(r => r.Media).AsNoTracking().Single(r => r.Id == id);

    private Task<PagedResult<MyRequestListItemDto>> MineAsync(GetMyRequests? query = null) =>
        new GetMyRequestsHandler(_data.Db, _data.Customer, _data.Language).HandleAsync(query ?? new GetMyRequests(), CancellationToken.None);

    private Task<MyRequestDto> GetAsync(Guid id) =>
        new GetMyRequestHandler(_data.Db, _data.Customer, _data.Language, _data.Files).HandleAsync(new GetMyRequest(id), CancellationToken.None);

    private Task<MyRequestDto> CancelAsync(Guid id, string? reason = null) =>
        new CancelMyRequestHandler(_data.Db, _data.Customer, _data.Language, _data.Files, _data.Clock).HandleAsync(new CancelMyRequest(id, reason), CancellationToken.None);

    [Fact]
    public async Task An_open_request_reaches_approved_partners_who_offer_the_category_in_the_area()
    {
        var p = _data.Partners;
        var wholeCity = _data.GivenPartner("Whole city");
        var inKentron = _data.GivenPartner("Kentron", profile => profile.SetAreas([(p.Yerevan.Id, p.Kentron.Id)]));
        _data.GivenPartner("Heating only", profile => profile.SetServices([p.Heating.Id]));
        _data.GivenPartner("Masis", profile => profile.SetAreas([(p.Masis.Id, null)]));
        _data.GivenPartner("Draft", status: PartnerStatus.Draft);
        _data.GivenPartner("Suspended", status: PartnerStatus.Suspended);
        var blocked = _data.GivenPartner("Blocked owner");
        _data.Db.Users.Single(u => u.Id == blocked.UserId).Block("Spam");
        await _data.Db.SaveChangesAsync();

        var dto = await _data.CreateAsync(_data.OpenRequest());

        var request = Stored(dto.Id);
        request.Recipients.Select(r => r.PartnerProfileId).ShouldBe(new[] { wholeCity.Id, inKentron.Id }, ignoreOrder: true);
        request.Recipients.ShouldAllBe(r => r.Source == RecipientSource.Matched && r.SentAt == RequestTestData.Now);
        request.CustomerId.ShouldBe(p.User.Id);
        request.NeedsAttention.ShouldBeFalse();

        dto.Kind.ShouldBe("Open");
        dto.Status.ShouldBe("Open");
        dto.SentTo.ShouldBe(2);
        dto.Partners.ShouldBeEmpty();
        dto.FindingPartners.ShouldBeFalse();
        dto.Place.ShouldBe(new RequestPlaceDto(p.Plumbing.Id, "Սանտեխնիկա", p.Yerevan.Id, "Երևան", null, null));
        dto.Description.ShouldBe("The kitchen tap is leaking, please help.");
        dto.PreferredDate.ShouldBe(new DateOnly(2026, 10, 7));
        dto.TimeNote.ShouldBe("evenings");
        dto.BudgetMin.ShouldBe(10_000);
        dto.BudgetMax.ShouldBe(30_000);
    }

    [Fact]
    public async Task A_district_request_skips_partners_working_only_in_other_districts()
    {
        var p = _data.Partners;
        var other = p.Yerevan.AddDistrict("arabkir", PartnerTestData.Text("Արաբկիր"), 3);
        _data.Db.Districts.Add(other);
        await _data.Db.SaveChangesAsync();
        var wholeCity = _data.GivenPartner("Whole city");
        _data.GivenPartner("Arabkir", profile => profile.SetAreas([(p.Yerevan.Id, other.Id)]));

        var dto = await _data.CreateAsync(_data.OpenRequest(districtId: p.Kentron.Id));

        Stored(dto.Id).Recipients.ShouldHaveSingleItem().PartnerProfileId.ShouldBe(wholeCity.Id);
        dto.Place.DistrictName.ShouldBe("Կենտրոն");
    }

    [Fact]
    public async Task A_subcategory_request_also_reaches_partners_of_its_parent_category()
    {
        var p = _data.Partners;
        var heating = _data.GivenPartner("Heating", profile => profile.SetServices([p.Heating.Id]));
        var boilers = _data.GivenPartner("Boilers", profile => profile.SetServices([p.Boilers.Id]));
        _data.GivenPartner("Plumbing");

        var dto = await _data.CreateAsync(_data.OpenRequest(categoryId: p.Boilers.Id));

        Stored(dto.Id).Recipients.Select(r => r.PartnerProfileId).ShouldBe(new[] { heating.Id, boilers.Id }, ignoreOrder: true);
    }

    [Fact]
    public async Task The_customers_own_partner_profile_never_receives_their_request()
    {
        var mine = _data.GivenPartner("Mine");
        var customer = RequestTestData.As(mine);

        var dto = await _data.CreateAsync(_data.OpenRequest(), customer);

        dto.SentTo.ShouldBe(0);
        dto.FindingPartners.ShouldBeTrue();
    }

    [Fact]
    public async Task Without_matching_partners_an_operator_takes_over()
    {
        var dto = await _data.CreateAsync(_data.OpenRequest());

        dto.SentTo.ShouldBe(0);
        dto.FindingPartners.ShouldBeTrue();
        var request = Stored(dto.Id);
        request.AttentionReason.ShouldBe(AttentionReason.NoMatchingPartners);
        request.NeedsAttentionSince.ShouldBe(RequestTestData.Now);
    }

    [Fact]
    public async Task At_most_the_configured_number_of_partners_receive_an_open_request()
    {
        _data.Settings.MaxRecipients = 2;
        for (var i = 0; i < 4; i++)
        {
            _data.GivenPartner($"Partner {i}");
        }

        var dto = await _data.CreateAsync(_data.OpenRequest());

        dto.SentTo.ShouldBe(2);
        Stored(dto.Id).Recipients.Count.ShouldBe(2);
    }

    [Fact]
    public async Task A_direct_request_goes_only_to_the_chosen_partner_who_is_shown_to_the_customer()
    {
        var chosen = _data.GivenPartner("Chosen");
        _data.GivenPartner("Other");

        var dto = await _data.CreateAsync(_data.DirectRequest(chosen.Id));

        dto.Kind.ShouldBe("Direct");
        dto.SentTo.ShouldBe(1);
        var partner = dto.Partners.ShouldHaveSingleItem();
        partner.PartnerId.ShouldBe(chosen.Id);
        partner.DisplayName.ShouldBe("Chosen");
        partner.Slug.ShouldBe(chosen.Slug);
        partner.Status.ShouldBe("New");
        Stored(dto.Id).Recipients.ShouldHaveSingleItem().Source.ShouldBe(RecipientSource.Direct);
    }

    [Fact]
    public async Task A_direct_request_needs_a_partner_who_can_take_requests_and_is_not_the_customer()
    {
        var suspended = _data.GivenPartner("Suspended", status: PartnerStatus.Suspended);
        var mine = _data.GivenPartner("Mine");

        (await Should.ThrowAsync<DomainException>(() => _data.CreateAsync(_data.DirectRequest(suspended.Id))))
            .Code.ShouldBe("request.partner_unavailable");
        (await Should.ThrowAsync<DomainException>(() => _data.CreateAsync(_data.DirectRequest(mine.Id), RequestTestData.As(mine))))
            .Code.ShouldBe("request.own_partner");
        _data.Db.ServiceRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Photos_are_attached_and_returned_with_links()
    {
        var photo = _data.Partners.GivenFile();
        var video = _data.Partners.GivenFile("video/mp4");

        var dto = await _data.CreateAsync(_data.OpenRequest() with { MediaFileIds = [photo.Id, video.Id] });

        dto.Media.Select(m => m.Id).ShouldBe(new[] { photo.Id, video.Id });
        dto.Media[0].Url.ShouldNotBeNull().ShouldStartWith("https://storage.test/");
        Stored(dto.Id).Media.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Staff_and_signed_out_callers_cannot_create_requests()
    {
        await Should.ThrowAsync<UnauthorizedException>(() => _data.CreateAsync(_data.OpenRequest(), RequestTestData.Staff));
    }

    [Fact]
    public async Task The_customer_lists_their_own_requests_newest_first_with_counts()
    {
        var aram = _data.GivenPartner("Aram");
        var gor = _data.GivenPartner("Gor");
        var older = _data.GivenRequest(partners: [aram, gor]);
        var newer = _data.GivenRequest(description: "Install a new shower cabin, please.");
        newer.FlagForOperator(AttentionReason.NoMatchingPartners, RequestTestData.Now);
        older.MarkResponded(aram.Id, RequestTestData.Now);
        await _data.Db.SaveChangesAsync();
        _data.GivenRequest(customerId: _data.Partners.OtherUser.Id);

        var page = await MineAsync();

        page.TotalCount.ShouldBe(2);
        page.Items.Select(i => i.Id).ShouldBe(new[] { newer.Id, older.Id });
        var item = page.Items[1];
        item.SentTo.ShouldBe(2);
        item.Responded.ShouldBe(1);
        item.FindingPartners.ShouldBeFalse();
        item.Excerpt.ShouldBe("Need a plumber for the bathroom.");
        item.Place.CityName.ShouldBe("Երևան");
        page.Items[0].FindingPartners.ShouldBeTrue();
    }

    [Fact]
    public async Task The_list_is_filtered_by_status_and_paged()
    {
        var cancelled = _data.GivenRequest();
        cancelled.Cancel(null, RequestTestData.Now);
        await _data.Db.SaveChangesAsync();
        _data.GivenRequest();
        _data.GivenRequest();

        (await MineAsync(new GetMyRequests(RequestStatus.Cancelled))).Items.ShouldHaveSingleItem().Id.ShouldBe(cancelled.Id);
        var page = await MineAsync(new GetMyRequests(Page: 2, PageSize: 2));
        page.TotalCount.ShouldBe(3);
        page.Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Long_descriptions_are_shortened_in_the_list()
    {
        _data.GivenRequest(description: new string('a', 300));

        var item = (await MineAsync()).Items.ShouldHaveSingleItem();

        item.Excerpt.Length.ShouldBe(160);
        item.Excerpt.ShouldEndWith("…");
    }

    [Fact]
    public async Task An_open_request_shows_only_partners_who_responded()
    {
        var aram = _data.GivenPartner("Aram");
        var gor = _data.GivenPartner("Gor");
        var request = _data.GivenRequest(partners: [aram, gor]);
        request.MarkResponded(gor.Id, RequestTestData.Now);
        request.Decline(aram.Id, "busy", RequestTestData.Now);
        await _data.Db.SaveChangesAsync();

        var dto = await GetAsync(request.Id);

        dto.Partners.ShouldHaveSingleItem().DisplayName.ShouldBe("Gor");
        dto.SentTo.ShouldBe(2);
    }

    [Fact]
    public async Task Someone_elses_request_is_not_found()
    {
        var request = _data.GivenRequest(customerId: _data.Partners.OtherUser.Id);

        (await Should.ThrowAsync<NotFoundException>(() => GetAsync(request.Id))).Code.ShouldBe("request.not_found");
        await Should.ThrowAsync<NotFoundException>(() => CancelAsync(request.Id));
    }

    [Fact]
    public async Task The_customer_cancels_an_open_request()
    {
        var request = _data.GivenRequest();

        var dto = await CancelAsync(request.Id, " Found someone ");

        dto.Status.ShouldBe("Cancelled");
        dto.CancelReason.ShouldBe("Found someone");
        dto.CancelledAt.ShouldBe(RequestTestData.Now);
        (await Should.ThrowAsync<DomainException>(() => CancelAsync(request.Id))).Code.ShouldBe("request.not_open");
    }
}
