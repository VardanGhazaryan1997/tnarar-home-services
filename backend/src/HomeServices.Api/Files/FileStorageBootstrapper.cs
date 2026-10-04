using HomeServices.Application.Files;

namespace HomeServices.Api.Files;

/// <summary>
/// On startup, prepares file storage: creates the bucket when "Storage:CreateBucket" is on and sets the browser
/// CORS rules when "Storage:CorsOrigins" lists sites.
/// When storage isn't running the API still starts; only uploads fail until it is.
/// </summary>
public static partial class FileStorageBootstrapper
{
    public static async Task PrepareFileStorageAsync(this IServiceProvider services)
    {
        try
        {
            await services.GetRequiredService<IFileStorage>().EnsureReadyAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            LogUnavailable(services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(FileStorageBootstrapper)), exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "File storage could not be prepared; uploads may fail until it is. Locally, start it with: docker compose --profile storage up -d")]
    private static partial void LogUnavailable(ILogger logger, Exception exception);
}
