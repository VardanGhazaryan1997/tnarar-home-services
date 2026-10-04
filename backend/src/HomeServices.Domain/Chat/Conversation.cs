using HomeServices.Domain.Common;

namespace HomeServices.Domain.Chat;

/// <summary>Which side of a conversation someone is on.</summary>
public enum ChatRole
{
    Customer = 1,
    Partner = 2,
}

/// <summary>
/// The chat between a customer and one partner about a request; it carries on once they agree on an order.
/// Messages are stored separately (<see cref="Message"/>) so a long chat is never loaded whole. Each side's
/// <c>ReadAt</c> is how far they have read: messages from the other side after it are unread.
/// </summary>
public sealed class Conversation : AuditableEntity
{
    private Conversation()
    {
    }

    public Guid RequestId { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid PartnerProfileId { get; private set; }

    public DateTimeOffset? LastMessageAt { get; private set; }

    public DateTimeOffset? CustomerReadAt { get; private set; }

    public DateTimeOffset? PartnerReadAt { get; private set; }

    public static Conversation Start(Guid requestId, Guid customerId, Guid partnerProfileId)
    {
        if (requestId == Guid.Empty || customerId == Guid.Empty || partnerProfileId == Guid.Empty)
        {
            throw new DomainException("conversation.parties_required", "A conversation needs a request, a customer and a partner.");
        }

        return new Conversation { RequestId = requestId, CustomerId = customerId, PartnerProfileId = partnerProfileId };
    }

    public DateTimeOffset? ReadAtOf(ChatRole role) => role == ChatRole.Customer ? CustomerReadAt : PartnerReadAt;

    /// <summary>A new message from <paramref name="role"/>. Writing it also means having read everything before it.</summary>
    public Message Post(Guid senderUserId, ChatRole role, string? body, IReadOnlyList<Guid> fileIds, DateTimeOffset now)
    {
        if (!Enum.IsDefined(role))
        {
            throw new DomainException("conversation.role_invalid", "Unknown conversation role.");
        }

        var message = new Message(Id, senderUserId, role, body, fileIds, now);
        LastMessageAt = now;
        MarkRead(role, now);
        return message;
    }

    /// <summary>Records that <paramref name="role"/> has read the conversation up to <paramref name="now"/>. Never moves back.</summary>
    public void MarkRead(ChatRole role, DateTimeOffset now)
    {
        if (role == ChatRole.Customer)
        {
            CustomerReadAt = CustomerReadAt is { } read && read > now ? read : now;
        }
        else
        {
            PartnerReadAt = PartnerReadAt is { } read && read > now ? read : now;
        }
    }
}

/// <summary>One message: text, files, or both.</summary>
public sealed class Message : Entity
{
    public const int BodyMaxLength = 4000;
    public const int MaxAttachments = 5;

    private readonly List<MessageAttachment> _attachments = [];

    private Message()
    {
    }

    internal Message(Guid conversationId, Guid senderUserId, ChatRole senderRole, string? body, IReadOnlyList<Guid> fileIds, DateTimeOffset sentAt)
    {
        if (senderUserId == Guid.Empty)
        {
            throw new DomainException("message.sender_required", "A message has a sender.");
        }

        var clean = string.IsNullOrWhiteSpace(body) ? null : body.Trim();
        var files = fileIds.Distinct().ToList();
        if (clean is null && files.Count == 0)
        {
            throw new DomainException("message.empty", "Write something or attach a file.");
        }

        if (clean?.Length > BodyMaxLength)
        {
            throw new DomainException("message.too_long", $"A message can be at most {BodyMaxLength} characters.");
        }

        if (files.Count > MaxAttachments)
        {
            throw new DomainException("message.too_many_files", $"At most {MaxAttachments} files per message.");
        }

        ConversationId = conversationId;
        SenderUserId = senderUserId;
        SenderRole = senderRole;
        Body = clean;
        SentAt = sentAt;
        for (var i = 0; i < files.Count; i++)
        {
            _attachments.Add(new MessageAttachment(Id, files[i], i + 1));
        }
    }

    public Guid ConversationId { get; private set; }

    public Guid SenderUserId { get; private set; }

    public ChatRole SenderRole { get; private set; }

    public string? Body { get; private set; }

    public DateTimeOffset SentAt { get; private set; }

    public IReadOnlyCollection<MessageAttachment> Attachments => _attachments.AsReadOnly();
}

/// <summary>A file sent with a message.</summary>
public sealed class MessageAttachment : Entity
{
    private MessageAttachment()
    {
    }

    internal MessageAttachment(Guid messageId, Guid fileId, int sortOrder)
    {
        MessageId = messageId;
        FileId = fileId;
        SortOrder = sortOrder;
    }

    public Guid MessageId { get; private set; }

    public Guid FileId { get; private set; }

    public int SortOrder { get; private set; }
}
