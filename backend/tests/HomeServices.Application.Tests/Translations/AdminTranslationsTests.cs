using System.Text.Json;
using HomeServices.Application.Errors;
using HomeServices.Application.Tests.Support;
using HomeServices.Application.Translations;
using HomeServices.Domain;
using HomeServices.Domain.Localization;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Tests.Translations;

public class AdminTranslationsTests
{
    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly FakeLanguageCatalog _catalog = new("hy", "hy", "ru", "en");
    private readonly UiTextsVersion _version = new();

    public AdminTranslationsTests()
    {
        var hy = Language.Create("hy", "Armenian", "Հայերեն", 1);
        hy.Activate();
        hy.MakeDefault();
        var ru = Language.Create("ru", "Russian", "Русский", 2);
        ru.Activate();
        var en = Language.Create("en", "English", "English", 3);
        en.Activate();
        _db.Languages.AddRange(hy, ru, en, Language.Create("fr", "French", "Français", 4));
        _db.UiTranslations.AddRange(
            UiTranslation.Create("portal", "home.title", "hy", "Բարի գալուստ"),
            UiTranslation.Create("portal", "home.cta", "hy", "Սկսել"),
            UiTranslation.Create("portal", "nav.search", "hy", "Որոնում"),
            UiTranslation.Create("portal", "home.title", "ru", "Добро пожаловать"),
            UiTranslation.Create("portal", "home.title", "en", "Welcome"),
            UiTranslation.Create("portal", "home.cta", "en", "Start"),
            UiTranslation.Create("backoffice", "nav.dashboard", "hy", "Վահանակ"));
        _db.SaveChanges();
    }

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private Task<TranslationRowDto> SaveAsync(string key, Dictionary<string, string?> values, string ns = "portal") =>
        new SaveTranslationHandler(_db, _catalog, _version).HandleAsync(new SaveTranslation(ns, key, values), CancellationToken.None);

    private Task<TranslationImportResultDto> ImportAsync(string language, string json, bool replace = false, string ns = "portal") =>
        new ImportTranslationsHandler(_db, _catalog, _version).HandleAsync(new ImportTranslations(ns, language, Json(json), replace), CancellationToken.None);

    private Task<string> ExportAsync(string language, bool withFallback = false) =>
        new ExportTranslationsHandler(_db, _catalog).HandleAsync(new ExportTranslations("portal", language, withFallback), CancellationToken.None);

    private async Task<Dictionary<string, string>> TextsAsync(string language, string ns = "portal")
    {
        _db.ChangeTracker.Clear();
        return await _db.UiTranslations.Where(t => t.Namespace == ns && t.LanguageCode == language).ToDictionaryAsync(t => t.Key, t => t.Value);
    }

    [Fact]
    public async Task Namespaces_show_each_languages_progress()
    {
        var namespaces = await new GetTranslationNamespacesHandler(_db, _catalog).HandleAsync(new GetTranslationNamespaces(), CancellationToken.None);

        namespaces.Select(n => n.Namespace).ShouldBe(new[] { "backoffice", "portal" });
        var portal = namespaces[1];
        portal.KeyCount.ShouldBe(3);
        portal.Languages.ShouldBe(new[]
        {
            new TranslationProgressDto("hy", 3, 0),
            new TranslationProgressDto("ru", 1, 2),
            new TranslationProgressDto("en", 2, 1),
            new TranslationProgressDto("fr", 0, 3),
        });
    }

