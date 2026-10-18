using HomeServices.Application.Abstractions;
using HomeServices.Application.Chat;
using HomeServices.Application.Requests;
using HomeServices.Application.Tests.Offers;
using HomeServices.Domain;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Requests;

namespace HomeServices.Application.Tests.Chat;

/// <summary>D1: a cancelled request's conversations stay open for 7 days, and say when the request was cancelled.</summary>
public class ChatAfterCancellationTests
{
    private readonly OfferTestData _data = new();
    private readonly FakeChatNotifier _notifier = new();

    private static DateTimeOffset Now => OfferTestData.Now;

    private Task<ConversationDto> OpenAsync(ICurrentUser user, Guid requestId, Guid? partnerId = null) =>
        new OpenConversationHandler(_data.Requests.Db, user, _data.Requests.Language, _data.Requests.Clock)
            .HandleAsync(new OpenConversation(requestId, partnerId), CancellationToken.None);

    private Task<ConversationDto> GetAsync(ICurrentUser user, Guid id) =>
        new GetConversationHandler(_data.Requests.Db, user, _data.Requests.Language, _data.Requests.Clock).HandleAsync(new GetConversation(id), CancellationToken.None);

    private Task<MessageDto> SendAsync(ICurrentUser user, Guid conversationId, string body) =>
        new SendMessageHandler(_data.Requests.Db, user, _data.Requests.Files, _notifier, _data.Requests.Clock)
            .HandleAsync(new SendMessage(conversationId, body, null), CancellationToken.None);

    private Task CancelAsync(Guid requestId) =>
        new CancelMyRequestHandler(_data.Requests.Db, _data.Customer, _data.Requests.Language, _data.Requests.Files, _data.Requests.Live, _data.Requests.Clock)
            .HandleAsync(new CancelMyRequest(requestId, "Found someone"), CancellationToken.None);

    private async Task<(PartnerProfile Aram, ServiceRequest Request, ConversationDto Conversation)> GivenConversationAsync()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var conversation = await OpenAsync(_data.Customer, request.Id, aram.Id);
        return (aram, request, conversation);
    }

    [Fact]
    public async Task An_open_request_has_no_cancellation_and_no_deadline()
    {
        var (_, _, conversation) = await GivenConversationAsync();

        conversation.CanSend.ShouldBeTrue();
        conversation.RequestCancelledAt.ShouldBeNull();
        conversation.CanSendUntil.ShouldBeNull();
    }

    [Fact]
    public async Task Both_sides_keep_writing_for_seven_days_after_the_request_is_cancelled()
    {
        var (aram, request, conversation) = await GivenConversationAsync();
        _data.Requests.Clock.Now = Now.AddHours(2);
        await CancelAsync(request.Id);

        _data.Requests.Clock.Now = Now.AddHours(2).AddDays(7).AddSeconds(-1);
        var view = await GetAsync(OfferTestData.As(aram), conversation.Id);
        var late = await SendAsync(OfferTestData.As(aram), conversation.Id, "Sorry to hear that. Good luck!");
        await SendAsync(_data.Customer, conversation.Id, "Thanks!");

        view.RequestStatus.ShouldBe("Cancelled");
        view.CanSend.ShouldBeTrue();
        view.RequestCancelledAt.ShouldBe(Now.AddHours(2));
        view.CanSendUntil.ShouldBe(Now.AddHours(2).AddDays(7));
        late.SentAt.ShouldBeGreaterThan(view.RequestCancelledAt!.Value);
    }

    [Fact]
    public async Task The_conversation_closes_seven_days_after_the_cancellation()
    {
        var (aram, request, conversation) = await GivenConversationAsync();
        await CancelAsync(request.Id);

        _data.Requests.Clock.Now = Now.AddDays(7);

        (await GetAsync(_data.Customer, conversation.Id)).CanSend.ShouldBeFalse();
        (await Should.ThrowAsync<DomainException>(() => SendAsync(OfferTestData.As(aram), conversation.Id, "Hello?"))).Code.ShouldBe("conversation.closed");
    }

    [Fact]
    public async Task A_partner_who_declined_cannot_write_after_the_cancellation()
    {
        var (aram, request, conversation) = await GivenConversationAsync();
        request.Decline(aram.Id, null, Now);
        await _data.Requests.Db.SaveChangesAsync();
        await CancelAsync(request.Id);

        (await GetAsync(_data.Customer, conversation.Id)).CanSend.ShouldBeFalse();
        (await Should.ThrowAsync<DomainException>(() => SendAsync(OfferTestData.As(aram), conversation.Id, "Hi"))).Code.ShouldBe("conversation.closed");
    }

    [Fact]
    public async Task No_new_conversation_starts_on_a_cancelled_request()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        await CancelAsync(request.Id);

        (await Should.ThrowAsync<DomainException>(() => OpenAsync(_data.Customer, request.Id, aram.Id))).Code.ShouldBe("conversation.unavailable");
    }

    [Fact]
    public async Task With_an_order_between_them_there_is_no_deadline_after_a_cancellation()
    {
        var (aram, request, conversation) = await GivenConversationAsync();
        var visit = await _data.SendAsync(aram, _data.Visit(request.Id));
        await _data.AcceptAsync(visit.Id);
        await CancelAsync(request.Id);

        _data.Requests.Clock.Now = Now.AddDays(30);
        var view = await GetAsync(_data.Customer, conversation.Id);

        view.RequestCancelledAt.ShouldBe(Now);
        view.CanSendUntil.ShouldBeNull();
        view.CanSend.ShouldBeTrue();
    }
}
