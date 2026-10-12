using HomeServices.Application.Abstractions;
using HomeServices.Domain.Stores;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Stores;

/// <summary>Keeps a store product's price history and finds the price it had a while ago.</summary>
internal static class StorePriceHistory
{
    /// <summary>Adds a history row for the product's current price (the caller saves).</summary>
    public static void Record(IAppDbContext db, StoreProduct product, DateTimeOffset now) =>
        db.StoreProductPrices.Add(StoreProductPrice.Record(product, now));

    /// <summary>
    /// For each product, the regular price it had <see cref="StorePricingSettings.HistoryDays"/> days ago (the latest
    /// entry at or before that moment), only when it differs from the current regular price. Products without such an
    /// entry (new ones) or with an unchanged price are left out.
    /// </summary>
    public static async Task<Dictionary<Guid, int>> PriceBeforeAsync(
        IAppDbContext db, StorePricingSettings settings, DateTimeOffset now, IReadOnlyCollection<StoreProduct> products, CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, int>();
        if (products.Count == 0)
        {
            return result;
        }

        var ids = products.Select(p => p.Id).ToList();
        var cutoff = now.AddDays(-settings.HistoryDays);
        var rows = await db.StoreProductPrices.AsNoTracking()
            .Where(r => ids.Contains(r.StoreProductId) && r.RecordedAt <= cutoff)
            .Select(r => new { r.StoreProductId, r.Price, r.RecordedAt })
            .ToListAsync(cancellationToken);

        var current = products.ToDictionary(p => p.Id, p => p.Price);
        foreach (var group in rows.GroupBy(r => r.StoreProductId))
        {
            var before = group.OrderByDescending(r => r.RecordedAt).First().Price;
            if (before != current[group.Key])
            {
                result[group.Key] = before;
            }
        }

        return result;
    }
}
