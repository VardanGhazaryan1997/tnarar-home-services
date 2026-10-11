using HomeServices.Domain.Common;
using HomeServices.Domain.Localization;

namespace HomeServices.Domain.Catalog;

/// <summary>
/// A building material the estimator counts, e.g. "Finishing putty" per kg, with a usual price per unit in AMD for each
/// <see cref="MaterialGrade"/> (budget ≤ standard ≤ premium). Work items say how much of which materials they use (see
/// <see cref="WorkItemMaterial"/>). Staff keep the list and prices in the Back Office.
/// </summary>
public sealed class Material : Entity, IAudited
{
    /// <summary>The highest price per unit staff can set (AMD).</summary>
    public const int PriceLimit = 10_000_000;

    private Material()
    {
    }

    public string Slug { get; private set; } = string.Empty;

    public LocalizedText Name { get; private set; } = LocalizedText.Empty;

    public MaterialUnit Unit { get; private set; }

    public int PriceBudget { get; private set; }

    public int PriceStandard { get; private set; }

    public int PricePremium { get; private set; }

    public int SortOrder { get; private set; }

    /// <summary>Hidden materials aren't counted in estimates.</summary>
    public bool IsActive { get; private set; }

    /// <summary>When staff last set or checked the prices; store prices drift, so they're checked now and then.</summary>
    public DateTimeOffset PricesCheckedAt { get; private set; }

    /// <summary>
    /// The usual price per unit worked out from store prices (see <c>StoreProduct</c>), per grade; null while too few
    /// stores sell materials of that grade. Calculated, so changes aren't written to the audit log.
    /// </summary>
    [NotAudited]
    public int? MarketBudget { get; private set; }

    [NotAudited]
    public int? MarketStandard { get; private set; }

    [NotAudited]
    public int? MarketPremium { get; private set; }

    /// <summary>How many stores' prices the last calculation counted (distinct stores across all grades).</summary>
    [NotAudited]
    public int MarketStoreCount { get; private set; }

    [NotAudited]
    public DateTimeOffset? MarketUpdatedAt { get; private set; }

    /// <summary>True when staff keep their own prices: store prices are shown but don't change estimates.</summary>
    public bool IsPriceLocked { get; private set; }

    public static Material Create(
        string slug, LocalizedText name, MaterialUnit unit, int priceBudget, int priceStandard, int pricePremium, int sortOrder, DateTimeOffset now)
    {
        var material = new Material { IsActive = true, PricesCheckedAt = now };
        material.Update(slug, name, unit, priceBudget, priceStandard, pricePremium, sortOrder, now);
        return material;
    }

    /// <summary>
    /// Changes every edited field at once; new prices count as checked <paramref name="now"/>. The caller checks that the
    /// slug is free.
    /// </summary>
    public void Update(
        string slug, LocalizedText name, MaterialUnit unit, int priceBudget, int priceStandard, int pricePremium, int sortOrder, DateTimeOffset now)
    {
        if (name.Values.Count == 0)
        {
            throw new DomainException("material.name_required", "A material needs a name in at least one language.");
        }

        if (!Enum.IsDefined(unit))
        {
            throw new DomainException("material.unit_invalid", "Unknown unit.");
        }

        if (!ArePricesValid(priceBudget, priceStandard, pricePremium))
        {
            throw new DomainException("material.price_invalid", $"Prices are 1–{PriceLimit} AMD, budget ≤ standard ≤ premium.");
        }

        if (priceBudget != PriceBudget || priceStandard != PriceStandard || pricePremium != PricePremium)
        {
            PricesCheckedAt = now;
        }

        Slug = Common.Slug.Normalize(slug, "material.slug_invalid");
        Name = name;
        Unit = unit;
        PriceBudget = priceBudget;
        PriceStandard = priceStandard;
        PricePremium = pricePremium;
        SortOrder = sortOrder;
    }

    public static bool ArePricesValid(int budget, int standard, int premium) =>
        budget >= 1 && budget <= standard && standard <= premium && premium <= PriceLimit;

    /// <summary>
    /// The price per unit for materials of this grade: the store-based price when staff didn't lock the material and
    /// stores gave a price for the grade, otherwise the staff price.
    /// </summary>
    public int PriceFor(MaterialGrade grade) => UsableMarketFor(grade) ?? StaffPriceFor(grade);

    /// <summary>Where <see cref="PriceFor"/> gets the price for this grade, for display.</summary>
    public MaterialPriceSource PriceSourceFor(MaterialGrade grade) =>
        UsableMarketFor(grade) is null ? MaterialPriceSource.Staff : MaterialPriceSource.Stores;

    /// <summary>The price staff set for this grade, whatever the stores ask.</summary>
    public int StaffPriceFor(MaterialGrade grade) => grade switch
    {
        MaterialGrade.Budget => PriceBudget,
        MaterialGrade.Premium => PricePremium,
        _ => PriceStandard,
    };

    /// <summary>The store-based price for this grade (null when there is none), even when staff locked the material.</summary>
    public int? MarketPriceFor(MaterialGrade grade) => grade switch
    {
        MaterialGrade.Budget => MarketBudget,
        MaterialGrade.Premium => MarketPremium,
        _ => MarketStandard,
    };

    /// <summary>Stores the prices worked out from store products; null for a grade without enough stores.</summary>
    public void UpdateMarket(int? budget, int? standard, int? premium, int storeCount, DateTimeOffset now)
    {
        if (!IsMarketPriceValid(budget) || !IsMarketPriceValid(standard) || !IsMarketPriceValid(premium) || storeCount < 0)
        {
            throw new DomainException("material.price_invalid", $"Store prices are 1–{PriceLimit} AMD.");
        }

        MarketBudget = budget;
        MarketStandard = standard;
        MarketPremium = premium;
        MarketStoreCount = storeCount;
        MarketUpdatedAt = now;
    }

    /// <summary>Keeps the staff prices (true) or lets store prices drive the estimator (false).</summary>
    public void LockPrice(bool locked) => IsPriceLocked = locked;

    private int? UsableMarketFor(MaterialGrade grade) => IsPriceLocked ? null : MarketPriceFor(grade);

    private static bool IsMarketPriceValid(int? price) => price is null or (>= 1 and <= PriceLimit);

    /// <summary>Staff checked the prices and they're still right.</summary>
    public void ConfirmPrices(DateTimeOffset now) => PricesCheckedAt = now;

    /// <summary>True when the prices weren't checked within <paramref name="checkEvery"/>.</summary>
    public bool PricesNeedChecking(DateTimeOffset now, TimeSpan checkEvery) => PricesCheckedAt < now - checkEvery;

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
