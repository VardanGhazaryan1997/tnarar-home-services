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
