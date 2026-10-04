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

    /// <summary>Active cities with their districts, names in the request language.</summary>
    [HttpGet("cities")]
    public Task<IReadOnlyList<CityDto>> GetCities(
        [FromServices] IQueryHandler<GetCities, IReadOnlyList<CityDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetCities(), cancellationToken);
}
