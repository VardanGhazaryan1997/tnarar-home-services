using HomeServices.Application.Abstractions;
using HomeServices.Application.Catalog;
using HomeServices.Application.Errors;
using HomeServices.Application.Files;
using HomeServices.Domain;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Files;
using HomeServices.Domain.Stores;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Stores;

/// <summary>A branch of a store as staff see it.</summary>
public sealed record AdminStoreBranchDto(Guid Id, string Name, Guid CityId, Guid? DistrictId, string Address, string? Phone, string? OpeningHours);

/// <summary>
/// A store for the Back Office, with how many branches and products it has: <see cref="LinkedCount"/> are linked to a
/// material (compared), <see cref="StaleCount"/> have a price nobody checked lately. <see cref="Branches"/> is set only
/// when one store is asked for.
/// </summary>
public sealed record AdminStoreDto(
    Guid Id,
    string Slug,
    string Name,
    IReadOnlyDictionary<string, string> Description,
    string? Website,
    string? Phone,
    Guid? LogoFileId,
    string? LogoUrl,
    int SortOrder,
    bool IsActive,
    int BranchCount,
    int ProductCount,
    int LinkedCount,
    int StaleCount,
    IReadOnlyList<AdminStoreBranchDto>? Branches = null);

/// <summary>
/// A store's product with its price today: <see cref="CurrentPrice"/> is the sale price while the sale lasts;
/// <see cref="UnitPrice"/> is per unit of the material (null while not linked).
/// </summary>
public sealed record AdminStoreProductDto(
    Guid Id,
    Guid StoreId,
    Guid? MaterialId,
    string? MaterialName,
    MaterialUnit? Unit,
    MaterialGrade? Grade,
    string Name,
    string? Brand,
    string? Code,
    decimal PackSize,
    string? PackLabel,
    int Price,
    int? SalePrice,
    DateOnly? SaleEndsOn,
    bool SaleActive,
    int CurrentPrice,
    decimal? UnitPrice,
    bool InStock,
    Guid? PhotoFileId,
    string? PhotoUrl,
    bool IsActive,
    DateTimeOffset PriceUpdatedAt,
    bool IsStale);

/// <summary>One store's product that is compared under a material, for the material's comparison in the Back Office.</summary>
public sealed record AdminMaterialOfferDto(Guid StoreId, string StoreName, bool StoreIsActive, AdminStoreProductDto Product);

internal sealed record StoreCounts(int Branches, int Products, int Linked, int Stale);

/// <summary>Loading and mapping shared by the store handlers.</summary>
internal static class StoreData
{
    public static async Task<Store> LoadAsync(IAppDbContext db, Guid id, bool withBranches, CancellationToken cancellationToken)
    {
        IQueryable<Store> stores = db.Stores;
        if (withBranches)
        {
            stores = stores.Include(s => s.Branches);
        }

        return await stores.SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException("The store does not exist.", "store.not_found");
    }

    public static async Task<StoreProduct> LoadProductAsync(IAppDbContext db, Guid storeId, Guid productId, CancellationToken cancellationToken) =>
        await db.StoreProducts.SingleOrDefaultAsync(p => p.Id == productId && p.StoreId == storeId, cancellationToken)
        ?? throw new NotFoundException("The product does not exist.", "store_product.not_found");

