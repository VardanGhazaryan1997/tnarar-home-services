using HomeServices.Domain.Common;

namespace HomeServices.Domain.Commissions;

/// <summary>
/// The share of an order's price a partner owes TnaShen. <see cref="CategoryId"/> null = the default rate; a category
/// rate applies to it and its subcategories (unless a subcategory has its own).
/// </summary>
public sealed class CommissionRate : Entity, IAudited
{
    public const decimal MaxPercent = 50m;

    /// <summary>The default rate's fixed id (seeded by a migration).</summary>
    public static readonly Guid DefaultId = new("019a0000-0000-7000-8000-000000000901");

    private CommissionRate()
    {
    }

    public Guid? CategoryId { get; private set; }

    public decimal Percent { get; private set; }

    /// <summary>The default rate row (normally seeded; created by the Back Office if missing).</summary>
    public static CommissionRate Default(decimal percent)
    {
        var rate = new CommissionRate { Id = DefaultId };
        rate.Change(percent);
        return rate;
    }

    public static CommissionRate ForCategory(Guid categoryId, decimal percent)
    {
        var rate = new CommissionRate { CategoryId = categoryId };
        rate.Change(percent);
        return rate;
    }

    public void Change(decimal percent)
    {
        if (percent < 0 || percent > MaxPercent || decimal.Round(percent, 2) != percent)
        {
            throw new DomainException("commission.rate_invalid", $"The rate is 0–{MaxPercent}% with at most two decimals.");
        }

        Percent = percent;
    }

    /// <summary>What <paramref name="price"/> costs at <paramref name="percent"/>, rounded to whole drams (half up).</summary>
    public static int AmountFor(int price, decimal percent) =>
        (int)decimal.Round(price * percent / 100m, 0, MidpointRounding.AwayFromZero);
}

/// <summary>
/// What a partner owes for one completed order: the final price × the rate in force when it completed. It goes on the
/// weekly statement of the week it completed in.
/// </summary>
public sealed class CommissionObligation : Entity, IAudited
{
    private CommissionObligation()
    {
    }

    public Guid OrderId { get; private set; }

    public Guid PartnerProfileId { get; private set; }

    public Guid CategoryId { get; private set; }

    public int OrderPrice { get; private set; }

    public decimal RatePercent { get; private set; }

    public int Amount { get; private set; }

    public DateTimeOffset CompletedAt { get; private set; }

    /// <summary>The statement it was billed on; null until the weekly job issues one.</summary>
    public Guid? StatementId { get; private set; }

    public static CommissionObligation For(Guid orderId, Guid partnerProfileId, Guid categoryId, int orderPrice, decimal ratePercent, DateTimeOffset completedAt)
    {
        if (orderPrice < 0)
        {
            throw new DomainException("commission.price_invalid", "The price can't be negative.");
        }

        return new CommissionObligation
        {
            OrderId = orderId,
            PartnerProfileId = partnerProfileId,
            CategoryId = categoryId,
            OrderPrice = orderPrice,
            RatePercent = ratePercent,
            Amount = CommissionRate.AmountFor(orderPrice, ratePercent),
            CompletedAt = completedAt,
        };
    }

    public void BillOn(Guid statementId)
    {
        if (StatementId is not null)
        {
            throw new DomainException("commission.already_billed", "This commission is already on a statement.");
        }

        StatementId = statementId;
    }
}

public enum StatementStatus
{
    Open = 1,
    Paid = 2,
}

/// <summary>
/// One partner's commissions for one week (Monday–Sunday), due <see cref="DueOn"/>. Partners pay TnaShen offline;
/// staff record each payment as a <see cref="Settlement"/>. Overdue = open after the due date.
/// </summary>
public sealed class CommissionStatement : Entity, IAudited
{
    private CommissionStatement()
    {
    }

    public Guid PartnerProfileId { get; private set; }

    public DateOnly PeriodStart { get; private set; }

    public DateOnly PeriodEnd { get; private set; }

    public int Total { get; private set; }

    public int PaidAmount { get; private set; }

    public StatementStatus Status { get; private set; }

    public DateTimeOffset IssuedAt { get; private set; }

    public DateOnly DueOn { get; private set; }

    public DateTimeOffset? PaidAt { get; private set; }

