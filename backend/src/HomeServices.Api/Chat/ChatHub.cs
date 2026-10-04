using HomeServices.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HomeServices.Api.Chat;

/// <summary>
/// Live chat events for signed-in Portal users at /hubs/chat. Clients only listen: messages are sent with the REST API.
/// Events: "messageReceived" (MessageDto), "conversationRead" ({ conversationId, readerRole, readAt }) and
/// "notificationReceived" (NotificationDto).
/// Browsers pass the access token as ?access_token=… (WebSockets can't send headers).
/// </summary>
[Authorize(AuthenticationSchemes = AuthSchemes.Portal)]
public sealed class ChatHub : Hub
{
    public const string Path = "/hubs/chat";

    public static string UserGroup(Guid userId) => $"user:{userId}";

    /// <summary>Every connection of a user joins that user's group, so all their devices get the events.</summary>
    public override async Task OnConnectedAsync()
    {
        if (Guid.TryParse(Context.User?.FindFirst("sub")?.Value, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        }

        await base.OnConnectedAsync();
    }
}
