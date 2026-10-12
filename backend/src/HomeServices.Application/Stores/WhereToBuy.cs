using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Stores;

/// <summary>
/// Where to buy the materials of an estimate: for each material the cheapest store offer in the grade, rounded up to
/// whole packs, and each store's total for the materials it has. Only active, in-stock, fresh products of active stores
/// count; with <paramref name="Region"/> (a region slug) only stores with a branch there. Anyone can call it.
/// </summary>
public sealed record WhereToBuy(MaterialGrade Grade, IReadOnlyList<WhereToBuyNeed> Materials, string? Region = null) : IQuery<WhereToBuyDto>;

/// <summary>An amount of a material (in its unit), as the estimate counted it.</summary>
public sealed record WhereToBuyNeed(Guid MaterialId, decimal Quantity);

/// <summary>
/// <see cref="Cheapest"/>: one row per material a store sells (others are left out). <see cref="Stores"/>: stores selling
/// at least one of the materials, most materials covered first, then cheapest.
/// </summary>
public sealed record WhereToBuyDto(IReadOnlyList<CheapestOfferDto> Cheapest, IReadOnlyList<StoreTotalDto> Stores);

public sealed record CheapestOfferDto(
    Guid MaterialId, string MaterialSlug, string StoreName, string StoreSlug, string ProductName, string? PackLabel, decimal PackSize, int Packs, int Cost);

public sealed record StoreTotalDto(string StoreName, string StoreSlug, int Total, int Covered, int Missing);

public sealed class WhereToBuyValidator : AbstractValidator<WhereToBuy>
{
    public const int MaxMaterials = 100;
    public const decimal MaxQuantity = 10_000_000m;

    public WhereToBuyValidator()
    {
        RuleFor(x => x.Grade).IsInEnum().WithErrorCode("materials.invalid");
        RuleFor(x => x.Materials).NotNull().WithErrorCode("materials.required")
            .Must(list => list is null || list.Count <= MaxMaterials).WithErrorCode("materials.too_many");
        RuleFor(x => x.Materials)
            .Must(list => list is null || list.Select(m => m.MaterialId).Distinct().Count() == list.Count)
            .WithErrorCode("materials.duplicate");
        RuleForEach(x => x.Materials)
            .Must(m => m.Quantity is > 0 and <= MaxQuantity)
            .WithErrorCode("materials.quantity_invalid");
    }
}

public sealed class WhereToBuyHandler(IAppDbContext db, IOptions<StorePricingSettings> settings, TimeProvider clock)
    : IQueryHandler<WhereToBuy, WhereToBuyDto>
{
    public async Task<WhereToBuyDto> HandleAsync(WhereToBuy query, CancellationToken cancellationToken)
    {
        var needs = query.Materials.ToDictionary(m => m.MaterialId, m => m.Quantity);
        if (needs.Count == 0)
        {
            return new WhereToBuyDto([], []);
        }

        var now = clock.GetUtcNow();
        var today = settings.Value.Today(now);
        var cityIds = await PublicStoreData.RegionCityIdsAsync(db, query.Region, cancellationToken);
        var materialIds = needs.Keys.Select(id => (Guid?)id).ToList();

        var products = await PublicStoreData.VisibleProducts(db, now - settings.Value.FreshFor, cityIds)
            .Where(p => p.InStock && p.Grade == query.Grade && materialIds.Contains(p.MaterialId))
            .ToListAsync(cancellationToken);
        if (products.Count == 0)
        {
            return new WhereToBuyDto([], []);
        }

        var storeIds = products.Select(p => p.StoreId).Distinct().ToList();
        var stores = await db.Stores.AsNoTracking().Where(s => storeIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);
        var needed = needs.Keys.ToList();
        var slugs = await db.Materials.AsNoTracking()
            .Where(m => needed.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Slug, cancellationToken);

        // Each store's cheapest product for each material.
        var offers = products
            .Select(p =>
            {
                var materialId = p.MaterialId!.Value;
                var packs = (int)Math.Ceiling(needs[materialId] / p.PackSize);
                var cost = (int)Math.Min((long)packs * p.CurrentPrice(today), int.MaxValue);
                return new Offer(materialId, p, packs, cost);
            })
            .GroupBy(o => (o.MaterialId, o.Product.StoreId))
            .Select(g => g.OrderBy(o => o.Cost).ThenBy(o => o.Product.UnitPrice(today)).ThenBy(o => o.Product.Name, StringComparer.Ordinal).First())
            .ToList();

        var order = query.Materials.Select((m, index) => (m.MaterialId, index)).ToDictionary(x => x.MaterialId, x => x.index);
        var cheapest = offers
            .GroupBy(o => o.MaterialId)
            .Select(g => g.OrderBy(o => o.Cost).ThenBy(o => stores[o.Product.StoreId].Name, StringComparer.Ordinal).First())
            .OrderBy(o => order[o.MaterialId])
            .Select(o => new CheapestOfferDto(
                o.MaterialId,
                slugs.GetValueOrDefault(o.MaterialId, string.Empty),
                stores[o.Product.StoreId].Name,
                stores[o.Product.StoreId].Slug,
                o.Product.Name,
                o.Product.PackLabel,
                o.Product.PackSize,
                o.Packs,
                o.Cost))
            .ToList();

        var totals = offers
            .GroupBy(o => o.Product.StoreId)
            .Select(g => new StoreTotalDto(
                stores[g.Key].Name,
                stores[g.Key].Slug,
                (int)Math.Min(g.Sum(o => (long)o.Cost), int.MaxValue),
                g.Count(),
                needs.Count - g.Count()))
            .OrderByDescending(t => t.Covered)
            .ThenBy(t => t.Total)
            .ThenBy(t => t.StoreName, StringComparer.Ordinal)
            .ToList();

        return new WhereToBuyDto(cheapest, totals);
    }

    private sealed record Offer(Guid MaterialId, StoreProduct Product, int Packs, int Cost);
}
