using HomeServices.Api.Authorization;
using HomeServices.Application.Common;
using HomeServices.Application.Messaging;
using HomeServices.Application.Orders;
using HomeServices.Domain.Orders;
using HomeServices.Domain.Staff;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers.Admin;

/// <summary>Every order for the Back Office: the list, one order with its history and changes, staff cancel and resolve.</summary>
[ApiController]
[Route("api/v1/admin/orders")]
public sealed class AdminOrdersController : ControllerBase
{
    public sealed record CancelBody(string Reason);

    /// <summary>
    /// Orders, newest first. <c>?needsAttention=true</c>: cancelled after work started, waiting longest first.
    /// <paramref name="search"/> is a phone number or part of a customer's or partner's name. <paramref name="pageSize"/> is at most 100.
    /// </summary>
    [HttpGet]
    [HasPermission(Permissions.OrdersView)]
    public Task<PagedResult<AdminOrderListItemDto>> List(
        [FromServices] IQueryHandler<GetAdminOrders, PagedResult<AdminOrderListItemDto>> handler,
        [FromQuery] OrderStatus? status,
        [FromQuery] OrderKind? kind,
        [FromQuery] bool? needsAttention,
        [FromQuery] string? search,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminOrders(status, kind, needsAttention, search, page ?? 1, pageSize ?? GetAdminOrders.DefaultPageSize), cancellationToken);

    /// <summary>The order with both parties' contacts, the full history (who and why) and every change.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.OrdersView)]
    public Task<OrderDto> Get(Guid id, [FromServices] IQueryHandler<GetAdminOrder, OrderDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminOrder(id), cancellationToken);

    /// <summary>Cancels any open order; the reason is required.</summary>
    [HttpPost("{id:guid}/cancel")]
    [HasPermission(Permissions.OrdersManage)]
    public Task<OrderDto> Cancel(Guid id, CancelBody body, [FromServices] ICommandHandler<CancelOrderByStaff, OrderDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new CancelOrderByStaff(id, body.Reason), cancellationToken);

    /// <summary>Marks an order that needed attention as dealt with (422 "order.not_flagged" otherwise).</summary>
    [HttpPost("{id:guid}/resolve")]
    [HasPermission(Permissions.OrdersManage)]
    public Task<OrderDto> Resolve(Guid id, [FromServices] ICommandHandler<ResolveOrder, OrderDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new ResolveOrder(id), cancellationToken);
}
