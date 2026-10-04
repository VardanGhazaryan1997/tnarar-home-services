using HomeServices.Application.Files;
using HomeServices.Application.Requests;

namespace HomeServices.Application.Chat;

/// <summary>The other side of a conversation: the partner (name, slug) for a customer; the customer (name) for a partner.</summary>
public sealed record ChatPartyDto(string? Name, Guid? PartnerId, string? Slug);

/// <summary><see cref="SenderRole"/>: Customer or Partner (compare with the conversation's MyRole to tell your own messages).</summary>
public sealed record MessageDto(Guid Id, Guid ConversationId, string SenderRole, string? Body, IReadOnlyList<FileDto> Attachments, DateTimeOffset SentAt);

/// <summary>The newest message, shortened, for conversation lists.</summary>
public sealed record MessageSummaryDto(string SenderRole, string? Excerpt, int AttachmentCount, DateTimeOffset SentAt);

/// <summary>
/// A conversation as one of its sides sees it. <see cref="OtherReadAt"/>: how far the other side has read (read receipts).
/// <see cref="CanSend"/>: false once the request ended without an order between them. <see cref="OrderId"/>: their order, if any.
/// </summary>
public sealed record ConversationDto(
    Guid Id,
    Guid RequestId,
    string RequestStatus,
    RequestPlaceDto Place,
    string MyRole,
    ChatPartyDto OtherParty,
    int UnreadCount,
    MessageSummaryDto? LastMessage,
    DateTimeOffset? OtherReadAt,
    bool CanSend,
    Guid? OrderId);

/// <summary>Messages oldest to newest. <see cref="HasMore"/>: older messages exist (ask again with before = the first item's sentAt).</summary>
public sealed record MessagePageDto(IReadOnlyList<MessageDto> Items, bool HasMore);

/// <summary>For badges: conversations with unread messages, and the unread messages in them.</summary>
public sealed record UnreadSummaryDto(int Conversations, int Messages);