    /// <summary>When the partner was reminded that it is overdue (once).</summary>
    public DateTimeOffset? OverdueReminderAt { get; private set; }

    public int Outstanding => Total - PaidAmount;

    public bool IsOverdue(DateOnly today) => Status == StatementStatus.Open && today > DueOn;

    /// <summary>A statement for the week starting <paramref name="weekStart"/> (a Monday) over these obligations.</summary>
    public static CommissionStatement Issue(Guid partnerProfileId, DateOnly weekStart, IReadOnlyCollection<CommissionObligation> obligations, DateOnly dueOn, DateTimeOffset now)
    {
        if (weekStart.DayOfWeek != DayOfWeek.Monday)
        {
            throw new DomainException("commission.week_invalid", "A statement week starts on a Monday.");
        }

        if (obligations.Count == 0 || obligations.Any(o => o.PartnerProfileId != partnerProfileId))
        {
            throw new DomainException("commission.statement_empty", "A statement needs this partner's commissions.");
        }

        var statement = new CommissionStatement
        {
            PartnerProfileId = partnerProfileId,
            PeriodStart = weekStart,
            PeriodEnd = weekStart.AddDays(6),
            Total = obligations.Sum(o => o.Amount),
            Status = StatementStatus.Open,
            IssuedAt = now,
            DueOn = dueOn,
        };
        foreach (var obligation in obligations)
        {
            obligation.BillOn(statement.Id);
        }

        if (statement.Total == 0)
        {
            statement.Status = StatementStatus.Paid;
            statement.PaidAt = now;
        }

        return statement;
    }

    /// <summary>Records a payment toward the statement; it is paid once nothing is outstanding.</summary>
    public Settlement Settle(int amount, SettlementMethod method, DateOnly paidOn, string? reference, DateTimeOffset now)
    {
        if (Status == StatementStatus.Paid)
        {
            throw new DomainException("commission.already_paid", "This statement is already paid.");
        }

        if (amount <= 0 || amount > Outstanding)
        {
            throw new DomainException("commission.settlement_amount_invalid", $"The amount must be 1–{Outstanding}.");
        }

        var settlement = Settlement.Create(Id, PartnerProfileId, amount, method, paidOn, reference, now);
        PaidAmount += amount;
        if (Outstanding == 0)
        {
            Status = StatementStatus.Paid;
            PaidAt = now;
        }

        return settlement;
    }

    /// <summary>Marks the overdue reminder as sent; false when it already was.</summary>
    public bool MarkOverdueReminded(DateTimeOffset now)
    {
        if (OverdueReminderAt is not null)
        {
            return false;
        }

        OverdueReminderAt = now;
        return true;
    }
}

public enum SettlementMethod
{
    BankTransfer = 1,
    Cash = 2,
    Card = 3,
    Other = 4,
}

/// <summary>A payment a partner made toward a statement, recorded by staff.</summary>
public sealed class Settlement : AuditableEntity, IAudited
{
    public const int ReferenceMaxLength = 200;

    private Settlement()
    {
    }

    public Guid StatementId { get; private set; }

    public Guid PartnerProfileId { get; private set; }

    public int Amount { get; private set; }

    public SettlementMethod Method { get; private set; }

    public DateOnly PaidOn { get; private set; }

    public string? Reference { get; private set; }

    public DateTimeOffset RecordedAt { get; private set; }

    internal static Settlement Create(Guid statementId, Guid partnerProfileId, int amount, SettlementMethod method, DateOnly paidOn, string? reference, DateTimeOffset now)
    {
        if (!Enum.IsDefined(method))
        {
            throw new DomainException("commission.method_invalid", "Unknown payment method.");
        }

        if (paidOn > DateOnly.FromDateTime(now.UtcDateTime).AddDays(1))
        {
            throw new DomainException("commission.date_future", "The payment date can't be in the future.");
        }

        var clean = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
        if (clean?.Length > ReferenceMaxLength)
        {
            throw new DomainException("commission.reference_too_long", $"The reference can be at most {ReferenceMaxLength} characters.");
        }

        return new Settlement
        {
            StatementId = statementId,
            PartnerProfileId = partnerProfileId,
            Amount = amount,
            Method = method,
            PaidOn = paidOn,
            Reference = clean,
            RecordedAt = now,
        };
    }
}
