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
/// the median as the usual price, so a few very cheap or very expensive prices don't move it. Counted: approved,
/// visible partners' labour-only prices (a from–to range counts as its middle), and the line prices of work offers
/// customers accepted in the last <see cref="PricingSettings.AcceptedOfferDays"/> days without materials — real deals,
/// so each counts <see cref="PricingSettings.AcceptedOfferWeight"/> times. The partner count is the number of different
/// partners behind those prices.
/// </summary>
public static class MarketPrices
{
    /// <summary>
    /// Updates the market range of the given work items (all when null) on the tracked entities; the caller saves.
    /// Returns the items updated.
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
        var counted = PublicPartnerQueries.Visible(db).Select(p => p.Id);
        var listed = await db.PartnerPrices.AsNoTracking()
            .Where(p => ids.Contains(p.WorkItemId) && !p.IncludesMaterials && counted.Contains(p.PartnerProfileId))
            .Select(p => new { p.WorkItemId, p.PartnerProfileId, p.PriceFrom, p.PriceTo })
            .ToListAsync(cancellationToken);
        var observations = listed
            .Select(p => new PriceObservation(p.WorkItemId, p.PartnerProfileId, p.PriceTo is { } to ? (p.PriceFrom + to) / 2 : p.PriceFrom, 1))
            .Concat(await AcceptedAsync(db, settings, ids, now, cancellationToken))
            .ToLookup(o => o.WorkItemId);

        foreach (var item in items)
        {
            var found = observations[item.Id].ToList();
            var values = found.SelectMany(o => Enumerable.Repeat(o.Price, o.Weight)).ToList();
            item.UpdateMarket(RangeOf(values), found.Select(o => o.PartnerId).Distinct().Count(), settings.MinPartners, now);
        }

        return items;
    }

    // Unit prices of accepted, labour-only work offers, line by line, for the request lines' work items.
    private static async Task<List<PriceObservation>> AcceptedAsync(
        IAppDbContext db, PricingSettings settings, List<Guid> workItemIds, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var since = now.AddDays(-settings.AcceptedOfferDays);
        var lines = await db.Offers.AsNoTracking()
            .Where(o => o.Kind == OfferKind.Work && o.Status == OfferStatus.Accepted && o.DecidedAt >= since && !o.MaterialsIncluded)
            .SelectMany(o => o.Items
                .Where(i => i.Included && i.UnitPrice != null && i.RequestLineId != null)
                .Select(i => new { o.PartnerProfileId, RequestLineId = i.RequestLineId!.Value, UnitPrice = i.UnitPrice!.Value }))
            .ToListAsync(cancellationToken);
        if (lines.Count == 0)
        {
            return [];
        }

        var requestLineIds = lines.Select(l => l.RequestLineId).Distinct().ToList();
        var work = await db.ServiceRequests.AsNoTracking()
            .SelectMany(r => r.Lines)
            .Where(l => requestLineIds.Contains(l.Id) && workItemIds.Contains(l.WorkItemId))
            .ToDictionaryAsync(l => l.Id, l => l.WorkItemId, cancellationToken);
        return lines
            .Where(l => work.ContainsKey(l.RequestLineId))
            .Select(l => new PriceObservation(work[l.RequestLineId], l.PartnerProfileId, l.UnitPrice, settings.AcceptedOfferWeight))
            .ToList();
    }

    private sealed record PriceObservation(Guid WorkItemId, Guid PartnerId, int Price, int Weight);

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
