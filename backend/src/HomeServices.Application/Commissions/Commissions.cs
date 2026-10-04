using System.Globalization;
using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Messaging;
using HomeServices.Application.Orders;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Commissions;
using HomeServices.Domain.Notifications;
using HomeServices.Domain.Partners;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Commissions;

/// <summary>The default rate and every category's rate, in the request language.</summary>
public sealed record GetCommissionRates : IQuery<CommissionRatesDto>;

public sealed record SetDefaultCommissionRate(decimal Percent) : ICommand<CommissionRatesDto>;

/// <summary>Gives a category its own rate; null removes it (the parent's or the default applies again).</summary>
public sealed record SetCategoryCommissionRate(Guid CategoryId, decimal? Percent) : ICommand<CommissionRatesDto>;

public enum StatementFilter
{
    Open = 1,
    Overdue = 2,
    Paid = 3,
}

/// <summary>Statements for the Back Office: open (oldest due first), overdue, paid, or all (newest first).</summary>
public sealed record GetAdminStatements(StatementFilter? Filter = null, Guid? PartnerId = null, int Page = 1, int PageSize = GetAdminStatements.DefaultPageSize)
    : IQuery<PagedResult<AdminStatementDto>>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

public sealed record GetAdminStatement(Guid Id) : IQuery<AdminStatementDetailDto>;

/// <summary>Staff record a payment the partner made toward a statement.</summary>
public sealed record RecordSettlement(Guid StatementId, int Amount, SettlementMethod Method, DateOnly PaidOn, string? Reference) : ICommand<AdminStatementDetailDto>;

/// <summary>The weekly job: issues last weeks' statements, reminds overdue partners, pauses and resumes requests.</summary>
public sealed record RunCommissionCycle : ICommand<CommissionCycleResult>;

/// <summary>The signed-in partner's commission position.</summary>
public sealed record GetMyCommissionSummary : IQuery<CommissionSummaryDto>;

/// <summary>The signed-in partner's charged orders, newest first; <see cref="Unbilled"/> keeps those not on a statement yet.</summary>
public sealed record GetMyCommissions(bool Unbilled = false, int Page = 1, int PageSize = GetMyCommissions.DefaultPageSize) : IQuery<PagedResult<CommissionLineDto>>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

public sealed record GetMyStatements(int Page = 1, int PageSize = GetMyStatements.DefaultPageSize) : IQuery<PagedResult<StatementDto>>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

public sealed record GetMyStatement(Guid Id) : IQuery<StatementDetailDto>;

internal static class PercentRules
{
    public static IRuleBuilderOptions<T, decimal> ValidPercent<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.InclusiveBetween(0m, CommissionRate.MaxPercent).WithErrorCode("percent.invalid")
            .Must(value => decimal.Round(value, 2) == value).WithErrorCode("percent.invalid");
}

public sealed class SetDefaultCommissionRateValidator : AbstractValidator<SetDefaultCommissionRate>
{
    public SetDefaultCommissionRateValidator() => RuleFor(x => x.Percent).ValidPercent();
}

public sealed class SetCategoryCommissionRateValidator : AbstractValidator<SetCategoryCommissionRate>
{
    public SetCategoryCommissionRateValidator() =>
        RuleFor(x => x.Percent!.Value).ValidPercent().OverridePropertyName(nameof(SetCategoryCommissionRate.Percent)).When(x => x.Percent is not null);
}

public sealed class GetAdminStatementsValidator : AbstractValidator<GetAdminStatements>
{
    public GetAdminStatementsValidator()
    {
        RuleFor(x => x.Filter).IsInEnum().WithErrorCode("filter.invalid");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetAdminStatements.MaxPageSize).WithErrorCode("page_size.invalid");
    }
}

public sealed class RecordSettlementValidator : AbstractValidator<RecordSettlement>
{
    public RecordSettlementValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithErrorCode("amount.invalid");
        RuleFor(x => x.Method).IsInEnum().WithErrorCode("method.invalid");
        RuleFor(x => x.Reference)
            .Must(value => value!.Trim().Length <= Settlement.ReferenceMaxLength).WithErrorCode("reference.too_long")
            .When(x => x.Reference is not null);
    }
}

