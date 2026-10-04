using HomeServices.Api.Chat;
using HomeServices.Application.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace HomeServices.Api.Notifications;

/// <summary>Sends a new notification to the user's open Portal tabs over the chat hub. A failed push is logged; the notification is in the list anyway.</summary>
public sealed partial class SignalRNotificationPusher(IHubContext<ChatHub> hub, ILogger<SignalRNotificationPusher> logger) : INotificationPusher
{
    public const string NotificationReceived = "notificationReceived";

    public async Task PushAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken)
    {
        try
        {
            await hub.Clients.Group(ChatHub.UserGroup(userId)).SendAsync(NotificationReceived, notification, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogPushFailed(logger, exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Notification push failed")]
    private static partial void LogPushFailed(ILogger logger, Exception exception);
}
