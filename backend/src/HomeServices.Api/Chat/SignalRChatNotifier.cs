using HomeServices.Application.Chat;
using Microsoft.AspNetCore.SignalR;

namespace HomeServices.Api.Chat;

/// <summary>Sends chat events to the users' SignalR groups. A failed push is logged; the message itself is already saved.</summary>
public sealed partial class SignalRChatNotifier(IHubContext<ChatHub> hub, ILogger<SignalRChatNotifier> logger) : IChatNotifier
{
    public const string MessageReceived = "messageReceived";
    public const string ConversationRead = "conversationRead";

    public Task MessageSentAsync(IReadOnlyCollection<Guid> userIds, MessageDto message, CancellationToken cancellationToken) =>
        SendAsync(userIds, MessageReceived, message, cancellationToken);

    public Task ConversationReadAsync(IReadOnlyCollection<Guid> userIds, Guid conversationId, string readerRole, DateTimeOffset readAt, CancellationToken cancellationToken) =>
        SendAsync(userIds, ConversationRead, new { conversationId, readerRole, readAt }, cancellationToken);

    private async Task SendAsync(IReadOnlyCollection<Guid> userIds, string method, object payload, CancellationToken cancellationToken)
    {
        try
        {
            await hub.Clients.Groups(userIds.Distinct().Select(ChatHub.UserGroup).ToList()).SendAsync(method, payload, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogPushFailed(logger, method, exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Chat push {Method} failed")]
    private static partial void LogPushFailed(ILogger logger, string method, Exception exception);
}