    [Fact]
    public async Task Keys_are_listed_with_every_text_and_can_be_filtered()
    {
        var handler = new GetTranslationsHandler(_db, _catalog);

        var all = await handler.HandleAsync(new GetTranslations("portal"), CancellationToken.None);
        all.Items.Select(r => r.Key).ShouldBe(new[] { "home.cta", "home.title", "nav.search" });
        all.Items[1].Values.ShouldBe(new Dictionary<string, string> { ["hy"] = "Բարի գալուստ", ["ru"] = "Добро пожаловать", ["en"] = "Welcome" }, ignoreOrder: true);

        (await handler.HandleAsync(new GetTranslations("portal", MissingIn: "RU"), CancellationToken.None)).Items.Select(r => r.Key)
            .ShouldBe(new[] { "home.cta", "nav.search" });
        (await handler.HandleAsync(new GetTranslations("portal", Search: "welcome"), CancellationToken.None)).Items.ShouldHaveSingleItem().Key.ShouldBe("home.title");
        (await handler.HandleAsync(new GetTranslations("portal", Search: "NAV."), CancellationToken.None)).Items.ShouldHaveSingleItem().Key.ShouldBe("nav.search");
        var paged = await handler.HandleAsync(new GetTranslations("portal", Page: 2, PageSize: 2), CancellationToken.None);
        paged.TotalCount.ShouldBe(3);
        paged.Items.ShouldHaveSingleItem().Key.ShouldBe("nav.search");
    }

    [Fact]
    public async Task A_new_key_needs_a_default_language_text()
    {
        (await Should.ThrowAsync<DomainException>(() => SaveAsync("footer.copy", new() { ["ru"] = "©" }))).Code.ShouldBe("translation.default_required");

        var row = await SaveAsync("footer.copy", new() { ["hy"] = "© Home Services", ["RU"] = "© Хоум Сервисез" });

        row.Values.ShouldBe(new Dictionary<string, string> { ["hy"] = "© Home Services", ["ru"] = "© Хоум Сервисез" }, ignoreOrder: true);
        _version.Current.ShouldBe(1);
    }

    [Fact]
    public async Task Texts_are_changed_added_and_removed_but_the_default_stays()
    {
        var row = await SaveAsync("home.title", new() { ["ru"] = "Здравствуйте", ["en"] = "", ["fr"] = "Bienvenue" });

        row.Values.ShouldBe(new Dictionary<string, string> { ["hy"] = "Բարի գալուստ", ["ru"] = "Здравствуйте", ["fr"] = "Bienvenue" }, ignoreOrder: true);
        (await TextsAsync("en")).ShouldNotContainKey("home.title");
        (await Should.ThrowAsync<DomainException>(() => SaveAsync("home.title", new() { ["hy"] = "" }))).Code.ShouldBe("translation.default_required");
    }

    [Fact]
    public async Task A_new_key_cannot_clash_with_a_group_or_a_text()
    {
        (await Should.ThrowAsync<ConflictException>(() => SaveAsync("home", new() { ["hy"] = "x" }))).Code.ShouldBe("translation.key_conflict");
        (await Should.ThrowAsync<ConflictException>(() => SaveAsync("nav.search.label", new() { ["hy"] = "x" }))).Code.ShouldBe("translation.key_conflict");
    }

    [Fact]
    public async Task A_key_is_deleted_in_every_language()
    {
        var handler = new DeleteTranslationKeyHandler(_db, _version);

        (await handler.HandleAsync(new DeleteTranslationKey("portal", "home.title"), CancellationToken.None)).ShouldBeTrue();

        (await _db.UiTranslations.AnyAsync(t => t.Key == "home.title")).ShouldBeFalse();
        (await Should.ThrowAsync<NotFoundException>(() => handler.HandleAsync(new DeleteTranslationKey("portal", "home.title"), CancellationToken.None)))
            .Code.ShouldBe("translation.not_found");
    }

    [Fact]
    public async Task The_missing_report_lists_gaps_of_active_languages()
    {
        var report = await new GetMissingTranslationsHandler(_db, _catalog).HandleAsync(new GetMissingTranslations(), CancellationToken.None);

        report.Select(r => (r.Namespace, r.Language)).ShouldBe(new[] { ("backoffice", "ru"), ("backoffice", "en"), ("portal", "ru"), ("portal", "en") });
        var portalRu = report.Single(r => r.Namespace == "portal" && r.Language == "ru");
        portalRu.Total.ShouldBe(3);
        portalRu.Translated.ShouldBe(1);
        portalRu.MissingKeys.ShouldBe(new[] { "home.cta", "nav.search" });
    }

