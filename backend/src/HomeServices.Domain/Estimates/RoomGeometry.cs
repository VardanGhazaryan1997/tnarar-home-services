using HomeServices.Domain.Catalog;

namespace HomeServices.Domain.Estimates;

/// <summary>A door or a window in a room's walls: its size in metres and how many of them there are.</summary>
public sealed record RoomOpening(OpeningKind Kind, decimal Width, decimal Height, int Count = 1)
{
    public const decimal MaxSize = 10m;
    public const int MaxCount = 50;

    public decimal Area => Width * Height * Count;

    public void EnsureValid()
    {
        if (!Enum.IsDefined(Kind) || Width is <= 0 or > MaxSize || Height is <= 0 or > MaxSize || Count is < 1 or > MaxCount)
        {
            throw new DomainException("estimate.opening_invalid", $"Doors and windows are up to {MaxSize} m wide and high, 1–{MaxCount} of each.");
        }
    }
}

public enum OpeningKind
{
    Door = 1,
    Window = 2,
}

/// <summary>
/// A room's size in metres: length × width, or only the floor area when the shape isn't a rectangle, and the ceiling
/// height. With only an area the room is treated as a square for the walls.
/// </summary>
public sealed record RoomSize(decimal? Length, decimal? Width, decimal? Area, decimal Height)
{
    public const decimal MaxSide = 100m;
    public const decimal MaxArea = 2000m;
    public const decimal MinHeight = 1.5m;
    public const decimal MaxHeight = 10m;
    public const decimal DefaultHeight = 2.7m;

    public void EnsureValid()
    {
        var sides = Length is not null || Width is not null;
        var validSides = Length is > 0 and <= MaxSide && Width is > 0 and <= MaxSide;
        var validArea = Area is > 0 and <= MaxArea;
        if ((sides ? !validSides : !validArea) || Height is < MinHeight or > MaxHeight)
        {
            throw new DomainException(
                "estimate.size_invalid",
                $"Give the length and width (up to {MaxSide} m) or the floor area (up to {MaxArea} m²), and a height of {MinHeight}–{MaxHeight} m.");
        }
    }
}

/// <summary>Areas and lengths worked out from a room's size and openings, rounded to 0.01.</summary>
public sealed record RoomGeometry(decimal FloorArea, decimal Perimeter, decimal WallArea, decimal CeilingArea, decimal SkirtingLength)
{
    public static RoomGeometry Of(RoomSize size, IEnumerable<RoomOpening> openings)
    {
        size.EnsureValid();
        var list = openings.ToList();
        list.ForEach(o => o.EnsureValid());

        decimal floor;
        decimal perimeter;
        if (size.Length is { } length && size.Width is { } width)
        {
            floor = length * width;
            perimeter = 2 * (length + width);
        }
        else
        {
            floor = size.Area!.Value;
            perimeter = 4 * (decimal)Math.Sqrt((double)floor);
        }

        var walls = Math.Max(0, (perimeter * size.Height) - list.Sum(o => o.Area));
        var doorWidths = list.Where(o => o.Kind == OpeningKind.Door).Sum(o => o.Width * o.Count);
        var skirting = Math.Max(0, perimeter - doorWidths);
        return new RoomGeometry(Round(floor), Round(perimeter), Round(walls), Round(floor), Round(skirting));
    }

    /// <summary>
    /// How much of a work item a room needs, from its surface and unit: m² of floor, walls or ceiling; running metres of
    /// skirting (floor), cornice (ceiling) or wall perimeter; 1 for a fixed-price job. Null when it can't be measured from
    /// the room (pieces, points, hours, m³): the customer enters it.
    /// </summary>
    public decimal? QuantityFor(WorkUnit unit, WorkSurface surface) => (unit, surface) switch
    {
        (WorkUnit.SquareMeter, WorkSurface.Floor) => FloorArea,
        (WorkUnit.SquareMeter, WorkSurface.Wall) => WallArea,
        (WorkUnit.SquareMeter, WorkSurface.Ceiling) => CeilingArea,
        (WorkUnit.SquareMeter, WorkSurface.None) => FloorArea,
        (WorkUnit.RunningMeter, WorkSurface.Floor) => SkirtingLength,
        (WorkUnit.RunningMeter, WorkSurface.Ceiling or WorkSurface.Wall) => Perimeter,
        (WorkUnit.Fixed, _) => 1m,
        _ => null,
    };

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
