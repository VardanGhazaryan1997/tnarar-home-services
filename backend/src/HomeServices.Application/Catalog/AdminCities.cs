using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Languages;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Catalog;

/// <summary>All cities and districts (inactive too), with every translation, for the Back Office.</summary>
public sealed record GetAdminCities : IQuery<IReadOnlyList<AdminCityDto>>;

/// <summary>Yerevan and the regions, with every translation.</summary>
public sealed record GetAdminRegions : IQuery<IReadOnlyList<AdminRegionDto>>;

/// <summary>The fields a staff member edits on a city or district.</summary>
public interface IPlaceFields
{
    string Slug { get; }

    IReadOnlyDictionary<string, string> Name { get; }

    int SortOrder { get; }
}

/// <summary>Where a town or village is: its region (null for none) and whether it is a town or a village.</summary>
public interface ICityPlacement
{
    Guid? RegionId { get; }

    SettlementKind Kind { get; }
}

public sealed record CreateCity(
    string Slug,
    IReadOnlyDictionary<string, string> Name,
    int SortOrder,
    Guid? RegionId = null,
    SettlementKind Kind = SettlementKind.City)
    : ICommand<AdminCityDto>, IPlaceFields, ICityPlacement;

public sealed record UpdateCity(
    Guid Id,
    string Slug,
    IReadOnlyDictionary<string, string> Name,
    int SortOrder,
    Guid? RegionId = null,
    SettlementKind Kind = SettlementKind.City)
    : ICommand<AdminCityDto>, IPlaceFields, ICityPlacement;

/// <summary>Shows or hides a city on the Portal. Cities aren't deleted: partners and orders refer to them.</summary>
public sealed record SetCityActive(Guid Id, bool IsActive) : ICommand<AdminCityDto>;

public sealed record AddDistrict(Guid CityId, string Slug, IReadOnlyDictionary<string, string> Name, int SortOrder)
    : ICommand<AdminDistrictDto>, IPlaceFields;

public sealed record UpdateDistrict(Guid CityId, Guid DistrictId, string Slug, IReadOnlyDictionary<string, string> Name, int SortOrder)
    : ICommand<AdminDistrictDto>, IPlaceFields;

public sealed record SetDistrictActive(Guid CityId, Guid DistrictId, bool IsActive) : ICommand<AdminDistrictDto>;

public abstract class PlaceFieldsValidator<T> : AbstractValidator<T>
    where T : IPlaceFields
{
    protected PlaceFieldsValidator(ILanguageCatalog languages)
    {
        RuleFor(x => x.Slug).ValidSlug();
        RuleFor(x => x.Name).LocalizedName(languages);
        RuleFor(x => x.SortOrder).ValidSortOrder();
    }
}

public abstract class CityFieldsValidator<T> : PlaceFieldsValidator<T>
    where T : IPlaceFields, ICityPlacement
{
    protected CityFieldsValidator(ILanguageCatalog languages, IAppDbContext db)
        : base(languages)
    {
        RuleFor(x => x.Kind).IsInEnum().WithErrorCode("kind.invalid");
        RuleFor(x => x.RegionId)
            .MustAsync((regionId, ct) => db.Regions.AnyAsync(r => r.Id == regionId, ct)).WithErrorCode("region.invalid")
            .When(x => x.RegionId is not null);
    }
}

public sealed class CreateCityValidator(ILanguageCatalog languages, IAppDbContext db) : CityFieldsValidator<CreateCity>(languages, db);

public sealed class UpdateCityValidator(ILanguageCatalog languages, IAppDbContext db) : CityFieldsValidator<UpdateCity>(languages, db);

public sealed class AddDistrictValidator(ILanguageCatalog languages) : PlaceFieldsValidator<AddDistrict>(languages);

public sealed class UpdateDistrictValidator(ILanguageCatalog languages) : PlaceFieldsValidator<UpdateDistrict>(languages);

public sealed class GetAdminCitiesHandler(IAppDbContext db) : IQueryHandler<GetAdminCities, IReadOnlyList<AdminCityDto>>
{
    public async Task<IReadOnlyList<AdminCityDto>> HandleAsync(GetAdminCities query, CancellationToken cancellationToken)
    {
        var cities = await db.Cities.AsNoTracking().Include(c => c.Districts).ToListAsync(cancellationToken);
        return cities
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Slug, StringComparer.Ordinal)
            .Select(AdminCityDto.From)
            .ToList();
    }
}

public sealed class GetAdminRegionsHandler(IAppDbContext db) : IQueryHandler<GetAdminRegions, IReadOnlyList<AdminRegionDto>>
{
    public async Task<IReadOnlyList<AdminRegionDto>> HandleAsync(GetAdminRegions query, CancellationToken cancellationToken)
    {
        var regions = await db.Regions.AsNoTracking().OrderBy(r => r.SortOrder).ToListAsync(cancellationToken);
        return regions.Select(AdminRegionDto.From).ToList();
    }
}

