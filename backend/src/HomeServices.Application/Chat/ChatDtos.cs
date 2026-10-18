using HomeServices.Application.Files;
using HomeServices.Application.Requests;

namespace HomeServices.Application.Chat;

/// <summary>The other side of a conversation: the partner (name, slug) for a customer; the customer (name) for a partner.</summary>
public sealed record ChatPartyDto(string? Name, Guid? PartnerId, string? Slug);

/// <summary>
/// <see cref="SenderRole"/>: Customer or Partner (compare with the conversation's MyRole to tell your own messages).
/// <see cref="ClientId"/>: the id the sender's app gave it, if any (to match a message it is still showing as sending).
/// </summary>
public sealed record MessageDto(Guid Id, Guid ConversationId, string SenderRole, string? Body, IReadOnlyList<FileDto> Attachments, DateTimeOffset SentAt, Guid? ClientId = null);

/// <summary>The newest message, shortened, for conversation lists.</summary>
public sealed record MessageSummaryDto(string SenderRole, string? Excerpt, int AttachmentCount, DateTimeOffset SentAt);

/// <summary>
/// A conversation as one of its sides sees it. <see cref="OtherReadAt"/>: how far the other side has read (read receipts).
/// <see cref="CanSend"/>: false once the request ended without an order between them (a cancelled request's
/// conversations stay open until <see cref="CanSendUntil"/>). <see cref="OrderId"/>: their order, if any.
/// <see cref="RequestCancelledAt"/>: when the request was cancelled (and <see cref="RequestCancelReason"/>, why); later
/// messages came after the cancellation.
/// <see cref="Offers"/>: the partner's offers on the request, oldest first (the customer doesn't see withdrawn ones).
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
    Guid? OrderId,
    DateTimeOffset? RequestCancelledAt = null,
    DateTimeOffset? CanSendUntil = null,
    IReadOnlyList<ChatOfferDto>? Offers = null,
    string? RequestCancelReason = null);

/// <summary>
/// An offer between the two sides of a conversation, for the offer panel and the timeline. <see cref="Status"/> is as
/// people see it: an offer past its expiry is Expired. <see cref="SentAt"/>: when it was sent.
/// </summary>
public sealed record ChatOfferDto(
    Guid Id,
    string Kind,
    string Status,
    int Price,
    string Summary,
    DateTimeOffset? VisitAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset SentAt,
    DateTimeOffset? DecidedAt,
    Guid? OrderId);

/// <summary>Messages oldest to newest. <see cref="HasMore"/>: older messages exist (ask again with before = the first item's sentAt).</summary>
public sealed record MessagePageDto(IReadOnlyList<MessageDto> Items, bool HasMore);

/// <summary>For badges: conversations with unread messages, and the unread messages in them.</summary>
public sealed record UnreadSummaryDto(int Conversations, int Messages);
