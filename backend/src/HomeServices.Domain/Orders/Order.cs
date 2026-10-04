using HomeServices.Domain.Common;
using HomeServices.Domain.Offers;

namespace HomeServices.Domain.Orders;

/// <summary><see cref="Work"/>: the job. <see cref="Visit"/>: an assessment visit; a later work order links to it.</summary>
public enum OrderKind
{
    Work = 1,
    Visit = 2,
}

/// <summary>
/// An order's progress: Confirmed → (InProgress) → CompletionRequested → Completed; CompletionRequested goes back to
/// InProgress when the customer says the work isn't finished. Anything before Completed can be Cancelled.
/// </summary>
public enum OrderStatus
{
    Confirmed = 1,
    InProgress = 2,
    CompletionRequested = 3,
    Completed = 4,
    Cancelled = 5,
}

/// <summary>Who did something to an order.</summary>
public enum OrderParty
{
    Customer = 1,
    Partner = 2,
    Staff = 3,
    /// <summary>The platform itself, e.g. completing an order the customer didn't answer.</summary>
    System = 4,
}

/// <summary>
/// Agreed work between a customer and a partner, created when the customer accepts an offer. The accepted
/// terms are frozen in <see cref="Terms"/> (JSON), so later changes elsewhere never rewrite what was agreed.
/// Every status change is written to <see cref="StatusChanges"/>, with who made it. Either side can propose a
/// change (<see cref="ChangeRequests"/>): extra work, or a new schedule; it applies once the other side accepts.
/// </summary>
public sealed class Order : AuditableEntity, IAudited
{
    public const int ReasonMaxLength = 1000;
    public const int MaxDurationDays = 365;

    private static readonly OrderStatus[] Open = [OrderStatus.Confirmed, OrderStatus.InProgress, OrderStatus.CompletionRequested];

    private readonly List<OrderStage> _stages = [];
    private readonly List<OrderStatusChange> _statusChanges = [];
    private readonly List<OrderChangeRequest> _changeRequests = [];

    private Order()
    {
    }

    public Guid RequestId { get; private set; }

    public Guid OfferId { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid PartnerProfileId { get; private set; }

    public OrderKind Kind { get; private set; }

    public OrderStatus Status { get; private set; }

    /// <summary>The assessment visit order this work order followed, if any.</summary>
    public Guid? ParentOrderId { get; private set; }

    public int Price { get; private set; }

    public DateOnly? StartDate { get; private set; }

    public int? DurationDays { get; private set; }

    public DateTimeOffset? VisitAt { get; private set; }

    /// <summary>The accepted offer's terms as JSON (summary, lines, materials, dates, stages).</summary>
    public string Terms { get; private set; } = "{}";

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletionRequestedAt { get; private set; }

    /// <summary>When an order marked as done completes by itself if the customer doesn't answer.</summary>
    public DateTimeOffset? AutoCompleteAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public OrderParty? CancelledBy { get; private set; }

    public string? CancelReason { get; private set; }

    /// <summary>Set when a customer or partner cancels after work started: the team checks payments and disputes.</summary>
    public DateTimeOffset? NeedsAttentionSince { get; private set; }

    public bool IsOpen => Open.Contains(Status);

    public IReadOnlyCollection<OrderStage> Stages => _stages.AsReadOnly();

    public IReadOnlyCollection<OrderStatusChange> StatusChanges => _statusChanges.AsReadOnly();

    public IReadOnlyCollection<OrderChangeRequest> ChangeRequests => _changeRequests.AsReadOnly();

    public OrderChangeRequest? PendingChange => _changeRequests.SingleOrDefault(c => c.Status == ChangeRequestStatus.Pending);

    /// <summary>The order for an accepted offer. <paramref name="termsJson"/> is the offer's terms, frozen.</summary>
    public static Order FromOffer(Offer offer, Guid customerId, Guid? parentOrderId, string termsJson, DateTimeOffset now)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("order.customer_required", "An order belongs to a customer.");
        }

        if (parentOrderId is not null && offer.Kind != OfferKind.Work)
        {
            throw new DomainException("order.parent_invalid", "Only a work order follows a visit.");
        }

        if (string.IsNullOrWhiteSpace(termsJson))
        {
            throw new DomainException("order.terms_required", "An order keeps the agreed terms.");
        }

        var order = new Order
        {
            RequestId = offer.RequestId,
            OfferId = offer.Id,
            CustomerId = customerId,
            PartnerProfileId = offer.PartnerProfileId,
            Kind = offer.Kind == OfferKind.Visit ? OrderKind.Visit : OrderKind.Work,
            Status = OrderStatus.Confirmed,
            ParentOrderId = parentOrderId,
            Price = offer.Price,
            StartDate = offer.StartDate,
            DurationDays = offer.DurationDays,
            VisitAt = offer.VisitAt,
            Terms = termsJson,
        };

