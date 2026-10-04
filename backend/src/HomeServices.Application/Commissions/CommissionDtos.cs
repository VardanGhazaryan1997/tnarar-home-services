namespace HomeServices.Application.Commissions;

/// <summary>Commission settings (configuration section "Commissions").</summary>
public sealed class CommissionSettings
{
    public const string SectionName = "Commissions";

    /// <summary>Weeks run Monday–Sunday in this time zone (Armenia: UTC+4, no daylight saving).</summary>
    public int UtcOffsetHours { get; set; } = 4;

    /// <summary>A statement is due this many days after it is issued.</summary>
    public int DueDays { get; set; } = 7;

    /// <summary>Overdue by more than this many days → the partner gets no new requests until they pay.</summary>
    public int PauseAfterOverdueDays { get; set; } = 14;

    /// <summary>How often the commission job runs (statements, reminders, pauses).</summary>
    public int JobMinutes { get; set; } = 60;

    public TimeSpan Offset => TimeSpan.FromHours(UtcOffsetHours);

    /// <summary>The local date at <paramref name="at"/>.</summary>
    public DateOnly LocalDate(DateTimeOffset at) => DateOnly.FromDateTime(at.ToOffset(Offset).DateTime);

    /// <summary>The Monday of the week <paramref name="date"/> is in.</summary>
    public static DateOnly WeekStart(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
}

/// <summary>One category's rate: its own <see cref="Percent"/> (null = inherited) and the one that applies.</summary>
public sealed record CategoryRateDto(Guid CategoryId, string Name, Guid? ParentId, decimal? Percent, decimal EffectivePercent);

public sealed record CommissionRatesDto(decimal DefaultPercent, IReadOnlyList<CategoryRateDto> Categories);

/// <summary>A completed order on a statement (or waiting for the next one).</summary>
public sealed record CommissionLineDto(Guid Id, Guid OrderId, string Summary, DateTimeOffset CompletedAt, int OrderPrice, decimal RatePercent, int Amount, Guid? StatementId);

public sealed record SettlementDto(Guid Id, int Amount, string Method, DateOnly PaidOn, string? Reference, DateTimeOffset RecordedAt);

/// <summary>A weekly statement. <see cref="Overdue"/>: open after <see cref="DueOn"/>.</summary>
public sealed record StatementDto(
    Guid Id,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int Total,
    int PaidAmount,
    int Outstanding,
    string Status,
    bool Overdue,
    DateOnly DueOn,
    DateTimeOffset IssuedAt,
    DateTimeOffset? PaidAt);

public sealed record StatementDetailDto(StatementDto Statement, IReadOnlyList<CommissionLineDto> Lines, IReadOnlyList<SettlementDto> Settlements);

/// <summary>A statement in the Back Office list, with its partner. <see cref="PartnerPaused"/>: getting no new requests.</summary>
public sealed record AdminStatementDto(StatementDto Statement, Guid PartnerId, string PartnerName, string PartnerPhone, bool PartnerPaused);

public sealed record AdminStatementDetailDto(AdminStatementDto Summary, IReadOnlyList<CommissionLineDto> Lines, IReadOnlyList<SettlementDto> Settlements);

/// <summary>
/// The partner's position: <see cref="Unbilled"/> = completed orders waiting for next Monday's statement;
/// <see cref="Outstanding"/> = unpaid on issued statements, of which <see cref="Overdue"/> is past due.
/// <see cref="PausedSince"/>: new requests stopped until the overdue amount is paid.
/// </summary>
public sealed record CommissionSummaryDto(int Unbilled, int Outstanding, int Overdue, DateOnly? NextDueOn, DateTimeOffset? PausedSince, int PauseAfterOverdueDays);

/// <summary>What one pass of the commission job did.</summary>
public sealed record CommissionCycleResult(int StatementsIssued, int RemindersSent, int PartnersPaused, int PartnersResumed);
