using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Auditing;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Auditing;

/// <summary>Audit log entries, newest first, optionally filtered. Every filter is optional.</summary>
public sealed record GetAuditLog(
    string? EntityType = null,
    string? EntityId = null,
    string? ActorId = null,
    AuditAction? Action = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Page = 1,
    int PageSize = GetAuditLog.DefaultPageSize) : IQuery<PagedResult<AuditLogEntryDto>>
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;
}

public sealed record AuditChangeDto(string Property, string? OldValue, string? NewValue);

/// <summary>An audit entry. <see cref="ActorName"/> is the staff member's or user's name when known.</summary>
public sealed record AuditLogEntryDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    string ActorType,
    string? ActorId,
    string? ActorName,
    string Action,
    string EntityType,
    string EntityId,
    IReadOnlyList<AuditChangeDto> Changes,
    string? TraceId);

public sealed class GetAuditLogValidator : AbstractValidator<GetAuditLog>
{
    public GetAuditLogValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetAuditLog.MaxPageSize).WithErrorCode("page_size.invalid");
        RuleFor(x => x.Action).IsInEnum().WithErrorCode("action.invalid");
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From).WithErrorCode("range.invalid")
            .When(x => x.From is not null && x.To is not null);
    }
}

public sealed class GetAuditLogHandler(IAppDbContext db) : IQueryHandler<GetAuditLog, PagedResult<AuditLogEntryDto>>
{
    public async Task<PagedResult<AuditLogEntryDto>> HandleAsync(GetAuditLog query, CancellationToken cancellationToken)
    {
        var entries = db.AuditLog.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            entries = entries.Where(e => e.EntityType == query.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            entries = entries.Where(e => e.EntityId == query.EntityId);
        }

        if (!string.IsNullOrWhiteSpace(query.ActorId))
        {
            entries = entries.Where(e => e.ActorId == query.ActorId);
        }

        if (query.Action is { } action)
        {
            entries = entries.Where(e => e.Action == action);
        }

        if (query.From is { } from)
        {
            entries = entries.Where(e => e.OccurredAt >= from);
        }

        if (query.To is { } to)
        {
            entries = entries.Where(e => e.OccurredAt <= to);
        }

        var total = await entries.CountAsync(cancellationToken);
        var page = await entries
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var names = await LoadActorNamesAsync(page, cancellationToken);
        var items = page.Select(e => new AuditLogEntryDto(
                e.Id,
                e.OccurredAt,
                e.ActorType.ToString(),
                e.ActorId,
                e.ActorId is not null && names.TryGetValue(e.ActorId, out var name) ? name : null,
                e.Action.ToString(),
                e.EntityType,
                e.EntityId,
                e.Changes.Select(c => new AuditChangeDto(c.Property, c.OldValue, c.NewValue)).ToList(),
                e.TraceId))
            .ToList();

        return new PagedResult<AuditLogEntryDto>(items, query.Page, query.PageSize, total);
    }

    private async Task<Dictionary<string, string>> LoadActorNamesAsync(List<AuditLogEntry> page, CancellationToken cancellationToken)
    {
        var staffIds = ActorIds(page, AuditActorType.Staff);
        var userIds = ActorIds(page, AuditActorType.User);
        var names = new Dictionary<string, string>();

        if (staffIds.Count > 0)
        {
            var staff = await db.StaffUsers.AsNoTracking()
                .Where(s => staffIds.Contains(s.Id))
                .Select(s => new { s.Id, s.FullName })
                .ToListAsync(cancellationToken);
            staff.ForEach(s => names[s.Id.ToString()] = s.FullName);
        }

        if (userIds.Count > 0)
        {
            var users = await db.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id) && u.FullName != null)
                .Select(u => new { u.Id, u.FullName })
                .ToListAsync(cancellationToken);
            users.ForEach(u => names[u.Id.ToString()] = u.FullName!);
        }

        return names;
    }

    private static List<Guid> ActorIds(List<AuditLogEntry> page, AuditActorType type) =>
        page.Where(e => e.ActorType == type)
            .Select(e => Guid.TryParse(e.ActorId, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
}