        foreach (var stage in offer.Stages.OrderBy(s => s.SortOrder))
        {
            order._stages.Add(new OrderStage(order.Id, stage.Title, stage.Purpose, stage.Amount, stage.SortOrder));
        }

        order._statusChanges.Add(new OrderStatusChange(order.Id, OrderStatus.Confirmed, OrderParty.Customer, null, now, 1));
        return order;
    }

    /// <summary>The partner starts the work (optional: asking for completion works from Confirmed too).</summary>
    public void Start(OrderParty by, DateTimeOffset now)
    {
        RequireParty(by, OrderParty.Partner);
        if (Status != OrderStatus.Confirmed)
        {
            throw new DomainException("order.cannot_start", $"An order that is {Status} can't be started.");
        }

        StartedAt = now;
        ChangeStatus(OrderStatus.InProgress, by, null, now);
    }

    /// <summary>
    /// The partner says the work (or the visit) is done and asks the customer to confirm. Without an answer
    /// within <paramref name="autoCompleteAfter"/> it completes by itself.
    /// </summary>
    public void RequestCompletion(OrderParty by, TimeSpan autoCompleteAfter, DateTimeOffset now)
    {
        RequireParty(by, OrderParty.Partner);
        if (Status is not (OrderStatus.Confirmed or OrderStatus.InProgress))
        {
            throw new DomainException("order.cannot_request_completion", $"An order that is {Status} can't be marked as done.");
        }

        StartedAt ??= now;
        CompletionRequestedAt = now;
        AutoCompleteAt = now + autoCompleteAfter;
        CloseChanges(now);
        ChangeStatus(OrderStatus.CompletionRequested, by, null, now);
    }

    /// <summary>The customer confirms the work is done.</summary>
    public void ConfirmCompletion(OrderParty by, DateTimeOffset now)
    {
        RequireParty(by, OrderParty.Customer);
        RequireCompletionRequested();
        Complete(by, now);
    }

    /// <summary>The customer says the work isn't finished: back to in progress, with the reason.</summary>
    public void RejectCompletion(OrderParty by, string? reason, DateTimeOffset now)
    {
        RequireParty(by, OrderParty.Customer);
        RequireCompletionRequested();
        var why = RequireReason(reason);
        CompletionRequestedAt = null;
        AutoCompleteAt = null;
        ChangeStatus(OrderStatus.InProgress, by, why, now);
    }

    /// <summary>Completes an order marked as done whose customer didn't answer by <see cref="AutoCompleteAt"/>. Returns whether it did.</summary>
    public bool CompleteIfUnanswered(DateTimeOffset now)
    {
        if (Status != OrderStatus.CompletionRequested || AutoCompleteAt is not { } due || due > now)
        {
            return false;
        }

        Complete(OrderParty.System, now);
        return true;
    }

    /// <summary>
    /// Cancels the order with a reason. A customer or partner can cancel until it's completed; if work had started,
    /// the order is flagged for the team (<see cref="NeedsAttentionSince"/>).
    /// </summary>
    public void Cancel(OrderParty by, string? reason, DateTimeOffset now)
    {
        if (by is not (OrderParty.Customer or OrderParty.Partner or OrderParty.Staff))
        {
            throw new DomainException("order.wrong_party", "Only the customer, the partner or staff can cancel an order.");
        }

        if (!IsOpen)
        {
            throw new DomainException("order.cannot_cancel", $"An order that is {Status} can't be cancelled.");
        }

        var why = RequireReason(reason);
        if (Status != OrderStatus.Confirmed && by != OrderParty.Staff)
        {
            NeedsAttentionSince = now;
        }

        CancelledAt = now;
        AutoCompleteAt = null;
        CancelledBy = by;
        CancelReason = why;
        CloseChanges(now);
        ChangeStatus(OrderStatus.Cancelled, by, why, now);
    }

    /// <summary>Staff have dealt with an order that needed attention.</summary>
    public void Resolve()
    {
        if (NeedsAttentionSince is null)
        {
            throw new DomainException("order.not_flagged", "This order doesn't need attention.");
        }

        NeedsAttentionSince = null;
    }

    /// <summary>One side proposes extra work or a new schedule; it waits for the other side.</summary>
    public OrderChangeRequest ProposeChange(OrderParty by, ChangeProposal proposal, DateTimeOffset now)
    {
        if (by is not (OrderParty.Customer or OrderParty.Partner))
        {
            throw new DomainException("order.wrong_party", "Only the customer or the partner can propose a change.");
        }

        if (Status is not (OrderStatus.Confirmed or OrderStatus.InProgress))
        {
            throw new DomainException("order.cannot_change", $"An order that is {Status} can't be changed.");
        }

        if (PendingChange is not null)
        {
            throw new DomainException("change.pending_exists", "Answer or withdraw the change that is waiting first.");
        }

        var change = new OrderChangeRequest(Id, by, ValidProposal(proposal, now), now);
        _changeRequests.Add(change);
        return change;
    }

    /// <summary>The other side accepts the change: extra work raises the price and adds a payment; a schedule moves the dates.</summary>
    public void AcceptChange(OrderParty by, Guid changeId, DateTimeOffset now)
    {
        var change = PendingChangeFor(changeId, by, otherSide: true);
        if (change.Kind == ChangeRequestKind.ExtraWork)
        {
            var amount = change.Amount!.Value;
            if (Price + (long)amount > Offer.MaxPrice)
            {
                throw new DomainException("change.price_too_high", "The total price would be too high.");
            }

            Price += amount;

            // A stage payment for the extra work, before the final payment (which stays last).
            var final = _stages.SingleOrDefault(s => s.Purpose == PaymentPurpose.Final);
            var sortOrder = final?.SortOrder ?? (_stages.Count == 0 ? 1 : _stages.Max(s => s.SortOrder) + 1);
            final?.MoveTo(sortOrder + 1);
            _stages.Add(new OrderStage(Id, change.Title, PaymentPurpose.Stage, amount, sortOrder));
        }
        else if (Kind == OrderKind.Visit)
        {
            VisitAt = change.NewVisitAt;
        }
        else
        {
            StartDate = change.NewStartDate ?? StartDate;
            DurationDays = change.NewDurationDays ?? DurationDays;
        }

        change.Decide(ChangeRequestStatus.Accepted, null, now);
    }

    /// <summary>The other side turns the change down, optionally saying why.</summary>
    public void RejectChange(OrderParty by, Guid changeId, string? note, DateTimeOffset now)
    {
        var change = PendingChangeFor(changeId, by, otherSide: true);
        var clean = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (clean?.Length > ReasonMaxLength)
        {
            throw new DomainException("change.note_too_long", $"The note can be at most {ReasonMaxLength} characters.");
        }

        change.Decide(ChangeRequestStatus.Rejected, clean, now);
    }

    /// <summary>Whoever proposed the change takes it back.</summary>
    public void WithdrawChange(OrderParty by, Guid changeId, DateTimeOffset now) =>
        PendingChangeFor(changeId, by, otherSide: false).Decide(ChangeRequestStatus.Withdrawn, null, now);

    private ChangeProposal ValidProposal(ChangeProposal proposal, DateTimeOffset now)
    {
        var description = string.IsNullOrWhiteSpace(proposal.Description) ? null : proposal.Description.Trim();
        if (description?.Length > ReasonMaxLength)
        {
            throw new DomainException("change.description_too_long", $"The description can be at most {ReasonMaxLength} characters.");
        }

        switch (proposal.Kind)
        {
            case ChangeRequestKind.ExtraWork:
                if (Kind == OrderKind.Visit)
                {
                    throw new DomainException("change.extra_work_on_visit", "A visit has no extra work; send a work offer instead.");
                }

                var title = proposal.Title?.Trim();
                if (string.IsNullOrEmpty(title) || title.Length > Offer.StageTitleMaxLength)
                {
                    throw new DomainException("change.title_invalid", $"Name the extra work in 1–{Offer.StageTitleMaxLength} characters.");
                }

                if (proposal.Amount is not (> 0 and <= Offer.MaxPrice))
                {
                    throw new DomainException("change.amount_invalid", "The extra work needs a price.");
                }

                return new ChangeProposal(ChangeRequestKind.ExtraWork, title, description, proposal.Amount, null, null, null);

            case ChangeRequestKind.Schedule when Kind == OrderKind.Visit:
                if (proposal.NewVisitAt is not { } visitAt || visitAt <= now)
                {
                    throw new DomainException("change.visit_time_invalid", "Choose a new visit time in the future.");
                }

                return new ChangeProposal(ChangeRequestKind.Schedule, null, description, null, null, null, visitAt);

            case ChangeRequestKind.Schedule:
                if (proposal.NewStartDate is null && proposal.NewDurationDays is null)
                {
                    throw new DomainException("change.schedule_empty", "Choose a new start date or duration.");
                }

                if (proposal.NewDurationDays is { } days && days is < 1 or > MaxDurationDays)
                {
                    throw new DomainException("change.duration_invalid", $"The duration must be 1–{MaxDurationDays} days.");
                }

                if (proposal.NewStartDate is { } start && start < DateOnly.FromDateTime(now.UtcDateTime))
                {
                    throw new DomainException("change.start_date_past", "The new start date can't be in the past.");
                }

                return new ChangeProposal(ChangeRequestKind.Schedule, null, description, null, proposal.NewStartDate, proposal.NewDurationDays, null);

            default:
                throw new DomainException("change.kind_invalid", "Unknown kind of change.");
        }
    }

    private OrderChangeRequest PendingChangeFor(Guid changeId, OrderParty by, bool otherSide)
    {
        var change = _changeRequests.SingleOrDefault(c => c.Id == changeId)
            ?? throw new DomainException("change.not_found", "No such change on this order.");
        if (change.Status != ChangeRequestStatus.Pending)
        {
            throw new DomainException("change.not_pending", $"This change is already {change.Status}.");
        }

        var allowed = otherSide
            ? by is OrderParty.Customer or OrderParty.Partner && by != change.ProposedBy
            : by == change.ProposedBy;
        if (!allowed)
        {
            throw new DomainException("change.wrong_party", otherSide ? "The other side answers a change." : "Only who proposed a change can withdraw it.");
        }

        if (Status is not (OrderStatus.Confirmed or OrderStatus.InProgress))
        {
            throw new DomainException("order.cannot_change", $"An order that is {Status} can't be changed.");
        }

        return change;
    }

    private void Complete(OrderParty by, DateTimeOffset now)
    {
        CompletedAt = now;
        AutoCompleteAt = null;
        ChangeStatus(OrderStatus.Completed, by, null, now);
    }

    private void RequireCompletionRequested()
    {
        if (Status != OrderStatus.CompletionRequested)
        {
            throw new DomainException("order.completion_not_requested", "The partner hasn't marked this order as done.");
        }
    }

    private void CloseChanges(DateTimeOffset now)
    {
        foreach (var change in _changeRequests.Where(c => c.Status == ChangeRequestStatus.Pending))
        {
            change.Decide(ChangeRequestStatus.Closed, null, now);
        }
    }

    private static void RequireParty(OrderParty by, OrderParty expected)
    {
        if (by != expected)
        {
            throw new DomainException("order.wrong_party", $"Only the {expected.ToString().ToLowerInvariant()} can do this.");
        }
    }

    private static string RequireReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("order.reason_required", "Say why.");
        }

        var clean = reason.Trim();
        return clean.Length <= ReasonMaxLength
            ? clean
            : throw new DomainException("order.reason_too_long", $"The reason can be at most {ReasonMaxLength} characters.");
    }

    private void ChangeStatus(OrderStatus status, OrderParty by, string? note, DateTimeOffset now)
    {
        Status = status;
        _statusChanges.Add(new OrderStatusChange(Id, status, by, note, now, _statusChanges.Count + 1));
    }
}

