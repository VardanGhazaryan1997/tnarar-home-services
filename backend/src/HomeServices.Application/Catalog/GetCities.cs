using HomeServices.Application.Abstractions;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Localization;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Catalog;

/// <summary>Active towns and villages with their districts, names in the request language, grouped by region.</summary>
public sealed record GetCities : IQuery<IReadOnlyList<CityDto>>;

/// <summary>Yerevan and the regions (marzes), names in the request language.</summary>
public sealed record GetRegions : IQuery<IReadOnlyList<RegionDto>>;

/// <summary>A town or village. <see cref="Kind"/> is "City" or "Village"; <see cref="RegionId"/> is null for places without a region.</summary>
public sealed record CityDto(Guid Id, string Slug, string Name, IReadOnlyList<DistrictDto> Districts, Guid? RegionId = null, string Kind = "City");

public sealed record RegionDto(Guid Id, string Slug, string Name);

public sealed record DistrictDto(Guid Id, string Slug, string Name);

public sealed class GetCitiesHandler(IAppDbContext db, ICurrentLanguage language)
    : IQueryHandler<GetCities, IReadOnlyList<CityDto>>
{
    public async Task<IReadOnlyList<CityDto>> HandleAsync(GetCities query, CancellationToken cancellationToken)
    {
        var cities = await db.Cities
            .AsNoTracking()
            .Include(c => c.Districts)
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);

        return cities
            .Select(c => new CityDto(
                c.Id,
                c.Slug,
                Localize(c.Name),
                c.Districts
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.SortOrder)
                    .Select(d => new DistrictDto(d.Id, d.Slug, Localize(d.Name)))
                    .ToList(),
                c.RegionId,
                c.Kind.ToString()))
            .ToList();
    }

    private string Localize(LocalizedText text) => text.Get(language.Code, language.DefaultCode);
}

public sealed class GetRegionsHandler(IAppDbContext db, ICurrentLanguage language)
    : IQueryHandler<GetRegions, IReadOnlyList<RegionDto>>
{
    public async Task<IReadOnlyList<RegionDto>> HandleAsync(GetRegions query, CancellationToken cancellationToken)
    {
        var regions = await db.Regions.AsNoTracking().OrderBy(r => r.SortOrder).ToListAsync(cancellationToken);
        return regions.Select(r => new RegionDto(r.Id, r.Slug, r.Name.Get(language.Code, language.DefaultCode))).ToList();
    }
}
