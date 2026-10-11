using HomeServices.Application.Abstractions;
using HomeServices.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Catalog;

/// <summary>
/// Reads regional market ranges (C2). A work item has one in a region once enough partners working there priced it
/// (see <see cref="MarketPrices"/>); otherwise the national range applies.
/// </summary>
public static class RegionalPrices
{
    /// <summary>The region with this slug, or null for none or an unknown one (the national ranges apply).</summary>
    public static async Task<Guid?> RegionIdAsync(IAppDbContext db, string? slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var wanted = slug.Trim().ToLowerInvariant();
        return await db.Regions.AsNoTracking().Where(r => r.Slug == wanted).Select(r => (Guid?)r.Id).SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>The region of a town or village, or null when it has none (or there's no town).</summary>
    public static async Task<Guid?> RegionOfCityAsync(IAppDbContext db, Guid? cityId, CancellationToken cancellationToken) =>
        cityId is { } id
            ? await db.Cities.AsNoTracking().Where(c => c.Id == id).Select(c => c.RegionId).SingleOrDefaultAsync(cancellationToken)
            : null;

    /// <summary>The regional ranges of these work items in the region, by work item; empty without a region.</summary>
    public static async Task<Dictionary<Guid, RegionalMarketPrice>> LoadAsync(
        IAppDbContext db, Guid? regionId, IEnumerable<Guid> workItemIds, CancellationToken cancellationToken)
    {
        if (regionId is not { } region)
        {
            return [];
        }

        var ids = workItemIds.Distinct().ToList();
        return await db.RegionalMarketPrices.AsNoTracking()
            .Where(p => p.RegionId == region && ids.Contains(p.WorkItemId))
            .ToDictionaryAsync(p => p.WorkItemId, cancellationToken);
    }
}
