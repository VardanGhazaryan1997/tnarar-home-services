using HomeServices.Domain.Common;
using HomeServices.Domain.Localization;

namespace HomeServices.Domain.Catalog;

/// <summary>An area within a city. Created and changed through its <see cref="City"/>.</summary>
public sealed class District : Entity, IAudited
{
    private District()
    {
    }

    public Guid CityId { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public LocalizedText Name { get; private set; } = LocalizedText.Empty;

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    internal static District Create(Guid cityId, string slug, LocalizedText name, int sortOrder)
    {
        EnsureNamed(name);
        return new District
        {
            CityId = cityId,
            Slug = Common.Slug.Normalize(slug, "district.slug_invalid"),
            Name = name,
            SortOrder = sortOrder,
            IsActive = true,
        };
    }

    /// <summary>Called by <see cref="City.UpdateDistrict"/> with an already normalized, unique slug.</summary>
    internal void Update(string slug, LocalizedText name, int sortOrder)
    {
        EnsureNamed(name);
        Slug = slug;
        Name = name;
        SortOrder = sortOrder;
    }

    internal void SetActive(bool isActive) => IsActive = isActive;

    private static void EnsureNamed(LocalizedText name)
    {
        if (name.Values.Count == 0)
        {
            throw new DomainException("district.name_required", "A district needs a name in at least one language.");
        }
    }
}
