using HomeServices.Application.Commissions;
using HomeServices.Application.Common;
using HomeServices.Application.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers;

/// <summary>The signed-in partner's commissions: what they owe per order and their weekly statements.</summary>
[ApiController]
[Authorize]
[Route("api/v1/me")]
public sealed class CommissionsController : ControllerBase
{
    /// <summary>Totals for the top of the page: unbilled, outstanding, overdue, next due date, and whether new requests are paused.</summary>
    [HttpGet("commissions/summary")]
    public Task<CommissionSummaryDto> Summary([FromServices] IQueryHandler<GetMyCommissionSummary, CommissionSummaryDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetMyCommissionSummary(), cancellationToken);

    /// <summary>Commission per completed order, newest first; <c>?unbilled=true</c> keeps those not on a statement yet.</summary>
    [HttpGet("commissions")]
    public Task<PagedResult<CommissionLineDto>> Lines(
        [FromServices] IQueryHandler<GetMyCommissions, PagedResult<CommissionLineDto>> handler,
        [FromQuery] bool? unbilled,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetMyCommissions(unbilled ?? false, page ?? 1, pageSize ?? GetMyCommissions.DefaultPageSize), cancellationToken);

    /// <summary>Weekly statements, newest first.</summary>
    [HttpGet("commission-statements")]
    public Task<PagedResult<StatementDto>> Statements(
        [FromServices] IQueryHandler<GetMyStatements, PagedResult<StatementDto>> handler,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetMyStatements(page ?? 1, pageSize ?? GetMyStatements.DefaultPageSize), cancellationToken);

    /// <summary>One statement with its orders and the payments recorded against it.</summary>
    [HttpGet("commission-statements/{id:guid}")]
    public Task<StatementDetailDto> Statement(Guid id, [FromServices] IQueryHandler<GetMyStatement, StatementDetailDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetMyStatement(id), cancellationToken);
}
