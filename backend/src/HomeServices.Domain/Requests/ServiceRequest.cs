using HomeServices.Domain.Common;

namespace HomeServices.Domain.Requests;

/// <summary>
/// A customer's request for work: what (category), where (city, optionally a district), a description,
/// and optionally when, a budget and photos. A direct request goes to one chosen partner; an open request
/// goes to matching partners. When nobody can take it, it waits for an operator (<see cref="AttentionReason"/>).
/// </summary>
public sealed class ServiceRequest : AuditableEntity, IAudited
{
    public const int DescriptionMinLength = 20;
    public const int DescriptionMaxLength = 4000;
    public const int TimeNoteMaxLength = 200;
    public const int CancelReasonMaxLength = 500;
    public const int MaxMedia = 10;
    public const int MaxBudget = 1_000_000_000;

    private readonly List<RequestRecipient> _recipients = [];
    private readonly List<RequestMedia> _media = [];

    private ServiceRequest()
    {
    }

    public Guid CustomerId { get; private set; }

    public RequestKind Kind { get; private set; }

    public RequestStatus Status { get; private set; }

    public Guid CategoryId { get; private set; }

    public Guid CityId { get; private set; }

    /// <summary>Null means anywhere in the city.</summary>
    public Guid? DistrictId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public DateOnly? PreferredDate { get; private set; }

    /// <summary>Free text such as "weekday evenings".</summary>
    public string? TimeNote { get; private set; }

    /// <summary>Budget range in AMD; either end may be open.</summary>
    public int? BudgetMin { get; private set; }

    public int? BudgetMax { get; private set; }

    /// <summary>Since when the request has been waiting for an operator; null when it isn't.</summary>
    public DateTimeOffset? NeedsAttentionSince { get; private set; }

    public AttentionReason? AttentionReason { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public string? CancelReason { get; private set; }

    /// <summary>When the customer accepted a work offer, which closes the request.</summary>
    public DateTimeOffset? ClosedAt { get; private set; }

    public IReadOnlyCollection<RequestRecipient> Recipients => _recipients.AsReadOnly();

    public IReadOnlyCollection<RequestMedia> Media => _media.AsReadOnly();

    public bool NeedsAttention => NeedsAttentionSince is not null;

    public static ServiceRequest Create(
        Guid customerId,
        RequestKind kind,
        Guid categoryId,
        Guid cityId,
        Guid? districtId,
        string description,
        DateOnly? preferredDate,
        string? timeNote,
        int? budgetMin,
        int? budgetMax)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("request.customer_required", "A request belongs to a customer.");
        }

        if (!Enum.IsDefined(kind))
        {
            throw new DomainException("request.kind_invalid", "Unknown request kind.");
        }

        if (categoryId == Guid.Empty || cityId == Guid.Empty)
        {
            throw new DomainException("request.place_required", "A request needs a category and a city.");
        }

        var cleanDescription = (description ?? string.Empty).Trim();
        if (cleanDescription.Length is < DescriptionMinLength or > DescriptionMaxLength)
        {
            throw new DomainException(
                "request.description_length",
                $"The description must be {DescriptionMinLength}–{DescriptionMaxLength} characters.");
        }

        var cleanNote = string.IsNullOrWhiteSpace(timeNote) ? null : timeNote.Trim();
        if (cleanNote?.Length > TimeNoteMaxLength)
        {
            throw new DomainException("request.time_note_too_long", $"The time note must be at most {TimeNoteMaxLength} characters.");
        }

        if (budgetMin is < 0 or > MaxBudget || budgetMax is < 0 or > MaxBudget || budgetMin > budgetMax)
        {
            throw new DomainException("request.budget_invalid", "The budget range is not valid.");
        }

