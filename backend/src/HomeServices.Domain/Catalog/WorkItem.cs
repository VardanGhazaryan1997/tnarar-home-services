using HomeServices.Domain.Common;
using HomeServices.Domain.Localization;

namespace HomeServices.Domain.Catalog;

/// <summary>
/// A unit of work a partner prices and a customer can estimate, e.g. "Wall plastering" per m². Work items sit under a
/// subcategory ("Plastering and putty"); price lists, price ranges and the estimator are built on them.
/// </summary>
public sealed class WorkItem : SoftDeletableEntity, IAudited
{
    private WorkItem()
    {
    }

    /// <summary>The subcategory the work belongs to.</summary>
    public Guid CategoryId { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public LocalizedText Name { get; private set; } = LocalizedText.Empty;

    /// <summary>What a price is quoted per.</summary>
    public WorkUnit Unit { get; private set; }

    /// <summary>The room surface the work is done on, so the estimator can work out the quantity from room sizes.</summary>
    public WorkSurface Surface { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Lowest usual labour price per unit in AMD, set by staff; null until priced.</summary>
    public int? PriceMin { get; private set; }

    /// <summary>The usual labour price per unit in AMD.</summary>
    public int? PriceTypical { get; private set; }

    /// <summary>Highest usual labour price per unit in AMD.</summary>
    public int? PriceMax { get; private set; }

    /// <summary>
    /// True when staff fixed the price range: partners' own prices won't move it (the market range is worked out from
    /// partner prices once enough partners priced the item).
    /// </summary>
    public bool IsPriceLocked { get; private set; }

    /// <summary>
    /// The usual market labour price per unit, as customers and partners see it: partners' prices once enough partners
    /// priced the item (and staff didn't lock it), otherwise the staff range. Null when there is neither.
    /// </summary>
    public int? MarketMin { get; private set; }

    public int? MarketTypical { get; private set; }

    public int? MarketMax { get; private set; }

    public MarketPriceSource MarketSource { get; private set; }

    /// <summary>How many partners' prices the last calculation counted.</summary>
    public int MarketPartnerCount { get; private set; }

    public DateTimeOffset? MarketUpdatedAt { get; private set; }

    /// <summary>The market range, or null without prices.</summary>
    public PriceRange? Market => MarketMin is { } min && MarketTypical is { } typical && MarketMax is { } max ? new PriceRange(min, typical, max) : null;

    /// <summary>The staff price range, or null when the item has no prices yet.</summary>
    public PriceRange? Price => PriceMin is { } min && PriceTypical is { } typical && PriceMax is { } max ? new PriceRange(min, typical, max) : null;

    public static WorkItem Create(Guid categoryId, string slug, LocalizedText name, WorkUnit unit, WorkSurface surface, int sortOrder)
    {
        EnsureNamed(name);
        EnsureDefined(unit, surface);
        return new WorkItem
        {
            CategoryId = categoryId,
            Slug = NormalizeSlug(slug),
            Name = name,
            Unit = unit,
            Surface = surface,
            SortOrder = sortOrder,
            IsActive = true,
        };
    }

    /// <summary>Changes every edited field at once. The caller checks the category and that the slug is free.</summary>
    public void Update(Guid categoryId, string slug, LocalizedText name, WorkUnit unit, WorkSurface surface, int sortOrder)
    {
        EnsureNamed(name);
        EnsureDefined(unit, surface);
        CategoryId = categoryId;
        Slug = NormalizeSlug(slug);
        Name = name;
        Unit = unit;
        Surface = surface;
        SortOrder = sortOrder;
    }

    /// <summary>Sets the staff price range (labour, AMD per unit), or clears it with null.</summary>
    public void SetPrice(PriceRange? price)
    {
        price?.EnsureValid();
        PriceMin = price?.Min;
        PriceTypical = price?.Typical;
        PriceMax = price?.Max;
        if (MarketSource == MarketPriceSource.Staff)
        {
            SetMarket(price);
        }
    }

    /// <summary>
    /// Updates the market range from partners' prices: <paramref name="partnerRange"/> from <paramref name="partnerCount"/>
    /// partners counts once there are at least <paramref name="minPartners"/> and the staff range isn't locked; otherwise
    /// the market range is the staff range.
    /// </summary>
    public void UpdateMarket(PriceRange? partnerRange, int partnerCount, int minPartners, DateTimeOffset now)
    {
        var fromPartners = !IsPriceLocked && partnerRange is not null && partnerCount >= minPartners;
        partnerRange?.EnsureValid();
        MarketSource = fromPartners ? MarketPriceSource.Partners : MarketPriceSource.Staff;
        SetMarket(fromPartners ? partnerRange : Price);
        MarketPartnerCount = partnerCount;
        MarketUpdatedAt = now;
    }

    private void SetMarket(PriceRange? range)
    {
        MarketMin = range?.Min;
        MarketTypical = range?.Typical;
        MarketMax = range?.Max;
    }

    /// <summary>Fixes the staff price range (true) or lets partner prices adjust it (false).</summary>
    public void LockPrice(bool locked)
    {
        IsPriceLocked = locked;
        if (locked)
        {
            MarketSource = MarketPriceSource.Staff;
            SetMarket(Price);
        }
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static string NormalizeSlug(string slug) => Common.Slug.Normalize(slug, "work_item.slug_invalid");

    private static void EnsureNamed(LocalizedText name)
    {
        if (name.Values.Count == 0)
        {
            throw new DomainException("work_item.name_required", "A work item needs a name in at least one language.");
        }
    }

    private static void EnsureDefined(WorkUnit unit, WorkSurface surface)
    {
        if (!Enum.IsDefined(unit))
        {
            throw new DomainException("work_item.unit_invalid", "Unknown unit.");
        }

        if (!Enum.IsDefined(surface))
        {
            throw new DomainException("work_item.surface_invalid", "Unknown surface.");
        }
    }
}
