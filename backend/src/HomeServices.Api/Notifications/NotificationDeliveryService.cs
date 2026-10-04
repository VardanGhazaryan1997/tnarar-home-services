using HomeServices.Application.Messaging;
using HomeServices.Application.Notifications;
using Microsoft.Extensions.Options;

namespace HomeServices.Api.Notifications;

/// <summary>
/// Every Notifications:DeliverySeconds, sends new notifications (SMS for the important ones, a live update for all).
/// Turned off with Notifications:DeliveryEnabled = false (the integration tests do this).
/// </summary>
public sealed partial class NotificationDeliveryService(
    IServiceScopeFactory scopes,
    IOptions<NotificationSettings> settings,
    IConfiguration configuration,
    TimeProvider clock,
    ILogger<NotificationDeliveryService> logger) : BackgroundService
{
    public const string EnabledKey = "Notifications:DeliveryEnabled";

    /// <summary>One pass. Failures are logged, not thrown, so the next pass still runs.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<DeliverNotifications, int>>();
            return await handler.HandleAsync(new DeliverNotifications(), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogFailed(logger, exception);
            return 0;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue(EnabledKey, defaultValue: true))
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.Value.DeliverySeconds), clock);
        do
        {
            // A full batch means more may be waiting: go again right away.
            int delivered;
            do
            {
                delivered = await RunOnceAsync(stoppingToken);
            }
            while (delivered >= settings.Value.BatchSize);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "The notification delivery pass failed")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
