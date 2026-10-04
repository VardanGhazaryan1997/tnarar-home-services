using HomeServices.Application.Common;
using HomeServices.Application.Messaging;
using HomeServices.Application.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers;

/// <summary>
/// The signed-in user's notifications. New ones also arrive live on the chat hub as "notificationReceived" (NotificationDto).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public sealed class NotificationsController : ControllerBase
{
    /// <summary>Newest first. <paramref name="unreadOnly"/> keeps unread ones; <paramref name="pageSize"/> is at most 100.</summary>
    [HttpGet]
    public Task<PagedResult<NotificationDto>> List(
        [FromServices] IQueryHandler<GetMyNotifications, PagedResult<NotificationDto>> handler,
        [FromQuery] bool? unreadOnly,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetMyNotifications(unreadOnly ?? false, page ?? 1, pageSize ?? GetMyNotifications.DefaultPageSize), cancellationToken);

    /// <summary>How many are unread, for the bell's badge: <c>{ "count": 3 }</c>.</summary>
    [HttpGet("unread-count")]
    public async Task<object> UnreadCountAsync([FromServices] IQueryHandler<GetUnreadNotificationCount, int> handler, CancellationToken cancellationToken) =>
        new { count = await handler.HandleAsync(new GetUnreadNotificationCount(), cancellationToken) };

    [HttpPost("{id:guid}/read")]
    public Task<NotificationDto> Read(Guid id, [FromServices] ICommandHandler<MarkNotificationRead, NotificationDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new MarkNotificationRead(id), cancellationToken);

    /// <summary>Marks all as read: <c>{ "count": n }</c> changed.</summary>
    [HttpPost("read-all")]
    public async Task<object> ReadAllAsync([FromServices] ICommandHandler<MarkAllNotificationsRead, int> handler, CancellationToken cancellationToken) =>
        new { count = await handler.HandleAsync(new MarkAllNotificationsRead(), cancellationToken) };
}
