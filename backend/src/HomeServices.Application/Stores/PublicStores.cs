using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Files;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Stores;

/// <summary>
/// Active stores for customers, in display order, with their branches. With <paramref name="Region"/> (a region slug)
/// only stores with a branch there are listed, and only those branches.
/// </summary>
public sealed record GetPublicStores(string? Region = null) : IQuery<IReadOnlyList<PublicStoreDto>>;

/// <summary>One active store with its branches and its products grouped by material.</summary>
public sealed record GetPublicStore(string Slug) : IQuery<PublicStoreDto>;

/// <summary>
/// Active materials with their usual prices per unit and what the stores ask. <paramref name="Search"/> matches the name
/// (in the request language) and the slug; <paramref name="Region"/> limits the stores counted to those with a branch there.
/// </summary>
public sealed record GetPublicMaterials(string? Search = null, string? Region = null) : IQuery<IReadOnlyList<PublicMaterialDto>>;

/// <summary>
/// One material with every store offer for it, in-stock offers first and then cheapest per unit. <paramref name="Region"/>,
/// <paramref name="Grade"/> and <paramref name="InStockOnly"/> narrow the offers.
/// </summary>
public sealed record GetPublicMaterial(string Slug, string? Region = null, MaterialGrade? Grade = null, bool InStockOnly = false)
    : IQuery<PublicMaterialDetailDto>;

/// <summary>
/// Products on sale today (a sale that has not ended, in stock, a current price, an active store and material), biggest
/// discount first and then the sale that ends soonest. <paramref name="Region"/> keeps only stores with a branch there;
/// <paramref name="Limit"/> is 1–50.
/// </summary>
public sealed record GetOnSale(string? Region = null, int Limit = GetOnSale.DefaultLimit) : IQuery<IReadOnlyList<PublicSaleDto>>
{
    public const int DefaultLimit = 24;
    public const int MaxLimit = 50;
}

/// <summary>A branch as customers see it: the town and district in the request language.</summary>
public sealed record PublicBranchDto(string Name, string City, string? District, string Address, string? Phone, string? OpeningHours);

/// <summary>A store for customers. <see cref="Materials"/> is set only when one store is asked for.</summary>
public sealed record PublicStoreDto(
    string Slug,
    string Name,
    string? Description,
    string? Website,
    string? Phone,
    string? LogoUrl,
    IReadOnlyList<PublicBranchDto> Branches,
    IReadOnlyList<PublicStoreMaterialDto>? Materials = null);

/// <summary>A store's products under one material.</summary>
public sealed record PublicStoreMaterialDto(string Slug, string Name, MaterialUnit Unit, IReadOnlyList<PublicProductDto> Products);

/// <summary>
/// A store product with today's price: <see cref="SalePrice"/> and <see cref="SaleEndsOn"/> are set only while the sale
/// lasts, <see cref="PriceBefore"/> is the usual price about three months ago when it was different, <see cref="CurrentPrice"/> is what a pack costs now and <see cref="UnitPrice"/> is per unit of the material. <see cref="PhotoUrl"/> is a thumbnail, <see cref="PhotoLargeUrl"/> the full-size photo.
/// </summary>
public sealed record PublicProductDto(
    Guid Id,
    string Name,
    string? Brand,
    MaterialGrade Grade,
    string? PackLabel,
    decimal PackSize,
    int Price,
    int? SalePrice,
    DateOnly? SaleEndsOn,
    int CurrentPrice,
    decimal UnitPrice,
    bool InStock,
    string? PhotoUrl,
    DateTimeOffset PriceUpdatedAt,
    int? PriceBefore = null,
    string? PhotoLargeUrl = null);

/// <summary>The price per unit for one grade and where it comes from (staff or stores).</summary>
public sealed record PublicGradePriceDto(int Price, MaterialPriceSource Source);

public sealed record PublicMaterialPricesDto(PublicGradePriceDto Budget, PublicGradePriceDto Standard, PublicGradePriceDto Premium);

