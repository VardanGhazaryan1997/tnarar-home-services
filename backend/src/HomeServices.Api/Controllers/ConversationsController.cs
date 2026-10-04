using HomeServices.Application.Chat;
using HomeServices.Application.Common;
using HomeServices.Application.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers;

/// <summary>
/// Chat between a customer and a partner about a request (and the order that follows). Start one with
/// POST /api/v1/requests/{id}/conversation; live updates arrive over SignalR at /hubs/chat.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/conversations")]
public sealed class ConversationsController : ControllerBase
{
    public sealed record MessageBody(string? Body, IReadOnlyList<Guid>? FileIds);

    public sealed record ReadResponse(DateTimeOffset ReadAt);

    /// <summary>The user's conversations (as customer and as partner), most recent first, with unread counts.</summary>
    [HttpGet]
    public Task<PagedResult<ConversationDto>> List(
        [FromServices] IQueryHandler<GetConversations, PagedResult<ConversationDto>> handler,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetConversations(page ?? 1, pageSize ?? GetConversations.DefaultPageSize), cancellationToken);

    /// <summary>Unread conversations and messages, for badges.</summary>
    [HttpGet("unread")]
    public Task<UnreadSummaryDto> Unread(
        [FromServices] IQueryHandler<GetUnreadSummary, UnreadSummaryDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetUnreadSummary(), cancellationToken);

    /// <summary>One conversation (404 "conversation.not_found" for anyone else's).</summary>
    [HttpGet("{id:guid}")]
    public Task<ConversationDto> Get(
        Guid id,
        [FromServices] IQueryHandler<GetConversation, ConversationDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetConversation(id), cancellationToken);

    /// <summary>Messages oldest to newest: the latest page, or the page before <c>before</c> (a message's sentAt).</summary>
    [HttpGet("{id:guid}/messages")]
    public Task<MessagePageDto> Messages(
        Guid id,
        [FromServices] IQueryHandler<GetMessages, MessagePageDto> handler,
        [FromQuery] DateTimeOffset? before,
        [FromQuery] int? limit,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetMessages(id, before, limit ?? GetMessages.DefaultLimit), cancellationToken);

    /// <summary>Sends a message (text and/or up to 5 uploaded files). 422 "conversation.closed" once the request ended without an order.</summary>
    [HttpPost("{id:guid}/messages")]
    public async Task<ActionResult<MessageDto>> SendAsync(
        Guid id,
        MessageBody body,
        [FromServices] ICommandHandler<SendMessage, MessageDto> handler,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await handler.HandleAsync(new SendMessage(id, body.Body, body.FileIds), cancellationToken));

    /// <summary>Marks the conversation read up to now.</summary>
    [HttpPost("{id:guid}/read")]
    public async Task<ReadResponse> ReadAsync(
        Guid id,
        [FromServices] ICommandHandler<MarkConversationRead, DateTimeOffset> handler,
        CancellationToken cancellationToken) =>
        new(await handler.HandleAsync(new MarkConversationRead(id), cancellationToken));
}