public sealed class GetMyCommissionsValidator : AbstractValidator<GetMyCommissions>
{
    public GetMyCommissionsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetMyCommissions.MaxPageSize).WithErrorCode("page_size.invalid");
    }
}

public sealed class GetMyStatementsValidator : AbstractValidator<GetMyStatements>
{
    public GetMyStatementsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetMyStatements.MaxPageSize).WithErrorCode("page_size.invalid");
    }
}

internal static class RateViews
{
    public static async Task<CommissionRatesDto> LoadAsync(IAppDbContext db, ICurrentLanguage language, CancellationToken cancellationToken)
    {
        var rates = await db.CommissionRates.AsNoTracking().ToListAsync(cancellationToken);
        var categories = await db.Categories.AsNoTracking().OrderBy(c => c.SortOrder).ToListAsync(cancellationToken);
        var byParent = categories.ToLookup(c => c.ParentId);
        var items = new List<CategoryRateDto>();
        foreach (var top in byParent[null])
        {
            items.Add(Row(top));
            items.AddRange(byParent[top.Id].Select(Row));
        }

        return new CommissionRatesDto(rates.FirstOrDefault(r => r.CategoryId == null)?.Percent ?? 0m, items);

        CategoryRateDto Row(Category c) => new(
            c.Id,
            c.Name.Get(language.Code, language.DefaultCode),
            c.ParentId,
            rates.FirstOrDefault(r => r.CategoryId == c.Id)?.Percent,
            CommissionLogic.Effective(rates, c.Id, c.ParentId));
    }
}

public sealed class GetCommissionRatesHandler(IAppDbContext db, ICurrentLanguage language) : IQueryHandler<GetCommissionRates, CommissionRatesDto>
{
    public Task<CommissionRatesDto> HandleAsync(GetCommissionRates query, CancellationToken cancellationToken) => RateViews.LoadAsync(db, language, cancellationToken);
}

public sealed class SetDefaultCommissionRateHandler(IAppDbContext db, ICurrentLanguage language) : ICommandHandler<SetDefaultCommissionRate, CommissionRatesDto>
{
    public async Task<CommissionRatesDto> HandleAsync(SetDefaultCommissionRate command, CancellationToken cancellationToken)
    {
        var rate = await db.CommissionRates.SingleOrDefaultAsync(r => r.CategoryId == null, cancellationToken);
        if (rate is null)
        {
            db.CommissionRates.Add(CommissionRate.Default(command.Percent));
        }
        else
        {
            rate.Change(command.Percent);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await RateViews.LoadAsync(db, language, cancellationToken);
    }
}

public sealed class SetCategoryCommissionRateHandler(IAppDbContext db, ICurrentLanguage language) : ICommandHandler<SetCategoryCommissionRate, CommissionRatesDto>
{
    public async Task<CommissionRatesDto> HandleAsync(SetCategoryCommissionRate command, CancellationToken cancellationToken)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == command.CategoryId, cancellationToken))
        {
            throw new NotFoundException("Category not found.", "category.not_found");
        }

        var rate = await db.CommissionRates.SingleOrDefaultAsync(r => r.CategoryId == command.CategoryId, cancellationToken);
        if (command.Percent is { } percent)
        {
            if (rate is null)
            {
                db.CommissionRates.Add(CommissionRate.ForCategory(command.CategoryId, percent));
            }
            else
            {
                rate.Change(percent);
            }
        }
        else if (rate is not null)
        {
            db.CommissionRates.Remove(rate);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await RateViews.LoadAsync(db, language, cancellationToken);
    }
}