/// <summary>
/// A material in the list. <see cref="StoreCount"/> is the stores with a current offer for it and
/// <see cref="LowestUnitPrice"/> the cheapest in-stock offer per unit (null when none).
/// </summary>
public sealed record PublicMaterialDto(
    string Slug,
    string Name,
    MaterialUnit Unit,
    PublicMaterialPricesDto Prices,
    int StoreCount,
    decimal? LowestUnitPrice);

/// <summary>The store behind an offer.</summary>
public sealed record PublicOfferStoreDto(string Slug, string Name, string? LogoUrl, string? Website, IReadOnlyList<PublicBranchDto> Branches);

/// <summary>One store's product for a material, with the store (and its branches in the asked region).</summary>
public sealed record PublicOfferDto(PublicOfferStoreDto Store, PublicProductDto Product);

/// <summary>One material with its offers (see <see cref="GetPublicMaterial"/>).</summary>
public sealed record PublicMaterialDetailDto(
    string Slug,
    string Name,
    MaterialUnit Unit,
    PublicMaterialPricesDto Prices,
    int StoreCount,
    IReadOnlyList<PublicOfferDto> Offers);

/// <summary>One product on sale with the material it is sold under and the store that sells it.</summary>
public sealed record PublicSaleDto(string MaterialSlug, string MaterialName, MaterialUnit Unit, PublicOfferStoreDto Store, PublicProductDto Product);

/// <summary>Loading and mapping shared by the public store queries.</summary>
internal static class PublicStoreData
{
    /// <summary>The ids of the active towns in a region (null when no region was asked for; empty for an unknown region).</summary>
    public static async Task<List<Guid>?> RegionCityIdsAsync(IAppDbContext db, string? region, CancellationToken cancellationToken)
    {
        var slug = region?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(slug))
        {
            return null;
        }

        return await db.Cities.AsNoTracking()
            .Where(c => c.IsActive && db.Regions.Any(r => r.Slug == slug && r.Id == c.RegionId))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Products customers may see: active, linked, of an active store, with a price checked within the fresh period.</summary>
    public static IQueryable<StoreProduct> VisibleProducts(IAppDbContext db, DateTimeOffset freshSince, List<Guid>? cityIds)
    {
        var products = db.StoreProducts.AsNoTracking()
            .Where(p => p.IsActive && p.MaterialId != null && p.PriceUpdatedAt >= freshSince && db.Stores.Any(s => s.Id == p.StoreId && s.IsActive));
        return cityIds is null
            ? products
            : products.Where(p => db.StoreBranches.Any(b => b.StoreId == p.StoreId && cityIds.Contains(b.CityId)));
    }

    public static async Task<Dictionary<Guid, List<PublicBranchDto>>> BranchesAsync(
        IAppDbContext db, ICurrentLanguage language, IReadOnlyCollection<Guid> storeIds, List<Guid>? cityIds, CancellationToken cancellationToken)
    {
        var query = db.StoreBranches.AsNoTracking().Where(b => storeIds.Contains(b.StoreId));
        if (cityIds is not null)
        {
            query = query.Where(b => cityIds.Contains(b.CityId));
        }

        var branches = await query.OrderBy(b => b.SortOrder).ToListAsync(cancellationToken);
        var townIds = branches.Select(b => b.CityId).Distinct().ToList();
        var towns = await db.Cities.AsNoTracking().Include(c => c.Districts).Where(c => townIds.Contains(c.Id)).ToListAsync(cancellationToken);

        return branches
            .GroupBy(b => b.StoreId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(b =>
                {
                    var town = towns.First(c => c.Id == b.CityId);
                    var district = town.Districts.FirstOrDefault(d => d.Id == b.DistrictId);
                    return new PublicBranchDto(
                        b.Name,
                        town.Name.Get(language.Code, language.DefaultCode),
                        district?.Name.Get(language.Code, language.DefaultCode),
                        b.Address,
                        b.Phone,
                        b.OpeningHours);
                }).ToList());
    }

