using HomeServices.Application.Abstractions;
using HomeServices.Application.Chat;
using HomeServices.Application.Tests.Offers;
using HomeServices.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Tests.Chat;

/// <summary>O7: a send the app tries again (same client id) never posts the same message twice.</summary>
public class ChatRetryTests
{
    private readonly OfferTestData _data = new();
    private readonly FakeChatNotifier _notifier = new();

    private Task<MessageDto> SendAsync(ICurrentUser user, Guid conversationId, string body, Guid? clientId) =>
        new SendMessageHandler(_data.Requests.Db, user, _data.Requests.Files, _notifier, _data.Requests.Clock)
            .HandleAsync(new SendMessage(conversationId, body, null, clientId), CancellationToken.None);

    private async Task<(Guid ConversationId, ServiceRequest Request, ICurrentUser Partner)> GivenConversationAsync()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var conversation = await new OpenConversationHandler(_data.Requests.Db, _data.Customer, _data.Requests.Language, _data.Requests.Clock)
            .HandleAsync(new OpenConversation(request.Id, aram.Id), CancellationToken.None);
        return (conversation.Id, request, OfferTestData.As(aram));
    }

    [Fact]
    public async Task Sending_again_with_the_same_client_id_returns_the_message_already_sent()
    {
        var (conversationId, _, _) = await GivenConversationAsync();
        var clientId = Guid.NewGuid();

        var first = await SendAsync(_data.Customer, conversationId, "Can you come at 10?", clientId);
        var retry = await SendAsync(_data.Customer, conversationId, "Can you come at 10?", clientId);

        (retry with { Attachments = first.Attachments }).ShouldBe(first);
        first.ClientId.ShouldBe(clientId);
        (await _data.Requests.Db.Messages.CountAsync()).ShouldBe(1);
        _notifier.Messages.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Different_client_ids_or_none_are_different_messages()
    {
        var (conversationId, _, partner) = await GivenConversationAsync();
        var clientId = Guid.NewGuid();

        await SendAsync(_data.Customer, conversationId, "One", Guid.NewGuid());
        await SendAsync(_data.Customer, conversationId, "Two", null);
        await SendAsync(_data.Customer, conversationId, "Two", null);
        await SendAsync(_data.Customer, conversationId, "Three", clientId);
        // The other side happening to use the same id is not a retry of the customer's message.
        await SendAsync(partner, conversationId, "Four", clientId);

        (await _data.Requests.Db.Messages.CountAsync()).ShouldBe(5);
    }

    [Fact]
    public async Task A_retry_still_gets_its_message_after_the_conversation_closed()
    {
        var (conversationId, request, _) = await GivenConversationAsync();
        var clientId = Guid.NewGuid();
        var sent = await SendAsync(_data.Customer, conversationId, "Thanks, bye", clientId);
        request.Cancel(null, OfferTestData.Now);
        await _data.Requests.Db.SaveChangesAsync();
        _data.Requests.Clock.Now = OfferTestData.Now.AddDays(30);

        (await SendAsync(_data.Customer, conversationId, "Thanks, bye", clientId)).Id.ShouldBe(sent.Id);
    }
}
