using HomeServices.Api.Authorization;
using HomeServices.Application.Catalog;
using HomeServices.Application.Common;
using HomeServices.Application.Messaging;
using HomeServices.Application.Stores;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Staff;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers.Admin;

/// <summary>
/// Back Office management of stores, their branches and their products (needs <c>materials.pricing</c>). A store has
/// one price list; products linked to a material are compared per unit of that material.
/// </summary>
[ApiController]
[Route("api/v1/admin/stores")]
[HasPermission(Permissions.MaterialsPricing)]
public sealed class AdminStoresController : ControllerBase
{
    /// <summary>A store's fields. <c>Description</c> is text per language code (optional); the name is the same in every language.</summary>
    public sealed record StoreRequest(
        string Slug,
        string Name,
        Dictionary<string, string>? Description,
        string? Website,
        string? Phone,
        Guid? LogoFileId,
        int SortOrder,
        bool IsActive = true);

    public sealed record BranchesRequest(IReadOnlyList<StoreBranchInput> Branches);

    /// <summary>
    /// A product's fields. <c>Price</c> is AMD per pack; <c>PackSize</c> is in the material's unit; link it with
    /// <c>MaterialId</c> and <c>Grade</c> (Budget, Standard or Premium); a sale needs both <c>SalePrice</c> and <c>SaleEndsOn</c>.
    /// </summary>
    public sealed record ProductRequest(
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
        bool IsActive = true);

    /// <summary>Stores in display order, hidden ones too, with branch, product, linked and stale counts.</summary>
    [HttpGet]
    public Task<IReadOnlyList<AdminStoreDto>> List(
        [FromServices] IQueryHandler<GetAdminStores, IReadOnlyList<AdminStoreDto>> handler,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminStores(search, isActive), cancellationToken);

    /// <summary>
    /// The materials products can be linked to (every material, hidden ones included, with its staff and store prices), so
    /// staff who manage prices don't need <c>catalog.manage</c> to pick one.
    /// </summary>
    [HttpGet("materials")]
    public Task<IReadOnlyList<AdminMaterialDto>> GetMaterials(
        [FromServices] IQueryHandler<GetAdminMaterials, IReadOnlyList<AdminMaterialDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminMaterials(), cancellationToken);

    /// <summary>One store with its branches.</summary>
    [HttpGet("{id:guid}")]
    public Task<AdminStoreDto> Get(Guid id, [FromServices] IQueryHandler<GetAdminStore, AdminStoreDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminStore(id), cancellationToken);

    /// <summary>Adds a store; 409 "store.slug_taken".</summary>
    [HttpPost]
    public async Task<ActionResult<AdminStoreDto>> CreateAsync(
        StoreRequest request,
        [FromServices] ICommandHandler<CreateStore, AdminStoreDto> handler,
        CancellationToken cancellationToken) =>
        StatusCode(
            StatusCodes.Status201Created,
            await handler.HandleAsync(
                new CreateStore(request.Slug, request.Name, request.Description, request.Website, request.Phone, request.LogoFileId, request.SortOrder, request.IsActive),
                cancellationToken));

    [HttpPut("{id:guid}")]
    public Task<AdminStoreDto> Update(
        Guid id,
        StoreRequest request,
        [FromServices] ICommandHandler<UpdateStore, AdminStoreDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new UpdateStore(id, request.Slug, request.Name, request.Description, request.Website, request.Phone, request.LogoFileId, request.SortOrder, request.IsActive),
            cancellationToken);

    /// <summary>Deletes a store without products; 422 "store.has_products" otherwise.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, [FromServices] ICommandHandler<DeleteStore, bool> handler, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteStore(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Replaces the store's branches with the list sent (in that order) and returns the store.</summary>
    [HttpPut("{id:guid}/branches")]
    public Task<AdminStoreDto> SetBranches(
        Guid id,
        BranchesRequest request,
        [FromServices] ICommandHandler<SetStoreBranches, AdminStoreDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetStoreBranches(id, request.Branches), cancellationToken);

    /// <summary>The store's products by name, paged. Filters: <c>search</c>, <c>materialId</c>, <c>linked</c>, <c>stale</c>.</summary>
    [HttpGet("{id:guid}/products")]
    public Task<PagedResult<AdminStoreProductDto>> ListProducts(
        Guid id,
        [FromServices] IQueryHandler<GetAdminStoreProducts, PagedResult<AdminStoreProductDto>> handler,
        [FromQuery] string? search,
        [FromQuery] Guid? materialId,
        [FromQuery] bool? linked,
        [FromQuery] bool? stale,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new GetAdminStoreProducts(id, search, materialId, linked, stale, page ?? 1, pageSize ?? GetAdminStoreProducts.DefaultPageSize),
            cancellationToken);

    /// <summary>Adds a product; 409 "store_product.code_taken" when the store already has the code.</summary>
    [HttpPost("{id:guid}/products")]
    public async Task<ActionResult<AdminStoreProductDto>> CreateProductAsync(
        Guid id,
        ProductRequest request,
        [FromServices] ICommandHandler<CreateStoreProduct, AdminStoreProductDto> handler,
        CancellationToken cancellationToken) =>
        StatusCode(
            StatusCodes.Status201Created,
            await handler.HandleAsync(
                new CreateStoreProduct(
                    id, request.Name, request.Brand, request.Code, request.PackSize, request.PackLabel, request.Price, request.SalePrice,
                    request.SaleEndsOn, request.InStock, request.PhotoFileId, request.MaterialId, request.Grade, request.IsActive),
                cancellationToken));

    [HttpPut("{id:guid}/products/{productId:guid}")]
    public Task<AdminStoreProductDto> UpdateProduct(
        Guid id,
        Guid productId,
        ProductRequest request,
        [FromServices] ICommandHandler<UpdateStoreProduct, AdminStoreProductDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new UpdateStoreProduct(
                id, productId, request.Name, request.Brand, request.Code, request.PackSize, request.PackLabel, request.Price, request.SalePrice,
                request.SaleEndsOn, request.InStock, request.PhotoFileId, request.MaterialId, request.Grade, request.IsActive),
            cancellationToken);

    [HttpDelete("{id:guid}/products/{productId:guid}")]
    public async Task<IActionResult> DeleteProductAsync(
        Guid id,
        Guid productId,
        [FromServices] ICommandHandler<DeleteStoreProduct, bool> handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteStoreProduct(id, productId), cancellationToken);
        return NoContent();
    }

    /// <summary>The store's price is still right: sets "price updated" to now without changing anything else.</summary>
    [HttpPost("{id:guid}/products/{productId:guid}/confirm")]
    public Task<AdminStoreProductDto> ConfirmProductPrice(
        Guid id,
        Guid productId,
        [FromServices] ICommandHandler<ConfirmStoreProductPrice, AdminStoreProductDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new ConfirmStoreProductPrice(id, productId), cancellationToken);

    /// <summary>Every product linked to the material across stores, by grade and then per-unit price.</summary>
    [HttpGet("~/api/v1/admin/catalog/materials/{id:guid}/store-products")]
    public Task<IReadOnlyList<AdminMaterialOfferDto>> GetMaterialStoreProducts(
        Guid id,
        [FromServices] IQueryHandler<GetMaterialStoreProducts, IReadOnlyList<AdminMaterialOfferDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetMaterialStoreProducts(id), cancellationToken);
}
