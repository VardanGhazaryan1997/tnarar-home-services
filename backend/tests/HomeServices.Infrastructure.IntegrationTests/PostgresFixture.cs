using HomeServices.Infrastructure.IntegrationTests.Fakes;
using HomeServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace HomeServices.Infrastructure.IntegrationTests;

/// <summary>One PostgreSQL container for the whole test run (needs Docker running).</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public FixedTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 5, 9, 0, 0, TimeSpan.Zero));

    public FakeCurrentUser CurrentUser { get; } = new();

    public string ConnectionString => _container.GetConnectionString();

    /// <summary>Connection string for a separate, empty database on the same server.</summary>
    public string ConnectionStringFor(string database) =>
        new NpgsqlConnectionStringBuilder(ConnectionString) { Database = database }.ConnectionString;

    public TestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql(ConnectionString)
            .AddInterceptors(new AuditingInterceptor(Clock, CurrentUser), new AuditLogInterceptor(Clock, CurrentUser))
            .Options);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
