using HomeServices.Application.Messaging;
using HomeServices.Application.Staff;

namespace HomeServices.Api.Staff;

/// <summary>
/// On startup, creates the first Super Admin from "Bootstrap:SuperAdmin" settings when no staff exists.
/// In production, pass these as environment variables for the first deployment only, then remove them.
/// </summary>
public static partial class SuperAdminBootstrapper
{
    public const string SectionName = "Bootstrap:SuperAdmin";

    public static async Task BootstrapSuperAdminAsync(this IServiceProvider services, IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var email = section["Email"];
        var password = section["Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<BootstrapSuperAdmin, bool>>();
        var created = await handler.HandleAsync(new BootstrapSuperAdmin(email, section["FullName"] ?? "Super Admin", password), CancellationToken.None);

        if (created)
        {
            LogCreated(scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(SuperAdminBootstrapper)), email);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Created the first Super Admin account {Email}. Sign in and set up two-factor authentication.")]
    private static partial void LogCreated(ILogger logger, string email);
}
