using HomeServices.Application.Languages;
using HomeServices.Domain.Localization;
using HomeServices.Infrastructure.Localization;
using HomeServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HomeServices.Infrastructure.IntegrationTests.Localization;

[Collection(PostgresCollection.Name)]
public class CachedLanguageCatalogTests(PostgresFixture db)
{
    [Fact]
    public async Task Lists_active_languages_in_order_with_the_default()
    {
        await using var context = await MigratedContext("catalog_list");
        using var cache = new MemoryCache(new MemoryCacheOptions());

        var catalog = await new CachedLanguageCatalog(context, cache).GetAsync(CancellationToken.None);

        catalog.ActiveCodes.ShouldBe(new[] { "hy", "ru", "en" });
        catalog.DefaultCode.ShouldBe("hy");
    }

    [Fact]
    public async Task Serves_later_requests_from_the_cache()
    {
        await using var context = await MigratedContext("catalog_cache");
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new CachedLanguageCatalog(context, cache);
        await sut.GetAsync(CancellationToken.None);

        var french = Language.Create("fr", "French", "Français", 4);
        french.Activate();
        context.Languages.Add(french);
        await context.SaveChangesAsync();

        (await sut.GetAsync(CancellationToken.None)).ActiveCodes.ShouldNotContain("fr");
    }

    [Fact]
    public async Task Falls_back_to_Armenian_when_no_default_is_set()
    {
        await using var context = await MigratedContext("catalog_no_default");
        await context.Languages.ExecuteUpdateAsync(s => s.SetProperty(l => l.IsDefault, false));
        using var cache = new MemoryCache(new MemoryCacheOptions());

        var catalog = await new CachedLanguageCatalog(context, cache).GetAsync(CancellationToken.None);

        catalog.DefaultCode.ShouldBe(LanguageCatalog.FallbackLanguage);
    }

    private async Task<AppDbContext> MigratedContext(string database)
    {
        var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(db.ConnectionStringFor(database)).Options);
        await context.Database.MigrateAsync();
        return context;
    }
}
