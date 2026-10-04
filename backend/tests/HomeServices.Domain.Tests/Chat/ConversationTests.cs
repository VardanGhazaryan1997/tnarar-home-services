using HomeServices.Domain.Chat;

namespace HomeServices.Domain.Tests.Chat;

public class ConversationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Customer = Guid.NewGuid();
    private static readonly Guid PartnerUser = Guid.NewGuid();

    private static Conversation NewConversation() => Conversation.Start(Guid.NewGuid(), Customer, Guid.NewGuid());

    [Fact]
    public void A_conversation_needs_its_parties()
    {
        Should.Throw<DomainException>(() => Conversation.Start(Guid.Empty, Customer, Guid.NewGuid())).Code.ShouldBe("conversation.parties_required");
        Should.Throw<DomainException>(() => Conversation.Start(Guid.NewGuid(), Guid.Empty, Guid.NewGuid())).Code.ShouldBe("conversation.parties_required");
        Should.Throw<DomainException>(() => Conversation.Start(Guid.NewGuid(), Customer, Guid.Empty)).Code.ShouldBe("conversation.parties_required");
    }

    [Fact]
    public void Posting_keeps_clean_text_files_and_marks_the_sender_as_read()
    {
        var conversation = NewConversation();
        var file = Guid.NewGuid();

        var message = conversation.Post(PartnerUser, ChatRole.Partner, "  When can I come?  ", [file, file], Now);

        message.ConversationId.ShouldBe(conversation.Id);
        message.SenderUserId.ShouldBe(PartnerUser);
        message.SenderRole.ShouldBe(ChatRole.Partner);
        message.Body.ShouldBe("When can I come?");
        message.SentAt.ShouldBe(Now);
        var attachment = message.Attachments.ShouldHaveSingleItem();
        attachment.FileId.ShouldBe(file);
        attachment.SortOrder.ShouldBe(1);
        attachment.MessageId.ShouldBe(message.Id);
        conversation.LastMessageAt.ShouldBe(Now);
        conversation.PartnerReadAt.ShouldBe(Now);
        conversation.CustomerReadAt.ShouldBeNull();
    }

    [Fact]
    public void A_message_has_text_or_files_within_limits()
    {
        var conversation = NewConversation();

        Should.Throw<DomainException>(() => conversation.Post(Customer, ChatRole.Customer, "  ", [], Now)).Code.ShouldBe("message.empty");
        Should.Throw<DomainException>(() => conversation.Post(Customer, ChatRole.Customer, new string('a', Message.BodyMaxLength + 1), [], Now)).Code.ShouldBe("message.too_long");
        Should.Throw<DomainException>(() => conversation.Post(Customer, ChatRole.Customer, null, Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToList(), Now))
            .Code.ShouldBe("message.too_many_files");
        Should.Throw<DomainException>(() => conversation.Post(Guid.Empty, ChatRole.Customer, "Hi", [], Now)).Code.ShouldBe("message.sender_required");
        Should.Throw<DomainException>(() => conversation.Post(Customer, (ChatRole)9, "Hi", [], Now)).Code.ShouldBe("conversation.role_invalid");
        conversation.Post(Customer, ChatRole.Customer, null, [Guid.NewGuid()], Now).Body.ShouldBeNull();
    }

    [Fact]
    public void Reading_moves_forward_only()
    {
        var conversation = NewConversation();

        conversation.MarkRead(ChatRole.Customer, Now.AddHours(1));
        conversation.MarkRead(ChatRole.Customer, Now);
        conversation.MarkRead(ChatRole.Partner, Now);
        conversation.MarkRead(ChatRole.Partner, Now.AddMinutes(5));

        conversation.ReadAtOf(ChatRole.Customer).ShouldBe(Now.AddHours(1));
        conversation.ReadAtOf(ChatRole.Partner).ShouldBe(Now.AddMinutes(5));
    }
}
