using HomeServices.Application.Abstractions;
using HomeServices.Application.Catalog;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Stores;

/// <summary>
/// Recalculates every material's prices from store prices (the background job runs it; prices also update as store
/// products, stores and materials change).
/// </summary>
public sealed record RecalculateMaterialPrices : ICommand<MaterialPricesResult>;

/// <summary>How many materials the pass looked at and how many now have a store price for at least one grade.</summary>
public sealed record MaterialPricesResult(int Materials, int FromStores);

public sealed class RecalculateMaterialPricesHandler(IAppDbContext db, IOptions<StorePricingSettings> settings, TimeProvider clock)
    : ICommandHandler<RecalculateMaterialPrices, MaterialPricesResult>
{
    public async Task<MaterialPricesResult> HandleAsync(RecalculateMaterialPrices command, CancellationToken cancellationToken)
    {
        var materials = await MaterialMarketPrices.RecalculateAsync(db, settings.Value, clock, materialIds: null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new MaterialPricesResult(materials.Count, materials.Count(m => m.MarketBudget is not null || m.MarketStandard is not null || m.MarketPremium is not null));
    }
}

/// <summary>
/// A material's usual price per unit for each grade from what stores ask. Counted: products that are active and in stock,
/// of active stores, linked to the material with that grade, whose price was set or checked within
/// <see cref="StorePricingSettings.FreshDays"/>. A product's price per unit is its current price (the sale price until
/// the sale's last day) over its pack size. A store counts once per grade (the median of its products), and a grade
/// gets a price once at least <see cref="StorePricingSettings.MinStores"/> stores sell it: the median of the stores'
/// prices; otherwise the staff price stays. Prices keep the order budget ≤ standard ≤ premium.
/// </summary>
public static class MaterialMarketPrices
{
    /// <summary>
    /// Updates the market prices of the given materials (all when null) on the tracked entities; the caller saves. The
    /// products must already be saved. Returns the materials updated.
    /// </summary>
    public static async Task<List<Material>> RecalculateAsync(
        IAppDbContext db, StorePricingSettings settings, TimeProvider clock, IReadOnlyCollection<Guid>? materialIds, CancellationToken cancellationToken)
    {
        var materials = materialIds is null
            ? await db.Materials.ToListAsync(cancellationToken)
            : await db.Materials.Where(m => materialIds.Contains(m.Id)).ToListAsync(cancellationToken);
        if (materials.Count == 0)
        {
            return materials;
        }

        var now = clock.GetUtcNow();
        var today = settings.Today(now);
        var freshSince = now - settings.FreshFor;
        var ids = materials.Select(m => m.Id).ToList();

        var rows = await (
            from product in db.StoreProducts.AsNoTracking()
            join store in db.Stores.AsNoTracking() on product.StoreId equals store.Id
            where product.MaterialId != null && ids.Contains(product.MaterialId.Value) && product.Grade != null
                && product.IsActive && product.InStock && store.IsActive && product.PriceUpdatedAt >= freshSince
            select new Offer(product.MaterialId!.Value, product.Grade!.Value, product.StoreId, product.Price, product.SalePrice, product.SaleEndsOn, product.PackSize))
            .ToListAsync(cancellationToken);
        var byMaterial = rows.ToLookup(r => r.MaterialId);

        foreach (var material in materials)
        {
            var offers = byMaterial[material.Id].ToList();
            var prices = new int?[]
            {
                GradePrice(offers, MaterialGrade.Budget, today, settings.MinStores),
                GradePrice(offers, MaterialGrade.Standard, today, settings.MinStores),
                GradePrice(offers, MaterialGrade.Premium, today, settings.MinStores),
            };
            KeepInOrder(prices);
            material.UpdateMarket(prices[0], prices[1], prices[2], offers.Select(o => o.StoreId).Distinct().Count(), now);
        }

        return materials;
    }

    /// <summary>Raises a price that is below the cheaper grade's to that price (budget ≤ standard ≤ premium).</summary>
    internal static void KeepInOrder(int?[] prices)
    {
        int? floor = null;
        for (var i = 0; i < prices.Length; i++)
        {
            if (prices[i] is not { } price)
            {
                continue;
            }

            if (floor is { } lowest && price < lowest)
            {
                prices[i] = lowest;
            }
            else
            {
                floor = price;
            }
        }
    }

    private static int? GradePrice(List<Offer> offers, MaterialGrade grade, DateOnly today, int minStores)
    {
        var perStore = offers
            .Where(o => o.Grade == grade)
            .GroupBy(o => o.StoreId)
            .Select(g => Median(g.Select(o => o.UnitPrice(today)).ToList()))
            .ToList();
        return perStore.Count >= minStores && perStore.Count > 0 ? Round(Median(perStore)) : null;
    }

    /// <summary>The middle value (the mean of the two middle values for an even count).</summary>
    internal static decimal Median(IReadOnlyList<decimal> values)
    {
        var sorted = values.Order().ToArray();
        var middle = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2m;
    }

    /// <summary>Whole dram under 100 (unit prices are small for bulk materials), otherwise a tidy amount; never below 1.</summary>
    internal static int Round(decimal unitPrice) =>
        unitPrice < 100m ? Math.Max(1, (int)Math.Round(unitPrice, MidpointRounding.AwayFromZero)) : Math.Min(Material.PriceLimit, MarketPrices.Tidy((double)unitPrice));

    internal sealed record Offer(Guid MaterialId, MaterialGrade Grade, Guid StoreId, int Price, int? SalePrice, DateOnly? SaleEndsOn, decimal PackSize)
    {
        public decimal UnitPrice(DateOnly today) => (SalePrice is { } sale && today <= SaleEndsOn ? sale : Price) / PackSize;
    }
}
