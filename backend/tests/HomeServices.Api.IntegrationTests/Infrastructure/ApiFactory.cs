using HomeServices.Application.Files;
using HomeServices.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace HomeServices.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Runs the real API in memory against a real PostgreSQL container (needs Docker running).
/// Adds this test assembly's controllers so tests can hit test-only endpoints.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    /// <summary>File storage for these tests (instead of S3).</summary>
    public InMemoryFileStorage Storage { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Added last, so it overrides the connection string in appsettings.Development.json.
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{DependencyInjection.ConnectionStringName}"] = _database.GetConnectionString(),
                // Short SMS resend cooldown so tests can sign in twice with one phone number.
                ["Auth:OtpResendCooldownSeconds"] = "1",
                // Tests run the request follow-up pass themselves.
                ["Requests:FollowUpEnabled"] = "false",
                ["Orders:AutoCompleteEnabled"] = "false",
                ["Notifications:DeliveryEnabled"] = "false",
                ["Commissions:JobEnabled"] = "false",
                // Keep test output readable: only warnings and errors.
                ["Serilog:MinimumLevel:Default"] = "Warning",
                ["Serilog:MinimumLevel:Override:Microsoft.AspNetCore"] = "Warning",
                ["Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore"] = "Warning",
            }));
        builder.ConfigureTestServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(ApiFactory).Assembly);
            services.RemoveAll<IFileStorage>();
            services.AddSingleton<IFileStorage>(Storage);
        });
    }

    public Task InitializeAsync() => _database.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _database.DisposeAsync();
    }
}

/// <summary>All API tests share one API instance and one database container.</summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
