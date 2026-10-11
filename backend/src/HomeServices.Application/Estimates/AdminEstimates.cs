using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Messaging;
using HomeServices.Application.Requests;
using HomeServices.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Estimates;

/// <summary>
/// Customers' saved estimates for the Back Office, last changed first. <see cref="Search"/> matches the title or the
/// owner's phone; <see cref="WithRequest"/> keeps the ones that did (true) or didn't (false) become a request.
/// </summary>
public sealed record GetAdminEstimates(
    string? Search = null,
    bool? WithRequest = null,
    bool? Shared = null,
    int Page = 1,
    int PageSize = GetAdminEstimates.DefaultPageSize) : IQuery<PagedResult<AdminEstimateListItemDto>>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

/// <summary>How many estimates there are and how many became requests (all time and in the last 30 days).</summary>
public sealed record GetEstimateStats : IQuery<EstimateStatsDto>;

/// <summary>One estimate with its owner, rooms, prices and the requests made from it.</summary>
public sealed record GetAdminEstimate(Guid Id) : IQuery<AdminEstimateDto>;

public sealed record EstimateOwnerDto(Guid UserId, string Phone, string? FullName);

public sealed record AdminEstimateListItemDto(
    Guid Id,
    string Title,
    EstimateOwnerDto? Owner,
    int RoomCount,
    int LineCount,
    int TotalMin,
    int TotalTypical,
    int TotalMax,
    bool IsShared,
    int RequestCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

/// <summary><see cref="ConversionPercent"/>: estimates that became at least one request, of all estimates (0–100).</summary>
public sealed record EstimateStatsDto(
    int Total,
    int WithRequest,
    int Shared,
    int ConversionPercent,
    int TotalLast30Days,
    int WithRequestLast30Days);

/// <summary>A request made from an estimate, as listed on the estimate.</summary>
public sealed record EstimateRequestDto(Guid Id, string Status, string CategoryName, int LineCount, DateTimeOffset CreatedAt);

public sealed record AdminEstimateDto(
    Guid Id,
    string Title,
    EstimateOwnerDto? Owner,
    Guid? CityId,
    bool OldBuilding,
    bool IsShared,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<EstimateRoomDto> Rooms,
    EstimateMeasurementDto Measurement,
    IReadOnlyList<EstimateRequestDto> Requests);

public sealed class GetAdminEstimatesValidator : AbstractValidator<GetAdminEstimates>
{
    public GetAdminEstimatesValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100).WithErrorCode("search.too_long");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetAdminEstimates.MaxPageSize).WithErrorCode("page_size.invalid");
    }
}

public sealed class GetAdminEstimatesHandler(IAppDbContext db, ICurrentLanguage language)
    : IQueryHandler<GetAdminEstimates, PagedResult<AdminEstimateListItemDto>>
{
    public async Task<PagedResult<AdminEstimateListItemDto>> HandleAsync(GetAdminEstimates query, CancellationToken cancellationToken)
    {
        var estimates = db.Estimates.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            if (PhoneNumber.TryParse(search, out var phone))
            {
                estimates = estimates.Where(e => db.Users.Any(u => u.Id == e.UserId && u.Phone == phone));
            }
            else
            {
                var lowered = search.ToLowerInvariant();
                estimates = estimates.Where(e => e.Title.ToLower().Contains(lowered));
            }
        }

        if (query.WithRequest is { } withRequest)
        {
            estimates = withRequest
                ? estimates.Where(e => db.ServiceRequests.Any(r => r.EstimateId == e.Id))
                : estimates.Where(e => !db.ServiceRequests.Any(r => r.EstimateId == e.Id));
        }

        if (query.Shared is { } shared)
        {
            estimates = shared ? estimates.Where(e => e.ShareToken != null) : estimates.Where(e => e.ShareToken == null);
        }

        var total = await estimates.CountAsync(cancellationToken);
        var page = await MyEstimateData.WithRooms(estimates)
            .OrderByDescending(e => e.UpdatedAt ?? e.CreatedAt).ThenByDescending(e => e.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var owners = await AdminEstimateData.OwnersAsync(db, page.Select(e => e.UserId), cancellationToken);
        var ids = page.Select(e => e.Id).ToList();
        var requestCounts = await db.ServiceRequests.AsNoTracking()
            .Where(r => r.EstimateId != null && ids.Contains(r.EstimateId.Value))
            .GroupBy(r => r.EstimateId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);

        var items = new List<AdminEstimateListItemDto>(page.Count);
        foreach (var estimate in page)
        {
            var measurement = await MyEstimateData.MeasureAsync(db, language, estimate, cancellationToken);
            items.Add(new AdminEstimateListItemDto(
                estimate.Id,
                estimate.Title,
                AdminEstimateData.Owner(owners, estimate.UserId),
                estimate.Rooms.Count,
                estimate.Rooms.Sum(r => r.Lines.Count),
                measurement.TotalMin,
                measurement.TotalTypical,
                measurement.TotalMax,
                estimate.ShareToken is not null,
                requestCounts.GetValueOrDefault(estimate.Id),
                estimate.CreatedAt,
                estimate.UpdatedAt));
        }

        return new PagedResult<AdminEstimateListItemDto>(items, query.Page, query.PageSize, total);
    }
}

