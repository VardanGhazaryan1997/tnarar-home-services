using HomeServices.Application.Messaging;
using HomeServices.Application.Orders;
using Microsoft.Extensions.Options;

namespace HomeServices.Api.Orders;

/// <summary>
/// Every Orders:AutoCompleteCheckMinutes, completes orders the partner marked as done when the customer didn't
/// answer in time. Turned off with Orders:AutoCompleteEnabled = false (the integration tests do this).
/// </summary>
public sealed partial class OrderAutoCompleteService(
    IServiceScopeFactory scopes,
    IOptions<OrderSettings> settings,
    IConfiguration configuration,
    TimeProvider clock,
    ILogger<OrderAutoCompleteService> logger) : BackgroundService
{
    public const string EnabledKey = "Orders:AutoCompleteEnabled";

    /// <summary>One pass. Failures are logged, not thrown, so the next pass still runs.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CompleteUnansweredOrders, int>>();
            var completed = await handler.HandleAsync(new CompleteUnansweredOrders(), cancellationToken);
            if (completed > 0)
            {
                LogCompleted(logger, completed);
            }

            return completed;
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

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(settings.Value.AutoCompleteCheckMinutes), clock);
        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Count} orders completed because the customer didn't answer")]
    private static partial void LogCompleted(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "The order auto-complete pass failed")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
