using HomeServices.Domain.Common;

namespace HomeServices.Domain.Orders;

/// <summary><see cref="ExtraWork"/>: more work for more money. <see cref="Schedule"/>: new dates (work) or a new visit time.</summary>
public enum ChangeRequestKind
{
    ExtraWork = 1,
    Schedule = 2,
}

/// <summary>Pending until the other side answers; <see cref="Closed"/> when the order moved on (done or cancelled) first.</summary>
public enum ChangeRequestStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    Withdrawn = 4,
    Closed = 5,
}

/// <summary>
/// What a change proposes. Extra work: <see cref="Title"/> and <see cref="Amount"/>. Schedule: <see cref="NewStartDate"/>
/// and/or <see cref="NewDurationDays"/> for work, <see cref="NewVisitAt"/> for a visit. <see cref="Description"/> explains it.
/// </summary>
public sealed record ChangeProposal(
    ChangeRequestKind Kind,
    string? Title,
    string? Description,
    int? Amount,
    DateOnly? NewStartDate,
    int? NewDurationDays,
    DateTimeOffset? NewVisitAt);

/// <summary>A change to an order proposed by one side; created and answered through <see cref="Order"/>.</summary>
public sealed class OrderChangeRequest : Entity, IAudited
{
    private OrderChangeRequest()
    {
    }

    internal OrderChangeRequest(Guid orderId, OrderParty proposedBy, ChangeProposal proposal, DateTimeOffset now)
    {
        OrderId = orderId;
        ProposedBy = proposedBy;
        Kind = proposal.Kind;
        Title = proposal.Title;
        Description = proposal.Description;
        Amount = proposal.Amount;
        NewStartDate = proposal.NewStartDate;
        NewDurationDays = proposal.NewDurationDays;
        NewVisitAt = proposal.NewVisitAt;
        Status = ChangeRequestStatus.Pending;
        ProposedAt = now;
    }

    public Guid OrderId { get; private set; }

    public OrderParty ProposedBy { get; private set; }

    public ChangeRequestKind Kind { get; private set; }

    public ChangeRequestStatus Status { get; private set; }

    public string? Title { get; private set; }

    public string? Description { get; private set; }

    public int? Amount { get; private set; }

    public DateOnly? NewStartDate { get; private set; }

    public int? NewDurationDays { get; private set; }

    public DateTimeOffset? NewVisitAt { get; private set; }

    /// <summary>Why the other side said no, if they said.</summary>
    public string? ResponseNote { get; private set; }

    public DateTimeOffset ProposedAt { get; private set; }

    public DateTimeOffset? DecidedAt { get; private set; }

    internal void Decide(ChangeRequestStatus status, string? note, DateTimeOffset now)
    {
        Status = status;
        ResponseNote = note;
        DecidedAt = now;
    }
}