    public static PublicProductDto Product(
        StoreProduct product, DateOnly today, IReadOnlyDictionary<Guid, PictureLink> photos, IReadOnlyDictionary<Guid, int>? pricesBefore = null)
    {
        var photo = product.PhotoFileId is { } photoId && photos.TryGetValue(photoId, out var link) ? link : (PictureLink?)null;
        var onSale = product.SalePrice is not null && today <= product.SaleEndsOn;
        return new PublicProductDto(
            product.Id,
            product.Name,
            product.Brand,
            product.Grade ?? MaterialGrade.Standard,
            product.PackLabel,
            product.PackSize,
            product.Price,
            onSale ? product.SalePrice : null,
            onSale ? product.SaleEndsOn : null,
            product.CurrentPrice(today),
            product.UnitPrice(today),
            product.InStock,
            photo?.Thumbnail,
            product.PriceUpdatedAt,
            pricesBefore is not null && pricesBefore.TryGetValue(product.Id, out var before) ? before : null,
            photo?.Full);
    }

    public static PublicMaterialPricesDto Prices(Material material) => new(
        GradePrice(material, MaterialGrade.Budget),
        GradePrice(material, MaterialGrade.Standard),
        GradePrice(material, MaterialGrade.Premium));

    private static PublicGradePriceDto GradePrice(Material material, MaterialGrade grade) =>
        new(material.PriceFor(grade), material.PriceSourceFor(grade));

    public static string? NullIfEmpty(string text) => string.IsNullOrWhiteSpace(text) ? null : text;
}

public sealed class GetPublicStoresHandler(IAppDbContext db, FileDtoFactory files, ICurrentLanguage language)
    : IQueryHandler<GetPublicStores, IReadOnlyList<PublicStoreDto>>
{
    public async Task<IReadOnlyList<PublicStoreDto>> HandleAsync(GetPublicStores query, CancellationToken cancellationToken)
    {
        var cityIds = await PublicStoreData.RegionCityIdsAsync(db, query.Region, cancellationToken);
        var stores = db.Stores.AsNoTracking().Where(s => s.IsActive);
        if (cityIds is not null)
        {
            stores = stores.Where(s => db.StoreBranches.Any(b => b.StoreId == s.Id && cityIds.Contains(b.CityId)));
        }

        var list = await stores.OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync(cancellationToken);
        var branches = await PublicStoreData.BranchesAsync(db, language, list.Select(s => s.Id).ToList(), cityIds, cancellationToken);
        var logos = await StoreData.PictureUrlsAsync(db, files, list.Select(s => s.LogoFileId), cancellationToken);

        return list
            .Select(s => new PublicStoreDto(
                s.Slug,
                s.Name,
                PublicStoreData.NullIfEmpty(s.Description.Get(language.Code, language.DefaultCode)),
                s.Website,
                s.Phone,
                s.LogoFileId is { } logo ? logos.GetValueOrDefault(logo) : null,
                branches.GetValueOrDefault(s.Id) ?? []))
            .ToList();
    }
}

public sealed class GetPublicStoreHandler(
    IAppDbContext db, FileDtoFactory files, ICurrentLanguage language, IOptions<StorePricingSettings> settings, TimeProvider clock)
    : IQueryHandler<GetPublicStore, PublicStoreDto>
{
    public async Task<PublicStoreDto> HandleAsync(GetPublicStore query, CancellationToken cancellationToken)
    {
        var slug = query.Slug.Trim().ToLowerInvariant();
        var store = await db.Stores.AsNoTracking().SingleOrDefaultAsync(s => s.Slug == slug && s.IsActive, cancellationToken)
            ?? throw new NotFoundException("The store does not exist.", "store.not_found");

        var now = clock.GetUtcNow();
        var today = settings.Value.Today(now);
        var branches = await PublicStoreData.BranchesAsync(db, language, [store.Id], null, cancellationToken);
        var products = await PublicStoreData.VisibleProducts(db, now - settings.Value.FreshFor, null)
            .Where(p => p.StoreId == store.Id)
            .ToListAsync(cancellationToken);

        var materialIds = products.Select(p => p.MaterialId).Distinct().ToList();
        var materials = await db.Materials.AsNoTracking()
            .Where(m => m.IsActive && materialIds.Contains(m.Id))
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken);
        var pictures = await StoreData.PictureLinksAsync(db, files, products.Select(p => p.PhotoFileId).Append(store.LogoFileId), cancellationToken);
        var before = await StorePriceHistory.PriceBeforeAsync(db, settings.Value, now, products, cancellationToken);

        var groups = materials
            .Select(m => new PublicStoreMaterialDto(
                m.Slug,
                m.Name.Get(language.Code, language.DefaultCode),
                m.Unit,
                products
                    .Where(p => p.MaterialId == m.Id)
                    .OrderBy(p => p.Grade)
                    .ThenBy(p => p.UnitPrice(today))
                    .Select(p => PublicStoreData.Product(p, today, pictures, before))
                    .ToList()))
            .ToList();

        return new PublicStoreDto(
            store.Slug,
            store.Name,
            PublicStoreData.NullIfEmpty(store.Description.Get(language.Code, language.DefaultCode)),
            store.Website,
            store.Phone,
            store.LogoFileId is { } logo && pictures.TryGetValue(logo, out var logoLink) ? logoLink.Thumbnail : null,
            branches.GetValueOrDefault(store.Id) ?? [],
            groups);
    }
}

