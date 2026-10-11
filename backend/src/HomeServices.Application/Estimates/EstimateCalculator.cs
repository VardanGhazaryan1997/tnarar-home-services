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
    /// <summary>
    /// Measures and prices <paramref name="rooms"/>. Work that isn't offered (deleted or hidden) fails with
    /// "estimate.work_item_not_found", or with <paramref name="skipUnavailable"/> (saved estimates) is left out of the
    /// result, so the caller can tell the customer which of their lines are gone. With <paramref name="cityId"/>, work
    /// priced by enough partners in the town's region uses that region's range (C2). With <paramref name="materials"/>, the
    /// materials the work needs are counted and priced for that grade (E1).
    /// </summary>
    public static async Task<EstimateMeasurementDto> CalculateAsync(
        IAppDbContext db,
        ICurrentLanguage language,
        IReadOnlyList<RoomInput> rooms,
        bool oldBuilding,
        Guid? cityId,
        MaterialGrade? materials,
        bool skipUnavailable,
        CancellationToken cancellationToken)
    {
        var ids = rooms.SelectMany(r => r.Lines ?? Array.Empty<LineInput>()).Select(l => l.WorkItemId).Distinct().ToList();
        var items = await db.WorkItems.AsNoTracking()
            .Where(w => ids.Contains(w.Id) && w.IsActive)
            .ToDictionaryAsync(w => w.Id, cancellationToken);
        if (items.Count != ids.Count)
        {
            if (!skipUnavailable)
            {
                throw new DomainException("estimate.work_item_not_found", "Some of the work is no longer offered. Refresh the page.");
            }

            rooms = rooms.Select(r => r with { Lines = (r.Lines ?? Array.Empty<LineInput>()).Where(l => items.ContainsKey(l.WorkItemId)).ToList() }).ToList();
        }

        var regionId = await RegionalPrices.RegionOfCityAsync(db, cityId, cancellationToken);
        var regional = await RegionalPrices.LoadAsync(db, regionId, items.Keys, cancellationToken);
        PriceRange? MarketOf(WorkItem item) => regional.TryGetValue(item.Id, out var local) ? local.Range : item.Market;

        var measured = rooms.Select(room => Measure(room, items, MarketOf, oldBuilding, language)).ToList();
        var lines = measured.SelectMany(r => r.Lines).ToList();
        var result = new EstimateMeasurementDto(
            measured,
            measured.Sum(r => r.TotalMin),
            measured.Sum(r => r.TotalTypical),
            measured.Sum(r => r.TotalMax),
            lines.Count(l => l.PriceTypical is null),
            Materials: []);
        return materials is { } grade
            ? await EstimateMaterials.AddAsync(db, language, result, grade, cancellationToken)
            : result;
    }

    private static RoomMeasurementDto Measure(
        RoomInput room, Dictionary<Guid, WorkItem> items, Func<WorkItem, PriceRange?> marketOf, bool oldBuilding, ICurrentLanguage language)
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
                var price = quantity is { } q ? EstimatePricing.Price(q, marketOf(item), factor) : null;
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

/// <summary>
/// The materials an estimate's work needs (E1): each line's quantity × the work item's materials (amount per unit plus
/// waste), added up over all rooms, rounded up (whole pieces, otherwise tenths) and priced for the grade.
/// </summary>
public static class EstimateMaterials
{
    internal static async Task<EstimateMeasurementDto> AddAsync(
        IAppDbContext db, ICurrentLanguage language, EstimateMeasurementDto measurement, MaterialGrade grade, CancellationToken cancellationToken)
    {
        var workItemIds = measurement.Rooms.SelectMany(r => r.Lines).Select(l => l.WorkItemId).Distinct().ToList();
        var links = await db.WorkItemMaterials.AsNoTracking()
            .Where(m => workItemIds.Contains(m.WorkItemId))
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken);
        var materialIds = links.Select(l => l.MaterialId).Distinct().ToList();
        var materials = await db.Materials.AsNoTracking()
            .Where(m => materialIds.Contains(m.Id) && m.IsActive)
            .ToDictionaryAsync(m => m.Id, cancellationToken);
        var linksOf = links.Where(l => materials.ContainsKey(l.MaterialId)).ToLookup(l => l.WorkItemId);

        var needed = new Dictionary<Guid, decimal>();
        var rooms = new List<RoomMeasurementDto>(measurement.Rooms.Count);
        foreach (var room in measurement.Rooms)
        {
            var roomCost = 0m;
            foreach (var line in room.Lines.Where(l => l.Quantity is > 0))
            {
                foreach (var link in linksOf[line.WorkItemId])
                {
                    var amount = link.AmountFor(line.Quantity!.Value);
                    needed[link.MaterialId] = needed.GetValueOrDefault(link.MaterialId) + amount;
                    roomCost += amount * materials[link.MaterialId].PriceFor(grade);
                }
            }

            rooms.Add(room with { MaterialsCost = Cost(roomCost) });
        }

        // In the catalog's material order.
        var list = needed
            .Select(entry => materials[entry.Key])
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Slug, StringComparer.Ordinal)
            .Select(material =>
            {
                var quantity = RoundUp(needed[material.Id], material.Unit);
                var unitPrice = material.PriceFor(grade);
                return new MaterialNeedDto(
                    material.Id,
                    material.Name.Get(language.Code, language.DefaultCode),
                    material.Unit,
                    quantity,
                    unitPrice,
                    Cost(quantity * unitPrice));
            })
            .ToList();

        return measurement with { Rooms = rooms, MaterialGrade = grade, Materials = list, MaterialsTotal = list.Sum(m => m.Cost) };
    }

    /// <summary>Whole pieces; tenths of other units.</summary>
    public static decimal RoundUp(decimal quantity, MaterialUnit unit) =>
        unit == MaterialUnit.Piece ? Math.Ceiling(quantity) : Math.Ceiling(quantity * 10) / 10;

    private static int Cost(decimal amount) => MarketPrices.Tidy((double)Math.Min(amount, PriceRange.Limit));
}
