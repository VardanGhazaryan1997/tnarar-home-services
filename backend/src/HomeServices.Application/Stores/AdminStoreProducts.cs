using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Files;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Stores;

/// <summary>
/// A store's products, by name, paged. <c>Linked</c> filters by whether a product is linked to a material, <c>Stale</c>
/// by whether its price wasn't checked within the fresh period; <c>Search</c> matches name, brand and code.
/// </summary>
public sealed record GetAdminStoreProducts(
    Guid StoreId,
    string? Search = null,
    Guid? MaterialId = null,
    bool? Linked = null,
    bool? Stale = null,
    int Page = 1,
    int PageSize = GetAdminStoreProducts.DefaultPageSize) : IQuery<PagedResult<AdminStoreProductDto>>
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;
}

/// <summary>Every product linked to a material, across stores, for the material's comparison in the Back Office.</summary>
public sealed record GetMaterialStoreProducts(Guid MaterialId) : IQuery<IReadOnlyList<AdminMaterialOfferDto>>;

/// <summary>The fields a staff member edits on a store product.</summary>
public interface IStoreProductFields
{
    string Name { get; }

    string? Brand { get; }

    /// <summary>The store's own product code; unique within the store.</summary>
    string? Code { get; }

    /// <summary>How much of the material's unit one pack holds.</summary>
    decimal PackSize { get; }

    string? PackLabel { get; }

    /// <summary>The price of one pack in AMD.</summary>
    int Price { get; }

    int? SalePrice { get; }

    /// <summary>The last day of the sale; set exactly when there is a sale price.</summary>
    DateOnly? SaleEndsOn { get; }

    bool InStock { get; }

    Guid? PhotoFileId { get; }

    bool IsActive { get; }

    /// <summary>The material the product is compared under; null while not linked.</summary>
    Guid? MaterialId { get; }

    /// <summary>Required with a material.</summary>
    MaterialGrade? Grade { get; }
}

public sealed record CreateStoreProduct(
    Guid StoreId,
    string Name,
    string? Brand,
    string? Code,
    decimal PackSize,
    string? PackLabel,
    int Price,
    int? SalePrice,
    DateOnly? SaleEndsOn,
    bool InStock,
    Guid? PhotoFileId,
    Guid? MaterialId,
    MaterialGrade? Grade,
    bool IsActive = true) : ICommand<AdminStoreProductDto>, IStoreProductFields;

public sealed record UpdateStoreProduct(
    Guid StoreId,
    Guid ProductId,
    string Name,
    string? Brand,
    string? Code,
    decimal PackSize,
    string? PackLabel,
    int Price,
    int? SalePrice,
    DateOnly? SaleEndsOn,
    bool InStock,
    Guid? PhotoFileId,
    Guid? MaterialId,
    MaterialGrade? Grade,
    bool IsActive = true) : ICommand<AdminStoreProductDto>, IStoreProductFields;

public sealed record DeleteStoreProduct(Guid StoreId, Guid ProductId) : ICommand<bool>;

/// <summary>Staff checked the store's price and it is still right (sets "price updated" to now).</summary>
public sealed record ConfirmStoreProductPrice(Guid StoreId, Guid ProductId) : ICommand<AdminStoreProductDto>;

public abstract class StoreProductFieldsValidator<T> : AbstractValidator<T>
    where T : IStoreProductFields
{
    protected StoreProductFieldsValidator()
    {
        RuleFor(x => x.Name).Must(name => !string.IsNullOrWhiteSpace(name)).WithErrorCode("name.required");
        RuleFor(x => x.Name).Must(name => (name?.Trim().Length ?? 0) <= StoreProduct.MaxNameLength).WithErrorCode("name.too_long");
        RuleFor(x => x.Brand).Must(brand => (brand?.Trim().Length ?? 0) <= StoreProduct.MaxBrandLength).WithErrorCode("brand.too_long");
        RuleFor(x => x.Code).Must(code => (code?.Trim().Length ?? 0) <= StoreProduct.MaxCodeLength).WithErrorCode("code.too_long");
        RuleFor(x => x.PackLabel).Must(label => (label?.Trim().Length ?? 0) <= StoreProduct.MaxPackLabelLength).WithErrorCode("pack_label.too_long");
        RuleFor(x => x.PackSize).Must(size => size is > 0m and <= StoreProduct.MaxPackSize).WithErrorCode("pack_size.invalid");
        RuleFor(x => x.Price).InclusiveBetween(1, Material.PriceLimit).WithErrorCode("price.invalid");
        RuleFor(x => x)
            .Must(x => (x.SalePrice is null) == (x.SaleEndsOn is null) && (x.SalePrice is null || (x.SalePrice >= 1 && x.SalePrice < x.Price)))
            .OverridePropertyName("sale")
            .WithErrorCode("sale.invalid");
        RuleFor(x => x.Grade).Must(grade => grade is null || Enum.IsDefined(grade.Value)).WithErrorCode("grade.invalid");
        RuleFor(x => x)
            .Must(x => x.MaterialId is null || x.Grade is not null)
            .OverridePropertyName("grade")
            .WithErrorCode("grade.required");
    }
}