public sealed class GetPublicMaterialsHandler(
    IAppDbContext db, ICurrentLanguage language, IOptions<StorePricingSettings> settings, TimeProvider clock)
    : IQueryHandler<GetPublicMaterials, IReadOnlyList<PublicMaterialDto>>
{
    public async Task<IReadOnlyList<PublicMaterialDto>> HandleAsync(GetPublicMaterials query, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var today = settings.Value.Today(now);
        var cityIds = await PublicStoreData.RegionCityIdsAsync(db, query.Region, cancellationToken);

        var materials = await db.Materials.AsNoTracking().Where(m => m.IsActive).OrderBy(m => m.SortOrder).ToListAsync(cancellationToken);
        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            materials = materials
                .Where(m => m.Slug.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || m.Name.Get(language.Code, language.DefaultCode).Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var materialIds = materials.Select(m => (Guid?)m.Id).ToList();
        var products = await PublicStoreData.VisibleProducts(db, now - settings.Value.FreshFor, cityIds)
            .Where(p => materialIds.Contains(p.MaterialId))
            .ToListAsync(cancellationToken);
        var byMaterial = products.ToLookup(p => p.MaterialId);

        return materials
            .Select(m =>
            {
                var offers = byMaterial[m.Id].ToList();
                var inStock = offers.Where(p => p.InStock).Select(p => p.UnitPrice(today)).ToList();
                return new PublicMaterialDto(
                    m.Slug,
                    m.Name.Get(language.Code, language.DefaultCode),
                    m.Unit,
                    PublicStoreData.Prices(m),
                    offers.Select(p => p.StoreId).Distinct().Count(),
                    inStock.Count == 0 ? null : inStock.Min());
            })
            .ToList();
    }
}

public sealed class GetPublicMaterialHandler(
    IAppDbContext db, FileDtoFactory files, ICurrentLanguage language, IOptions<StorePricingSettings> settings, TimeProvider clock)
    : IQueryHandler<GetPublicMaterial, PublicMaterialDetailDto>
{
    public async Task<PublicMaterialDetailDto> HandleAsync(GetPublicMaterial query, CancellationToken cancellationToken)
    {
        var slug = query.Slug.Trim().ToLowerInvariant();
        var material = await db.Materials.AsNoTracking().SingleOrDefaultAsync(m => m.Slug == slug && m.IsActive, cancellationToken)
            ?? throw new NotFoundException("The material does not exist.", "material.not_found");

        var now = clock.GetUtcNow();
        var today = settings.Value.Today(now);
        var cityIds = await PublicStoreData.RegionCityIdsAsync(db, query.Region, cancellationToken);

        var offers = PublicStoreData.VisibleProducts(db, now - settings.Value.FreshFor, cityIds).Where(p => p.MaterialId == material.Id);
        if (query.Grade is { } grade)
        {
            offers = offers.Where(p => p.Grade == grade);
        }

        if (query.InStockOnly)
        {
            offers = offers.Where(p => p.InStock);
        }

        var products = (await offers.ToListAsync(cancellationToken))
            .OrderByDescending(p => p.InStock)
            .ThenBy(p => p.UnitPrice(today))
            .ToList();

        var storeIds = products.Select(p => p.StoreId).Distinct().ToList();
        var stores = await db.Stores.AsNoTracking().Where(s => storeIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);
        var branches = await PublicStoreData.BranchesAsync(db, language, storeIds, cityIds, cancellationToken);
        var pictures = await StoreData.PictureLinksAsync(
            db, files, products.Select(p => p.PhotoFileId).Concat(stores.Values.Select(s => s.LogoFileId)), cancellationToken);
        var before = await StorePriceHistory.PriceBeforeAsync(db, settings.Value, now, products, cancellationToken);

        var result = products
            .Select(p =>
            {
                var store = stores[p.StoreId];
                return new PublicOfferDto(
                    new PublicOfferStoreDto(
                        store.Slug,
                        store.Name,
                        store.LogoFileId is { } logo && pictures.TryGetValue(logo, out var logoLink) ? logoLink.Thumbnail : null,
                        store.Website,
                        branches.GetValueOrDefault(store.Id) ?? []),
                    PublicStoreData.Product(p, today, pictures, before));
            })
            .ToList();

        return new PublicMaterialDetailDto(
            material.Slug,
            material.Name.Get(language.Code, language.DefaultCode),
            material.Unit,
            PublicStoreData.Prices(material),
            storeIds.Count,
            result);
    }
}

public sealed class GetOnSaleValidator : AbstractValidator<GetOnSale>
{
    public GetOnSaleValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, GetOnSale.MaxLimit).WithErrorCode("limit.invalid");
    }
}

