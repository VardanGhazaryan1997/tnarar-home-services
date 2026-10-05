namespace HomeServices.Domain.Catalog;

/// <summary>A labour price range per unit in whole AMD: the lowest usual price, the usual one and the highest usual one.</summary>
public sealed record PriceRange(int Min, int Typical, int Max)
{
    /// <summary>The highest price a range may have (100 million AMD).</summary>
    public const int Limit = 100_000_000;

    /// <summary>True when 0 ≤ min ≤ typical ≤ max ≤ <see cref="Limit"/>.</summary>
    public bool IsValid => Min >= 0 && Min <= Typical && Typical <= Max && Max <= Limit;

    public void EnsureValid()
    {
        if (!IsValid)
        {
            throw new DomainException("price.invalid", $"Prices must satisfy 0 ≤ min ≤ typical ≤ max ≤ {Limit}.");
        }
    }
}
