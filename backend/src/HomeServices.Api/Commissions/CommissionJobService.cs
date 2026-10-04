using HomeServices.Application.Commissions;
using HomeServices.Application.Messaging;
using Microsoft.Extensions.Options;

namespace HomeServices.Api.Commissions;

/// <summary>
/// Every Commissions:JobMinutes: issues statements for finished weeks, reminds partners whose statements became overdue,
/// pauses partners overdue for too long and resumes those who paid. Turned off with Commissions:JobEnabled = false
/// (the integration tests do this).
/// </summary>
public sealed partial class CommissionJobService(
    IServiceScopeFactory scopes,
    IOptions<CommissionSettings> settings,
    IConfiguration configuration,
    TimeProvider clock,
    ILogger<CommissionJobService> logger) : BackgroundService
{
    public const string EnabledKey = "Commissions:JobEnabled";

    /// <summary>One pass. Failures are logged, not thrown, so the next pass still runs.</summary>
    public async Task<CommissionCycleResult?> RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<RunCommissionCycle, CommissionCycleResult>>();
            var result = await handler.HandleAsync(new RunCommissionCycle(), cancellationToken);
            if (result.StatementsIssued + result.RemindersSent + result.PartnersPaused + result.PartnersResumed > 0)
            {
                LogCycle(logger, result.StatementsIssued, result.RemindersSent, result.PartnersPaused, result.PartnersResumed);
            }

            return result;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogFailed(logger, exception);
            return null;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue(EnabledKey, defaultValue: true))
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(settings.Value.JobMinutes), clock);
        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Commissions: {Issued} statements issued, {Reminded} reminders, {Paused} partners paused, {Resumed} resumed")]
    private static partial void LogCycle(ILogger logger, int issued, int reminded, int paused, int resumed);

    [LoggerMessage(Level = LogLevel.Error, Message = "The commission pass failed")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