public sealed class GetEstimateStatsHandler(IAppDbContext db, TimeProvider clock) : IQueryHandler<GetEstimateStats, EstimateStatsDto>
{
    public async Task<EstimateStatsDto> HandleAsync(GetEstimateStats query, CancellationToken cancellationToken)
    {
        var since = clock.GetUtcNow().AddDays(-30);
        var estimates = db.Estimates.AsNoTracking();
        var converted = estimates.Where(e => db.ServiceRequests.Any(r => r.EstimateId == e.Id));

        var total = await estimates.CountAsync(cancellationToken);
        var withRequest = await converted.CountAsync(cancellationToken);
        return new EstimateStatsDto(
            total,
            withRequest,
            await estimates.CountAsync(e => e.ShareToken != null, cancellationToken),
            total == 0 ? 0 : (int)Math.Round(withRequest * 100m / total, MidpointRounding.AwayFromZero),
            await estimates.CountAsync(e => e.CreatedAt >= since, cancellationToken),
            await converted.CountAsync(e => e.CreatedAt >= since, cancellationToken));
    }
}

public sealed class GetAdminEstimateHandler(IAppDbContext db, ICurrentLanguage language) : IQueryHandler<GetAdminEstimate, AdminEstimateDto>
{
    public async Task<AdminEstimateDto> HandleAsync(GetAdminEstimate query, CancellationToken cancellationToken)
    {
        var estimate = await MyEstimateData.WithRooms(db.Estimates.AsNoTracking()).SingleOrDefaultAsync(e => e.Id == query.Id, cancellationToken)
            ?? throw new NotFoundException("This estimate doesn't exist.", "estimate.not_found");
        var dto = await MyEstimateData.ToDtoAsync(db, language, estimate, cancellationToken);
        var owners = await AdminEstimateData.OwnersAsync(db, [estimate.UserId], cancellationToken);

        var requests = await db.ServiceRequests.AsNoTracking()
            .Where(r => r.EstimateId == estimate.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new { Request = r, LineCount = r.Lines.Count })
            .ToListAsync(cancellationToken);
        var lookup = await RequestLookup.LoadAsync(db, language, requests.Select(r => r.Request), cancellationToken);

        return new AdminEstimateDto(
            dto.Id,
            dto.Title,
            AdminEstimateData.Owner(owners, estimate.UserId),
            dto.CityId,
            dto.OldBuilding,
            dto.ShareToken is not null,
            dto.CreatedAt,
            dto.UpdatedAt,
            dto.Rooms,
            dto.Measurement,
            requests
                .Select(r => new EstimateRequestDto(r.Request.Id, r.Request.Status.ToString(), lookup.Place(r.Request).CategoryName, r.LineCount, r.Request.CreatedAt))
                .ToList());
    }
}

internal static class AdminEstimateData
{
    public static async Task<Dictionary<Guid, EstimateOwnerDto>> OwnersAsync(IAppDbContext db, IEnumerable<Guid?> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Where(id => id is not null).Select(id => id!.Value).Distinct().ToList();
        var users = await db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.Phone, u.FullName })
            .ToListAsync(cancellationToken);
        return users.ToDictionary(u => u.Id, u => new EstimateOwnerDto(u.Id, u.Phone.Value, u.FullName));
    }

    public static EstimateOwnerDto? Owner(Dictionary<Guid, EstimateOwnerDto> owners, Guid? userId) =>
        userId is { } id ? owners.GetValueOrDefault(id) : null;
}
