using HomeServices.Application.Catalog;
using HomeServices.Application.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers;

/// <summary>Public reference data for browsing and request forms.</summary>
[ApiController]
[Route("api/v1")]
public sealed class CatalogController : ControllerBase
{
    /// <summary>Active service categories as a tree, names in the request language.</summary>
    [HttpGet("categories")]
    public Task<IReadOnlyList<CategoryDto>> GetCategories(
        [FromServices] IQueryHandler<GetCategories, IReadOnlyList<CategoryDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetCategories(), cancellationToken);

    /// <summary>
    /// Active work items (what partners price and customers estimate), names in the request language, in catalog order.
    /// <paramref name="category"/> is a category slug; a main category gives the items of all its subcategories.
    /// <paramref name="region"/> is a region slug: prices are that region's where enough partners there priced the work.
    /// </summary>
    [HttpGet("work-items")]
    public Task<IReadOnlyList<WorkItemDto>> GetWorkItems(
        [FromQuery] string? category,
        [FromQuery] string? region,
        [FromServices] IQueryHandler<GetWorkItems, IReadOnlyList<WorkItemDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetWorkItems(category, region), cancellationToken);

    /// <summary>Yerevan and the regions (marzes), names in the request language.</summary>
    [HttpGet("regions")]
    public Task<IReadOnlyList<RegionDto>> GetRegions(
        [FromServices] IQueryHandler<GetRegions, IReadOnlyList<RegionDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetRegions(), cancellationToken);

    /// <summary>Active towns and villages with their districts, names in the request language.</summary>
    [HttpGet("cities")]
    public Task<IReadOnlyList<CityDto>> GetCities(
        [FromServices] IQueryHandler<GetCities, IReadOnlyList<CityDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetCities(), cancellationToken);
}