public sealed class CreateStoreProductValidator : StoreProductFieldsValidator<CreateStoreProduct>;

public sealed class UpdateStoreProductValidator : StoreProductFieldsValidator<UpdateStoreProduct>;

public sealed class GetAdminStoreProductsValidator : AbstractValidator<GetAdminStoreProducts>
{
    public GetAdminStoreProductsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetAdminStoreProducts.MaxPageSize).WithErrorCode("page_size.invalid");
    }
}

public sealed class GetAdminStoreProductsHandler(
    IAppDbContext db, FileDtoFactory files, ICurrentLanguage language, IOptions<StorePricingSettings> settings, TimeProvider clock)
    : IQueryHandler<GetAdminStoreProducts, PagedResult<AdminStoreProductDto>>
{
    public async Task<PagedResult<AdminStoreProductDto>> HandleAsync(GetAdminStoreProducts query, CancellationToken cancellationToken)
    {
        if (!await db.Stores.AnyAsync(s => s.Id == query.StoreId, cancellationToken))
        {
            throw new NotFoundException("The store does not exist.", "store.not_found");
        }

        var now = clock.GetUtcNow();
        var staleBefore = now - settings.Value.FreshFor;
        var products = db.StoreProducts.AsNoTracking().Where(p => p.StoreId == query.StoreId);
        if (query.MaterialId is { } materialId)
        {
            products = products.Where(p => p.MaterialId == materialId);
        }

        if (query.Linked is { } linked)
        {
            products = linked ? products.Where(p => p.MaterialId != null) : products.Where(p => p.MaterialId == null);
        }

        if (query.Stale is { } stale)
        {
            products = stale ? products.Where(p => p.PriceUpdatedAt < staleBefore) : products.Where(p => p.PriceUpdatedAt >= staleBefore);
        }

        if (query.Search?.Trim().ToLowerInvariant() is { Length: > 0 } term)
        {
            products = products.Where(p =>
                p.Name.ToLower().Contains(term)
                || (p.Brand != null && p.Brand.ToLower().Contains(term))
                || (p.Code != null && p.Code.ToLower().Contains(term)));
        }

        var total = await products.CountAsync(cancellationToken);
        var page = await products
            .OrderBy(p => p.Name).ThenBy(p => p.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        var items = await StoreData.ProductDtosAsync(db, files, language, settings.Value, now, page, cancellationToken);
        return new PagedResult<AdminStoreProductDto>(items, query.Page, query.PageSize, total);
    }
}

public sealed class GetMaterialStoreProductsHandler(
    IAppDbContext db, FileDtoFactory files, ICurrentLanguage language, IOptions<StorePricingSettings> settings, TimeProvider clock)
    : IQueryHandler<GetMaterialStoreProducts, IReadOnlyList<AdminMaterialOfferDto>>
{
    public async Task<IReadOnlyList<AdminMaterialOfferDto>> HandleAsync(GetMaterialStoreProducts query, CancellationToken cancellationToken)
    {
        if (!await db.Materials.AnyAsync(m => m.Id == query.MaterialId, cancellationToken))
        {
            throw new NotFoundException("The material does not exist.", "material.not_found");
        }

        var products = await db.StoreProducts.AsNoTracking().Where(p => p.MaterialId == query.MaterialId).ToListAsync(cancellationToken);
        var storeIds = products.Select(p => p.StoreId).Distinct().ToList();
        var stores = await db.Stores.AsNoTracking().Where(s => storeIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);
        var dtos = await StoreData.ProductDtosAsync(db, files, language, settings.Value, clock.GetUtcNow(), products, cancellationToken);
        return dtos
            .Select(p => new AdminMaterialOfferDto(p.StoreId, stores[p.StoreId].Name, stores[p.StoreId].IsActive, p))
            .OrderBy(o => o.Product.Grade)
            .ThenBy(o => o.Product.UnitPrice)
            .ThenBy(o => o.StoreName, StringComparer.Ordinal)
            .ToList();
    }
}

public sealed class CreateStoreProductHandler(
    IAppDbContext db, FileDtoFactory files, ICurrentLanguage language, IOptions<StorePricingSettings> settings, TimeProvider clock)
    : ICommandHandler<CreateStoreProduct, AdminStoreProductDto>
{
    public async Task<AdminStoreProductDto> HandleAsync(CreateStoreProduct command, CancellationToken cancellationToken)
    {
        _ = await StoreData.LoadAsync(db, command.StoreId, withBranches: false, cancellationToken);
        await StoreData.EnsureCodeFreeAsync(db, command.StoreId, command.Code, exceptId: null, cancellationToken);
        await StoreData.EnsureImageAsync(db, command.PhotoFileId, cancellationToken);

        var now = clock.GetUtcNow();
        var product = StoreProduct.Create(
            command.StoreId,
            command.Name,
            command.Brand,
            command.Code,
            command.PackSize,
            command.PackLabel,
            command.Price,
            command.SalePrice,
            command.SaleEndsOn,
            command.InStock,
            command.PhotoFileId,
            now);
        await StoreData.ApplyLinkAsync(db, product, command.MaterialId, command.Grade, cancellationToken);
        if (!command.IsActive)
        {
            product.Deactivate();
        }

        db.StoreProducts.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        await StoreData.RefreshMaterialPricesAsync(db, settings.Value, clock, [product.MaterialId], cancellationToken);
        return await StoreData.ProductDtoAsync(db, files, language, settings.Value, now, product, cancellationToken);
    }
}

public sealed class UpdateStoreProductHandler(
    IAppDbContext db, FileDtoFactory files, ICurrentLanguage language, IOptions<StorePricingSettings> settings, TimeProvider clock)
    : ICommandHandler<UpdateStoreProduct, AdminStoreProductDto>
{
    public async Task<AdminStoreProductDto> HandleAsync(UpdateStoreProduct command, CancellationToken cancellationToken)
    {
        var product = await StoreData.LoadProductAsync(db, command.StoreId, command.ProductId, cancellationToken);
        await StoreData.EnsureCodeFreeAsync(db, command.StoreId, command.Code, product.Id, cancellationToken);
        await StoreData.EnsureImageAsync(db, command.PhotoFileId, cancellationToken);

        var now = clock.GetUtcNow();
        var oldMaterialId = product.MaterialId;
        product.Update(
            command.Name,
            command.Brand,
            command.Code,
            command.PackSize,
            command.PackLabel,
            command.Price,
            command.SalePrice,
            command.SaleEndsOn,
            command.InStock,
            command.PhotoFileId,
            now);
        await StoreData.ApplyLinkAsync(db, product, command.MaterialId, command.Grade, cancellationToken);
        if (command.IsActive)
        {
            product.Activate();
        }
        else
        {
            product.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);
        await StoreData.RefreshMaterialPricesAsync(db, settings.Value, clock, [oldMaterialId, product.MaterialId], cancellationToken);
        return await StoreData.ProductDtoAsync(db, files, language, settings.Value, now, product, cancellationToken);
    }
}

public sealed class DeleteStoreProductHandler(IAppDbContext db, IOptions<StorePricingSettings> settings, TimeProvider clock)
    : ICommandHandler<DeleteStoreProduct, bool>
{
    public async Task<bool> HandleAsync(DeleteStoreProduct command, CancellationToken cancellationToken)
    {
        var product = await StoreData.LoadProductAsync(db, command.StoreId, command.ProductId, cancellationToken);
        db.StoreProducts.Remove(product);
        await db.SaveChangesAsync(cancellationToken);
        await StoreData.RefreshMaterialPricesAsync(db, settings.Value, clock, [product.MaterialId], cancellationToken);
        return true;
    }
}

public sealed class ConfirmStoreProductPriceHandler(
    IAppDbContext db, FileDtoFactory files, ICurrentLanguage language, IOptions<StorePricingSettings> settings, TimeProvider clock)
    : ICommandHandler<ConfirmStoreProductPrice, AdminStoreProductDto>
{
    public async Task<AdminStoreProductDto> HandleAsync(ConfirmStoreProductPrice command, CancellationToken cancellationToken)
    {
        var product = await StoreData.LoadProductAsync(db, command.StoreId, command.ProductId, cancellationToken);
        var now = clock.GetUtcNow();
        product.ConfirmPrice(now);
        await db.SaveChangesAsync(cancellationToken);
        await StoreData.RefreshMaterialPricesAsync(db, settings.Value, clock, [product.MaterialId], cancellationToken);
        return await StoreData.ProductDtoAsync(db, files, language, settings.Value, now, product, cancellationToken);
    }
}
