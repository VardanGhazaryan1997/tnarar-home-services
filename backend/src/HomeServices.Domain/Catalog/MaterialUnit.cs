namespace HomeServices.Domain.Catalog;

/// <summary>What a material's price is quoted per.</summary>
public enum MaterialUnit
{
    /// <summary>Kilogram: plaster, putty, tile adhesive, screed mix.</summary>
    Kilogram = 1,

    /// <summary>Litre: paint, primer.</summary>
    Liter = 2,

    /// <summary>Square metre (m²): tiles, laminate, drywall sheets.</summary>
    SquareMeter = 3,

    /// <summary>Running metre: skirting boards, profiles, cable.</summary>
    RunningMeter = 4,

    /// <summary>Cubic metre (m³): concrete.</summary>
    CubicMeter = 5,

    /// <summary>One piece: a socket, a toilet, a door.</summary>
    Piece = 6,
}

/// <summary>The quality of materials an estimate is priced with.</summary>
public enum MaterialGrade
{
    Budget = 1,
    Standard = 2,
    Premium = 3,
}
