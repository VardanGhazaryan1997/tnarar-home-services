using HomeServices.Domain.Common;
using HomeServices.Domain.Offers;
using HomeServices.Domain.Orders;

namespace HomeServices.Domain.Payments;

public enum PaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    Card = 3,
    Other = 4,
}

/// <summary>
/// Pending until the other side answers: Confirmed (counts as paid) or Disputed (the team decides: Confirmed or
/// Rejected). Withdrawn: the one who recorded it took it back.
/// </summary>
public enum PaymentStatus
{
    Pending = 1,
    Confirmed = 2,
    Disputed = 3,
    Withdrawn = 4,
    Rejected = 5,
}

/// <summary>
/// A payment made directly between the customer and the partner (TnaShen doesn't take the money in release 1).
/// One side records it, the other confirms or disputes it; a dispute goes to the team.
/// </summary>
public sealed class Payment : AuditableEntity, IAudited
{
    public const int NoteMaxLength = 500;
    public const int ReasonMaxLength = 1000;

    private Payment()
    {
    }

    public Guid OrderId { get; private set; }

    /// <summary>The payment stage of the order this pays, if the recorder chose one.</summary>
    public Guid? StageId { get; private set; }

    public int Amount { get; private set; }

    public PaymentMethod Method { get; private set; }

    public DateOnly PaidOn { get; private set; }

    public string? Note { get; private set; }

    public OrderParty RecordedBy { get; private set; }

    public Guid RecordedByUserId { get; private set; }

    public PaymentStatus Status { get; private set; }

    public DateTimeOffset RecordedAt { get; private set; }

    /// <summary>When the other side confirmed or disputed it, or the recorder withdrew it.</summary>
    public DateTimeOffset? AnsweredAt { get; private set; }

    public string? DisputeReason { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public string? ResolutionNote { get; private set; }

    /// <summary>Counts against the order's price: everything except withdrawn and rejected records.</summary>
    public bool Counts => Status is PaymentStatus.Pending or PaymentStatus.Confirmed or PaymentStatus.Disputed;

    /// <summary>
    /// Records a payment on <paramref name="order"/>. <paramref name="alreadyCounted"/> is the total of the order's
    /// other counting payments: together they can't exceed the price.
    /// </summary>
    public static Payment Record(
        Order order,
        OrderParty by,
        Guid userId,
        Guid? stageId,
        int amount,
        PaymentMethod method,
        DateOnly paidOn,
        string? note,
        int alreadyCounted,
        DateTimeOffset now)
    {
        if (by is not (OrderParty.Customer or OrderParty.Partner))
        {
            throw new DomainException("payment.wrong_party", "Only the customer or the partner records a payment.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new DomainException("payment.order_cancelled", "Payments can't be recorded on a cancelled order.");
        }

        if (!Enum.IsDefined(method))
        {
            throw new DomainException("payment.method_invalid", "Unknown payment method.");
        }

        if (amount <= 0 || amount > Offer.MaxPrice)
        {
            throw new DomainException("payment.amount_invalid", "The amount must be more than zero.");
        }

        if ((long)alreadyCounted + amount > order.Price)
        {
            throw new DomainException("payment.exceeds_price", "Payments can't add up to more than the order's price.");
        }

        if (paidOn > DateOnly.FromDateTime(now.UtcDateTime).AddDays(1))
        {
            throw new DomainException("payment.date_future", "The payment date can't be in the future.");
        }

        if (stageId is { } stage && order.Stages.All(s => s.Id != stage))
        {
            throw new DomainException("payment.stage_invalid", "This stage isn't on the order.");
        }

        var clean = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (clean?.Length > NoteMaxLength)
        {
            throw new DomainException("payment.note_too_long", $"The note can be at most {NoteMaxLength} characters.");
        }

        return new Payment
        {
            OrderId = order.Id,
            StageId = stageId,
            Amount = amount,
            Method = method,
            PaidOn = paidOn,
            Note = clean,
            RecordedBy = by,
            RecordedByUserId = userId,
            Status = PaymentStatus.Pending,
            RecordedAt = now,
        };
    }

    /// <summary>The other side says the payment happened.</summary>
    public void Confirm(OrderParty by, DateTimeOffset now)
    {
        RequirePending(by, otherSide: true);
        Status = PaymentStatus.Confirmed;
        AnsweredAt = now;
    }

    /// <summary>The other side says it didn't happen like that; the team decides.</summary>
    public void Dispute(OrderParty by, string? reason, DateTimeOffset now)
    {
        RequirePending(by, otherSide: true);
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("payment.reason_required", "Say what is wrong with this payment.");
        }

        var clean = reason.Trim();
        if (clean.Length > ReasonMaxLength)
        {
            throw new DomainException("payment.reason_too_long", $"The reason can be at most {ReasonMaxLength} characters.");
        }

        Status = PaymentStatus.Disputed;
        DisputeReason = clean;
        AnsweredAt = now;
    }

    /// <summary>The one who recorded it takes it back while it waits.</summary>
    public void Withdraw(OrderParty by, DateTimeOffset now)
    {
        RequirePending(by, otherSide: false);
        Status = PaymentStatus.Withdrawn;
        AnsweredAt = now;
    }

    /// <summary>Staff decide a dispute: the payment counts as made (Confirmed) or not (Rejected).</summary>
    public void Resolve(bool counts, string? note, DateTimeOffset now)
    {
        if (Status != PaymentStatus.Disputed)
        {
            throw new DomainException("payment.not_disputed", "Only a disputed payment is resolved.");
        }

        var clean = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (clean?.Length > ReasonMaxLength)
        {
            throw new DomainException("payment.note_too_long", $"The note can be at most {ReasonMaxLength} characters.");
        }

        Status = counts ? PaymentStatus.Confirmed : PaymentStatus.Rejected;
        ResolutionNote = clean;
        ResolvedAt = now;
    }

    private void RequirePending(OrderParty by, bool otherSide)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new DomainException("payment.not_pending", $"This payment is already {Status}.");
        }

        var allowed = otherSide ? by is OrderParty.Customer or OrderParty.Partner && by != RecordedBy : by == RecordedBy;
        if (!allowed)
        {
            throw new DomainException("payment.wrong_party", otherSide ? "The other side answers a payment record." : "Only who recorded a payment can withdraw it.");
        }
    }
}
