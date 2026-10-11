using HomeServices.Application.Abstractions;
using HomeServices.Application.Messaging;
using HomeServices.Application.Partners;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Offers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Catalog;

/// <summary>Recalculates every work item's market range (the background job runs it; prices also update as they change).</summary>
public sealed record RecalculateMarketPrices : ICommand<MarketPricesResult>;

/// <summary>How many work items the pass looked at and how many now use partners' prices.</summary>
public sealed record MarketPricesResult(int WorkItems, int FromPartners);

public sealed class RecalculateMarketPricesHandler(IAppDbContext db, IOptions<PricingSettings> settings, TimeProvider clock)
    : ICommandHandler<RecalculateMarketPrices, MarketPricesResult>
{
    public async Task<MarketPricesResult> HandleAsync(RecalculateMarketPrices command, CancellationToken cancellationToken)
    {
        var items = await MarketPrices.RecalculateAsync(db, settings.Value, clock, workItemIds: null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new MarketPricesResult(items.Count, items.Count(i => i.MarketSource == MarketPriceSource.Partners));
    }
}

/// <summary>
/// The market range of a work item from partners' prices: the middle half of their prices (25th–75th percentile) with
/// the median as the usual price, so a few very cheap or very expensive prices don't move it. Counted:
/// <list type="bullet">
/// <item>approved, visible partners' labour-only prices confirmed in the last <see cref="PricingSettings.PriceFreshDays"/>
/// days (C3); a from–to range counts as its middle;</item>
/// <item>the line prices of work offers customers accepted in the last <see cref="PricingSettings.AcceptedOfferDays"/>
/// days without materials (C1): real deals, so they count <see cref="PricingSettings.AcceptedOfferWeight"/> times; one
/// partner's deals for a work item count as one price, their median (C4), so nobody moves the market with many deals.</item>
/// </list>
/// Prices far outside the rest (beyond 1.5× the interquartile range, with at least four prices) are left out (C4). The
/// partner count is the number of different partners behind the prices kept. The same is worked out per region (C2) from
/// the partners who work there and the deals made there; a region gets its own range once enough partners are behind it.
/// </summary>
public static class MarketPrices
{
    /// <summary>
    /// Updates the market range of the given work items (all when null) and their regional ranges on the tracked
    /// entities; the caller saves. Returns the items updated.
    /// </summary>
    public static async Task<List<WorkItem>> RecalculateAsync(
        IAppDbContext db, PricingSettings settings, TimeProvider clock, IReadOnlyCollection<Guid>? workItemIds, CancellationToken cancellationToken)
    {
        var items = workItemIds is null
            ? await db.WorkItems.ToListAsync(cancellationToken)
            : await db.WorkItems.Where(w => workItemIds.Contains(w.Id)).ToListAsync(cancellationToken);
        if (items.Count == 0)
        {
            return items;
        }

        var now = clock.GetUtcNow();
        var ids = items.Select(i => i.Id).ToList();
        var observations = (await ObservationsAsync(db, settings, ids, now, cancellationToken)).ToLookup(o => o.WorkItemId);
        var regional = await db.RegionalMarketPrices.Where(r => ids.Contains(r.WorkItemId)).ToListAsync(cancellationToken);
        var stored = regional.ToDictionary(r => (r.WorkItemId, r.RegionId));
        var kept = new HashSet<(Guid, Guid)>();

        foreach (var item in items)
        {
            var all = observations[item.Id].ToList();
            var counted = WithoutOutliers(all);
            item.UpdateMarket(RangeOf(Values(counted)), PartnerCount(counted), settings.MinPartners, now);
            if (item.IsPriceLocked)
            {
                continue; // staff keep their range everywhere
            }

            foreach (var regionId in all.SelectMany(o => o.Regions).Distinct())
            {
                var inRegion = WithoutOutliers(all.Where(o => o.Regions.Contains(regionId)).ToList());
                var partners = PartnerCount(inRegion);
                if (partners < settings.MinPartners || RangeOf(Values(inRegion)) is not { } range)
                {
                    continue;
                }

                if (stored.TryGetValue((item.Id, regionId), out var existing))
                {
                    existing.Update(range, partners, now);
                }
                else
                {
                    db.RegionalMarketPrices.Add(RegionalMarketPrice.Create(item.Id, regionId, range, partners, now));
                }

                kept.Add((item.Id, regionId));
            }
        }

        foreach (var row in regional.Where(r => !kept.Contains((r.WorkItemId, r.RegionId))))
        {
            db.RegionalMarketPrices.Remove(row);
        }

        return items;
    }

    /// <summary>
    /// The observations kept after dropping outliers: with at least four prices, those beyond 1.5 times the interquartile
    /// range from the 25th or 75th percentile (weights counted).
    /// </summary>
    internal static List<PriceObservation> WithoutOutliers(List<PriceObservation> observations)
    {
        var values = Values(observations).Order().ToArray();
        if (values.Length < 4)
        {
            return observations;
        }

        var q1 = Percentile(values, 0.25);
        var q3 = Percentile(values, 0.75);
        var fence = 1.5 * (q3 - q1);
        return observations.Where(o => o.Price >= q1 - fence && o.Price <= q3 + fence).ToList();
    }

    private static List<int> Values(IEnumerable<PriceObservation> observations) =>
        observations.SelectMany(o => Enumerable.Repeat(o.Price, o.Weight)).ToList();

    private static int PartnerCount(IEnumerable<PriceObservation> observations) => observations.Select(o => o.PartnerId).Distinct().Count();

    // Listed prices and accepted deals for the work items, with the regions each one speaks for.
    private static async Task<List<PriceObservation>> ObservationsAsync(
        IAppDbContext db, PricingSettings settings, List<Guid> workItemIds, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var freshSince = now - settings.PriceFreshFor;
        var visible = PublicPartnerQueries.Visible(db).Select(p => p.Id);
        var listed = await db.PartnerPrices.AsNoTracking()
            .Where(p => workItemIds.Contains(p.WorkItemId) && !p.IncludesMaterials && p.ConfirmedAt >= freshSince && visible.Contains(p.PartnerProfileId))
            .Select(p => new { p.WorkItemId, p.PartnerProfileId, p.PriceFrom, p.PriceTo })
            .ToListAsync(cancellationToken);

        var acceptedSince = now.AddDays(-settings.AcceptedOfferDays);
        var deals = await db.Offers.AsNoTracking()
            .Where(o => o.Kind == OfferKind.Work && o.Status == OfferStatus.Accepted && o.DecidedAt >= acceptedSince && !o.MaterialsIncluded)
            .SelectMany(o => o.Items
                .Where(i => i.Included && i.UnitPrice != null && i.RequestLineId != null)
                .Select(i => new { o.PartnerProfileId, RequestLineId = i.RequestLineId!.Value, UnitPrice = i.UnitPrice!.Value }))
            .ToListAsync(cancellationToken);
        var lineIds = deals.Select(d => d.RequestLineId).Distinct().ToList();
        var lineById = await db.ServiceRequests.AsNoTracking()
            .SelectMany(r => r.Lines.Select(l => new { l.Id, l.WorkItemId, r.CityId }))
            .Where(l => lineIds.Contains(l.Id) && workItemIds.Contains(l.WorkItemId))
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        var regionOfCity = await db.Cities.AsNoTracking()
            .Where(c => c.RegionId != null)
            .ToDictionaryAsync(c => c.Id, c => c.RegionId!.Value, cancellationToken);
        var partnerIds = listed.Select(p => p.PartnerProfileId).Distinct().ToList();
        var areas = await db.PartnerProfiles.AsNoTracking()
            .Where(p => partnerIds.Contains(p.Id))
            .SelectMany(p => p.Areas.Select(a => new { a.PartnerProfileId, a.RegionId, a.CityId }))
            .ToListAsync(cancellationToken);
        var regionsOf = areas
            .GroupBy(a => a.PartnerProfileId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlySet<Guid>)g
                    .Select(a => a.RegionId ?? (a.CityId is { } city && regionOfCity.TryGetValue(city, out var region) ? region : (Guid?)null))
                    .Where(r => r is not null)
                    .Select(r => r!.Value)
                    .ToHashSet());
        IReadOnlySet<Guid> none = new HashSet<Guid>();

        var observations = listed
            .Select(p => new PriceObservation(
                p.WorkItemId,
                p.PartnerProfileId,
                p.PriceTo is { } to ? (p.PriceFrom + to) / 2 : p.PriceFrom,
                1,
                regionsOf.GetValueOrDefault(p.PartnerProfileId) ?? none))
            .ToList();

        // One observation per partner and work item: the median of their deals, speaking for the regions they were in.
        observations.AddRange(deals
            .Where(d => lineById.ContainsKey(d.RequestLineId))
            .Select(d => new { d.PartnerProfileId, d.UnitPrice, Line = lineById[d.RequestLineId] })
            .GroupBy(d => (d.Line.WorkItemId, d.PartnerProfileId))
            .Select(g => new PriceObservation(
                g.Key.WorkItemId,
                g.Key.PartnerProfileId,
                (int)Math.Round(Percentile(g.Select(d => d.UnitPrice).Order().ToArray(), 0.5), MidpointRounding.AwayFromZero),
                settings.AcceptedOfferWeight,
                g.Select(d => regionOfCity.TryGetValue(d.Line.CityId, out var region) ? region : (Guid?)null)
                    .Where(r => r is not null)
                    .Select(r => r!.Value)
                    .ToHashSet())));
        return observations;
    }

    internal sealed record PriceObservation(Guid WorkItemId, Guid PartnerId, int Price, int Weight, IReadOnlySet<Guid> Regions);

    /// <summary>25th percentile, median and 75th percentile of the prices, rounded to tidy amounts; null for none.</summary>
    public static PriceRange? RangeOf(IReadOnlyCollection<int> prices)
    {
        if (prices.Count == 0)
        {
            return null;
        }

        var sorted = prices.Order().ToArray();
        return new PriceRange(Tidy(Percentile(sorted, 0.25)), Tidy(Percentile(sorted, 0.5)), Tidy(Percentile(sorted, 0.75)));
    }

    /// <summary>The percentile by linear interpolation between the closest ranks.</summary>
    public static double Percentile(IReadOnlyList<int> sorted, double fraction)
    {
        var position = (sorted.Count - 1) * fraction;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        return sorted[lower] + ((sorted[upper] - sorted[lower]) * (position - lower));
    }

    /// <summary>Rounds to 10 drams under 1,000, to 100 under 100,000 and to 1,000 above, so ranges read naturally.</summary>
    public static int Tidy(double price)
    {
        var step = price < 1_000 ? 10 : price < 100_000 ? 100 : 1_000;
        return (int)(Math.Round(price / step, MidpointRounding.AwayFromZero) * step);
    }
}
