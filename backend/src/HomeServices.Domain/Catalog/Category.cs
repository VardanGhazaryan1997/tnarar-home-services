using HomeServices.Domain.Common;
using HomeServices.Domain.Localization;

namespace HomeServices.Domain.Catalog;

/// <summary>
/// A service category (plumbing, cleaning, …). Categories form a tree through
/// <see cref="ParentId"/>; partners and requests are tagged with them.
/// </summary>
public sealed class Category : SoftDeletableEntity, IAudited
{
    public const int IconMaxLength = 32;

    private Category()
    {
    }

    public string Slug { get; private set; } = string.Empty;

    public LocalizedText Name { get; private set; } = LocalizedText.Empty;

    /// <summary>Icon key used by the frontends, e.g. "pipe".</summary>
    public string? Icon { get; private set; }

    public Guid? ParentId { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public static Category Create(string slug, LocalizedText name, int sortOrder, Guid? parentId = null, string? icon = null)
    {
        EnsureNamed(name);
        return new Category
        {
            Slug = NormalizeSlug(slug),
            Name = name,
            SortOrder = sortOrder,
            ParentId = parentId,
            Icon = NormalizeIcon(icon),
            IsActive = true,
        };
    }

    public void Rename(LocalizedText name)
    {
        EnsureNamed(name);
        Name = name;
    }

    /// <summary>Changes the URL key. The caller checks that no other category uses it.</summary>
    public void ChangeSlug(string slug) => Slug = NormalizeSlug(slug);

    /// <summary>Sets or clears (blank) the icon key.</summary>
    public void ChangeIcon(string? icon) => Icon = NormalizeIcon(icon);

    /// <summary>Moves the category under another one, or to the top level (null).</summary>
    public void MoveTo(Guid? parentId)
    {
        if (parentId == Id)
        {
            throw new DomainException("category.parent_invalid", "A category can't be its own parent.");
        }

        ParentId = parentId;
    }

    public void Reorder(int sortOrder) => SortOrder = sortOrder;

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static string NormalizeSlug(string slug) => Common.Slug.Normalize(slug, "category.slug_invalid");

    private static string? NormalizeIcon(string? icon)
    {
        var trimmed = icon?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        if (trimmed.Length > IconMaxLength)
        {
            throw new DomainException("category.icon_too_long", $"The icon key can be at most {IconMaxLength} characters.");
        }

        return trimmed;
    }

    private static void EnsureNamed(LocalizedText name)
    {
        if (name.Values.Count == 0)
        {
            throw new DomainException("category.name_required", "A category needs a name in at least one language.");
        }
    }
}