/// <summary>One payment of an order, copied from the accepted offer.</summary>
public sealed class OrderStage : Entity, IAudited
{
    private OrderStage()
    {
    }

    internal OrderStage(Guid orderId, string? title, PaymentPurpose purpose, int amount, int sortOrder)
    {
        OrderId = orderId;
        Title = title;
        Purpose = purpose;
        Amount = amount;
        SortOrder = sortOrder;
    }

    public Guid OrderId { get; private set; }

    public string? Title { get; private set; }

    public PaymentPurpose Purpose { get; private set; }

    public int Amount { get; private set; }

    public int SortOrder { get; private set; }

    internal void MoveTo(int sortOrder) => SortOrder = sortOrder;
}

/// <summary>A row of an order's status history: the new status, who set it and why (cancel and reject reasons).</summary>
public sealed class OrderStatusChange : Entity
{
    private OrderStatusChange()
    {
    }

    internal OrderStatusChange(Guid orderId, OrderStatus status, OrderParty by, string? note, DateTimeOffset changedAt, int sequence)
    {
        OrderId = orderId;
        Status = status;
        ChangedBy = by;
        Note = note;
        ChangedAt = changedAt;
        Sequence = sequence;
    }

    public Guid OrderId { get; private set; }

    public OrderStatus Status { get; private set; }

    /// <summary>Null on rows written before this was recorded.</summary>
    public OrderParty? ChangedBy { get; private set; }

    public string? Note { get; private set; }

    public DateTimeOffset ChangedAt { get; private set; }

    public int Sequence { get; private set; }
}
