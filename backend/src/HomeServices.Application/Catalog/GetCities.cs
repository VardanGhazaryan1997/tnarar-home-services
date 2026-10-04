using HomeServices.Application.Abstractions;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Localization;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Catalog;

/// <summary>Active cities with their districts, names in the request language.</summary>
public sealed record GetCities : IQuery<IReadOnlyList<CityDto>>;

public sealed record CityDto(Guid Id, string Slug, string Name, IReadOnlyList<DistrictDto> Districts);

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
                    .ToList()))
            .ToList();
    }

    private string Localize(LocalizedText text) => text.Get(language.Code, language.DefaultCode);
}
