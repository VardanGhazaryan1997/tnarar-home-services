using HomeServices.Api.Authorization;
using HomeServices.Application.Common;
using HomeServices.Application.Messaging;
using HomeServices.Application.Requests;
using HomeServices.Domain.Requests;
using HomeServices.Domain.Staff;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers.Admin;

/// <summary>All requests, the operator queue, and manual distribution to partners.</summary>
[ApiController]
[Route("api/v1/admin/requests")]
public sealed class AdminRequestsController : ControllerBase
{
    public sealed record AssignBody(IReadOnlyList<Guid> PartnerIds);

    public sealed record CancelBody(string Reason);

    /// <summary>
    /// Requests, newest first. <c>?needsAttention=true</c> is the operator queue (waiting longest first).
    /// <paramref name="search"/> matches the description or the customer's phone. <paramref name="pageSize"/> is at most 100.
    /// </summary>
    [HttpGet]
    [HasPermission(Permissions.RequestsView)]
    public Task<PagedResult<AdminRequestListItemDto>> List(
        [FromServices] IQueryHandler<GetAdminRequests, PagedResult<AdminRequestListItemDto>> handler,
        [FromQuery] RequestStatus? status,
        [FromQuery] RequestKind? kind,
        [FromQuery] bool? needsAttention,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? cityId,
        [FromQuery] string? search,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new GetAdminRequests(status, kind, needsAttention, categoryId, cityId, search, page ?? 1, pageSize ?? GetAdminRequests.DefaultPageSize),
            cancellationToken);

    /// <summary>The request with its customer, every partner who received it and what they did.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.RequestsView)]
    public Task<AdminRequestDto> Get(Guid id, [FromServices] IQueryHandler<GetAdminRequest, AdminRequestDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminRequest(id), cancellationToken);

    /// <summary>Sends the request to the chosen partners (422 "request.partner_unavailable" if one can't take requests).</summary>
    [HttpPost("{id:guid}/recipients")]
    [HasPermission(Permissions.RequestsManage)]
    public Task<AdminRequestDto> Assign(
        Guid id,
        AssignBody body,
        [FromServices] ICommandHandler<AssignRequest, AdminRequestDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new AssignRequest(id, body.PartnerIds), cancellationToken);

    /// <summary>Withdraws an open request; the reason is required.</summary>
    [HttpPost("{id:guid}/cancel")]
    [HasPermission(Permissions.RequestsManage)]
    public Task<AdminRequestDto> Cancel(
        Guid id,
        CancelBody body,
        [FromServices] ICommandHandler<CancelRequestByStaff, AdminRequestDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new CancelRequestByStaff(id, body.Reason), cancellationToken);
}
