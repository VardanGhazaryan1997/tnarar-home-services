using HomeServices.Domain.Common;
using HomeServices.Domain.Localization;

namespace HomeServices.Domain.Catalog;

/// <summary>A city the platform serves, with optional districts (Yerevan has 12).</summary>
public sealed class City : Entity, IAudited
{
    private readonly List<District> _districts = [];

    private City()
    {
    }

    public string Slug { get; private set; } = string.Empty;

    public LocalizedText Name { get; private set; } = LocalizedText.Empty;

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<District> Districts => _districts.AsReadOnly();

    public static City Create(string slug, LocalizedText name, int sortOrder)
    {
        EnsureNamed(name);
        return new City
        {
            Slug = Common.Slug.Normalize(slug, "city.slug_invalid"),
            Name = name,
            SortOrder = sortOrder,
            IsActive = true,
        };
    }

    /// <summary>Changes slug, name and position. The caller checks that no other city uses the slug.</summary>
    public void Update(string slug, LocalizedText name, int sortOrder)
    {
        EnsureNamed(name);
        Slug = Common.Slug.Normalize(slug, "city.slug_invalid");
        Name = name;
        SortOrder = sortOrder;
    }

    public District AddDistrict(string slug, LocalizedText name, int sortOrder)
    {
        var district = District.Create(Id, slug, name, sortOrder);
        EnsureDistrictSlugFree(district.Slug, exceptId: null);
        _districts.Add(district);
        return district;
    }

    public District UpdateDistrict(Guid districtId, string slug, LocalizedText name, int sortOrder)
    {
        var district = FindDistrict(districtId);
        var normalized = Common.Slug.Normalize(slug, "district.slug_invalid");
        EnsureDistrictSlugFree(normalized, exceptId: districtId);
        district.Update(normalized, name, sortOrder);
        return district;
    }

    public District SetDistrictActive(Guid districtId, bool isActive)
    {
        var district = FindDistrict(districtId);
        district.SetActive(isActive);
        return district;
    }

    public bool HasDistrict(Guid districtId) => _districts.Exists(d => d.Id == districtId);

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private District FindDistrict(Guid districtId) =>
        _districts.Find(d => d.Id == districtId)
        ?? throw new DomainException("district.not_found", "The district does not belong to this city.");

    private void EnsureDistrictSlugFree(string slug, Guid? exceptId)
    {
        if (_districts.Exists(d => d.Slug == slug && d.Id != exceptId))
        {
            throw new DomainException("district.slug_taken", $"'{slug}' already exists in this city.");
        }
    }

    private static void EnsureNamed(LocalizedText name)
    {
        if (name.Values.Count == 0)
        {
            throw new DomainException("city.name_required", "A city needs a name in at least one language.");
        }
    }
}
