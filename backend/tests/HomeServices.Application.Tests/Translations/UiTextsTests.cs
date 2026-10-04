using HomeServices.Application.Errors;
using HomeServices.Application.Tests.Support;
using HomeServices.Application.Translations;
using HomeServices.Domain.Localization;
using Microsoft.Extensions.Caching.Memory;

namespace HomeServices.Application.Tests.Translations;

public class UiTextsTests
{
    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly UiTextsVersion _version = new();

    public UiTextsTests()
    {
        _db.UiTranslations.AddRange(
            UiTranslation.Create("portal", "home.title", "hy", "Բարի գալուստ"),
            UiTranslation.Create("portal", "home.cta", "hy", "Սկսել"),
            UiTranslation.Create("portal", "home.title", "ru", "Добро пожаловать"),
            UiTranslation.Create("portal", "orphan", "ru", "Нет в армянском"),
            UiTranslation.Create("backoffice", "nav.dashboard", "hy", "Վահանակ"));
        _db.SaveChanges();
    }

    private Task<UiTextsDto> GetAsync(string language, string ns, params string[] active) =>
        new GetUiTextsHandler(_db, new FakeLanguageCatalog("hy", active), _cache, _version).HandleAsync(new GetUiTexts(language, ns), CancellationToken.None);

    [Fact]
    public async Task Default_language_texts_come_as_nested_JSON()
    {
        var texts = await GetAsync("hy", "portal");

        texts.Json.ShouldBe("""{"home":{"cta":"Սկսել","title":"Բարի գալուստ"}}""");
        texts.ETag.ShouldStartWith("\"");
        texts.ETag.Length.ShouldBe(34);
    }

    [Fact]
    public async Task Missing_texts_fall_back_to_the_default_language_and_extra_keys_are_ignored()
    {
        var texts = await GetAsync(" RU ", "Portal");

        texts.Json.ShouldBe("""{"home":{"cta":"Սկսել","title":"Добро пожаловать"}}""");
    }

    [Fact]
    public async Task A_language_without_texts_gets_the_default_texts()
    {
        (await GetAsync("en", "backoffice")).Json.ShouldBe("""{"nav":{"dashboard":"Վահանակ"}}""");
        (await GetAsync("en", "unknown-app")).Json.ShouldBe("{}");
    }

    [Fact]
    public async Task Unknown_or_inactive_languages_and_bad_namespaces_are_404()
    {
        (await Should.ThrowAsync<NotFoundException>(() => GetAsync("fr", "portal"))).Code.ShouldBe("language.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => GetAsync("en", "portal", "hy", "ru"))).Code.ShouldBe("language.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => GetAsync("hy", "Bad Namespace"))).Code.ShouldBe("translation.namespace_not_found");
    }

    [Fact]
    public async Task Texts_are_cached_until_something_changes()
    {
        var first = await GetAsync("hy", "portal");
        _db.UiTranslations.Add(UiTranslation.Create("portal", "home.subtitle", "hy", "Նոր"));
        await _db.SaveChangesAsync();

        (await GetAsync("hy", "portal")).ShouldBeSameAs(first);

        _version.Bump();
        var refreshed = await GetAsync("hy", "portal");
        refreshed.Json.ShouldContain("Նոր");
        refreshed.ETag.ShouldNotBe(first.ETag);
    }
}
