using HomeServices.Application.Abstractions;
using HomeServices.Application.Chat;
using HomeServices.Application.Errors;
using HomeServices.Application.Tests.Offers;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Tests.Chat;

public sealed class FakeChatNotifier : IChatNotifier
{
    public List<(IReadOnlyCollection<Guid> Users, MessageDto Message)> Messages { get; } = [];

    public List<(IReadOnlyCollection<Guid> Users, Guid ConversationId, string Role, DateTimeOffset ReadAt)> Reads { get; } = [];

    public Task MessageSentAsync(IReadOnlyCollection<Guid> userIds, MessageDto message, CancellationToken cancellationToken)
    {
        Messages.Add((userIds, message));
        return Task.CompletedTask;
    }

    public Task ConversationReadAsync(IReadOnlyCollection<Guid> userIds, Guid conversationId, string readerRole, DateTimeOffset readAt, CancellationToken cancellationToken)
    {
        Reads.Add((userIds, conversationId, readerRole, readAt));
        return Task.CompletedTask;
    }
}

public class ChatTests
{
    private readonly OfferTestData _data = new();
    private readonly FakeChatNotifier _notifier = new();

    private static DateTimeOffset Now => OfferTestData.Now;

    private Task<ConversationDto> OpenAsync(ICurrentUser user, Guid requestId, Guid? partnerId = null) =>
        new OpenConversationHandler(_data.Requests.Db, user, _data.Requests.Language).HandleAsync(new OpenConversation(requestId, partnerId), CancellationToken.None);

    private Task<MessageDto> SendAsync(ICurrentUser user, Guid conversationId, string? body, params Guid[] files) =>
        new SendMessageHandler(_data.Requests.Db, user, _data.Requests.Files, _notifier, _data.Requests.Clock)
            .HandleAsync(new SendMessage(conversationId, body, files), CancellationToken.None);

    private Task<MessagePageDto> MessagesAsync(ICurrentUser user, Guid conversationId, DateTimeOffset? before = null, int limit = 50) =>
        new GetMessagesHandler(_data.Requests.Db, user, _data.Requests.Files).HandleAsync(new GetMessages(conversationId, before, limit), CancellationToken.None);

    private Task<ConversationDto> GetAsync(ICurrentUser user, Guid id) =>
        new GetConversationHandler(_data.Requests.Db, user, _data.Requests.Language).HandleAsync(new GetConversation(id), CancellationToken.None);

    private Task<UnreadSummaryDto> UnreadAsync(ICurrentUser user) =>
        new GetUnreadSummaryHandler(_data.Requests.Db, user).HandleAsync(new GetUnreadSummary(), CancellationToken.None);

    private Task<DateTimeOffset> ReadAsync(ICurrentUser user, Guid id) =>
        new MarkConversationReadHandler(_data.Requests.Db, user, _notifier, _data.Requests.Clock).HandleAsync(new MarkConversationRead(id), CancellationToken.None);

