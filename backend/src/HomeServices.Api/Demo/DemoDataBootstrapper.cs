using HomeServices.Application.Demo;
using HomeServices.Application.Messaging;

namespace HomeServices.Api.Demo;

/// <summary>
/// On startup, adds demo companies and specialists when "DemoData:SeedPartners" is true (staging and local
/// development only; Program refuses it in Production), and gives demo partners without prices a price list.
/// Both steps skip what already exists, so restarts change nothing.
/// </summary>
public static partial class DemoDataBootstrapper
{
    public const string SettingName = "DemoData:SeedPartners";

    public static async Task SeedDemoDataAsync(this IServiceProvider services, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>(SettingName))
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SeedDemoPartners, int>>();
        var created = await handler.HandleAsync(new SeedDemoPartners(), CancellationToken.None);
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DemoDataBootstrapper));
        if (created > 0)
        {
            LogSeeded(logger, created);
        }

        var prices = scope.ServiceProvider.GetRequiredService<ICommandHandler<SeedDemoPrices, int>>();
        var priced = await prices.HandleAsync(new SeedDemoPrices(), CancellationToken.None);
        if (priced > 0)
        {
            LogPriced(logger, priced);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Added {Count} demo partners (DemoData:SeedPartners is on).")]
    private static partial void LogSeeded(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gave {Count} demo partners a price list (DemoData:SeedPartners is on).")]
    private static partial void LogPriced(ILogger logger, int count);
}
