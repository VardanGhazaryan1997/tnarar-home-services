using HomeServices.Application.Errors;
using HomeServices.Application.Tests.Support;
using HomeServices.Application.Translations;
using HomeServices.Domain;
using HomeServices.Domain.Localization;

namespace HomeServices.Application.Tests.Translations;

public class AdminLanguagesTests
{
    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly FakeLanguageCatalog _catalog = new();
    private readonly UiTextsVersion _version = new();

    public AdminLanguagesTests()
    {
        var hy = Language.Create("hy", "Armenian", "Հայերեն", 1);
        hy.Activate();
        hy.MakeDefault();
        var ru = Language.Create("ru", "Russian", "Русский", 2);
        ru.Activate();
        _db.Languages.AddRange(hy, ru);
        _db.SaveChanges();
    }

    private Task<AdminLanguageDto> SetActiveAsync(string code, bool active) =>
        new SetLanguageActiveHandler(_db, _catalog, _version).HandleAsync(new SetLanguageActive(code, active), CancellationToken.None);

    [Fact]
    public async Task A_new_language_starts_hidden_and_is_activated_later()
    {
        var created = await new CreateLanguageHandler(_db).HandleAsync(new CreateLanguage(" FR ", "French", "Français", 3), CancellationToken.None);

        created.ShouldBe(new AdminLanguageDto("fr", "French", "Français", false, false, 3));

        var activated = await SetActiveAsync("fr", true);
        activated.IsActive.ShouldBeTrue();
        _catalog.Invalidations.ShouldBe(1);
        _version.Current.ShouldBe(1);

        var all = await new GetAdminLanguagesHandler(_db).HandleAsync(new GetAdminLanguages(), CancellationToken.None);
        all.Select(l => l.Code).ShouldBe(new[] { "hy", "ru", "fr" });
    }

    [Fact]
    public async Task A_language_code_is_used_once()
    {
        (await Should.ThrowAsync<ConflictException>(() =>
            new CreateLanguageHandler(_db).HandleAsync(new CreateLanguage("RU", "Russian", "Русский", 5), CancellationToken.None)))
            .Code.ShouldBe("language.code_taken");
    }

    [Fact]
    public async Task Names_and_order_change_but_the_default_stays_active()
    {
        var updated = await new UpdateLanguageHandler(_db, _catalog, _version).HandleAsync(new UpdateLanguage("ru", "Russian", "Русский язык", 9), CancellationToken.None);

        updated.NativeName.ShouldBe("Русский язык");
        updated.SortOrder.ShouldBe(9);
        (await SetActiveAsync("ru", false)).IsActive.ShouldBeFalse();
        (await Should.ThrowAsync<DomainException>(() => SetActiveAsync("hy", false))).Code.ShouldBe("language.default_cannot_be_deactivated");
        (await Should.ThrowAsync<NotFoundException>(() => SetActiveAsync("de", true))).Code.ShouldBe("language.not_found");
    }

    [Fact]
    public void Language_fields_are_validated()
    {
        new CreateLanguageValidator().Validate(new CreateLanguage("fr", "French", "Français", 3)).IsValid.ShouldBeTrue();
        new CreateLanguageValidator().Validate(new CreateLanguage("french", " ", new string('n', Language.NameMaxLength + 1), -1))
            .Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "code.invalid", "name.required", "native_name.too_long", "sort_order.invalid" }, ignoreOrder: true);
        new UpdateLanguageValidator().Validate(new UpdateLanguage("fr", new string('n', Language.NameMaxLength + 1), "", 1))
            .Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "name.too_long", "native_name.required" }, ignoreOrder: true);
    }
}
