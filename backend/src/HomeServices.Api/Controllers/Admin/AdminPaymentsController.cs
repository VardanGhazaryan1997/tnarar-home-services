using HomeServices.Api.Authorization;
using HomeServices.Application.Common;
using HomeServices.Application.Messaging;
using HomeServices.Application.Payments;
using HomeServices.Domain.Payments;
using HomeServices.Domain.Staff;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers.Admin;

/// <summary>Payment records for the Back Office, and deciding disputes.</summary>
[ApiController]
[Route("api/v1/admin/payments")]
public sealed class AdminPaymentsController : ControllerBase
{
    public sealed record ResolveBody(bool Counts, string? Note);

    /// <summary>Payments, newest first; <c>?status=Disputed</c> is the dispute queue, waiting longest first.</summary>
    [HttpGet]
    [HasPermission(Permissions.PaymentsView)]
    public Task<PagedResult<AdminPaymentDto>> List(
        [FromServices] IQueryHandler<GetAdminPayments, PagedResult<AdminPaymentDto>> handler,
        [FromQuery] PaymentStatus? status,
        [FromQuery] Guid? orderId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminPayments(status, orderId, page ?? 1, pageSize ?? GetAdminPayments.DefaultPageSize), cancellationToken);

    /// <summary>Decides a dispute: <c>counts = true</c> → Confirmed, false → Rejected (422 "payment.not_disputed" otherwise).</summary>
    [HttpPost("{id:guid}/resolve")]
    [HasPermission(Permissions.PaymentsManage)]
    public Task<AdminPaymentDto> Resolve(
        Guid id, ResolveBody body, [FromServices] ICommandHandler<ResolvePayment, AdminPaymentDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new ResolvePayment(id, body.Counts, body.Note), cancellationToken);
}
