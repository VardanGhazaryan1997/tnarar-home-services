using System.Globalization;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Notifications;
using HomeServices.Application.Orders;
using HomeServices.Domain.Commissions;
using HomeServices.Domain.Notifications;
using HomeServices.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Commissions;

/// <summary>Rates, charging completed orders, and turning stored rows into DTOs.</summary>
internal static class CommissionLogic
{
    public const string PortalLink = "/commissions";

    public static NotFoundException StatementNotFound() => new("Statement not found.", "commission.statement_not_found");

    /// <summary>The rate for a category: its own, else its parent's, else the default.</summary>
    public static async Task<decimal> RateForAsync(IAppDbContext db, Guid categoryId, CancellationToken cancellationToken)
    {
        var rates = await db.CommissionRates.AsNoTracking().ToListAsync(cancellationToken);
        var parentId = await db.Categories.IgnoreQueryFilters().AsNoTracking()
            .Where(c => c.Id == categoryId)
            .Select(c => c.ParentId)
            .SingleOrDefaultAsync(cancellationToken);
        return Effective(rates, categoryId, parentId);
    }

    public static decimal Effective(IReadOnlyCollection<CommissionRate> rates, Guid categoryId, Guid? parentId) =>
        rates.FirstOrDefault(r => r.CategoryId == categoryId)?.Percent
        ?? (parentId is { } parent ? rates.FirstOrDefault(r => r.CategoryId == parent)?.Percent : null)
        ?? rates.FirstOrDefault(r => r.CategoryId == null)?.Percent
        ?? 0m;

    /// <summary>
    /// Charges a just-completed order: final price × the rate of the request's category, frozen now. Free orders and
    /// orders already charged owe nothing more. Added to the context; saved with the completion.
    /// </summary>
    public static async Task ChargeAsync(IAppDbContext db, Order order, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (order.Status != OrderStatus.Completed || order.Price <= 0
            || await db.CommissionObligations.AnyAsync(o => o.OrderId == order.Id, cancellationToken)
            || db.CommissionObligations.Local.Any(o => o.OrderId == order.Id))
        {
            return;
        }

        var categoryId = await db.ServiceRequests.AsNoTracking().Where(r => r.Id == order.RequestId).Select(r => r.CategoryId).SingleAsync(cancellationToken);
        var rate = await RateForAsync(db, categoryId, cancellationToken);
        var obligation = CommissionObligation.For(order.Id, order.PartnerProfileId, categoryId, order.Price, rate, order.CompletedAt ?? now);
        if (obligation.Amount > 0)
        {
            db.CommissionObligations.Add(obligation);
        }
    }

    public static StatementDto ToDto(CommissionStatement s, DateOnly today) => new(
        s.Id, s.PeriodStart, s.PeriodEnd, s.Total, s.PaidAmount, s.Outstanding, s.Status.ToString(), s.IsOverdue(today), s.DueOn, s.IssuedAt, s.PaidAt);

    public static SettlementDto ToDto(Settlement s) => new(s.Id, s.Amount, s.Method.ToString(), s.PaidOn, s.Reference, s.RecordedAt);

    /// <summary>Obligations as lines, with each order's agreed summary.</summary>
    public static async Task<List<CommissionLineDto>> LinesAsync(IAppDbContext db, IReadOnlyList<CommissionObligation> obligations, CancellationToken cancellationToken)
    {
        var orderIds = obligations.Select(o => o.OrderId).ToList();
        var orders = await db.Orders.AsNoTracking().Where(o => orderIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, cancellationToken);
        return obligations
            .Select(o => new CommissionLineDto(
                o.Id,
                o.OrderId,
                orders.TryGetValue(o.OrderId, out var order) ? OrderViews.ReadTerms(order).Summary : string.Empty,
                o.CompletedAt,
                o.OrderPrice,
                o.RatePercent,
                o.Amount,
                o.StatementId))
            .ToList();
    }

    public static async Task<(List<CommissionLineDto> Lines, List<SettlementDto> Settlements)> DetailAsync(
        IAppDbContext db, Guid statementId, CancellationToken cancellationToken)
    {
        var obligations = await db.CommissionObligations.AsNoTracking()
            .Where(o => o.StatementId == statementId)
            .OrderBy(o => o.CompletedAt).ThenBy(o => o.Id)
            .ToListAsync(cancellationToken);
        var settlements = await db.Settlements.AsNoTracking()
            .Where(s => s.StatementId == statementId)
            .OrderBy(s => s.RecordedAt).ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);
        return (await LinesAsync(db, obligations, cancellationToken), settlements.Select(ToDto).ToList());
    }

    public static string Money(int amount) => amount.ToString(CultureInfo.InvariantCulture);

    /// <summary>Tells the owner of a partner profile.</summary>
    public static async Task NotifyPartnerAsync(
        IAppDbContext db, Guid partnerId, NotificationType type, IReadOnlyDictionary<string, string?>? values, DateTimeOffset now, CancellationToken cancellationToken) =>
        await Notifier.ToPartnersAsync(db, [partnerId], type, _ => PortalLink, values, now, cancellationToken);

    /// <summary>
    /// Pauses or resumes one partner's new requests from their open statements: paused while any is overdue by more
    /// than the allowed days. Returns +1 paused, -1 resumed, 0 unchanged.
    /// </summary>
    public static async Task<int> ReviewPauseAsync(IAppDbContext db, Guid partnerId, CommissionSettings settings, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var cutoff = settings.LocalDate(now).AddDays(-settings.PauseAfterOverdueDays);
        var longOverdue = await db.CommissionStatements
            .AnyAsync(s => s.PartnerProfileId == partnerId && s.Status == StatementStatus.Open && s.DueOn < cutoff, cancellationToken);
        var partner = await db.PartnerProfiles.SingleAsync(p => p.Id == partnerId, cancellationToken);
        if (longOverdue && partner.PauseForDebt(now))
        {
            await NotifyPartnerAsync(db, partnerId, NotificationType.PartnerPausedForDebt, null, now, cancellationToken);
            return 1;
        }

        if (!longOverdue && partner.ResumeAfterDebt())
        {
            await NotifyPartnerAsync(db, partnerId, NotificationType.PartnerResumed, null, now, cancellationToken);
            return -1;
        }

        return 0;
    }
}