    public static async Task EnsureSlugFreeAsync(IAppDbContext db, string slug, Guid? exceptId, CancellationToken cancellationToken)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        if (await db.Stores.AnyAsync(s => s.Slug == normalized && s.Id != exceptId, cancellationToken))
        {
            throw new ConflictException($"Another store already uses '{normalized}'.", "store.slug_taken");
        }
    }

    public static async Task EnsureCodeFreeAsync(IAppDbContext db, Guid storeId, string? code, Guid? exceptId, CancellationToken cancellationToken)
    {
        var normalized = code?.Trim();
        if (!string.IsNullOrEmpty(normalized)
            && await db.StoreProducts.AnyAsync(p => p.StoreId == storeId && p.Code == normalized && p.Id != exceptId, cancellationToken))
        {
            throw new ConflictException($"This store already has a product with the code '{normalized}'.", "store_product.code_taken");
        }
    }

    /// <summary>A logo or photo must be an uploaded, checked image.</summary>
    public static async Task EnsureImageAsync(IAppDbContext db, Guid? fileId, CancellationToken cancellationToken)
    {
        if (fileId is { } id
            && !await db.Files.AnyAsync(f => f.Id == id && f.Kind == FileKind.Image && f.Status == FileStatus.Ready, cancellationToken))
        {
            throw new DomainException("store.file_invalid", "The picture was not uploaded or is not an image.");
        }
    }

    public static async Task EnsureMaterialAsync(IAppDbContext db, Guid materialId, CancellationToken cancellationToken)
    {
        if (!await db.Materials.AnyAsync(m => m.Id == materialId, cancellationToken))
        {
            throw new DomainException("store_product.material_not_found", "The material no longer exists.");
        }
    }

    /// <summary>Links the product to the material (with its grade), or unlinks it when <paramref name="materialId"/> is null.</summary>
    public static async Task ApplyLinkAsync(IAppDbContext db, StoreProduct product, Guid? materialId, MaterialGrade? grade, CancellationToken cancellationToken)
    {
        if (materialId is null)
        {
            product.Unlink();
            return;
        }

        await EnsureMaterialAsync(db, materialId.Value, cancellationToken);
        product.LinkTo(materialId.Value, grade);
    }

    /// <summary>Thumbnail links (full-size when there is no thumbnail) for the ready files among <paramref name="fileIds"/>.</summary>
    public static async Task<Dictionary<Guid, string>> PictureUrlsAsync(
        IAppDbContext db, FileDtoFactory files, IEnumerable<Guid?> fileIds, CancellationToken cancellationToken)
    {
        var ids = fileIds.OfType<Guid>().Distinct().ToList();
        var urls = new Dictionary<Guid, string>();
        if (ids.Count == 0)
        {
            return urls;
        }

        var stored = await db.Files.AsNoTracking().Where(f => ids.Contains(f.Id) && f.Status == FileStatus.Ready).ToListAsync(cancellationToken);
        foreach (var file in stored)
        {
            var dto = await files.CreateAsync(file, cancellationToken);
            var link = dto.ThumbnailUrl ?? dto.Url;
            if (link is not null)
            {
                urls[file.Id] = link;
            }
        }

        return urls;
    }

    /// <summary>
    /// Works out the market prices of the materials again after their store products changed (the products must be saved).
    /// </summary>
    public static async Task RefreshMaterialPricesAsync(
        IAppDbContext db, StorePricingSettings settings, TimeProvider clock, IEnumerable<Guid?> materialIds, CancellationToken cancellationToken)
    {
        var ids = materialIds.OfType<Guid>().Distinct().ToList();
        if (ids.Count == 0)
        {
            return;
        }

        await MaterialMarketPrices.RecalculateAsync(db, settings, clock, ids, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task<Dictionary<Guid, StoreCounts>> CountsAsync(
        IAppDbContext db, IReadOnlyCollection<Guid> storeIds, DateTimeOffset staleBefore, CancellationToken cancellationToken)
    {
        var branches = await db.StoreBranches.AsNoTracking()
            .Where(b => storeIds.Contains(b.StoreId))
            .GroupBy(b => b.StoreId)
            .Select(g => new { StoreId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.StoreId, g => g.Count, cancellationToken);
        var products = await db.StoreProducts.AsNoTracking()
            .Where(p => storeIds.Contains(p.StoreId))
            .GroupBy(p => p.StoreId)
            .Select(g => new
            {
                StoreId = g.Key,
                Total = g.Count(),
                Linked = g.Count(p => p.MaterialId != null),
                Stale = g.Count(p => p.PriceUpdatedAt < staleBefore),
            })
            .ToDictionaryAsync(g => g.StoreId, cancellationToken);

        return storeIds.ToDictionary(
            id => id,
            id => products.TryGetValue(id, out var p)
                ? new StoreCounts(branches.GetValueOrDefault(id), p.Total, p.Linked, p.Stale)
                : new StoreCounts(branches.GetValueOrDefault(id), 0, 0, 0));
    }

    public static async Task<List<AdminStoreDto>> StoreDtosAsync(
        IAppDbContext db, FileDtoFactory files, StorePricingSettings settings, DateTimeOffset now, IReadOnlyCollection<Store> stores, bool withBranches, CancellationToken cancellationToken)
    {
        var counts = await CountsAsync(db, stores.Select(s => s.Id).ToList(), now - settings.FreshFor, cancellationToken);
        var logos = await PictureUrlsAsync(db, files, stores.Select(s => s.LogoFileId), cancellationToken);
        return stores.Select(s =>
        {
            var count = counts[s.Id];
            return new AdminStoreDto(
                s.Id,
                s.Slug,
                s.Name,
                AdminCategoryDto.Translations(s.Description),
                s.Website,
                s.Phone,
                s.LogoFileId,
                s.LogoFileId is { } logo ? logos.GetValueOrDefault(logo) : null,
                s.SortOrder,
                s.IsActive,
                count.Branches,
                count.Products,
                count.Linked,
                count.Stale,
                withBranches
                    ? s.Branches.OrderBy(b => b.SortOrder).Select(b => new AdminStoreBranchDto(b.Id, b.Name, b.CityId, b.DistrictId, b.Address, b.Phone, b.OpeningHours)).ToList()
                    : null);
        }).ToList();
    }

    public static async Task<AdminStoreDto> StoreDtoAsync(
        IAppDbContext db, FileDtoFactory files, StorePricingSettings settings, DateTimeOffset now, Store store, bool withBranches, CancellationToken cancellationToken) =>
        (await StoreDtosAsync(db, files, settings, now, [store], withBranches, cancellationToken))[0];

    public static async Task<List<AdminStoreProductDto>> ProductDtosAsync(
        IAppDbContext db,
        FileDtoFactory files,
        ICurrentLanguage language,
        StorePricingSettings settings,
        DateTimeOffset now,
        IReadOnlyCollection<StoreProduct> products,
        CancellationToken cancellationToken)
    {
        var materialIds = products.Select(p => p.MaterialId).OfType<Guid>().Distinct().ToList();
        var materials = await db.Materials.AsNoTracking().Where(m => materialIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id, cancellationToken);
        var photos = await PictureUrlsAsync(db, files, products.Select(p => p.PhotoFileId), cancellationToken);
        var today = settings.Today(now);

        return products.Select(p =>
        {
            var material = p.MaterialId is { } id ? materials.GetValueOrDefault(id) : null;
            return new AdminStoreProductDto(
                p.Id,
                p.StoreId,
                p.MaterialId,
                material?.Name.Get(language.Code, language.DefaultCode),
                material?.Unit,
                p.Grade,
                p.Name,
                p.Brand,
                p.Code,
                p.PackSize,
                p.PackLabel,
                p.Price,
                p.SalePrice,
                p.SaleEndsOn,
                p.SalePrice is not null && today <= p.SaleEndsOn,
                p.CurrentPrice(today),
                p.MaterialId is null ? null : p.UnitPrice(today),
                p.InStock,
                p.PhotoFileId,
                p.PhotoFileId is { } photo ? photos.GetValueOrDefault(photo) : null,
                p.IsActive,
                p.PriceUpdatedAt,
                p.IsStale(now, settings.FreshFor));
        }).ToList();
    }

    public static async Task<AdminStoreProductDto> ProductDtoAsync(
        IAppDbContext db,
        FileDtoFactory files,
        ICurrentLanguage language,
        StorePricingSettings settings,
        DateTimeOffset now,
        StoreProduct product,
        CancellationToken cancellationToken) =>
        (await ProductDtosAsync(db, files, language, settings, now, [product], cancellationToken))[0];
}
