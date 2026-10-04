using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Requests;
using HomeServices.Domain;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Tests.Requests;

public class PartnerInboxTests
{
    private readonly RequestTestData _data = new();

    private Task<PagedResult<InboxItemDto>> InboxAsync(ICurrentUser user, GetInbox? query = null) =>
        new GetInboxHandler(_data.Db, user, _data.Language).HandleAsync(query ?? new GetInbox(), CancellationToken.None);

    private Task<InboxRequestDto> OpenAsync(ICurrentUser user, Guid id) =>
        new GetInboxRequestHandler(_data.Db, user, _data.Language, _data.Files, _data.Clock).HandleAsync(new GetInboxRequest(id), CancellationToken.None);

    private Task<InboxRequestDto> DeclineAsync(ICurrentUser user, Guid id, string? reason = null) =>
        new DeclineInboxRequestHandler(_data.Db, user, _data.Language, _data.Files, _data.Clock).HandleAsync(new DeclineInboxRequest(id, reason), CancellationToken.None);

    private RequestRecipient Recipient(Guid requestId, PartnerProfile partner) =>
        _data.Db.ServiceRequests.Include(r => r.Recipients).AsNoTracking().Single(r => r.Id == requestId).FindRecipient(partner.Id)!;

    [Fact]
    public async Task A_partner_sees_the_requests_they_received_newest_first()
    {
        var aram = _data.GivenPartner("Aram");
        var gor = _data.GivenPartner("Gor");
        var older = _data.GivenRequest(sentAt: RequestTestData.Now.AddHours(-2), partners: [aram, gor]);
        var newer = _data.GivenRequest(RequestKind.Direct, sentAt: RequestTestData.Now, partners: [aram]);
        newer.AddMedia(Guid.NewGuid());
        await _data.Db.SaveChangesAsync();
        _data.GivenRequest(partners: [gor]);

        var page = await InboxAsync(RequestTestData.As(aram));

        page.TotalCount.ShouldBe(2);
        page.Items.Select(i => i.Id).ShouldBe(new[] { newer.Id, older.Id });
        var item = page.Items[0];
        item.Kind.ShouldBe("Direct");
        item.RequestStatus.ShouldBe("Open");
        item.MyStatus.ShouldBe("New");
        item.MediaCount.ShouldBe(1);
        item.SentAt.ShouldBe(RequestTestData.Now);
        item.Place.CategoryName.ShouldBe("Սանտեխնիկա");
        item.Excerpt.ShouldBe("Need a plumber for the bathroom.");
    }

    [Fact]
    public async Task The_inbox_is_filtered_by_what_the_partner_did_and_paged()
    {
        var aram = _data.GivenPartner("Aram");
        var declined = _data.GivenRequest(partners: [aram]);
        declined.Decline(aram.Id, null, RequestTestData.Now);
        await _data.Db.SaveChangesAsync();
        _data.GivenRequest(partners: [aram]);
        _data.GivenRequest(partners: [aram]);

        (await InboxAsync(RequestTestData.As(aram), new GetInbox(RecipientStatus.Declined))).Items.ShouldHaveSingleItem().Id.ShouldBe(declined.Id);
        var page = await InboxAsync(RequestTestData.As(aram), new GetInbox(Page: 2, PageSize: 2));
        page.TotalCount.ShouldBe(3);
        page.Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Users_without_a_partner_profile_have_no_inbox()
    {
        (await Should.ThrowAsync<NotFoundException>(() => InboxAsync(_data.Customer))).Code.ShouldBe("partner.not_found");
    }

    [Fact]
    public async Task Opening_a_request_shows_its_details_and_marks_it_viewed_once()
    {
        var aram = _data.GivenPartner("Aram");
        var customer = _data.Db.Users.Single(u => u.Id == _data.Partners.User.Id);
        customer.UpdateProfile("  Ani   Petrosyan ", null);
        var photo = _data.Partners.GivenFile();
        var request = _data.GivenRequest(partners: [aram]);
        request.AddMedia(photo.Id);
        await _data.Db.SaveChangesAsync();

        var dto = await OpenAsync(RequestTestData.As(aram), request.Id);
        _data.Clock.Now = RequestTestData.Now.AddHours(1);
        await OpenAsync(RequestTestData.As(aram), request.Id);

        dto.MyStatus.ShouldBe("Viewed");
        dto.CustomerFirstName.ShouldBe("Ani");
        dto.Description.ShouldBe("Need a plumber for the bathroom.");
        dto.Media.ShouldHaveSingleItem().Id.ShouldBe(photo.Id);
        dto.SentAt.ShouldBe(RequestTestData.Now);
        Recipient(request.Id, aram).ViewedAt.ShouldBe(RequestTestData.Now);
    }

    [Fact]
    public async Task Customers_without_a_name_are_shown_without_one()
    {
        var aram = _data.GivenPartner("Aram");
        var request = _data.GivenRequest(partners: [aram]);

        (await OpenAsync(RequestTestData.As(aram), request.Id)).CustomerFirstName.ShouldBeNull();
    }

    [Fact]
    public async Task Requests_sent_to_other_partners_are_not_found()
    {
        var aram = _data.GivenPartner("Aram");
        var gor = _data.GivenPartner("Gor");
        var request = _data.GivenRequest(partners: [gor]);

        (await Should.ThrowAsync<NotFoundException>(() => OpenAsync(RequestTestData.As(aram), request.Id))).Code.ShouldBe("request.not_found");
        await Should.ThrowAsync<NotFoundException>(() => DeclineAsync(RequestTestData.As(aram), request.Id));
    }

    [Fact]
    public async Task Declining_records_the_reason_and_hands_a_direct_request_to_an_operator()
    {
        var aram = _data.GivenPartner("Aram");
        var request = _data.GivenRequest(RequestKind.Direct, partners: [aram]);

        var dto = await DeclineAsync(RequestTestData.As(aram), request.Id, " Too far ");

        dto.MyStatus.ShouldBe("Declined");
        var recipient = Recipient(request.Id, aram);
        recipient.DeclineReason.ShouldBe("Too far");
        _data.Db.ServiceRequests.AsNoTracking().Single(r => r.Id == request.Id).AttentionReason.ShouldBe(AttentionReason.DirectPartnerDeclined);
        (await Should.ThrowAsync<DomainException>(() => DeclineAsync(RequestTestData.As(aram), request.Id))).Code.ShouldBe("request.already_declined");
    }

    [Fact]
    public async Task Opening_an_already_viewed_request_changes_nothing()
    {
        var aram = _data.GivenPartner("Aram");
        var request = _data.GivenRequest(partners: [aram]);
        request.Decline(aram.Id, null, RequestTestData.Now);
        await _data.Db.SaveChangesAsync();

        (await OpenAsync(RequestTestData.As(aram), request.Id)).MyStatus.ShouldBe("Declined");
    }
}
