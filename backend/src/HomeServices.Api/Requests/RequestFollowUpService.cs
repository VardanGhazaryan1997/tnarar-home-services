using HomeServices.Application.Messaging;
using HomeServices.Application.Requests;
using Microsoft.Extensions.Options;

namespace HomeServices.Api.Requests;

/// <summary>
/// Every few minutes (Requests:FollowUpMinutes), moves open requests nobody responded to in time to the
/// operator queue. Turned off with Requests:FollowUpEnabled = false (the integration tests do this).
/// </summary>
public sealed partial class RequestFollowUpService(
    IServiceScopeFactory scopes,
    IOptions<RequestSettings> settings,
    IConfiguration configuration,
    TimeProvider clock,
    ILogger<RequestFollowUpService> logger) : BackgroundService
{
    public const string EnabledKey = "Requests:FollowUpEnabled";

    /// <summary>One pass. Failures are logged, not thrown, so the next pass still runs.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<FlagUnansweredRequests, int>>();
            var flagged = await handler.HandleAsync(new FlagUnansweredRequests(), cancellationToken);
            if (flagged > 0)
            {
                LogFlagged(logger, flagged);
            }

            return flagged;
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

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(settings.Value.FollowUpMinutes), clock);
        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Count} unanswered requests moved to the operator queue")]
    private static partial void LogFlagged(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "The request follow-up pass failed")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
