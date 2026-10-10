using HomeServices.Domain.Common;

namespace HomeServices.Domain.Estimates;

/// <summary>
/// Work usually wanted in a kind of room, ticked by default when a customer adds such a room to an estimate. Work that
/// can't be measured from the room (pieces, points) has a default count: a fixed <see cref="Quantity"/> (one toilet), or
/// one per so many square metres of floor (<see cref="QuantityPerSquareMeter"/>, e.g. sockets).
/// </summary>
public sealed class RoomTemplate : Entity
{
    public const decimal MaxQuantity = 1000m;

    private RoomTemplate()
    {
    }

    public RoomType RoomType { get; private set; }

    public Guid WorkItemId { get; private set; }

    public int SortOrder { get; private set; }

    public decimal? Quantity { get; private set; }

    public decimal? QuantityPerSquareMeter { get; private set; }

    public static RoomTemplate Create(RoomType roomType, Guid workItemId, int sortOrder, decimal? quantity = null, decimal? quantityPerSquareMeter = null)
    {
        if (!Enum.IsDefined(roomType))
        {
            throw new DomainException("room_template.type_invalid", "Unknown room type.");
        }

        if (quantity is <= 0 or > MaxQuantity || quantityPerSquareMeter is <= 0 or > 100 || (quantity is not null && quantityPerSquareMeter is not null))
        {
            throw new DomainException("room_template.quantity_invalid", "Give a count, or a count per m² of floor, not both.");
        }

        return new RoomTemplate
        {
            RoomType = roomType,
            WorkItemId = workItemId,
            SortOrder = sortOrder,
            Quantity = quantity,
            QuantityPerSquareMeter = quantityPerSquareMeter,
        };
    }

    /// <summary>The default quantity in a room of <paramref name="floorArea"/> m², or null to measure it from the room.</summary>
    public decimal? DefaultQuantity(decimal floorArea) =>
        Quantity ?? (QuantityPerSquareMeter is { } perSquareMeter ? Math.Max(1, Math.Ceiling(floorArea * perSquareMeter)) : null);
}
