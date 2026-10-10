using HomeServices.Application.Estimates;
using HomeServices.Application.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers;

/// <summary>The renovation estimator. Open to everyone, signed in or not.</summary>
[ApiController]
[Route("api/v1/estimates")]
public sealed class EstimatesController : ControllerBase
{
    public sealed record MeasureRequest(IReadOnlyList<RoomInput> Rooms);

    /// <summary>
    /// Measures rooms (floor, walls, ceiling, perimeter, skirting) and the quantity of each work item in them, without
    /// saving anything. 422 "estimate.size_invalid" / "estimate.opening_invalid" for impossible sizes,
    /// "estimate.work_item_not_found" for work that isn't offered.
    /// </summary>
    [HttpPost("measure")]
    public Task<EstimateMeasurementDto> Measure(
        MeasureRequest request,
        [FromServices] IQueryHandler<MeasureEstimate, EstimateMeasurementDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new MeasureEstimate(request.Rooms), cancellationToken);
}
