using HomeServices.Application.Abstractions;
using HomeServices.Application.Partners;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Requests;

/// <summary>
/// Which partners can receive a request. The distribution rule: approved partners (owner not blocked) who
/// offer the request's category or its parent category, and work in its city — the whole city, or the
/// request's district. The customer's own profile never receives it.
/// </summary>
internal static class RequestMatching
{
    /// <summary>Partners who can receive requests at all.</summary>
    public static IQueryable<PartnerProfile> Available(IAppDbContext db) => PublicPartnerQueries.Visible(db);

    /// <summary>Partners who can take NEW requests: available and not paused for overdue commissions.</summary>
    public static IQueryable<PartnerProfile> Receiving(IAppDbContext db) => Available(db).Where(p => p.DebtPausedSince == null);

    /// <summary>
    /// Matching partners for <paramref name="request"/>, at most <paramref name="max"/> of them; when more match,
    /// a random selection, so new and established partners get a fair share.
    /// </summary>
    public static async Task<List<Guid>> MatchAsync(IAppDbContext db, ServiceRequest request, int max, CancellationToken cancellationToken)
    {
        var parentId = await db.Categories.AsNoTracking()
            .Where(c => c.Id == request.CategoryId)
            .Select(c => c.ParentId)
            .SingleOrDefaultAsync(cancellationToken);
        var categoryIds = parentId is { } parent ? new[] { request.CategoryId, parent } : new[] { request.CategoryId };
        var customerId = request.CustomerId;
        var cityId = request.CityId;
        var districtId = request.DistrictId;

        var matches = await Receiving(db)
            .Where(p => p.UserId != customerId)
            .Where(p => p.Services.Any(s => categoryIds.Contains(s.CategoryId)))
            .Where(p => p.Areas.Any(a => a.CityId == cityId && (districtId == null || a.DistrictId == null || a.DistrictId == districtId)))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        return matches.Count <= max ? matches : matches.OrderBy(_ => Random.Shared.Next()).Take(max).ToList();
    }
}
