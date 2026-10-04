using HomeServices.Application.Abstractions;
using HomeServices.Application.Commissions;
using HomeServices.Application.Files;
using HomeServices.Application.Identity;
using HomeServices.Application.Languages;
using HomeServices.Application.Notifications;
using HomeServices.Application.Orders;
using HomeServices.Application.Requests;
using HomeServices.Application.Staff;
using HomeServices.Infrastructure.Files;
using HomeServices.Infrastructure.Identity;
using HomeServices.Infrastructure.Localization;
using HomeServices.Infrastructure.Persistence;
using HomeServices.Infrastructure.Staff;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HomeServices.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Database";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICurrentUser, SystemCurrentUser>();
        services.AddScoped<AuditingInterceptor>();
        services.AddScoped<AuditLogInterceptor>();

        // The connection string is read when the context is first needed, so tests and
        // hosts can supply configuration after registration.
        services.AddDbContext<AppDbContext>((provider, options) => options
            .UseNpgsql(provider.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection string 'ConnectionStrings:{ConnectionStringName}' is missing. Add it to appsettings or environment variables."),
                // Loading a profile with its services, areas and media: one query per collection, not one huge join.
                npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            // Order matters: AuditingInterceptor turns soft deletes into updates before the audit log reads them.
            .AddInterceptors(
                provider.GetRequiredService<AuditingInterceptor>(),
                provider.GetRequiredService<AuditLogInterceptor>()));

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddMemoryCache();
        services.AddScoped<ILanguageCatalog, CachedLanguageCatalog>();

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

        services.AddIdentityServices();
        services.AddFileServices();
        services.AddOptions<RequestSettings>()
            .BindConfiguration(RequestSettings.SectionName)
            .Validate(s => s.MaxRecipients is >= 1 and <= 200, "Requests:MaxRecipients must be between 1 and 200.")
            .Validate(s => s.ResponseHours is >= 1 and <= 720, "Requests:ResponseHours must be between 1 and 720.")
            .Validate(s => s.FollowUpMinutes is >= 1 and <= 1440, "Requests:FollowUpMinutes must be between 1 and 1440.");
        services.AddOptions<OrderSettings>()
            .BindConfiguration(OrderSettings.SectionName)
            .Validate(s => s.AutoCompleteDays is >= 1 and <= 60, "Orders:AutoCompleteDays must be between 1 and 60.")
            .Validate(s => s.AutoCompleteCheckMinutes is >= 1 and <= 1440, "Orders:AutoCompleteCheckMinutes must be between 1 and 1440.");
        services.AddOptions<CommissionSettings>()
            .BindConfiguration(CommissionSettings.SectionName)
            .Validate(s => s.UtcOffsetHours is >= -12 and <= 14, "Commissions:UtcOffsetHours must be between -12 and 14.")
            .Validate(s => s.DueDays is >= 1 and <= 60, "Commissions:DueDays must be between 1 and 60.")
            .Validate(s => s.PauseAfterOverdueDays is >= 0 and <= 365, "Commissions:PauseAfterOverdueDays must be between 0 and 365.")
            .Validate(s => s.JobMinutes is >= 1 and <= 1440, "Commissions:JobMinutes must be between 1 and 1440.");
        services.AddOptions<NotificationSettings>()
            .BindConfiguration(NotificationSettings.SectionName)
            .Validate(s => Uri.TryCreate(s.PortalUrl, UriKind.Absolute, out _), "Notifications:PortalUrl must be an absolute URL.")
            .Validate(s => s.DeliverySeconds is >= 1 and <= 3600, "Notifications:DeliverySeconds must be between 1 and 3600.")
            .Validate(s => s.BatchSize is >= 1 and <= 1000, "Notifications:BatchSize must be between 1 and 1000.");

        return services;
    }

    private static void AddFileServices(this IServiceCollection services)
    {
        services.AddOptions<FileSettings>().BindConfiguration(FileSettings.SectionName);
        services.AddOptions<StorageSettings>()
            .BindConfiguration(StorageSettings.SectionName)
            .Validate(s => Uri.TryCreate(s.ServiceUrl, UriKind.Absolute, out _), "Storage:ServiceUrl must be an absolute URL.")
            .Validate(
                s => string.IsNullOrWhiteSpace(s.PublicServiceUrl) || Uri.TryCreate(s.PublicServiceUrl, UriKind.Absolute, out _),
                "Storage:PublicServiceUrl must be an absolute URL.")
            .Validate(s => s.AccessKey.Length > 0 && s.SecretKey.Length > 0, "Storage:AccessKey and Storage:SecretKey are required.");

        services.AddSingleton<IFileStorage, S3FileStorage>();
        services.AddSingleton<IImageProcessor, SkiaImageProcessor>();
    }

    private static void AddIdentityServices(this IServiceCollection services)
    {
        services.AddOptions<AuthSettings>()
            .BindConfiguration(AuthSettings.SectionName)
            .Validate(s => s.SecretHashKey.Length >= 32, "Auth:SecretHashKey must be at least 32 characters.")
            .ValidateOnStart();
        services.AddOptions<JwtSettings>()
            .BindConfiguration(JwtSettings.SectionName)
            .Validate(s => s.SigningKey.Length >= 32, "Jwt:SigningKey must be at least 32 characters.")
            .ValidateOnStart();

        services.AddSingleton<ISecretHasher, HmacSecretHasher>();
        services.AddSingleton<IOtpGenerator, RandomOtpGenerator>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<FakeSmsSender>();
        services.AddSingleton<ISmsSender>(provider => provider.GetRequiredService<FakeSmsSender>());

        services.AddOptions<StaffAuthSettings>().BindConfiguration(StaffAuthSettings.SectionName);
        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddSingleton<TotpService>();
        services.AddSingleton<ITotpService>(provider => provider.GetRequiredService<TotpService>());
        services.AddSingleton<IStaffTokenService, StaffTokenService>();

        services.AddAuthentication(AuthSchemes.Portal)
            .AddJwtBearer(AuthSchemes.Portal)
            .AddJwtBearer(AuthSchemes.Staff);
        ConfigureBearer(services, AuthSchemes.Portal, jwt => jwt.Audience);
        ConfigureBearer(services, AuthSchemes.Staff, jwt => jwt.StaffAudience);

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.SignedIn, policy => policy
                .AddAuthenticationSchemes(AuthSchemes.Portal, AuthSchemes.Staff)
                .RequireAuthenticatedUser())
            .AddPolicy(AuthPolicies.Staff, policy => policy
                .AddAuthenticationSchemes(AuthSchemes.Staff)
                .RequireAuthenticatedUser()
                .RequireClaim(StaffTokenService.StaffClaim, "true"));
    }

    private static void ConfigureBearer(IServiceCollection services, string scheme, Func<JwtSettings, string> audience)
    {
        services.AddOptions<JwtBearerOptions>(scheme)
            .Configure<IOptions<JwtSettings>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = audience(jwt.Value),
                    IssuerSigningKey = JwtTokenService.SigningKey(jwt.Value),
                    NameClaimType = "sub",
                    RoleClaimType = JwtTokenService.RoleClaim,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });
    }

    /// <summary>Applies pending EF Core migrations (at startup in development, and where Database:MigrateOnStartup is on).</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }
}