public sealed class GetOnSaleHandler(
    IAppDbContext db, FileDtoFactory files, ICurrentLanguage language, IOptions<StorePricingSettings> settings, TimeProvider clock)
    : IQueryHandler<GetOnSale, IReadOnlyList<PublicSaleDto>>
{
    public async Task<IReadOnlyList<PublicSaleDto>> HandleAsync(GetOnSale query, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var today = settings.Value.Today(now);
        var cityIds = await PublicStoreData.RegionCityIdsAsync(db, query.Region, cancellationToken);

        var onSale = await PublicStoreData.VisibleProducts(db, now - settings.Value.FreshFor, cityIds)
            .Where(p => p.InStock && p.SalePrice != null && p.SaleEndsOn >= today
                && db.Materials.Any(m => m.Id == p.MaterialId && m.IsActive))
            .ToListAsync(cancellationToken);

        var products = onSale
            .OrderByDescending(p => (p.Price - p.SalePrice!.Value) / (double)p.Price)
            .ThenBy(p => p.SaleEndsOn)
            .ThenBy(p => p.Id)
            .Take(query.Limit)
            .ToList();

        var materialIds = products.Select(p => p.MaterialId).Distinct().ToList();
        var materials = await db.Materials.AsNoTracking().Where(m => materialIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id, cancellationToken);
        var storeIds = products.Select(p => p.StoreId).Distinct().ToList();
        var stores = await db.Stores.AsNoTracking().Where(s => storeIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);
        var branches = await PublicStoreData.BranchesAsync(db, language, storeIds, cityIds, cancellationToken);
        var pictures = await StoreData.PictureLinksAsync(
            db, files, products.Select(p => p.PhotoFileId).Concat(stores.Values.Select(s => s.LogoFileId)), cancellationToken);
        var before = await StorePriceHistory.PriceBeforeAsync(db, settings.Value, now, products, cancellationToken);

        return products
            .Select(p =>
            {
                var material = materials[p.MaterialId!.Value];
                var store = stores[p.StoreId];
                return new PublicSaleDto(
                    material.Slug,
                    material.Name.Get(language.Code, language.DefaultCode),
                    material.Unit,
                    new PublicOfferStoreDto(
                        store.Slug,
                        store.Name,
                        store.LogoFileId is { } logo && pictures.TryGetValue(logo, out var logoLink) ? logoLink.Thumbnail : null,
                        store.Website,
                        branches.GetValueOrDefault(store.Id) ?? []),
                    PublicStoreData.Product(p, today, pictures, before));
            })
            .ToList();
    }
}
