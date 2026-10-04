using HomeServices.Application.Messaging;
using HomeServices.Application.Orders;
using HomeServices.Application.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers;

/// <summary>Answers to payment records on the user's orders (recorded with POST /orders/{id}/payments). Each returns the order.</summary>
[ApiController]
[Authorize]
[Route("api/v1/payments")]
public sealed class PaymentsController : ControllerBase
{
    public sealed record ReasonBody(string Reason);

    /// <summary>The other side confirms the payment happened.</summary>
    [HttpPost("{id:guid}/confirm")]
    public Task<OrderDto> Confirm(Guid id, [FromServices] ICommandHandler<ConfirmPayment, OrderDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new ConfirmPayment(id), cancellationToken);

    /// <summary>The other side disputes it, saying why; the team decides.</summary>
    [HttpPost("{id:guid}/dispute")]
    public Task<OrderDto> Dispute(Guid id, ReasonBody body, [FromServices] ICommandHandler<DisputePayment, OrderDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new DisputePayment(id, body.Reason), cancellationToken);

    /// <summary>Whoever recorded it takes it back while it waits.</summary>
    [HttpPost("{id:guid}/withdraw")]
    public Task<OrderDto> Withdraw(Guid id, [FromServices] ICommandHandler<WithdrawPayment, OrderDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new WithdrawPayment(id), cancellationToken);
}
