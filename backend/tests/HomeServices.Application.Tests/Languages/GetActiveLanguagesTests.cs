using HomeServices.Application.Languages;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Localization;

namespace HomeServices.Application.Tests.Languages;

public class GetActiveLanguagesTests
{
    [Fact]
    public async Task Returns_active_languages_in_display_order_and_marks_the_default()
    {
        await using var db = InMemoryAppDbContext.Create();
        db.Languages.AddRange(
            Active("en", "English", "English", 3),
            Active("hy", "Armenian", "Հայերեն", 1, isDefault: true),
            Active("ru", "Russian", "Русский", 2),
            Language.Create("fr", "French", "Français", 4));
        await db.SaveChangesAsync();

        var result = await new GetActiveLanguagesHandler(db).HandleAsync(new GetActiveLanguages(), CancellationToken.None);

        result.Select(l => l.Code).ShouldBe(new[] { "hy", "ru", "en" });
        result[0].ShouldBe(new LanguageDto("hy", "Armenian", "Հայերեն", IsDefault: true));
        result[1].IsDefault.ShouldBeFalse();
    }

    [Fact]
    public async Task Languages_with_the_same_sort_order_are_ordered_by_code()
    {
        await using var db = InMemoryAppDbContext.Create();
        db.Languages.AddRange(Active("ru", "Russian", "Русский", 1), Active("en", "English", "English", 1));
        await db.SaveChangesAsync();

        var result = await new GetActiveLanguagesHandler(db).HandleAsync(new GetActiveLanguages(), CancellationToken.None);

        result.Select(l => l.Code).ShouldBe(new[] { "en", "ru" });
    }

    private static Language Active(string code, string name, string nativeName, int sortOrder, bool isDefault = false)
    {
        var language = Language.Create(code, name, nativeName, sortOrder);
        language.Activate();
        if (isDefault)
        {
            language.MakeDefault();
        }

        return language;
    }
}
