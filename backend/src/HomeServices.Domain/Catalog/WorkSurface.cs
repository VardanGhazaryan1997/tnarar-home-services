namespace HomeServices.Domain.Catalog;

/// <summary>The room surface a work item is done on, for working out quantities from room sizes.</summary>
public enum WorkSurface
{
    /// <summary>Not tied to a surface (counted in pieces, points, hours…).</summary>
    None = 0,

    Floor = 1,

    Wall = 2,

    Ceiling = 3,
}