public sealed class CreateCityHandler(IAppDbContext db) : ICommandHandler<CreateCity, AdminCityDto>
{
    public async Task<AdminCityDto> HandleAsync(CreateCity command, CancellationToken cancellationToken)
    {
        await PlaceChecks.EnsureCitySlugFreeAsync(db, command.Slug, exceptId: null, cancellationToken);

        var city = City.Create(command.Slug, LocalizedText.From(command.Name), command.SortOrder, command.RegionId, command.Kind);
        db.Cities.Add(city);
        await db.SaveChangesAsync(cancellationToken);
        return AdminCityDto.From(city);
    }
}

public sealed class UpdateCityHandler(IAppDbContext db) : ICommandHandler<UpdateCity, AdminCityDto>
{
    public async Task<AdminCityDto> HandleAsync(UpdateCity command, CancellationToken cancellationToken)
    {
        var city = await PlaceChecks.LoadCityAsync(db, command.Id, cancellationToken);
        await PlaceChecks.EnsureCitySlugFreeAsync(db, command.Slug, city.Id, cancellationToken);

        city.Update(command.Slug, LocalizedText.From(command.Name), command.SortOrder);
        city.PlaceIn(command.RegionId, command.Kind);
        await db.SaveChangesAsync(cancellationToken);
        return AdminCityDto.From(city);
    }
}

public sealed class SetCityActiveHandler(IAppDbContext db) : ICommandHandler<SetCityActive, AdminCityDto>
{
    public async Task<AdminCityDto> HandleAsync(SetCityActive command, CancellationToken cancellationToken)
    {
        var city = await PlaceChecks.LoadCityAsync(db, command.Id, cancellationToken);
        if (command.IsActive)
        {
            city.Activate();
        }
        else
        {
            city.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);
        return AdminCityDto.From(city);
    }
}

public sealed class AddDistrictHandler(IAppDbContext db) : ICommandHandler<AddDistrict, AdminDistrictDto>
{
    public async Task<AdminDistrictDto> HandleAsync(AddDistrict command, CancellationToken cancellationToken)
    {
        var city = await PlaceChecks.LoadCityAsync(db, command.CityId, cancellationToken);
        PlaceChecks.EnsureDistrictSlugFree(city, command.Slug, exceptId: null);

        var district = city.AddDistrict(command.Slug, LocalizedText.From(command.Name), command.SortOrder);
        db.Districts.Add(district);
        await db.SaveChangesAsync(cancellationToken);
        return AdminDistrictDto.From(district);
    }
}

public sealed class UpdateDistrictHandler(IAppDbContext db) : ICommandHandler<UpdateDistrict, AdminDistrictDto>
{
    public async Task<AdminDistrictDto> HandleAsync(UpdateDistrict command, CancellationToken cancellationToken)
    {
        var city = await PlaceChecks.LoadCityAsync(db, command.CityId, cancellationToken);
        PlaceChecks.EnsureDistrictExists(city, command.DistrictId);
        PlaceChecks.EnsureDistrictSlugFree(city, command.Slug, command.DistrictId);

        var district = city.UpdateDistrict(command.DistrictId, command.Slug, LocalizedText.From(command.Name), command.SortOrder);
        await db.SaveChangesAsync(cancellationToken);
        return AdminDistrictDto.From(district);
    }
}

public sealed class SetDistrictActiveHandler(IAppDbContext db) : ICommandHandler<SetDistrictActive, AdminDistrictDto>
{
    public async Task<AdminDistrictDto> HandleAsync(SetDistrictActive command, CancellationToken cancellationToken)
    {
        var city = await PlaceChecks.LoadCityAsync(db, command.CityId, cancellationToken);
        PlaceChecks.EnsureDistrictExists(city, command.DistrictId);

        var district = city.SetDistrictActive(command.DistrictId, command.IsActive);
        await db.SaveChangesAsync(cancellationToken);
        return AdminDistrictDto.From(district);
    }
}

internal static class PlaceChecks
{
    public static async Task<City> LoadCityAsync(IAppDbContext db, Guid id, CancellationToken cancellationToken) =>
        await db.Cities.Include(c => c.Districts).SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
        ?? throw new NotFoundException("The city does not exist.", "city.not_found");

    public static async Task EnsureCitySlugFreeAsync(IAppDbContext db, string slug, Guid? exceptId, CancellationToken cancellationToken)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        if (await db.Cities.AnyAsync(c => c.Slug == normalized && c.Id != exceptId, cancellationToken))
        {
            throw new ConflictException($"Another city already uses '{normalized}'.", "city.slug_taken");
        }
    }

    public static void EnsureDistrictExists(City city, Guid districtId)
    {
        if (!city.HasDistrict(districtId))
        {
            throw new NotFoundException("The district does not exist in this city.", "district.not_found");
        }
    }

    public static void EnsureDistrictSlugFree(City city, string slug, Guid? exceptId)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        if (city.Districts.Any(d => d.Slug == normalized && d.Id != exceptId))
        {
            throw new ConflictException($"Another district in this city already uses '{normalized}'.", "district.slug_taken");
        }
    }
}
