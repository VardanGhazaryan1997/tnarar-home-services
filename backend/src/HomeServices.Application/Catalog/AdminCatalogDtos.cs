using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;

namespace HomeServices.Application.Catalog;

/// <summary>A category as the Back Office edits it: every translation, inactive ones included.</summary>
public sealed record AdminCategoryDto(
    Guid Id,
    string Slug,
    IReadOnlyDictionary<string, string> Name,
    string? Icon,
    Guid? ParentId,
    int SortOrder,
    bool IsActive)
{
    public static AdminCategoryDto From(Category category) =>
        new(category.Id, category.Slug, Translations(category.Name), category.Icon, category.ParentId, category.SortOrder, category.IsActive);

    internal static Dictionary<string, string> Translations(LocalizedText text) => new(text.Values, StringComparer.Ordinal);
}

/// <summary>A node of the Back Office category tree.</summary>
public sealed record AdminCategoryNodeDto(
    Guid Id,
    string Slug,
    IReadOnlyDictionary<string, string> Name,
    string? Icon,
    Guid? ParentId,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<AdminCategoryNodeDto> Children);

public sealed record AdminDistrictDto(
    Guid Id,
    Guid CityId,
    string Slug,
    IReadOnlyDictionary<string, string> Name,
    int SortOrder,
    bool IsActive)
{
    public static AdminDistrictDto From(District district) =>
        new(district.Id, district.CityId, district.Slug, AdminCategoryDto.Translations(district.Name), district.SortOrder, district.IsActive);
}

public sealed record AdminCityDto(
    Guid Id,
    string Slug,
    IReadOnlyDictionary<string, string> Name,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<AdminDistrictDto> Districts,
    Guid? RegionId = null,
    string Kind = "City")
{
    public static AdminCityDto From(City city) =>
        new(
            city.Id,
            city.Slug,
            AdminCategoryDto.Translations(city.Name),
            city.SortOrder,
            city.IsActive,
            city.Districts
                .OrderBy(d => d.SortOrder)
                .ThenBy(d => d.Slug, StringComparer.Ordinal)
                .Select(AdminDistrictDto.From)
                .ToList(),
            city.RegionId,
            city.Kind.ToString());
}

/// <summary>A region with every translation, for the Back Office.</summary>
public sealed record AdminRegionDto(Guid Id, string Slug, IReadOnlyDictionary<string, string> Name, int SortOrder)
{
    public static AdminRegionDto From(Region region) =>
        new(region.Id, region.Slug, AdminCategoryDto.Translations(region.Name), region.SortOrder);
}
