using HomeServices.Domain.Catalog;
using HomeServices.Domain.Common;

namespace HomeServices.Domain.Partners;

/// <summary>
/// A partner's own labour price for a work item, per the item's unit in AMD: a single price ("from" only) or a range
/// ("from–to"), and whether it includes materials. Partners keep their price list up to date themselves and confirm it
/// now and then (<see cref="ConfirmedAt"/>); a price nobody confirmed for long stops counting (C3).
/// </summary>
public sealed class PartnerPrice : AuditableEntity
{
    private PartnerPrice()
    {
    }

    public Guid PartnerProfileId { get; private set; }

    public Guid WorkItemId { get; private set; }

    /// <summary>The price, or the lower end of the range.</summary>
    public int PriceFrom { get; private set; }

    /// <summary>The upper end of the range; null for a single price.</summary>
    public int? PriceTo { get; private set; }

    public bool IncludesMaterials { get; private set; }

    /// <summary>When the partner last set or confirmed this price.</summary>
    public DateTimeOffset ConfirmedAt { get; private set; }

    public static PartnerPrice Create(Guid partnerProfileId, Guid workItemId, int priceFrom, int? priceTo, bool includesMaterials, DateTimeOffset now)
    {
        var price = new PartnerPrice { PartnerProfileId = partnerProfileId, WorkItemId = workItemId, ConfirmedAt = now };
        price.Change(priceFrom, priceTo, includesMaterials);
        return price;
    }

    /// <summary>The partner says the price is still right.</summary>
    public void Confirm(DateTimeOffset now) => ConfirmedAt = now;

    /// <summary>True when the price was last confirmed more than <paramref name="freshFor"/> ago.</summary>
    public bool IsStale(DateTimeOffset now, TimeSpan freshFor) => ConfirmedAt < now - freshFor;

    /// <summary>Sets the price (a range when <paramref name="priceTo"/> is above <paramref name="priceFrom"/>).</summary>
    public void Change(int priceFrom, int? priceTo, bool includesMaterials)
    {
        if (!IsValid(priceFrom, priceTo))
        {
            throw new DomainException("partner_price.invalid", $"A price must be 1–{PriceRange.Limit} AMD, and 'to' can't be below 'from'.");
        }

        PriceFrom = priceFrom;
        PriceTo = priceTo == priceFrom ? null : priceTo;
        IncludesMaterials = includesMaterials;
    }

    public static bool IsValid(int priceFrom, int? priceTo) =>
        priceFrom >= 1 && priceFrom <= PriceRange.Limit && (priceTo is null || (priceTo >= priceFrom && priceTo <= PriceRange.Limit));
}
