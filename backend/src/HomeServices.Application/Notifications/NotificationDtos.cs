namespace HomeServices.Application.Notifications;

/// <summary>
/// A notification in the user's list. <see cref="Type"/> names what happened (the Portal words it in the user's language);
/// <see cref="Link"/> is the Portal path to open, without the language prefix; <see cref="Params"/> holds values for the text.
/// </summary>
public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Link,
    IReadOnlyDictionary<string, string?> Params,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

/// <summary>Who of an order's two sides is told.</summary>
public enum NoticeTo
{
    /// <summary>The side that didn't act.</summary>
    OtherSide = 1,
    Customer = 2,
    Partner = 3,
    Both = 4,
}

/// <summary>Live delivery of a new notification to the user's open Portal tabs. Implemented in the API with SignalR.</summary>
public interface INotificationPusher
{
    Task PushAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken);
}

/// <summary>Notification delivery settings (configuration section "Notifications").</summary>
public sealed class NotificationSettings
{
    public const string SectionName = "Notifications";

    /// <summary>The Portal's address, for links in SMS messages.</summary>
    public string PortalUrl { get; set; } = "https://tnarar.am";

    /// <summary>How often the delivery job looks for new notifications.</summary>
    public int DeliverySeconds { get; set; } = 15;

    /// <summary>At most this many notifications per delivery pass.</summary>
    public int BatchSize { get; set; } = 100;
}