internal static class AdminStatements
{
    public static async Task<List<AdminStatementDto>> RowsAsync(IAppDbContext db, IReadOnlyList<CommissionStatement> statements, DateOnly today, CancellationToken cancellationToken)
    {
        var partnerIds = statements.Select(s => s.PartnerProfileId).Distinct().ToList();
        var partners = await (
            from partner in db.PartnerProfiles.AsNoTracking()
            join user in db.Users.AsNoTracking() on partner.UserId equals user.Id
            where partnerIds.Contains(partner.Id)
            select new { partner.Id, partner.DisplayName, user.Phone, partner.DebtPausedSince })
            .ToListAsync(cancellationToken);
        var byId = partners.ToDictionary(p => p.Id);
        return statements
            .Select(s => new AdminStatementDto(
                CommissionLogic.ToDto(s, today),
                s.PartnerProfileId,
                byId[s.PartnerProfileId].DisplayName,
                byId[s.PartnerProfileId].Phone.Value,
                byId[s.PartnerProfileId].DebtPausedSince is not null))
            .ToList();
    }

    public static async Task<AdminStatementDetailDto> DetailAsync(IAppDbContext db, CommissionStatement statement, DateOnly today, CancellationToken cancellationToken)
    {
        var row = (await RowsAsync(db, [statement], today, cancellationToken))[0];
        var (lines, settlements) = await CommissionLogic.DetailAsync(db, statement.Id, cancellationToken);
        return new AdminStatementDetailDto(row, lines, settlements);
    }
}

public sealed class GetAdminStatementsHandler(IAppDbContext db, IOptions<CommissionSettings> settings, TimeProvider clock)
    : IQueryHandler<GetAdminStatements, PagedResult<AdminStatementDto>>
{
    public async Task<PagedResult<AdminStatementDto>> HandleAsync(GetAdminStatements query, CancellationToken cancellationToken)
    {
        var today = settings.Value.LocalDate(clock.GetUtcNow());
        var statements = db.CommissionStatements.AsNoTracking();
        if (query.PartnerId is { } partnerId)
        {
            statements = statements.Where(s => s.PartnerProfileId == partnerId);
        }

        statements = query.Filter switch
        {
            StatementFilter.Open => statements.Where(s => s.Status == StatementStatus.Open).OrderBy(s => s.DueOn).ThenBy(s => s.Id),
            StatementFilter.Overdue => statements.Where(s => s.Status == StatementStatus.Open && s.DueOn < today).OrderBy(s => s.DueOn).ThenBy(s => s.Id),
            StatementFilter.Paid => statements.Where(s => s.Status == StatementStatus.Paid).OrderByDescending(s => s.PaidAt).ThenByDescending(s => s.Id),
            _ => statements.OrderByDescending(s => s.IssuedAt).ThenByDescending(s => s.Id),
        };

        var total = await statements.CountAsync(cancellationToken);
        var page = await statements.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<AdminStatementDto>(await AdminStatements.RowsAsync(db, page, today, cancellationToken), query.Page, query.PageSize, total);
    }
}

public sealed class GetAdminStatementHandler(IAppDbContext db, IOptions<CommissionSettings> settings, TimeProvider clock)
    : IQueryHandler<GetAdminStatement, AdminStatementDetailDto>
{
    public async Task<AdminStatementDetailDto> HandleAsync(GetAdminStatement query, CancellationToken cancellationToken)
    {
        var statement = await db.CommissionStatements.AsNoTracking().SingleOrDefaultAsync(s => s.Id == query.Id, cancellationToken)
            ?? throw CommissionLogic.StatementNotFound();
        return await AdminStatements.DetailAsync(db, statement, settings.Value.LocalDate(clock.GetUtcNow()), cancellationToken);
    }
}

