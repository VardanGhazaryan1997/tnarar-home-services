namespace HomeServices.Domain.Catalog;

/// <summary>What a work item's price is quoted per.</summary>
public enum WorkUnit
{
    /// <summary>Square metre (m²): plastering, tiling, painting.</summary>
    SquareMeter = 1,

    /// <summary>Running metre: skirting, pipes, cornices.</summary>
    RunningMeter = 2,

    /// <summary>One piece: a door, a toilet, a radiator.</summary>
    Piece = 3,

    /// <summary>One point: a socket, a switch, a light.</summary>
    Point = 4,

    /// <summary>Cubic metre (m³): concrete, excavation.</summary>
    CubicMeter = 5,

    /// <summary>An hour of work, when nothing else fits.</summary>
    Hour = 6,

    /// <summary>A fixed price for the whole job: a call-out, a diagnosis.</summary>
    Fixed = 7,
}
