using HomeServices.Application.Estimates;
using HomeServices.Application.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers;

/// <summary>
/// The signed-in user's saved renovation estimates (up to 50). Visitors keep their draft in the browser and save it here
/// after signing in. Rooms, doors, windows and work are always sent and stored whole.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/me/estimates")]
public sealed class MyEstimatesController : ControllerBase
{
    public sealed record SaveRequest(string Title, Guid? CityId, bool OldBuilding, IReadOnlyList<EstimateRoomInput> Rooms);

    /// <summary>The estimates, last changed first, with their totals.</summary>
    [HttpGet]
    public Task<IReadOnlyList<EstimateSummaryDto>> List(
        [FromServices] IQueryHandler<GetMyEstimates, IReadOnlyList<EstimateSummaryDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetMyEstimates(), cancellationToken);

    /// <summary>An estimate with its rooms and prices; 404 "estimate.not_found".</summary>
    [HttpGet("{id:guid}")]
    public Task<EstimateDto> Get(
        Guid id,
        [FromServices] IQueryHandler<GetMyEstimate, EstimateDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetMyEstimate(id), cancellationToken);

    /// <summary>Saves a new estimate. 422 "estimate.too_many" past 50.</summary>
    [HttpPost]
    public async Task<ActionResult<EstimateDto>> CreateAsync(
        SaveRequest request,
        [FromServices] ICommandHandler<SaveMyEstimate, EstimateDto> handler,
        CancellationToken cancellationToken) =>
        StatusCode(
            StatusCodes.Status201Created,
            await handler.HandleAsync(new SaveMyEstimate(null, request.Title, request.CityId, request.OldBuilding, request.Rooms), cancellationToken));

    /// <summary>Replaces the estimate's title, place, building age and rooms.</summary>
    [HttpPut("{id:guid}")]
    public Task<EstimateDto> Update(
        Guid id,
        SaveRequest request,
        [FromServices] ICommandHandler<SaveMyEstimate, EstimateDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new SaveMyEstimate(id, request.Title, request.CityId, request.OldBuilding, request.Rooms), cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(
        Guid id,
        [FromServices] ICommandHandler<DeleteMyEstimate, bool> handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteMyEstimate(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Turns the share link on; the estimate's <c>shareToken</c> goes into /estimates/shared/{token}.</summary>
    [HttpPost("{id:guid}/share")]
    public Task<EstimateDto> Share(
        Guid id,
        [FromServices] ICommandHandler<ShareMyEstimate, EstimateDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new ShareMyEstimate(id, Share: true), cancellationToken);

    /// <summary>Turns the share link off: the old link stops working.</summary>
    [HttpDelete("{id:guid}/share")]
    public Task<EstimateDto> StopSharing(
        Guid id,
        [FromServices] ICommandHandler<ShareMyEstimate, EstimateDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new ShareMyEstimate(id, Share: false), cancellationToken);
}
