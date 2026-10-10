using System.Security.Cryptography;
using System.Text;
using HomeServices.Domain.Estimates;

namespace HomeServices.Infrastructure.Persistence.Configurations;

// The work usually wanted in each kind of room (room templates), by work item slug. Counted work has a default count:
// a fixed number, or one per so many m² of floor. Staff change them in the Back Office later.
internal static partial class CatalogSeed
{
    private static readonly (string Slug, decimal? Quantity, decimal? PerSquareMeter)[] Living =
    [
        ("wall-putty", null, null),
        ("wall-painting", null, null),
        ("ceiling-putty", null, null),
        ("ceiling-painting", null, null),
        ("laminate-laying", null, null),
        ("skirting-installation", null, null),
        ("socket-installation", null, 0.25m),
        ("switch-installation", 2m, null),
        ("ceiling-light-installation", 1m, null),
    ];

    internal static readonly (RoomType Type, (string Slug, decimal? Quantity, decimal? PerSquareMeter)[] Items)[] RoomTemplates =
    [
        (RoomType.LivingRoom, Living),
        (RoomType.Bedroom, Living),
        (RoomType.KidsRoom, Living),
        (RoomType.Office, Living),
        (RoomType.Kitchen,
        [
            ("wall-putty", null, null),
            ("wall-painting", null, null),
            ("ceiling-painting", null, null),
            ("floor-tiling", null, null),
            ("kitchen-backsplash", 3m, null),
            ("socket-installation", 6m, null),
            ("kitchen-cabinet-installation", 4m, null),
            ("sink-installation", 1m, null),
            ("faucet-installation", 1m, null),
            ("kitchen-hood-installation", 1m, null),
            ("ceiling-light-installation", 1m, null),
        ]),
        (RoomType.Bathroom,
        [
            ("bathroom-waterproofing", null, null),
            ("floor-tiling", null, null),
            ("wall-tiling", null, null),
            ("stretch-ceiling", null, null),
            ("water-pipes-point", 4m, null),
            ("sewer-pipes-point", 3m, null),
            ("toilet-installation", 1m, null),
            ("sink-installation", 1m, null),
            ("shower-cabin-installation", 1m, null),
            ("faucet-installation", 2m, null),
            ("towel-rail-installation", 1m, null),
            ("water-heater-installation", 1m, null),
        ]),
        (RoomType.Toilet,
        [
            ("floor-tiling", null, null),
            ("wall-tiling", null, null),
            ("stretch-ceiling", null, null),
            ("water-pipes-point", 2m, null),
            ("sewer-pipes-point", 2m, null),
            ("toilet-installation", 1m, null),
            ("sink-installation", 1m, null),
            ("faucet-installation", 1m, null),
        ]),
        (RoomType.Hallway,
        [
            ("wall-putty", null, null),
            ("wall-painting", null, null),
            ("ceiling-painting", null, null),
            ("floor-tiling", null, null),
            ("skirting-installation", null, null),
            ("socket-installation", 2m, null),
            ("ceiling-light-installation", 1m, null),
            ("entrance-door-installation", 1m, null),
        ]),
        (RoomType.Balcony,
        [
            ("balcony-glazing", null, 1.5m),
            ("balcony-waterproofing", null, null),
            ("floor-tiling", null, null),
            ("wall-painting", null, null),
        ]),
        (RoomType.Garage,
        [
            ("floor-screed", null, null),
            ("wall-plastering", null, null),
            ("wall-painting", null, null),
            ("socket-installation", 2m, null),
            ("ceiling-light-installation", 2m, null),
            ("garage-door-installation", 1m, null),
        ]),
        (RoomType.Other,
        [
            ("wall-putty", null, null),
            ("wall-painting", null, null),
            ("ceiling-painting", null, null),
            ("laminate-laying", null, null),
            ("skirting-installation", null, null),
        ]),
    ];

    /// <summary>A fixed id made from a text, so seeded rows keep their ids between migrations.</summary>
    internal static Guid StableId(string text) => new(SHA256.HashData(Encoding.UTF8.GetBytes(text)).AsSpan(0, 16));
}
