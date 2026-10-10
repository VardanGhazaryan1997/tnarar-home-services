using HomeServices.Application.Estimates;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Estimates;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers;

/// <summary>The renovation estimator. Open to everyone, signed in or not.</summary>
[ApiController]
[Route("api/v1/estimates")]
public sealed class EstimatesController : ControllerBase
{
    public sealed record MeasureRequest(IReadOnlyList<RoomInput> Rooms, bool OldBuilding = false);

    public sealed record QuickRequest(RoomType RoomType, decimal Area, decimal? Height = null, bool OldBuilding = false);

    /// <summary>
    /// Measures and prices rooms (floor, walls, ceiling, perimeter, skirting; each work item's quantity and labour price
    /// range; totals) without saving anything. <c>oldBuilding</c> adds 15%; ceilings above 3 m add 10% to wall and ceiling
    /// work. 422 "estimate.size_invalid" / "estimate.opening_invalid" for impossible sizes, "estimate.work_item_not_found"
    /// for work that isn't offered.
    /// </summary>
    [HttpPost("measure")]
    public Task<EstimateMeasurementDto> Measure(
        MeasureRequest request,
        [FromServices] IQueryHandler<MeasureEstimate, EstimateMeasurementDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new MeasureEstimate(request.Rooms, request.OldBuilding), cancellationToken);

    /// <summary>The work usually wanted in each kind of room, with market prices and default counts.</summary>
    [HttpGet("templates")]
    public Task<IReadOnlyList<RoomTemplateDto>> Templates(
        [FromServices] IQueryHandler<GetRoomTemplates, IReadOnlyList<RoomTemplateDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetRoomTemplates(), cancellationToken);

    /// <summary>A quick estimate: one room of the given type and floor area with its usual work, measured and priced.</summary>
    [HttpPost("quick")]
    public Task<EstimateMeasurementDto> Quick(
        QuickRequest request,
        [FromServices] IQueryHandler<QuickEstimate, EstimateMeasurementDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new QuickEstimate(request.RoomType, request.Area, request.Height, request.OldBuilding), cancellationToken);
}
