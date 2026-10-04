using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Requests;
using HomeServices.Domain;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Requests;

public class AdminRequestsTests
{
    private readonly RequestTestData _data = new();

    private Task<PagedResult<AdminRequestListItemDto>> ListAsync(GetAdminRequests? query = null) =>
        new GetAdminRequestsHandler(_data.Db, _data.Language).HandleAsync(query ?? new GetAdminRequests(), CancellationToken.None);

    private Task<AdminRequestDto> GetAsync(Guid id) =>
        new GetAdminRequestHandler(_data.Db, _data.Language, _data.Files).HandleAsync(new GetAdminRequest(id), CancellationToken.None);

    private Task<AdminRequestDto> AssignAsync(Guid id, params Guid[] partnerIds) =>
        new AssignRequestHandler(_data.Db, _data.Language, _data.Files, _data.Clock).HandleAsync(new AssignRequest(id, partnerIds), CancellationToken.None);

    private Task<AdminRequestDto> CancelAsync(Guid id, string reason) =>
        new CancelRequestByStaffHandler(_data.Db, _data.Language, _data.Files, _data.Clock).HandleAsync(new CancelRequestByStaff(id, reason), CancellationToken.None);

    private Task<int> FollowUpAsync() =>
        new FlagUnansweredRequestsHandler(_data.Db, Options.Create(_data.Settings), _data.Clock).HandleAsync(new FlagUnansweredRequests(), CancellationToken.None);

    private ServiceRequest Stored(Guid id) => _data.Db.ServiceRequests.Include(r => r.Recipients).AsNoTracking().Single(r => r.Id == id);

    [Fact]
    public async Task Staff_see_every_request_newest_first_with_its_customer_and_counts()
    {
        var aram = _data.GivenPartner("Aram");
        var gor = _data.GivenPartner("Gor");
        var older = _data.GivenRequest(partners: [aram, gor]);
        older.MarkResponded(aram.Id, RequestTestData.Now);
        older.Decline(gor.Id, null, RequestTestData.Now);
        await _data.Db.SaveChangesAsync();
        var newer = _data.GivenRequest(customerId: _data.Partners.OtherUser.Id);

        var page = await ListAsync();

        page.Items.Select(i => i.Id).ShouldBe(new[] { newer.Id, older.Id });
        var item = page.Items[1];
        item.CustomerId.ShouldBe(_data.Partners.User.Id);
        item.CustomerPhone.ShouldBe("+37477123456");
        item.SentTo.ShouldBe(2);
        item.Responded.ShouldBe(1);
        item.Declined.ShouldBe(1);
        item.AttentionReason.ShouldBeNull();
        item.Place.CityName.ShouldBe("Երևան");
    }

    [Fact]
    public async Task The_operator_queue_lists_requests_waiting_longest_first()
    {
        var waitingLess = _data.GivenRequest();
        waitingLess.FlagForOperator(AttentionReason.NoResponse, RequestTestData.Now);
        var waitingLonger = _data.GivenRequest();
        waitingLonger.FlagForOperator(AttentionReason.NoMatchingPartners, RequestTestData.Now.AddHours(-3));
        await _data.Db.SaveChangesAsync();
        var fine = _data.GivenRequest();

        var queue = await ListAsync(new GetAdminRequests(NeedsAttention: true));

        queue.Items.Select(i => i.Id).ShouldBe(new[] { waitingLonger.Id, waitingLess.Id });
        queue.Items[0].AttentionReason.ShouldBe("NoMatchingPartners");
        queue.Items[0].NeedsAttentionSince.ShouldBe(RequestTestData.Now.AddHours(-3));
        (await ListAsync(new GetAdminRequests(NeedsAttention: false))).Items.ShouldHaveSingleItem().Id.ShouldBe(fine.Id);
    }

    [Fact]
    public async Task Requests_are_filtered_and_searched()
    {
        var p = _data.Partners;
        var direct = _data.GivenRequest(RequestKind.Direct, description: "Replace the boiler in the basement.");
        var cancelled = _data.GivenRequest(customerId: p.OtherUser.Id);
        cancelled.Cancel(null, RequestTestData.Now);
        await _data.Db.SaveChangesAsync();

        (await ListAsync(new GetAdminRequests(Kind: RequestKind.Direct))).Items.ShouldHaveSingleItem().Id.ShouldBe(direct.Id);
        (await ListAsync(new GetAdminRequests(RequestStatus.Cancelled))).Items.ShouldHaveSingleItem().Id.ShouldBe(cancelled.Id);
        (await ListAsync(new GetAdminRequests(Search: " BOILER "))).Items.ShouldHaveSingleItem().Id.ShouldBe(direct.Id);
        (await ListAsync(new GetAdminRequests(Search: "077 654 321"))).Items.ShouldHaveSingleItem().Id.ShouldBe(cancelled.Id);
        (await ListAsync(new GetAdminRequests(CategoryId: p.Plumbing.Id))).TotalCount.ShouldBe(2);
        (await ListAsync(new GetAdminRequests(CategoryId: p.Heating.Id))).Items.ShouldBeEmpty();
        (await ListAsync(new GetAdminRequests(CityId: p.Yerevan.Id))).TotalCount.ShouldBe(2);
        (await ListAsync(new GetAdminRequests(CityId: p.Masis.Id))).Items.ShouldBeEmpty();
        var page = await ListAsync(new GetAdminRequests(Page: 2, PageSize: 1));
        page.TotalCount.ShouldBe(2);
        page.Items.ShouldHaveSingleItem().Id.ShouldBe(direct.Id);
    }

