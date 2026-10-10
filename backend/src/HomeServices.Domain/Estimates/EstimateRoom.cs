using HomeServices.Domain.Common;

namespace HomeServices.Domain.Estimates;

/// <summary>Kinds of rooms; each will have its usual work ticked by default (room templates).</summary>
public enum RoomType
{
    LivingRoom = 1,
    Bedroom = 2,
    KidsRoom = 3,
    Kitchen = 4,
    Bathroom = 5,
    Toilet = 6,
    Hallway = 7,
    Balcony = 8,
    Office = 9,
    Garage = 10,
    Other = 11,
}

/// <summary>A room of an estimate: its size, doors and windows, and the work wanted in it.</summary>
public sealed class EstimateRoom : Entity
{
    public const int NameMaxLength = 60;
    public const int MaxOpenings = 20;
    public const int MaxLines = 60;

    private readonly List<EstimateOpening> _openings = [];
    private readonly List<EstimateLine> _lines = [];

    private EstimateRoom()
    {
    }

    internal EstimateRoom(Guid estimateId, int sortOrder, string name, RoomType type, RoomSize size, IEnumerable<RoomOpening> openings)
    {
        EstimateId = estimateId;
        SortOrder = sortOrder;
        Change(name, type, size, openings);
    }

    public Guid EstimateId { get; private set; }

    public int SortOrder { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public RoomType Type { get; private set; }

    public decimal? Length { get; private set; }

    public decimal? Width { get; private set; }

    public decimal? Area { get; private set; }

    public decimal Height { get; private set; }

    public RoomSize Size => new(Length, Width, Area, Height);

    public IReadOnlyCollection<EstimateOpening> Openings => _openings.AsReadOnly();

    public IReadOnlyCollection<EstimateLine> Lines => _lines.AsReadOnly();

    /// <summary>Areas and lengths of the room as it is now.</summary>
    public RoomGeometry Geometry => RoomGeometry.Of(Size, _openings.Select(o => o.ToOpening()));

    /// <summary>Changes the room's name, type, size and openings (the openings are replaced by the list given).</summary>
    public void Change(string name, RoomType type, RoomSize size, IEnumerable<RoomOpening> openings)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 or > NameMaxLength)
        {
            throw new DomainException("estimate.room_name_invalid", $"Name the room in up to {NameMaxLength} characters.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new DomainException("estimate.room_type_invalid", "Unknown room type.");
        }

        var list = openings.ToList();
        if (list.Count > MaxOpenings)
        {
            throw new DomainException("estimate.too_many_openings", $"At most {MaxOpenings} kinds of doors and windows per room.");
        }

        _ = RoomGeometry.Of(size, list); // validates the size and openings together

        Name = trimmed;
        Type = type;
        var bySides = size.Length is not null;
        Length = bySides ? size.Length : null;
        Width = bySides ? size.Width : null;
        Area = bySides ? null : size.Area;
        Height = size.Height;
        _openings.Clear();
        _openings.AddRange(list.Select(o => new EstimateOpening(Id, o)));
    }

    /// <summary>Adds work to the room; <paramref name="quantity"/> overrides the measured quantity (null = measure it).</summary>
    public EstimateLine AddLine(Guid workItemId, decimal? quantity)
    {
        if (_lines.Any(l => l.WorkItemId == workItemId))
        {
            throw new DomainException("estimate.line_exists", "This work is already in the room.");
        }

        if (_lines.Count >= MaxLines)
        {
            throw new DomainException("estimate.too_many_lines", $"At most {MaxLines} kinds of work per room.");
        }

        var line = new EstimateLine(Id, workItemId, quantity);
        _lines.Add(line);
        return line;
    }

    public void RemoveLine(Guid workItemId) => _lines.RemoveAll(l => l.WorkItemId == workItemId);

    internal void Reorder(int sortOrder) => SortOrder = sortOrder;
}

/// <summary>A stored door or window of a room.</summary>
public sealed class EstimateOpening : Entity
{
    private EstimateOpening()
    {
    }

    internal EstimateOpening(Guid roomId, RoomOpening opening)
    {
        RoomId = roomId;
        Kind = opening.Kind;
        Width = opening.Width;
        Height = opening.Height;
        Count = opening.Count;
    }

    public Guid RoomId { get; private set; }

    public OpeningKind Kind { get; private set; }

    public decimal Width { get; private set; }

    public decimal Height { get; private set; }

    public int Count { get; private set; }

    public RoomOpening ToOpening() => new(Kind, Width, Height, Count);
}

/// <summary>Work wanted in a room. <see cref="Quantity"/> is the customer's own quantity; null means measured from the room.</summary>
public sealed class EstimateLine : Entity
{
    public const decimal MaxQuantity = 100_000m;

    private EstimateLine()
    {
    }

    internal EstimateLine(Guid roomId, Guid workItemId, decimal? quantity)
    {
        RoomId = roomId;
        WorkItemId = workItemId;
        SetQuantity(quantity);
    }

    public Guid RoomId { get; private set; }

    public Guid WorkItemId { get; private set; }

    public decimal? Quantity { get; private set; }

    public void SetQuantity(decimal? quantity)
    {
        if (quantity is <= 0 or > MaxQuantity)
        {
            throw new DomainException("estimate.quantity_invalid", $"A quantity is above 0 and at most {MaxQuantity}.");
        }

        Quantity = quantity is null ? null : Math.Round(quantity.Value, 2, MidpointRounding.AwayFromZero);
    }
}
