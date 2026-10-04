using HomeServices.Domain.Common;

namespace HomeServices.Domain.Requests;

/// <summary>A partner who received a request, and what they did with it.</summary>
public sealed class RequestRecipient : Entity, IAudited
{
    public const int DeclineReasonMaxLength = 500;

    private RequestRecipient()
    {
    }

    internal RequestRecipient(Guid requestId, Guid partnerProfileId, RecipientSource source, DateTimeOffset sentAt)
    {
        RequestId = requestId;
        PartnerProfileId = partnerProfileId;
        Source = source;
        Status = RecipientStatus.New;
        SentAt = sentAt;
    }

    public Guid RequestId { get; private set; }

    public Guid PartnerProfileId { get; private set; }

    public RecipientSource Source { get; private set; }

    public RecipientStatus Status { get; private set; }

    public DateTimeOffset SentAt { get; private set; }

    /// <summary>When the partner first opened the request. Not audited: opening it changes nothing.</summary>
    [NotAudited]
    public DateTimeOffset? ViewedAt { get; private set; }

    public DateTimeOffset? DeclinedAt { get; private set; }

    public string? DeclineReason { get; private set; }

    public DateTimeOffset? RespondedAt { get; private set; }

    internal void MarkViewed(DateTimeOffset now)
    {
        ViewedAt ??= now;
        if (Status == RecipientStatus.New)
        {
            Status = RecipientStatus.Viewed;
        }
    }

    internal void Decline(string? reason, DateTimeOffset now)
    {
        if (Status == RecipientStatus.Declined)
        {
            throw new DomainException("request.already_declined", "You already declined this request.");
        }

        if (Status == RecipientStatus.Responded)
        {
            throw new DomainException("request.already_responded", "You already responded to this request.");
        }

        var clean = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (clean?.Length > DeclineReasonMaxLength)
        {
            throw new DomainException("request.reason_too_long", $"The reason must be at most {DeclineReasonMaxLength} characters.");
        }

        ViewedAt ??= now;
        Status = RecipientStatus.Declined;
        DeclinedAt = now;
        DeclineReason = clean;
    }

    internal void MarkResponded(DateTimeOffset now)
    {
        if (Status == RecipientStatus.Declined)
        {
            throw new DomainException("request.already_declined", "You already declined this request.");
        }

        ViewedAt ??= now;
        RespondedAt ??= now;
        Status = RecipientStatus.Responded;
    }
}

/// <summary>A photo or video the customer attached to a request.</summary>
public sealed class RequestMedia : Entity, IAudited
{
    private RequestMedia()
    {
    }

    internal RequestMedia(Guid requestId, Guid fileId, int sortOrder)
    {
        RequestId = requestId;
        FileId = fileId;
        SortOrder = sortOrder;
    }

    public Guid RequestId { get; private set; }

    public Guid FileId { get; private set; }

    public int SortOrder { get; private set; }
}
