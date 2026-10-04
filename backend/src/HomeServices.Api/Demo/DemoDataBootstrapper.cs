using HomeServices.Application.Demo;
using HomeServices.Application.Messaging;

namespace HomeServices.Api.Demo;

/// <summary>
/// On startup, adds demo companies and specialists when "DemoData:SeedPartners" is true (staging and local
/// development only; Program refuses it in Production). Runs once: it does nothing when they already exist.
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
        if (created > 0)
        {
            LogSeeded(scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DemoDataBootstrapper)), created);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Added {Count} demo partners (DemoData:SeedPartners is on).")]
    private static partial void LogSeeded(ILogger logger, int count);
}
