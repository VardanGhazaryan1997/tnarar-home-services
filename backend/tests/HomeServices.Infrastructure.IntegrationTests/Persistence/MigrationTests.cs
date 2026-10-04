using HomeServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public class MigrationTests(PostgresFixture db)
{
    [Fact]
    public async Task Migrations_apply_cleanly_to_an_empty_database()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(db.ConnectionStringFor("migrations_check"))
            .Options;
        await using var context = new AppDbContext(options);

        await context.Database.MigrateAsync();

        (await context.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
        (await context.Database.CanConnectAsync()).ShouldBeTrue();
    }

    [Fact]
    public void The_model_has_no_changes_missing_from_the_migrations()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(db.ConnectionStringFor("migrations_model_check"))
            .Options;
        using var context = new AppDbContext(options);

        context.Database.HasPendingModelChanges().ShouldBeFalse(
            "Run 'dotnet ef migrations add <Name>' and check that seed data compares by value.");
    }
}
