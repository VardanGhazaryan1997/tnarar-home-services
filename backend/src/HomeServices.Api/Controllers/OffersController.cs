using HomeServices.Application.Common;
using HomeServices.Application.Messaging;
using HomeServices.Application.Offers;
using HomeServices.Application.Orders;
using HomeServices.Domain.Offers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers;

/// <summary>
/// Offers. Partners send them with POST /api/v1/requests/{id}/offers; customers compare them with
/// GET /api/v1/requests/{id}/offers and accept one, which creates an order.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/offers")]
public sealed class OffersController : ControllerBase
{
    public sealed record ReasonBody(string? Reason);

    /// <summary>Offers the signed-in partner sent, newest first. 404 "partner.not_found" without a partner profile.</summary>
    [HttpGet("mine")]
    public Task<PagedResult<MyOfferListItemDto>> Mine(
        [FromServices] IQueryHandler<GetMyOffers, PagedResult<MyOfferListItemDto>> handler,
        [FromQuery] OfferStatus? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetMyOffers(status, page ?? 1, pageSize ?? GetMyOffers.DefaultPageSize), cancellationToken);

    /// <summary>One offer, for the customer of its request or the partner who sent it.</summary>
    [HttpGet("{id:guid}")]
    public Task<OfferDto> Get(
        Guid id,
        [FromServices] IQueryHandler<GetOffer, OfferDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetOffer(id), cancellationToken);

    /// <summary>
    /// The customer accepts an offer and gets the new order. A work offer closes the request and the other waiting
    /// offers; a visit leaves the request open. Repeating the call returns the same order.
    /// </summary>
    [HttpPost("{id:guid}/accept")]
    public Task<OrderDto> Accept(
        Guid id,
        [FromServices] ICommandHandler<AcceptOffer, OrderDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new AcceptOffer(id), cancellationToken);

    /// <summary>The customer turns an offer down; the optional reason is shown to the partner.</summary>
    [HttpPost("{id:guid}/reject")]
    public Task<OfferDto> Reject(
        Guid id,
        ReasonBody body,
        [FromServices] ICommandHandler<RejectOffer, OfferDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new RejectOffer(id, body.Reason), cancellationToken);

    /// <summary>The partner takes back an offer the customer hasn't decided on.</summary>
    [HttpPost("{id:guid}/withdraw")]
    public Task<OfferDto> Withdraw(
        Guid id,
        [FromServices] ICommandHandler<WithdrawOffer, OfferDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new WithdrawOffer(id), cancellationToken);
}
