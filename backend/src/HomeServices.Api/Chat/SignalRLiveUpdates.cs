using HomeServices.Application.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace HomeServices.Api.Chat;

/// <summary>Sends request and offer status changes to the users' SignalR groups. A failed push is logged; the change is already saved.</summary>
public sealed partial class SignalRLiveUpdates(IHubContext<ChatHub> hub, ILogger<SignalRLiveUpdates> logger) : ILiveUpdates
{
    public const string RequestUpdated = "requestUpdated";
    public const string OfferUpdated = "offerUpdated";

    public Task RequestUpdatedAsync(IReadOnlyCollection<Guid> userIds, RequestUpdateDto update, CancellationToken cancellationToken) =>
        SendAsync(userIds, RequestUpdated, update, cancellationToken);

    public Task OfferUpdatedAsync(IReadOnlyCollection<Guid> userIds, OfferUpdateDto update, CancellationToken cancellationToken) =>
        SendAsync(userIds, OfferUpdated, update, cancellationToken);

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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Live update {Method} failed")]
    private static partial void LogPushFailed(ILogger logger, string method, Exception exception);
}
