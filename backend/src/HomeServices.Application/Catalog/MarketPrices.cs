using HomeServices.Application.Abstractions;
using HomeServices.Application.Messaging;
using HomeServices.Application.Partners;
using HomeServices.Domain.Catalog;
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
/// visible partners' labour-only prices; a from–to range counts as its middle.
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

        var ids = items.Select(i => i.Id).ToList();
        var counted = PublicPartnerQueries.Visible(db).Select(p => p.Id);
        var prices = await db.PartnerPrices.AsNoTracking()
            .Where(p => ids.Contains(p.WorkItemId) && !p.IncludesMaterials && counted.Contains(p.PartnerProfileId))
            .Select(p => new { p.WorkItemId, p.PriceFrom, p.PriceTo })
            .ToListAsync(cancellationToken);
        var byItem = prices.ToLookup(p => p.WorkItemId, p => p.PriceTo is { } to ? (p.PriceFrom + to) / 2 : p.PriceFrom);

        var now = clock.GetUtcNow();
        foreach (var item in items)
        {
            var values = byItem[item.Id].ToList();
            item.UpdateMarket(RangeOf(values), values.Count, settings.MinPartners, now);
        }

        return items;
    }

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
