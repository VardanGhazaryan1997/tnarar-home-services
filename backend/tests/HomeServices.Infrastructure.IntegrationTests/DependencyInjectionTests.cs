using HomeServices.Application.Abstractions;
using HomeServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Infrastructure.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class DependencyInjectionTests(PostgresFixture db)
{
    [Fact]
    public void Registers_the_database_context_with_the_configured_connection_string()
    {
        using var provider = BuildProvider("Host=db.example;Database=home_services;Username=app;Password=x");
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.Database.GetConnectionString()!.ShouldContain("Host=db.example");
    }

    [Fact]
    public void Explains_clearly_when_the_connection_string_is_missing()
    {
        using var provider = BuildProvider(connectionString: null);
        using var scope = provider.CreateScope();

        var exception = Should.Throw<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<AppDbContext>());

        exception.Message.ShouldContain("ConnectionStrings:Database");
    }

    [Fact]
    public void Until_sign_in_exists_changes_are_attributed_to_no_user()
    {
        using var provider = BuildProvider("Host=localhost");
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ICurrentUser>().UserId.ShouldBeNull();
        provider.GetRequiredService<TimeProvider>().ShouldBeSameAs(TimeProvider.System);
    }

    [Fact]
    public async Task MigrateDatabaseAsync_applies_migrations_on_startup()
    {
        await using var provider = BuildProvider(db.ConnectionStringFor("startup_migrations"));

        await provider.MigrateDatabaseAsync();

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await context.Database.CanConnectAsync()).ShouldBeTrue();
        (await context.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
    }

    private static ServiceProvider BuildProvider(string? connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Database"] = connectionString })
            .Build();

        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddInfrastructure(configuration)
            .BuildServiceProvider();
    }
}
