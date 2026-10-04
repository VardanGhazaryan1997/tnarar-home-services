using HomeServices.Application.Chat;
using HomeServices.Application.Common;
using HomeServices.Application.Messaging;
using HomeServices.Application.Offers;
using HomeServices.Application.Requests;
using HomeServices.Domain.Offers;
using HomeServices.Domain.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers;

/// <summary>
/// Service requests. Customers create and follow their requests; partners work through the requests they
/// received (the inbox). Upload photos and videos with /api/v1/files first.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/requests")]
public sealed class RequestsController : ControllerBase
{
    public sealed record CreateBody(
        RequestKind Kind,
        Guid? PartnerId,
        Guid CategoryId,
        Guid CityId,
        Guid? DistrictId,
        string Description,
        DateOnly? PreferredDate,
        string? TimeNote,
        int? BudgetMin,
        int? BudgetMax,
        IReadOnlyList<Guid>? MediaFileIds);

    public sealed record ReasonBody(string? Reason);

    public sealed record ConversationBody(Guid? PartnerId);

    public sealed record OfferBody(
        OfferKind Kind,
        string Summary,
        IReadOnlyList<OfferLine>? Lines,
        int Price,
        bool MaterialsIncluded,
        string? MaterialsNote,
        DateOnly? StartDate,
        int? DurationDays,
        DateTimeOffset? VisitAt,
        IReadOnlyList<OfferStageTerms>? Stages,
        int? ValidDays);

    /// <summary>
    /// Creates a request. <c>Direct</c> needs <c>partnerId</c> (422 "request.partner_unavailable" if that partner can't
    /// take requests); <c>Open</c> goes to matching partners, or to an operator when none match.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<MyRequestDto>> CreateAsync(
        CreateBody body,
        [FromServices] ICommandHandler<CreateRequest, MyRequestDto> handler,
        CancellationToken cancellationToken) =>
        StatusCode(
            StatusCodes.Status201Created,
            await handler.HandleAsync(
                new CreateRequest(
                    body.Kind,
                    body.PartnerId,
                    body.CategoryId,
                    body.CityId,
                    body.DistrictId,
                    body.Description,
                    body.PreferredDate,
                    body.TimeNote,
                    body.BudgetMin,
                    body.BudgetMax,
                    body.MediaFileIds),
                cancellationToken));

    /// <summary>The signed-in customer's requests, newest first.</summary>
    [HttpGet("mine")]
    public Task<PagedResult<MyRequestListItemDto>> Mine(
        [FromServices] IQueryHandler<GetMyRequests, PagedResult<MyRequestListItemDto>> handler,
        [FromQuery] RequestStatus? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetMyRequests(status, page ?? 1, pageSize ?? GetMyRequests.DefaultPageSize), cancellationToken);

    /// <summary>One of the customer's requests (404 "request.not_found" for anyone else's).</summary>
    [HttpGet("{id:guid}")]
    public Task<MyRequestDto> Get(
        Guid id,
        [FromServices] IQueryHandler<GetMyRequest, MyRequestDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetMyRequest(id), cancellationToken);

    /// <summary>The customer withdraws an open request.</summary>
    [HttpPost("{id:guid}/cancel")]
    public Task<MyRequestDto> Cancel(
        Guid id,
        ReasonBody body,
        [FromServices] ICommandHandler<CancelMyRequest, MyRequestDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new CancelMyRequest(id, body.Reason), cancellationToken);

    /// <summary>Requests the signed-in partner received, newest first. 404 "partner.not_found" without a partner profile.</summary>
    [HttpGet("inbox")]
    public Task<PagedResult<InboxItemDto>> Inbox(
        [FromServices] IQueryHandler<GetInbox, PagedResult<InboxItemDto>> handler,
        [FromQuery] RecipientStatus? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetInbox(status, page ?? 1, pageSize ?? GetInbox.DefaultPageSize), cancellationToken);

    /// <summary>A received request; the first opening marks it viewed.</summary>
    [HttpGet("inbox/{id:guid}")]
    public Task<InboxRequestDto> InboxRequest(
        Guid id,
        [FromServices] ICommandHandler<GetInboxRequest, InboxRequestDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetInboxRequest(id), cancellationToken);

    /// <summary>The partner declines a received request. The reason is optional and only staff see it.</summary>
    [HttpPost("inbox/{id:guid}/decline")]
    public Task<InboxRequestDto> Decline(
        Guid id,
        ReasonBody body,
        [FromServices] ICommandHandler<DeclineInboxRequest, InboxRequestDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new DeclineInboxRequest(id, body.Reason), cancellationToken);

    /// <summary>
    /// The signed-in partner sends an offer on a request they received. <c>Work</c>: summary, lines, price, dates and
    /// payment stages adding up to the price. <c>Visit</c>: summary, <c>visitAt</c> and a fee (0 = free).
    /// 422 "offer.already_sent" while an offer of the same kind waits.
    /// </summary>
    [HttpPost("{id:guid}/offers")]
    public async Task<ActionResult<OfferDto>> SendOfferAsync(
        Guid id,
        OfferBody body,
        [FromServices] ICommandHandler<SendOffer, OfferDto> handler,
        CancellationToken cancellationToken) =>
        StatusCode(
            StatusCodes.Status201Created,
            await handler.HandleAsync(
                new SendOffer(
                    id,
                    body.Kind,
                    body.Summary,
                    body.Lines,
                    body.Price,
                    body.MaterialsIncluded,
                    body.MaterialsNote,
                    body.StartDate,
                    body.DurationDays,
                    body.VisitAt,
                    body.Stages,
                    body.ValidDays),
                cancellationToken));

    /// <summary>
    /// The offers on a request: all of them (except withdrawn) for its customer, waiting and cheapest first;
    /// their own for a partner who received it.
    /// </summary>
    [HttpGet("{id:guid}/offers")]
    public Task<IReadOnlyList<OfferDto>> Offers(
        Guid id,
        [FromServices] IQueryHandler<GetRequestOffers, IReadOnlyList<OfferDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetRequestOffers(id), cancellationToken);

    /// <summary>
    /// Opens (or returns) the conversation about this request: a partner who received it calls it with no body;
    /// the customer passes the partner's id. 422 "conversation.unavailable" when a new one can't start.
    /// </summary>
    [HttpPost("{id:guid}/conversation")]
    public Task<ConversationDto> OpenConversation(
        Guid id,
        ConversationBody? body,
        [FromServices] ICommandHandler<OpenConversation, ConversationDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new OpenConversation(id, body?.PartnerId), cancellationToken);
}