    [Fact]
    public async Task A_language_is_exported_as_an_i18next_file()
    {
        (await ExportAsync("en")).ShouldBe("""{"home":{"cta":"Start","title":"Welcome"}}""");
        (await ExportAsync("en", withFallback: true)).ShouldBe("""{"home":{"cta":"Start","title":"Welcome"},"nav":{"search":"Որոնում"}}""");
        (await ExportAsync("fr")).ShouldBe("{}");
        (await Should.ThrowAsync<NotFoundException>(() => ExportAsync("de"))).Code.ShouldBe("language.not_found");
    }

    [Fact]
    public async Task Importing_the_default_language_creates_and_updates_keys()
    {
        var result = await ImportAsync("hy", """{ "home": { "title": "Բարև", "cta": "Սկսել", "new": "Նոր" }, "home.title.big": "clash", "count": 3 }""");

        result.Added.ShouldBe(1);
        result.Updated.ShouldBe(1);
        result.Unchanged.ShouldBe(1);
        result.Removed.ShouldBe(0);
        result.Invalid.ShouldBe(new[] { "home.title.big", "count" }, ignoreOrder: true);
        var texts = await TextsAsync("hy");
        texts["home.title"].ShouldBe("Բարև");
        texts["home.new"].ShouldBe("Նոր");
        texts.ShouldContainKey("nav.search");
        _version.Current.ShouldBe(1);
    }

    [Fact]
    public async Task Replacing_the_default_language_removes_keys_in_every_language()
    {
        var result = await ImportAsync("hy", """{ "home": { "title": "Բարի գալուստ" } }""", replace: true);

        result.Removed.ShouldBe(2);
        (await TextsAsync("hy")).Keys.ShouldBe(new[] { "home.title" });
        (await TextsAsync("en")).Keys.ShouldBe(new[] { "home.title" });
    }

    [Fact]
    public async Task Other_languages_only_fill_existing_keys()
    {
        var result = await ImportAsync("ru", """{ "home": { "cta": "Начать", "unknown": "?" }, "nav": { "search": "Поиск" } }""", replace: true);

        result.Added.ShouldBe(2);
        result.Removed.ShouldBe(1);
        result.Skipped.ShouldBe(new[] { "home.unknown" });
        (await TextsAsync("ru")).ShouldBe(new Dictionary<string, string> { ["home.cta"] = "Начать", ["nav.search"] = "Поиск" }, ignoreOrder: true);
        (await TextsAsync("hy")).Count.ShouldBe(3);
    }

    [Fact]
    public async Task Importing_into_an_unknown_language_is_404()
    {
        (await Should.ThrowAsync<NotFoundException>(() => ImportAsync("de", "{}"))).Code.ShouldBe("language.not_found");
    }

    [Fact]
    public async Task Requests_are_validated()
    {
        (await new SaveTranslationValidator(_db).ValidateAsync(new SaveTranslation("portal", "home.title", new Dictionary<string, string?> { ["ru"] = "x" })))
            .IsValid.ShouldBeTrue();
        (await new SaveTranslationValidator(_db).ValidateAsync(new SaveTranslation("Bad NS", "bad key", new Dictionary<string, string?> { ["de"] = new string('v', UiTranslation.ValueMaxLength + 1) })))
            .Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "namespace.invalid", "key.invalid", "values.too_long", "values.language_invalid" }, ignoreOrder: true);
        (await new SaveTranslationValidator(_db).ValidateAsync(new SaveTranslation("portal", "a", new Dictionary<string, string?>())))
            .Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("values.required");

        new GetTranslationsValidator().Validate(new GetTranslations("Bad NS", new string('s', 101), "french", 0, 501)).Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "namespace.invalid", "search.too_long", "language.invalid", "page.invalid", "page_size.invalid" }, ignoreOrder: true);
        new ExportTranslationsValidator().Validate(new ExportTranslations("portal", "french")).Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("language.invalid");
        new ImportTranslationsValidator().Validate(new ImportTranslations("portal", "hy", Json("[1]"))).Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("content.invalid");
    }
}
