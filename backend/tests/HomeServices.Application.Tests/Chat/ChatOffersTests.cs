using HomeServices.Application.Abstractions;
using HomeServices.Application.Chat;
using HomeServices.Application.Tests.Offers;

namespace HomeServices.Application.Tests.Chat;

/// <summary>T3: a conversation carries the partner's offers, so the chat can show them and act on them.</summary>
public class ChatOffersTests
{
    private readonly OfferTestData _data = new();

    private Task<ConversationDto> OpenAsync(ICurrentUser user, Guid requestId, Guid? partnerId = null) =>
        new OpenConversationHandler(_data.Requests.Db, user, _data.Requests.Language, _data.Requests.Clock)
            .HandleAsync(new OpenConversation(requestId, partnerId), CancellationToken.None);

    private Task<ConversationDto> GetAsync(ICurrentUser user, Guid id) =>
        new GetConversationHandler(_data.Requests.Db, user, _data.Requests.Language, _data.Requests.Clock).HandleAsync(new GetConversation(id), CancellationToken.None);

    [Fact]
    public async Task A_conversation_lists_that_partners_offers_oldest_first_with_their_status()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var gor = _data.Requests.GivenPartner("Gor");
        var request = _data.Requests.GivenRequest(partners: [aram, gor]);
        var conversation = await OpenAsync(_data.Customer, request.Id, aram.Id);
        var visit = await _data.SendAsync(aram, _data.Visit(request.Id));
        _data.Requests.Clock.Now = OfferTestData.Now.AddMinutes(5);
        var work = await _data.SendAsync(aram, _data.Work(request.Id, 120_000));
        await _data.SendAsync(gor, _data.Work(request.Id, 90_000));

        var offers = (await GetAsync(_data.Customer, conversation.Id)).Offers!;

        offers.Select(o => o.Id).ShouldBe(new[] { visit.Id, work.Id });
        var shown = offers[1];
        // SentAt is the audit time the database sets when saving (not in these in-memory tests).
        (shown with { SentAt = default }).ShouldBe(new ChatOfferDto(
            work.Id, "Work", "Sent", 120_000, "Replace the kitchen tap and the pipes under the sink.", null, work.ExpiresAt, default, null, null));
    }

    [Fact]
    public async Task Decisions_show_with_their_time_and_order_and_an_offer_past_its_expiry_shows_as_expired()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var other = _data.Requests.GivenRequest(partners: [aram]);
        var conversation = await OpenAsync(_data.Customer, request.Id, aram.Id);
        var waiting = await OpenAsync(_data.Customer, other.Id, aram.Id);
        var offer = await _data.SendAsync(aram, _data.Work(request.Id));
        var late = await _data.SendAsync(aram, _data.Work(other.Id));
        _data.Requests.Clock.Now = OfferTestData.Now.AddHours(1);
        var order = await _data.AcceptAsync(offer.Id);

        var accepted = (await GetAsync(_data.Customer, conversation.Id)).Offers!.ShouldHaveSingleItem();
        accepted.Status.ShouldBe("Accepted");
        accepted.DecidedAt.ShouldBe(OfferTestData.Now.AddHours(1));
        accepted.OrderId.ShouldBe(order.Id);

        _data.Requests.Clock.Now = late.ExpiresAt;
        (await GetAsync(_data.Customer, waiting.Id)).Offers!.ShouldHaveSingleItem().Status.ShouldBe("Expired");
    }

    [Fact]
    public async Task The_customer_never_sees_a_withdrawn_offer_but_its_partner_does()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var conversation = await OpenAsync(_data.Customer, request.Id, aram.Id);
        var offer = await _data.SendAsync(aram, _data.Work(request.Id));
        await _data.WithdrawAsync(aram, offer.Id);

        (await GetAsync(_data.Customer, conversation.Id)).Offers.ShouldBeEmpty();
        (await GetAsync(OfferTestData.As(aram), conversation.Id)).Offers!.ShouldHaveSingleItem().Status.ShouldBe("Withdrawn");
    }
}
