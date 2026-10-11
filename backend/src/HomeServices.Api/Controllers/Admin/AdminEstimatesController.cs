using HomeServices.Api.Authorization;
using HomeServices.Application.Common;
using HomeServices.Application.Estimates;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Staff;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers.Admin;

/// <summary>Customers' saved estimates and how many become requests (read-only; needs requests.view).</summary>
[ApiController]
[Route("api/v1/admin/estimates")]
public sealed class AdminEstimatesController : ControllerBase
{
    /// <summary>
    /// Estimates, last changed first, with their totals. <paramref name="search"/> matches the title or the owner's phone;
    /// <paramref name="withRequest"/> and <paramref name="shared"/> filter. <paramref name="pageSize"/> is at most 100.
    /// </summary>
    [HttpGet]
    [HasPermission(Permissions.RequestsView)]
    public Task<PagedResult<AdminEstimateListItemDto>> List(
        [FromServices] IQueryHandler<GetAdminEstimates, PagedResult<AdminEstimateListItemDto>> handler,
        [FromQuery] string? search,
        [FromQuery] bool? withRequest,
        [FromQuery] bool? shared,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminEstimates(search, withRequest, shared, page ?? 1, pageSize ?? GetAdminEstimates.DefaultPageSize), cancellationToken);

    /// <summary>Counts and the share of estimates that became requests.</summary>
    [HttpGet("stats")]
    [HasPermission(Permissions.RequestsView)]
    public Task<EstimateStatsDto> Stats(
        [FromServices] IQueryHandler<GetEstimateStats, EstimateStatsDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetEstimateStats(), cancellationToken);

    /// <summary>One estimate with its owner, rooms, prices and the requests made from it; 404 "estimate.not_found".</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.RequestsView)]
    public Task<AdminEstimateDto> Get(
        Guid id,
        [FromServices] IQueryHandler<GetAdminEstimate, AdminEstimateDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminEstimate(id), cancellationToken);
}