        return new ServiceRequest
        {
            CustomerId = customerId,
            Kind = kind,
            Status = RequestStatus.Open,
            CategoryId = categoryId,
            CityId = cityId,
            DistrictId = districtId,
            Description = cleanDescription,
            PreferredDate = preferredDate,
            TimeNote = cleanNote,
            BudgetMin = budgetMin,
            BudgetMax = budgetMax,
        };
    }

    /// <summary>Attaches a photo or video. The caller checks the file is the customer's, ready and of a fitting kind.</summary>
    public void AddMedia(Guid fileId)
    {
        if (_media.Any(m => m.FileId == fileId))
        {
            return;
        }

        if (_media.Count >= MaxMedia)
        {
            throw new DomainException("request.too_many_media", $"At most {MaxMedia} photos or videos.");
        }

        _media.Add(new RequestMedia(Id, fileId, _media.Count + 1));
    }

    /// <summary>
    /// Sends the request to partners who don't have it yet and returns how many were added. Partners
    /// receiving it means it no longer waits for an operator. A direct request has exactly one partner.
    /// </summary>
    public int SendTo(IEnumerable<Guid> partnerProfileIds, RecipientSource source, DateTimeOffset now)
    {
        EnsureOpen();
        if (!Enum.IsDefined(source))
        {
            throw new DomainException("request.source_invalid", "Unknown recipient source.");
        }

        var added = 0;
        foreach (var partnerId in partnerProfileIds.Distinct().Where(id => _recipients.All(r => r.PartnerProfileId != id)))
        {
            if (Kind == RequestKind.Direct && source != RecipientSource.Manual && _recipients.Count > 0)
            {
                throw new DomainException("request.direct_has_partner", "A direct request goes to one partner.");
            }

            _recipients.Add(new RequestRecipient(Id, partnerId, source, now));
            added++;
        }

        if (added > 0)
        {
            ClearAttention();
        }

        return added;
    }

    /// <summary>Puts the request in the operator queue. Keeps the first reason and time while it stays there.</summary>
    public void FlagForOperator(AttentionReason reason, DateTimeOffset now)
    {
        EnsureOpen();
        if (!Enum.IsDefined(reason))
        {
            throw new DomainException("request.reason_invalid", "Unknown attention reason.");
        }

        if (NeedsAttentionSince is null)
        {
            NeedsAttentionSince = now;
            AttentionReason = reason;
        }
    }

    /// <summary>
    /// Flags an open request nobody has responded to within <paramref name="responseWindow"/> of its newest
    /// recipient. Returns whether it was flagged now.
    /// </summary>
    public bool FlagIfUnanswered(DateTimeOffset now, TimeSpan responseWindow)
    {
        if (Status != RequestStatus.Open || NeedsAttention || _recipients.Count == 0
            || _recipients.Any(r => r.Status == RecipientStatus.Responded))
        {
            return false;
        }

        if (_recipients.Max(r => r.SentAt) + responseWindow > now)
        {
            return false;
        }

        FlagForOperator(Requests.AttentionReason.NoResponse, now);
        return true;
    }

    public void MarkViewed(Guid partnerProfileId, DateTimeOffset now) => RecipientOf(partnerProfileId).MarkViewed(now);

    /// <summary>
    /// The partner turns the request down. When the direct partner, or every partner, has declined,
    /// the request goes to the operator queue.
    /// </summary>
    public void Decline(Guid partnerProfileId, string? reason, DateTimeOffset now)
    {
        EnsureOpen();
        RecipientOf(partnerProfileId).Decline(reason, now);

        if (Kind == RequestKind.Direct && _recipients.Count == 1)
        {
            FlagForOperator(Requests.AttentionReason.DirectPartnerDeclined, now);
        }
        else if (_recipients.All(r => r.Status == RecipientStatus.Declined))
        {
            FlagForOperator(Requests.AttentionReason.AllDeclined, now);
        }
    }

    /// <summary>The partner replied (question, visit or offer). Someone is on it, so no operator is needed.</summary>
    public void MarkResponded(Guid partnerProfileId, DateTimeOffset now)
    {
        EnsureOpen();
        RecipientOf(partnerProfileId).MarkResponded(now);
        ClearAttention();
    }

    /// <summary>The customer (or staff) withdraws the request.</summary>
    public void Cancel(string? reason, DateTimeOffset now)
    {
        EnsureOpen();
        var clean = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (clean?.Length > CancelReasonMaxLength)
        {
            throw new DomainException("request.reason_too_long", $"The reason must be at most {CancelReasonMaxLength} characters.");
        }

        Status = RequestStatus.Cancelled;
        CancelledAt = now;
        CancelReason = clean;
        ClearAttention();
    }

    /// <summary>The customer agreed on the work (accepted a work offer): the request is done.</summary>
    public void Close(DateTimeOffset now)
    {
        EnsureOpen();
        Status = RequestStatus.Closed;
        ClosedAt = now;
        ClearAttention();
    }

    public RequestRecipient? FindRecipient(Guid partnerProfileId) => _recipients.SingleOrDefault(r => r.PartnerProfileId == partnerProfileId);

    private RequestRecipient RecipientOf(Guid partnerProfileId) =>
        FindRecipient(partnerProfileId) ?? throw new DomainException("request.not_recipient", "This partner didn't receive the request.");

    private void ClearAttention()
    {
        NeedsAttentionSince = null;
        AttentionReason = null;
    }

    private void EnsureOpen()
    {
        if (Status != RequestStatus.Open)
        {
            throw new DomainException("request.not_open", $"The request is {Status}.");
        }
    }
}
