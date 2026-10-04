namespace HomeServices.Application.Chat;

/// <summary>Pushes chat events to the signed-in apps of the given users (SignalR in the API). Best effort: failures are not errors.</summary>
public interface IChatNotifier
{
    Task MessageSentAsync(IReadOnlyCollection<Guid> userIds, MessageDto message, CancellationToken cancellationToken);

    Task ConversationReadAsync(IReadOnlyCollection<Guid> userIds, Guid conversationId, string readerRole, DateTimeOffset readAt, CancellationToken cancellationToken);
}
