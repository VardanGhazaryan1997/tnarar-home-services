using HomeServices.Domain.Localization;
using HomeServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Infrastructure.IntegrationTests.Localization;

[Collection(PostgresCollection.Name)]
public class LanguageStorageTests(PostgresFixture db)
{
    [Fact]
    public async Task Armenian_Russian_and_English_are_seeded_by_migrations_with_Armenian_as_default()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(db.ConnectionStringFor("language_seed")).Options;
        await using var context = new AppDbContext(options);
        await context.Database.MigrateAsync();

        var languages = await context.Languages.OrderBy(l => l.SortOrder).ToListAsync();

        languages.Select(l => l.Code).ShouldBe(new[] { "hy", "ru", "en" });
        languages.ShouldAllBe(l => l.IsActive);
        languages.Single(l => l.IsDefault).Code.ShouldBe("hy");
        languages[0].NativeName.ShouldBe("Հայերեն");
    }

    [Fact]
    public async Task Language_codes_are_unique()
    {
        await using var context = db.CreateContext();
        context.Languages.Add(Language.Create("hy", "Armenian again", "Հայերեն", 9));

        await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Only_one_language_can_be_the_default()
    {
        await using var context = db.CreateContext();
        var german = Language.Create("de", "German", "Deutsch", 9);
        german.Activate();
        german.MakeDefault();
        context.Languages.Add(german);

        await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
