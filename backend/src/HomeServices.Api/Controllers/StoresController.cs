using HomeServices.Application.Messaging;
using HomeServices.Application.Stores;
using HomeServices.Domain.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers;

/// <summary>Public store prices: stores, building materials and where each is cheapest. Names are in the request language.</summary>
[ApiController]
[Route("api/v1")]
public sealed class StoresController : ControllerBase
{
    /// <summary>Active stores with their branches. <paramref name="region"/> is a region slug: only stores with a branch there, and those branches.</summary>
    [HttpGet("stores")]
    public Task<IReadOnlyList<PublicStoreDto>> GetStores(
        [FromQuery] string? region,
        [FromServices] IQueryHandler<GetPublicStores, IReadOnlyList<PublicStoreDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetPublicStores(region), cancellationToken);

    /// <summary>One store with its branches and its products grouped by material (404 <c>store.not_found</c> when hidden or unknown).</summary>
    [HttpGet("stores/{slug}")]
    public Task<PublicStoreDto> GetStore(
        string slug,
        [FromServices] IQueryHandler<GetPublicStore, PublicStoreDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetPublicStore(slug), cancellationToken);

    /// <summary>
    /// Active materials with the usual price per unit for each grade, how many stores sell them and the cheapest in-stock
    /// price per unit. <paramref name="search"/> matches the name and slug; <paramref name="region"/> is a region slug.
    /// </summary>
    [HttpGet("materials")]
    public Task<IReadOnlyList<PublicMaterialDto>> GetMaterials(
        [FromQuery] string? search,
        [FromQuery] string? region,
        [FromServices] IQueryHandler<GetPublicMaterials, IReadOnlyList<PublicMaterialDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetPublicMaterials(search, region), cancellationToken);

    /// <summary>
    /// Products on sale today, biggest discount first. <paramref name="region"/> is a region slug; <paramref name="limit"/>
    /// is 1–50 (400 <c>limit.invalid</c> otherwise, 24 when not given). Declared before <c>materials/{slug}</c> so "on-sale" is not read as a material.
    /// </summary>
    [HttpGet("materials/on-sale")]
    public Task<IReadOnlyList<PublicSaleDto>> OnSale(
        [FromQuery] string? region,
        [FromQuery] int? limit,
        [FromServices] IQueryHandler<GetOnSale, IReadOnlyList<PublicSaleDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetOnSale(region, limit ?? GetOnSale.DefaultLimit), cancellationToken);

    /// <summary>
    /// One material with every store offer, in-stock first and then cheapest per unit. <paramref name="grade"/> is
    /// Budget, Standard or Premium; <paramref name="inStock"/> keeps only offers in stock (404 <c>material.not_found</c>).
    /// </summary>
    [HttpGet("materials/{slug}")]
    public Task<PublicMaterialDetailDto> GetMaterial(
        string slug,
        [FromQuery] string? region,
        [FromQuery] MaterialGrade? grade,
        [FromQuery] bool inStock,
        [FromServices] IQueryHandler<GetPublicMaterial, PublicMaterialDetailDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetPublicMaterial(slug, region, grade, inStock), cancellationToken);

    public sealed record WhereToBuyRequest(MaterialGrade Grade, IReadOnlyList<WhereToBuyNeed> Materials, string? Region = null);

    /// <summary>
    /// Where to buy the materials of an estimate: send the grade and each material's quantity (as the estimate counted
    /// it). Returns, for each material a store sells in that grade, the cheapest offer rounded up to whole packs, and each
    /// store's total for the materials it has (most covered first, then cheapest). <c>region</c> (optional): a region slug.
    /// </summary>
    [HttpPost("materials/where-to-buy")]
    public Task<WhereToBuyDto> WhereToBuy(
        WhereToBuyRequest request,
        [FromServices] IQueryHandler<WhereToBuy, WhereToBuyDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new WhereToBuy(request.Grade, request.Materials, request.Region), cancellationToken);
}
