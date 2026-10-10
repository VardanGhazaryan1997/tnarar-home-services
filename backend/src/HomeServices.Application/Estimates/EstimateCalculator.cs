using HomeServices.Application.Abstractions;
using HomeServices.Application.Catalog;
using HomeServices.Domain;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Estimates;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Estimates;

/// <summary>Measures rooms and prices their work: the engine behind the measure and quick-estimate endpoints.</summary>
internal static class EstimateCalculator
{
    public static async Task<EstimateMeasurementDto> CalculateAsync(
        IAppDbContext db, ICurrentLanguage language, IReadOnlyList<RoomInput> rooms, bool oldBuilding, CancellationToken cancellationToken)
    {
        var ids = rooms.SelectMany(r => r.Lines ?? Array.Empty<LineInput>()).Select(l => l.WorkItemId).Distinct().ToList();
        var items = await db.WorkItems.AsNoTracking()
            .Where(w => ids.Contains(w.Id) && w.IsActive)
            .ToDictionaryAsync(w => w.Id, cancellationToken);
        if (items.Count != ids.Count)
        {
            throw new DomainException("estimate.work_item_not_found", "Some of the work is no longer offered. Refresh the page.");
        }

        var measured = rooms.Select(room => Measure(room, items, oldBuilding, language)).ToList();
        var lines = measured.SelectMany(r => r.Lines).ToList();
        return new EstimateMeasurementDto(
            measured,
            measured.Sum(r => r.TotalMin),
            measured.Sum(r => r.TotalTypical),
            measured.Sum(r => r.TotalMax),
            lines.Count(l => l.PriceTypical is null));
    }

    private static RoomMeasurementDto Measure(RoomInput room, Dictionary<Guid, WorkItem> items, bool oldBuilding, ICurrentLanguage language)
    {
        var height = room.Height ?? RoomSize.DefaultHeight;
        var geometry = RoomGeometry.Of(
            new RoomSize(room.Length, room.Width, room.Area, height),
            (room.Openings ?? Array.Empty<OpeningInput>()).Select(o => new RoomOpening(o.Kind, o.Width, o.Height, o.Count)));

        var lines = (room.Lines ?? Array.Empty<LineInput>())
            .Select(line =>
            {
                var item = items[line.WorkItemId];
                var measuredQuantity = geometry.QuantityFor(item.Unit, item.Surface);
                var quantity = line.Quantity ?? measuredQuantity;
                var factor = EstimatePricing.Factor(item, height, oldBuilding);
                var price = quantity is { } q ? EstimatePricing.Price(q, item.Market, factor) : null;
                return new LineMeasurementDto(
                    item.Id,
                    item.Name.Get(language.Code, language.DefaultCode),
                    item.Unit,
                    item.Surface,
                    measuredQuantity,
                    quantity,
                    quantity is null,
                    price?.Min,
                    price?.Typical,
                    price?.Max,
                    factor);
            })
            .ToList();

        return new RoomMeasurementDto(
            room.Type,
            geometry.FloorArea,
            geometry.Perimeter,
            geometry.WallArea,
            geometry.CeilingArea,
            geometry.SkirtingLength,
            lines,
            lines.Sum(l => l.PriceMin ?? 0),
            lines.Sum(l => l.PriceTypical ?? 0),
            lines.Sum(l => l.PriceMax ?? 0));
    }
}

/// <summary>
/// How a line is priced: quantity × the work item's market range × an adjustment, rounded to tidy amounts. Adjustments:
/// an old building (+15% on all work: uneven walls, old pipes and wiring); a ceiling above 3 m (+10% on wall and ceiling
/// work: scaffolding, more material handling). They multiply.
/// </summary>
public static class EstimatePricing
{
    public const decimal OldBuildingFactor = 1.15m;
    public const decimal HighCeilingFactor = 1.10m;
    public const decimal HighCeilingFrom = 3.0m;

    public static decimal Factor(WorkItem item, decimal height, bool oldBuilding)
    {
        var factor = oldBuilding ? OldBuildingFactor : 1m;
        if (height > HighCeilingFrom && item.Surface is WorkSurface.Wall or WorkSurface.Ceiling)
        {
            factor *= HighCeilingFactor;
        }

        return factor;
    }

    /// <summary>The labour price range for <paramref name="quantity"/>, or null when the work has no market price.</summary>
    public static PriceRange? Price(decimal quantity, PriceRange? market, decimal factor) =>
        market is null
            ? null
            : new PriceRange(Amount(quantity, market.Min, factor), Amount(quantity, market.Typical, factor), Amount(quantity, market.Max, factor));

    private static int Amount(decimal quantity, int unitPrice, decimal factor) =>
        MarketPrices.Tidy((double)Math.Min(quantity * unitPrice * factor, PriceRange.Limit));
}
