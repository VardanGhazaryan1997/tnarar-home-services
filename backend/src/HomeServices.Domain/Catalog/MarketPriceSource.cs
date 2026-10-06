namespace HomeServices.Domain.Catalog;

/// <summary>Where a work item's market price range comes from.</summary>
public enum MarketPriceSource
{
    /// <summary>The range staff set (too few partners priced the item, or staff locked it).</summary>
    Staff = 0,

    /// <summary>Worked out from partners' own prices.</summary>
    Partners = 1,
}
