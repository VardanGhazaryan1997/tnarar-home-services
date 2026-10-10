using HomeServices.Domain.Catalog;
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

/// <summary>
/// A line of work in a request made from an estimate: the room, the work item, the amount (null = to be agreed) and
/// the estimated labour range (shown to the customer only). Partners can price their offer line by line against these.
/// </summary>
public sealed class RequestLine : Entity, IAudited
{
    public const int RoomNameMaxLength = 60;
    public const decimal MaxQuantity = 100_000m;

    private RequestLine()
    {
    }

    internal RequestLine(Guid requestId, int sortOrder, string roomName, Guid workItemId, WorkUnit unit, decimal? quantity, int? estimateMin, int? estimateMax)
    {
        var room = roomName?.Trim() ?? string.Empty;
        if (room.Length is 0 or > RoomNameMaxLength)
        {
            throw new DomainException("request.line_invalid", $"Each line needs a room name of up to {RoomNameMaxLength} characters.");
        }

        if (workItemId == Guid.Empty || !Enum.IsDefined(unit) || quantity is <= 0 or > MaxQuantity
            || estimateMin is < 0 || estimateMax is < 0 || estimateMin > estimateMax)
        {
            throw new DomainException("request.line_invalid", "A line needs work, a unit and a sensible amount.");
        }

        RequestId = requestId;
        SortOrder = sortOrder;
        RoomName = room;
        WorkItemId = workItemId;
        Unit = unit;
        Quantity = quantity is null ? null : Math.Round(quantity.Value, 2, MidpointRounding.AwayFromZero);
        EstimateMin = estimateMin;
        EstimateMax = estimateMax;
    }

    public Guid RequestId { get; private set; }

    public int SortOrder { get; private set; }

    public string RoomName { get; private set; } = string.Empty;

    public Guid WorkItemId { get; private set; }

    public WorkUnit Unit { get; private set; }

    public decimal? Quantity { get; private set; }

    public int? EstimateMin { get; private set; }

    public int? EstimateMax { get; private set; }
}
