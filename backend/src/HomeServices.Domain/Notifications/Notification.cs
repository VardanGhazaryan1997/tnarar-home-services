using HomeServices.Domain.Common;

namespace HomeServices.Domain.Notifications;

/// <summary>What happened. The Portal words each type in the user's language from <see cref="Notification.Params"/>.</summary>
public enum NotificationType
{
    RequestReceived = 1,
    OfferReceived = 2,
    OfferAccepted = 3,
    OfferRejected = 4,
    OrderStarted = 5,
    CompletionRequested = 6,
    OrderCompleted = 7,
    CompletionRejected = 8,
    OrderCancelled = 9,
    ChangeProposed = 10,
    ChangeAnswered = 11,
    PaymentRecorded = 12,
    PaymentAnswered = 13,
    PaymentResolved = 14,
    ReviewReceived = 15,
    ReviewReplied = 16,
    PartnerApproved = 17,
    PartnerNeedsChanges = 18,
    PartnerRejected = 19,
    CommissionStatementIssued = 20,
    CommissionOverdue = 21,
    PartnerPausedForDebt = 22,
    PartnerResumed = 23,
    CommissionSettled = 24,
}

/// <summary>
/// Something a Portal user should know about, shown in their notification list. <see cref="Link"/> is the Portal path
/// to open (without the language prefix); <see cref="Params"/> is a JSON object of values for the text (names, amounts).
/// It is delivered (SMS when <see cref="SendSms"/>, a live update otherwise) by a background job.
/// </summary>
public sealed class Notification : Entity
{
    public const int LinkMaxLength = 300;

    private Notification()
    {
    }

    public Guid UserId { get; private set; }

    public NotificationType Type { get; private set; }

    public string Link { get; private set; } = string.Empty;

    public string Params { get; private set; } = "{}";

    public bool SendSms { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public DateTimeOffset? DeliveredAt { get; private set; }

    public static Notification Create(Guid userId, NotificationType type, string link, string paramsJson, bool sendSms, DateTimeOffset now)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("notification.user_required", "A notification is for a user.");
        }

        if (string.IsNullOrWhiteSpace(link) || link.Length > LinkMaxLength)
        {
            throw new DomainException("notification.link_invalid", "A notification links to a page.");
        }

        return new Notification { UserId = userId, Type = type, Link = link, Params = paramsJson, SendSms = sendSms, CreatedAt = now };
    }

    /// <summary>Keeps the first time it was read.</summary>
    public void MarkRead(DateTimeOffset now) => ReadAt ??= now;

    public void MarkDelivered(DateTimeOffset now) => DeliveredAt ??= now;
}
