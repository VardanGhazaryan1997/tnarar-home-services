using HomeServices.Domain.Common;

namespace HomeServices.Domain.Catalog;

/// <summary>
/// A work item's market range in one region (C2), from the prices of partners who work there and the deals made there.
/// Exists only while enough partners are behind it; elsewhere the national range (on the work item) applies.
/// </summary>
public sealed class RegionalMarketPrice : Entity
{
    private RegionalMarketPrice()
    {
    }

    public Guid WorkItemId { get; private set; }

    public Guid RegionId { get; private set; }

    public int Min { get; private set; }

    public int Typical { get; private set; }

    public int Max { get; private set; }

    /// <summary>How many different partners' prices the range comes from.</summary>
    public int PartnerCount { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public PriceRange Range => new(Min, Typical, Max);

    public static RegionalMarketPrice Create(Guid workItemId, Guid regionId, PriceRange range, int partnerCount, DateTimeOffset now)
    {
        if (workItemId == Guid.Empty || regionId == Guid.Empty)
        {
            throw new DomainException("regional_price.invalid", "A regional price needs a work item and a region.");
        }

        var price = new RegionalMarketPrice { WorkItemId = workItemId, RegionId = regionId };
        price.Update(range, partnerCount, now);
        return price;
    }

    public void Update(PriceRange range, int partnerCount, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(range);
        if (partnerCount < 1)
        {
            throw new DomainException("regional_price.invalid", "A regional price comes from at least one partner.");
        }

        Min = range.Min;
        Typical = range.Typical;
        Max = range.Max;
        PartnerCount = partnerCount;
        UpdatedAt = now;
    }
}
