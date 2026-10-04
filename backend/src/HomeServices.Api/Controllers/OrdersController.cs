using HomeServices.Application.Common;
using HomeServices.Application.Messaging;
using HomeServices.Application.Orders;
using HomeServices.Application.Payments;
using HomeServices.Application.Reviews;
using HomeServices.Domain.Orders;
using HomeServices.Domain.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers;

/// <summary>Orders the signed-in user is a party to, as customer or partner. Created by accepting an offer.</summary>
[ApiController]
[Authorize]
[Route("api/v1/orders")]
public sealed class OrdersController : ControllerBase
{
    public sealed record ReasonBody(string Reason);

    public sealed record NoteBody(string? Note);

    /// <summary>A payment made directly between the two sides. <c>stageId</c>: the payment stage it pays, if any.</summary>
    public sealed record PaymentBody(int Amount, PaymentMethod Method, DateOnly PaidOn, Guid? StageId, string? Note);

    public sealed record ReviewBody(int Rating, string? Text);

    public sealed record ReplyBody(string Text);

    /// <summary>Extra work: kind ExtraWork, title, amount, description. New schedule: kind Schedule, newStartDate and/or newDurationDays (work) or newVisitAt (visit).</summary>
    public sealed record ChangeBody(
        ChangeRequestKind Kind,
        string? Title,
        string? Description,
        int? Amount,
        DateOnly? NewStartDate,
        int? NewDurationDays,
        DateTimeOffset? NewVisitAt);

    /// <summary>The user's orders, newest first. <c>as</c> = Customer or Partner keeps one side only.</summary>
    [HttpGet]
    public Task<PagedResult<OrderListItemDto>> List(
        [FromServices] IQueryHandler<GetMyOrders, PagedResult<OrderListItemDto>> handler,
        [FromQuery(Name = "as")] OrderRole? role,
        [FromQuery] OrderStatus? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetMyOrders(role, status, page ?? 1, pageSize ?? GetMyOrders.DefaultPageSize), cancellationToken);

    /// <summary>One order; both parties see each other's contact details. 404 "order.not_found" for anyone else.</summary>
    [HttpGet("{id:guid}")]
    public Task<OrderDto> Get(
        Guid id,
        [FromServices] IQueryHandler<GetOrder, OrderDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetOrder(id), cancellationToken);

    /// <summary>The partner starts the work (422 "order.cannot_start" unless confirmed).</summary>
    [HttpPost("{id:guid}/start")]
    public Task<OrderDto> Start(Guid id, [FromServices] ICommandHandler<StartOrder, OrderDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new StartOrder(id), cancellationToken);

    /// <summary>The partner marks the work or visit as done; it completes by itself if the customer doesn't answer in time.</summary>
    [HttpPost("{id:guid}/request-completion")]
    public Task<OrderDto> RequestCompletion(
        Guid id, [FromServices] ICommandHandler<RequestOrderCompletion, OrderDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new RequestOrderCompletion(id), cancellationToken);

    /// <summary>The customer confirms the order is done.</summary>
    [HttpPost("{id:guid}/confirm-completion")]
    public Task<OrderDto> ConfirmCompletion(
        Guid id, [FromServices] ICommandHandler<ConfirmOrderCompletion, OrderDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new ConfirmOrderCompletion(id), cancellationToken);

    /// <summary>The customer says it isn't done yet; the reason is required.</summary>
    [HttpPost("{id:guid}/reject-completion")]
    public Task<OrderDto> RejectCompletion(
        Guid id, ReasonBody body, [FromServices] ICommandHandler<RejectOrderCompletion, OrderDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new RejectOrderCompletion(id, body.Reason), cancellationToken);

    /// <summary>Either side cancels, with a reason. After work started the order is flagged for the team.</summary>
    [HttpPost("{id:guid}/cancel")]
    public Task<OrderDto> Cancel(Guid id, ReasonBody body, [FromServices] ICommandHandler<CancelOrder, OrderDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new CancelOrder(id, body.Reason), cancellationToken);

    /// <summary>Proposes extra work or a new schedule; one change can wait at a time (422 "change.pending_exists").</summary>
    [HttpPost("{id:guid}/change-requests")]
    public Task<OrderDto> ProposeChange(
        Guid id, ChangeBody body, [FromServices] ICommandHandler<ProposeOrderChange, OrderDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new ProposeOrderChange(id, body.Kind, body.Title, body.Description, body.Amount, body.NewStartDate, body.NewDurationDays, body.NewVisitAt),
            cancellationToken);

    /// <summary>The other side accepts the change; it applies to the order right away.</summary>
    [HttpPost("{id:guid}/change-requests/{changeId:guid}/accept")]
    public Task<OrderDto> AcceptChange(
        Guid id, Guid changeId, [FromServices] ICommandHandler<AcceptOrderChange, OrderDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new AcceptOrderChange(id, changeId), cancellationToken);

    /// <summary>The other side turns the change down, optionally with a note.</summary>
    [HttpPost("{id:guid}/change-requests/{changeId:guid}/reject")]
    public Task<OrderDto> RejectChange(
        Guid id, Guid changeId, NoteBody body, [FromServices] ICommandHandler<RejectOrderChange, OrderDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new RejectOrderChange(id, changeId, body.Note), cancellationToken);

    /// <summary>Whoever proposed the change takes it back.</summary>
    [HttpPost("{id:guid}/change-requests/{changeId:guid}/withdraw")]
    public Task<OrderDto> WithdrawChange(
        Guid id, Guid changeId, [FromServices] ICommandHandler<WithdrawOrderChange, OrderDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new WithdrawOrderChange(id, changeId), cancellationToken);

    /// <summary>
    /// The customer ("I paid") or the partner ("I received") records a payment; the other side confirms or disputes it.
    /// 422 "payment.exceeds_price" when the records would add up to more than the price.
    /// </summary>
    [HttpPost("{id:guid}/payments")]
    public Task<OrderDto> AddPayment(
        Guid id, PaymentBody body, [FromServices] ICommandHandler<RecordPayment, OrderDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new RecordPayment(id, body.Amount, body.Method, body.PaidOn, body.StageId, body.Note), cancellationToken);

    /// <summary>The customer reviews a completed order, once (1–5 stars, optional text). It is public right away.</summary>
    [HttpPost("{id:guid}/review")]
    public Task<OrderDto> AddReview(Guid id, ReviewBody body, [FromServices] ICommandHandler<SubmitReview, OrderDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new SubmitReview(id, body.Rating, body.Text), cancellationToken);

    /// <summary>The partner replies to the order's review, once.</summary>
    [HttpPost("{id:guid}/review/reply")]
    public Task<OrderDto> AddReviewReply(
        Guid id, ReplyBody body, [FromServices] ICommandHandler<ReplyToReview, OrderDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new ReplyToReview(id, body.Text), cancellationToken);
}
