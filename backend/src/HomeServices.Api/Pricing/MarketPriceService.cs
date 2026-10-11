using HomeServices.Application.Catalog;
using HomeServices.Application.Messaging;
using HomeServices.Application.Partners;
using HomeServices.Application.Stores;
using Microsoft.Extensions.Options;

namespace HomeServices.Api.Pricing;

/// <summary>
/// Every Pricing:RecalculateHours: recalculates every work item's market range from partners' prices (prices also
/// update when partners or staff change them; this catches partners being approved or suspended, and prices going out
/// of date), recalculates every material's prices from store prices (this catches sales ending and store prices going
/// stale), then reminds partners whose prices are out of date to confirm them. Turned off with
/// Pricing:JobEnabled = false (the integration tests do this).
/// </summary>
public sealed partial class MarketPriceService(
    IServiceScopeFactory scopes,
    IOptions<PricingSettings> settings,
    IConfiguration configuration,
    TimeProvider clock,
    ILogger<MarketPriceService> logger) : BackgroundService
{
    public const string EnabledKey = "Pricing:JobEnabled";

    /// <summary>One pass. Failures are logged, not thrown, so the next pass still runs.</summary>
    public async Task<MarketPricesResult?> RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<RecalculateMarketPrices, MarketPricesResult>>();
            var result = await handler.HandleAsync(new RecalculateMarketPrices(), cancellationToken);
            var materialPrices = scope.ServiceProvider.GetRequiredService<ICommandHandler<RecalculateMaterialPrices, MaterialPricesResult>>();
            var materials = await materialPrices.HandleAsync(new RecalculateMaterialPrices(), cancellationToken);
            var reminders = scope.ServiceProvider.GetRequiredService<ICommandHandler<RemindStalePrices, int>>();
            var reminded = await reminders.HandleAsync(new RemindStalePrices(), cancellationToken);
            LogPass(logger, result.WorkItems, result.FromPartners, materials.Materials, materials.FromStores, reminded);
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

        using var timer = new PeriodicTimer(TimeSpan.FromHours(settings.Value.RecalculateHours), clock);
        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Market prices: {WorkItems} work items, {FromPartners} priced by partners; {Materials} materials, {FromStores} priced by stores; {Reminded} partners reminded to confirm prices")]
    private static partial void LogPass(ILogger logger, int workItems, int fromPartners, int materials, int fromStores, int reminded);

    [LoggerMessage(Level = LogLevel.Error, Message = "The market price pass failed")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