    private (PartnerProfile Aram, ServiceRequest Request) GivenRequest()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        return (aram, _data.Requests.GivenRequest(partners: [aram]));
    }

    [Fact]
    public async Task A_partner_asks_a_question_which_counts_as_responding()
    {
        var (aram, request) = GivenRequest();
        var partner = OfferTestData.As(aram);

        var conversation = await OpenAsync(partner, request.Id);
        var message = await SendAsync(partner, conversation.Id, "Is it the kitchen or the bathroom?");

        conversation.MyRole.ShouldBe("Partner");
        conversation.OtherParty.Name.ShouldBeNull();
        conversation.CanSend.ShouldBeTrue();
        message.SenderRole.ShouldBe("Partner");
        message.Body.ShouldBe("Is it the kitchen or the bathroom?");
        _data.StoredRequest(request.Id).FindRecipient(aram.Id)!.Status.ShouldBe(RecipientStatus.Responded);
        var pushed = _notifier.Messages.ShouldHaveSingleItem();
        pushed.Users.ShouldBe(new[] { _data.Requests.Partners.User.Id, aram.UserId }, ignoreOrder: true);
        pushed.Message.Id.ShouldBe(message.Id);
        (await OpenAsync(partner, request.Id)).Id.ShouldBe(conversation.Id);
    }

    [Fact]
    public async Task The_customer_opens_the_same_conversation_and_sees_unread_messages_until_reading()
    {
        var (aram, request) = GivenRequest();
        var customer = _data.Customer;
        var mine = await OpenAsync(OfferTestData.As(aram), request.Id);
        await SendAsync(OfferTestData.As(aram), mine.Id, "Hello");
        _data.Requests.Clock.Now = Now.AddMinutes(1);
        await SendAsync(OfferTestData.As(aram), mine.Id, "Are you home tomorrow?");

        var theirs = await OpenAsync(customer, request.Id, aram.Id);
        theirs.Id.ShouldBe(mine.Id);
        theirs.MyRole.ShouldBe("Customer");
        theirs.OtherParty.Name.ShouldBe("Aram");
        theirs.OtherParty.PartnerId.ShouldBe(aram.Id);
        theirs.UnreadCount.ShouldBe(2);
        theirs.LastMessage!.Excerpt.ShouldBe("Are you home tomorrow?");
        (await UnreadAsync(customer)).ShouldBe(new UnreadSummaryDto(1, 2));

        _data.Requests.Clock.Now = Now.AddMinutes(2);
        var readAt = await ReadAsync(customer, theirs.Id);

        readAt.ShouldBe(Now.AddMinutes(2));
        (await GetAsync(customer, theirs.Id)).UnreadCount.ShouldBe(0);
        (await GetAsync(OfferTestData.As(aram), theirs.Id)).OtherReadAt.ShouldBe(Now.AddMinutes(2));
        (await UnreadAsync(customer)).ShouldBe(new UnreadSummaryDto(0, 0));
        _notifier.Reads.ShouldHaveSingleItem().Role.ShouldBe("Customer");
    }

    [Fact]
    public async Task Messages_page_backwards_from_the_newest()
    {
        var (aram, request) = GivenRequest();
        var partner = OfferTestData.As(aram);
        var conversation = await OpenAsync(partner, request.Id);
        for (var i = 1; i <= 5; i++)
        {
            _data.Requests.Clock.Now = Now.AddMinutes(i);
            await SendAsync(partner, conversation.Id, $"Message {i}");
        }

        var latest = await MessagesAsync(_data.Customer, conversation.Id, limit: 2);
        latest.Items.Select(m => m.Body).ShouldBe(new[] { "Message 4", "Message 5" });
        latest.HasMore.ShouldBeTrue();

        var older = await MessagesAsync(_data.Customer, conversation.Id, latest.Items[0].SentAt, 10);
        older.Items.Select(m => m.Body).ShouldBe(new[] { "Message 1", "Message 2", "Message 3" });
        older.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task Files_are_sent_with_their_links_and_must_be_the_senders_ready_uploads()
    {
        var (aram, request) = GivenRequest();
        var customer = _data.Customer;
        var conversation = await OpenAsync(customer, request.Id, aram.Id);
        var photo = _data.Requests.Partners.GivenFile();
        var pending = _data.Requests.Partners.GivenFile(ready: false);
        var someoneElses = _data.Requests.Partners.GivenFile(owner: _data.Requests.Partners.OtherUser);

        var message = await SendAsync(customer, conversation.Id, null, photo.Id);

        message.Body.ShouldBeNull();
        message.Attachments.ShouldHaveSingleItem().Id.ShouldBe(photo.Id);
        (await MessagesAsync(customer, conversation.Id)).Items.ShouldHaveSingleItem().Attachments.ShouldHaveSingleItem();
        (await GetAsync(OfferTestData.As(aram), conversation.Id)).LastMessage!.AttachmentCount.ShouldBe(1);
        (await Should.ThrowAsync<DomainException>(() => SendAsync(customer, conversation.Id, "x", pending.Id))).Code.ShouldBe("message.files_invalid");
        (await Should.ThrowAsync<DomainException>(() => SendAsync(customer, conversation.Id, "x", someoneElses.Id))).Code.ShouldBe("message.files_invalid");
    }

    [Fact]
    public async Task Strangers_see_nothing()
    {
        var (aram, request) = GivenRequest();
        var gor = _data.Requests.GivenPartner("Gor");
        var stranger = new FakeCurrentUser(_data.Requests.Partners.OtherUser.Id);
        var conversation = await OpenAsync(OfferTestData.As(aram), request.Id);

        (await Should.ThrowAsync<NotFoundException>(() => GetAsync(stranger, conversation.Id))).Code.ShouldBe("conversation.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => GetAsync(OfferTestData.As(gor), conversation.Id))).Code.ShouldBe("conversation.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => MessagesAsync(stranger, conversation.Id))).Code.ShouldBe("conversation.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => GetAsync(stranger, Guid.NewGuid()))).Code.ShouldBe("conversation.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => OpenAsync(OfferTestData.As(gor), request.Id))).Code.ShouldBe("request.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => OpenAsync(stranger, request.Id, aram.Id))).Code.ShouldBe("request.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => OpenAsync(stranger, Guid.NewGuid()))).Code.ShouldBe("request.not_found");
    }

    [Fact]
    public async Task No_new_conversations_with_partners_who_declined_or_on_ended_requests()
    {
        var (aram, request) = GivenRequest();
        var gor = _data.Requests.GivenPartner("Gor");
        request.SendTo([gor.Id], RecipientSource.Manual, Now);
        request.Decline(gor.Id, null, Now);
        await _data.Requests.Db.SaveChangesAsync();

        (await Should.ThrowAsync<DomainException>(() => OpenAsync(_data.Customer, request.Id, gor.Id))).Code.ShouldBe("conversation.unavailable");
        (await Should.ThrowAsync<DomainException>(() => OpenAsync(_data.Customer, request.Id, Guid.NewGuid()))).Code.ShouldBe("conversation.unavailable");

        request.Cancel(null, Now);
        await _data.Requests.Db.SaveChangesAsync();
        (await Should.ThrowAsync<DomainException>(() => OpenAsync(OfferTestData.As(aram), request.Id))).Code.ShouldBe("conversation.unavailable");
    }

    [Fact]
    public async Task A_conversation_closes_with_the_request_unless_the_two_agreed_on_an_order()
    {
        var (aram, request) = GivenRequest();
        var gor = _data.Requests.GivenPartner("Gor");
        request.SendTo([gor.Id], RecipientSource.Manual, Now);
        await _data.Requests.Db.SaveChangesAsync();
        var withAram = await OpenAsync(_data.Customer, request.Id, aram.Id);
        var withGor = await OpenAsync(_data.Customer, request.Id, gor.Id);

        var offer = await _data.SendAsync(aram, _data.Work(request.Id));
        var order = await _data.AcceptAsync(offer.Id);

        var aramView = await GetAsync(OfferTestData.As(aram), withAram.Id);
        aramView.CanSend.ShouldBeTrue();
        aramView.OrderId.ShouldBe(order.Id);
        aramView.OtherParty.Name.ShouldBeNull();
        await SendAsync(OfferTestData.As(aram), withAram.Id, "See you tomorrow");

        (await GetAsync(_data.Customer, withGor.Id)).CanSend.ShouldBeFalse();
        (await Should.ThrowAsync<DomainException>(() => SendAsync(_data.Customer, withGor.Id, "Hi"))).Code.ShouldBe("conversation.closed");
    }

    [Fact]
    public async Task The_two_sides_of_an_order_can_start_talking_after_the_request_closed()
    {
        var (aram, request) = GivenRequest();
        var order = await _data.AcceptAsync((await _data.SendAsync(aram, _data.Work(request.Id))).Id);

        var conversation = await OpenAsync(OfferTestData.As(aram), request.Id);

        conversation.CanSend.ShouldBeTrue();
        conversation.OrderId.ShouldBe(order.Id);
        (await OpenAsync(_data.Customer, request.Id, aram.Id)).Id.ShouldBe(conversation.Id);
    }

    [Fact]
    public async Task After_an_order_the_partner_sees_the_customers_full_name()
    {
        var (aram, request) = GivenRequest();
        var customer = await _data.Requests.Db.Users.SingleAsync(u => u.Id == _data.Requests.Partners.User.Id);
        customer.UpdateProfile("Ani Petrosyan", null);
        await _data.Requests.Db.SaveChangesAsync();
        var conversation = await OpenAsync(OfferTestData.As(aram), request.Id);
        (await GetAsync(OfferTestData.As(aram), conversation.Id)).OtherParty.Name.ShouldBe("Ani");

        await _data.AcceptAsync((await _data.SendAsync(aram, _data.Work(request.Id))).Id);

        (await GetAsync(OfferTestData.As(aram), conversation.Id)).OtherParty.Name.ShouldBe("Ani Petrosyan");
    }

    [Fact]
    public async Task The_list_shows_both_sides_most_recent_first()
    {
        var (aram, request) = GivenRequest();
        var own = _data.Requests.GivenRequest(customerId: aram.UserId, partners: [_data.Requests.GivenPartner("Gor")]);
        var gor = _data.Requests.Db.PartnerProfiles.Single(p => p.DisplayName == "Gor");
        var asPartner = await OpenAsync(OfferTestData.As(aram), request.Id);
        _data.Requests.Clock.Now = Now.AddMinutes(5);
        var asCustomer = await OpenAsync(OfferTestData.As(aram), own.Id, gor.Id);
        await SendAsync(OfferTestData.As(gor), asCustomer.Id, new string('x', 150));

        var list = await new GetConversationsHandler(_data.Requests.Db, OfferTestData.As(aram), _data.Requests.Language)
            .HandleAsync(new GetConversations(), CancellationToken.None);

        list.Items.Select(c => (c.Id, c.MyRole)).ShouldBe(new[] { (asCustomer.Id, "Customer"), (asPartner.Id, "Partner") });
        list.Items[0].LastMessage!.Excerpt!.Length.ShouldBe(100);
        list.Items[0].UnreadCount.ShouldBe(1);
        list.Items[1].LastMessage.ShouldBeNull();
        (await UnreadAsync(OfferTestData.As(aram))).ShouldBe(new UnreadSummaryDto(1, 1));
    }

    [Fact]
    public async Task Validators_check_messages_and_paging()
    {
        (await new SendMessageValidator().ValidateAsync(new SendMessage(Guid.NewGuid(), " ", []))).Errors.Single().ErrorCode.ShouldBe("message.empty");
        (await new SendMessageValidator().ValidateAsync(new SendMessage(Guid.NewGuid(), new string('a', 4001), null))).Errors.Single().ErrorCode.ShouldBe("body.too_long");
        (await new SendMessageValidator().ValidateAsync(new SendMessage(Guid.NewGuid(), null, Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToList())))
            .Errors.Single().ErrorCode.ShouldBe("file_ids.too_many");
        (await new SendMessageValidator().ValidateAsync(new SendMessage(Guid.NewGuid(), null, [Guid.NewGuid()]))).IsValid.ShouldBeTrue();
        (await new GetMessagesValidator().ValidateAsync(new GetMessages(Guid.NewGuid(), null, 0))).Errors.Single().ErrorCode.ShouldBe("limit.invalid");
        (await new GetConversationsValidator().ValidateAsync(new GetConversations(0, 101))).Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "page.invalid", "page_size.invalid" }, ignoreOrder: true);
    }
}