    [Fact]
    public async Task A_request_shows_its_customer_and_what_each_partner_did()
    {
        var aram = _data.GivenPartner("Aram");
        var gor = _data.GivenPartner("Gor");
        var request = _data.GivenRequest(partners: [aram, gor]);
        request.Decline(gor.Id, "Busy this week", RequestTestData.Now.AddHours(1));
        request.MarkViewed(aram.Id, RequestTestData.Now.AddMinutes(30));
        await _data.Db.SaveChangesAsync();

        var dto = await GetAsync(request.Id);

        dto.Customer.UserId.ShouldBe(_data.Partners.User.Id);
        dto.Customer.Phone.ShouldBe("+37477123456");
        dto.Customer.IsBlocked.ShouldBeFalse();
        dto.Recipients.Select(r => r.DisplayName).ShouldBe(new[] { "Aram", "Gor" }, ignoreOrder: true);
        var declined = dto.Recipients.Single(r => r.PartnerId == gor.Id);
        declined.Status.ShouldBe("Declined");
        declined.DeclineReason.ShouldBe("Busy this week");
        declined.PartnerStatus.ShouldBe("Approved");
        declined.Source.ShouldBe("Matched");
        dto.Recipients.Single(r => r.PartnerId == aram.Id).ViewedAt.ShouldBe(RequestTestData.Now.AddMinutes(30));
        dto.Description.ShouldBe("Need a plumber for the bathroom.");
    }

    [Fact]
    public async Task Unknown_requests_are_not_found()
    {
        (await Should.ThrowAsync<NotFoundException>(() => GetAsync(Guid.NewGuid()))).Code.ShouldBe("request.not_found");
    }

    [Fact]
    public async Task Staff_send_a_waiting_request_to_partners_they_choose()
    {
        var aram = _data.GivenPartner("Aram");
        var gor = _data.GivenPartner("Gor");
        var request = _data.GivenRequest();
        request.FlagForOperator(AttentionReason.NoMatchingPartners, RequestTestData.Now);
        await _data.Db.SaveChangesAsync();

        var dto = await AssignAsync(request.Id, aram.Id, gor.Id, aram.Id);

        dto.Recipients.Count.ShouldBe(2);
        dto.Recipients.ShouldAllBe(r => r.Source == "Manual");
        dto.AttentionReason.ShouldBeNull();
        dto.NeedsAttentionSince.ShouldBeNull();
        Stored(request.Id).Recipients.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Staff_can_only_choose_partners_who_take_requests_and_are_not_the_customer()
    {
        var suspended = _data.GivenPartner("Suspended", status: PartnerStatus.Suspended);
        var customersOwn = _data.GivenPartner("Own");
        var request = _data.GivenRequest(customerId: customersOwn.UserId);
        var aram = _data.GivenPartner("Aram");

        (await Should.ThrowAsync<DomainException>(() => AssignAsync(request.Id, aram.Id, suspended.Id))).Code.ShouldBe("request.partner_unavailable");
        (await Should.ThrowAsync<DomainException>(() => AssignAsync(request.Id, customersOwn.Id))).Code.ShouldBe("request.partner_unavailable");
        Stored(request.Id).Recipients.ShouldBeEmpty();
    }

    [Fact]
    public async Task Staff_cancel_a_request_with_a_reason()
    {
        var request = _data.GivenRequest();

        var dto = await CancelAsync(request.Id, "Duplicate");

        dto.Status.ShouldBe("Cancelled");
        dto.CancelReason.ShouldBe("Duplicate");
        dto.CancelledAt.ShouldBe(RequestTestData.Now);
    }

    [Fact]
    public async Task The_follow_up_moves_unanswered_requests_to_the_operator_queue()
    {
        _data.Settings.ResponseHours = 24;
        var aram = _data.GivenPartner("Aram");
        var unanswered = _data.GivenRequest(sentAt: RequestTestData.Now.AddHours(-25), partners: [aram]);
        var recent = _data.GivenRequest(sentAt: RequestTestData.Now.AddHours(-2), partners: [aram]);
        var answered = _data.GivenRequest(sentAt: RequestTestData.Now.AddHours(-30), partners: [aram]);
        answered.MarkResponded(aram.Id, RequestTestData.Now.AddHours(-29));
        _data.GivenRequest();
        await _data.Db.SaveChangesAsync();

        (await FollowUpAsync()).ShouldBe(1);
        (await FollowUpAsync()).ShouldBe(0);

        Stored(unanswered.Id).AttentionReason.ShouldBe(AttentionReason.NoResponse);
        Stored(unanswered.Id).NeedsAttentionSince.ShouldBe(RequestTestData.Now);
        Stored(recent.Id).NeedsAttention.ShouldBeFalse();
        Stored(answered.Id).NeedsAttention.ShouldBeFalse();
    }
}
