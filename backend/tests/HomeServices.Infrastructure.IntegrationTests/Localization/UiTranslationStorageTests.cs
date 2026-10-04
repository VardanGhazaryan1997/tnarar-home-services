using HomeServices.Domain.Localization;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Infrastructure.IntegrationTests.Localization;

[Collection(PostgresCollection.Name)]
public class UiTranslationStorageTests(PostgresFixture db)
{
    private static string UniqueNamespace() => $"t{Guid.NewGuid():N}"[..20];

    [Fact]
    public async Task Texts_are_saved_per_namespace_key_and_language()
    {
        var ns = UniqueNamespace();
        await using (var context = db.CreateContext())
        {
            context.UiTranslations.AddRange(
                UiTranslation.Create(ns, "home.title", "hy", "Բարի գալուստ"),
                UiTranslation.Create(ns, "home.title", "ru", "Добро пожаловать"));
            await context.SaveChangesAsync();
        }

        await using (var context = db.CreateContext())
        {
            var texts = await context.UiTranslations.Where(t => t.Namespace == ns).OrderBy(t => t.LanguageCode).ToListAsync();
            texts.Select(t => t.Value).ShouldBe(new[] { "Բարի գալուստ", "Добро пожаловать" });
        }
    }

    [Fact]
    public async Task A_key_has_one_text_per_language()
    {
        var ns = UniqueNamespace();
        await using var context = db.CreateContext();
        context.UiTranslations.AddRange(UiTranslation.Create(ns, "a", "hy", "1"), UiTranslation.Create(ns, "a", "hy", "2"));

        await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Texts_need_an_existing_language()
    {
        await using var context = db.CreateContext();
        context.UiTranslations.Add(UiTranslation.Create(UniqueNamespace(), "a", "xx", "1"));

        await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