public sealed class RecordSettlementHandler(IAppDbContext db, IOptions<CommissionSettings> settings, TimeProvider clock)
    : ICommandHandler<RecordSettlement, AdminStatementDetailDto>
{
    public async Task<AdminStatementDetailDto> HandleAsync(RecordSettlement command, CancellationToken cancellationToken)
    {
        var statement = await db.CommissionStatements.SingleOrDefaultAsync(s => s.Id == command.StatementId, cancellationToken)
            ?? throw CommissionLogic.StatementNotFound();
        var now = clock.GetUtcNow();
        db.Settlements.Add(statement.Settle(command.Amount, command.Method, command.PaidOn, command.Reference, now));
        await CommissionLogic.NotifyPartnerAsync(
            db,
            statement.PartnerProfileId,
            NotificationType.CommissionSettled,
            new Dictionary<string, string?> { ["amount"] = CommissionLogic.Money(command.Amount), ["outstanding"] = CommissionLogic.Money(statement.Outstanding) },
            now,
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        // Paying the overdue amount lets new requests through again.
        if (await CommissionLogic.ReviewPauseAsync(db, statement.PartnerProfileId, settings.Value, now, cancellationToken) != 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return await AdminStatements.DetailAsync(db, statement, settings.Value.LocalDate(now), cancellationToken);
    }
}

public sealed class RunCommissionCycleHandler(IAppDbContext db, IOptions<CommissionSettings> settings, TimeProvider clock)
    : ICommandHandler<RunCommissionCycle, CommissionCycleResult>
{
    public async Task<CommissionCycleResult> HandleAsync(RunCommissionCycle command, CancellationToken cancellationToken)
    {
        var options = settings.Value;
        var now = clock.GetUtcNow();
        var today = options.LocalDate(now);
        var thisWeek = CommissionSettings.WeekStart(today);

        // 1. Statements for finished weeks: one per partner and week.
        var unbilled = await db.CommissionObligations.Where(o => o.StatementId == null).ToListAsync(cancellationToken);
        var due = unbilled
            .Select(o => (Obligation: o, Week: CommissionSettings.WeekStart(options.LocalDate(o.CompletedAt))))
            .Where(x => x.Week < thisWeek)
            .GroupBy(x => (x.Obligation.PartnerProfileId, x.Week))
            .ToList();
        foreach (var group in due)
        {
            var statement = CommissionStatement.Issue(group.Key.PartnerProfileId, group.Key.Week, group.Select(x => x.Obligation).ToList(), today.AddDays(options.DueDays), now);
            db.CommissionStatements.Add(statement);
            await CommissionLogic.NotifyPartnerAsync(
                db,
                statement.PartnerProfileId,
                NotificationType.CommissionStatementIssued,
                new Dictionary<string, string?>
                {
                    ["amount"] = CommissionLogic.Money(statement.Total),
                    ["dueOn"] = statement.DueOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                },
                now,
                cancellationToken);
        }

        // 2. Remind once when a statement becomes overdue.
        var overdue = await db.CommissionStatements.Where(s => s.Status == StatementStatus.Open && s.DueOn < today && s.OverdueReminderAt == null).ToListAsync(cancellationToken);
        foreach (var statement in overdue)
        {
            statement.MarkOverdueReminded(now);
            await CommissionLogic.NotifyPartnerAsync(
                db, statement.PartnerProfileId, NotificationType.CommissionOverdue, new Dictionary<string, string?> { ["amount"] = CommissionLogic.Money(statement.Outstanding) }, now, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        // 3. Pause partners overdue for too long; resume the ones who paid.
        var cutoff = today.AddDays(-options.PauseAfterOverdueDays);
        var toReview = await db.CommissionStatements
            .Where(s => s.Status == StatementStatus.Open && s.DueOn < cutoff)
            .Select(s => s.PartnerProfileId)
            .Union(db.PartnerProfiles.Where(p => p.DebtPausedSince != null).Select(p => p.Id))
            .Distinct()
            .ToListAsync(cancellationToken);
        int paused = 0, resumed = 0;
        foreach (var partnerId in toReview)
        {
            var change = await CommissionLogic.ReviewPauseAsync(db, partnerId, options, now, cancellationToken);
            paused += change > 0 ? 1 : 0;
            resumed += change < 0 ? 1 : 0;
        }

        if (paused + resumed > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return new CommissionCycleResult(due.Count, overdue.Count, paused, resumed);
    }
}

/// <summary>The signed-in partner's profile id; anyone without one gets "not found".</summary>
internal static class MyCommissions
{
    public static async Task<PartnerProfile> PartnerAsync(IAppDbContext db, ICurrentUser currentUser, CancellationToken cancellationToken)
    {
        var (_, partnerId) = await OrderViews.MeAsync(db, currentUser, cancellationToken);
        return partnerId is { } id
            ? await db.PartnerProfiles.AsNoTracking().SingleAsync(p => p.Id == id, cancellationToken)
            : throw new NotFoundException("You don't have a partner profile.", "partner.not_found");
    }
}

public sealed class GetMyCommissionSummaryHandler(IAppDbContext db, ICurrentUser currentUser, IOptions<CommissionSettings> settings, TimeProvider clock)
    : IQueryHandler<GetMyCommissionSummary, CommissionSummaryDto>
{
    public async Task<CommissionSummaryDto> HandleAsync(GetMyCommissionSummary query, CancellationToken cancellationToken)
    {
        var partner = await MyCommissions.PartnerAsync(db, currentUser, cancellationToken);
        var today = settings.Value.LocalDate(clock.GetUtcNow());
        var unbilled = await db.CommissionObligations.Where(o => o.PartnerProfileId == partner.Id && o.StatementId == null).SumAsync(o => o.Amount, cancellationToken);
        var open = await db.CommissionStatements.AsNoTracking()
            .Where(s => s.PartnerProfileId == partner.Id && s.Status == StatementStatus.Open)
            .ToListAsync(cancellationToken);
        return new CommissionSummaryDto(
            unbilled,
            open.Sum(s => s.Outstanding),
            open.Where(s => s.IsOverdue(today)).Sum(s => s.Outstanding),
            open.Count == 0 ? null : open.Min(s => s.DueOn),
            partner.DebtPausedSince,
            settings.Value.PauseAfterOverdueDays);
    }
}

public sealed class GetMyCommissionsHandler(IAppDbContext db, ICurrentUser currentUser) : IQueryHandler<GetMyCommissions, PagedResult<CommissionLineDto>>
{
    public async Task<PagedResult<CommissionLineDto>> HandleAsync(GetMyCommissions query, CancellationToken cancellationToken)
    {
        var partner = await MyCommissions.PartnerAsync(db, currentUser, cancellationToken);
        var lines = db.CommissionObligations.AsNoTracking().Where(o => o.PartnerProfileId == partner.Id);
        if (query.Unbilled)
        {
            lines = lines.Where(o => o.StatementId == null);
        }

        var total = await lines.CountAsync(cancellationToken);
        var page = await lines.OrderByDescending(o => o.CompletedAt).ThenByDescending(o => o.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<CommissionLineDto>(await CommissionLogic.LinesAsync(db, page, cancellationToken), query.Page, query.PageSize, total);
    }
}

public sealed class GetMyStatementsHandler(IAppDbContext db, ICurrentUser currentUser, IOptions<CommissionSettings> settings, TimeProvider clock)
    : IQueryHandler<GetMyStatements, PagedResult<StatementDto>>
{
    public async Task<PagedResult<StatementDto>> HandleAsync(GetMyStatements query, CancellationToken cancellationToken)
    {
        var partner = await MyCommissions.PartnerAsync(db, currentUser, cancellationToken);
        var today = settings.Value.LocalDate(clock.GetUtcNow());
        var statements = db.CommissionStatements.AsNoTracking().Where(s => s.PartnerProfileId == partner.Id);
        var total = await statements.CountAsync(cancellationToken);
        var page = await statements.OrderByDescending(s => s.PeriodStart).ThenByDescending(s => s.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<StatementDto>(page.Select(s => CommissionLogic.ToDto(s, today)).ToList(), query.Page, query.PageSize, total);
    }
}

public sealed class GetMyStatementHandler(IAppDbContext db, ICurrentUser currentUser, IOptions<CommissionSettings> settings, TimeProvider clock)
    : IQueryHandler<GetMyStatement, StatementDetailDto>
{
    public async Task<StatementDetailDto> HandleAsync(GetMyStatement query, CancellationToken cancellationToken)
    {
        var partner = await MyCommissions.PartnerAsync(db, currentUser, cancellationToken);
        var statement = await db.CommissionStatements.AsNoTracking().SingleOrDefaultAsync(s => s.Id == query.Id && s.PartnerProfileId == partner.Id, cancellationToken)
            ?? throw CommissionLogic.StatementNotFound();
        var (lines, settlements) = await CommissionLogic.DetailAsync(db, statement.Id, cancellationToken);
        return new StatementDetailDto(CommissionLogic.ToDto(statement, settings.Value.LocalDate(clock.GetUtcNow())), lines, settlements);
    }
}
