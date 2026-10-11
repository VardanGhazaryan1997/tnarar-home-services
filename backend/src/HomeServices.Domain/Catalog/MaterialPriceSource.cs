namespace HomeServices.Domain.Catalog;

/// <summary>Where a material's price for a grade comes from.</summary>
public enum MaterialPriceSource
{
    /// <summary>The price staff set on the material (no store prices yet, or staff locked the material).</summary>
    Staff = 0,

    /// <summary>Worked out from the prices stores ask for products linked to the material.</summary>
    Stores = 1,
}
