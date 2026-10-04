using HomeServices.Domain.Localization;

namespace HomeServices.Domain.Tests.Localization;

public class LocalizedTextTests
{
    private static readonly LocalizedText Plumbing = LocalizedText.From(new Dictionary<string, string>
    {
        ["hy"] = "Սանտեխնիկա",
        ["ru"] = "Сантехника",
    });

    [Fact]
    public void Returns_the_text_in_the_requested_language()
    {
        Plumbing.Get("ru", fallbackLanguage: "hy").ShouldBe("Сантехника");
    }

    [Fact]
    public void Falls_back_to_the_fallback_language_when_a_translation_is_missing()
    {
        Plumbing.Get("en", fallbackLanguage: "hy").ShouldBe("Սանտեխնիկա");
    }

    [Fact]
    public void Falls_back_to_any_available_text_when_both_are_missing()
    {
        var onlyRussian = LocalizedText.From(new Dictionary<string, string> { ["ru"] = "Уборка" });

        onlyRussian.Get("en", fallbackLanguage: "hy").ShouldBe("Уборка");
    }

    [Fact]
    public void Empty_text_returns_an_empty_string()
    {
        LocalizedText.Empty.Get("hy", "hy").ShouldBe(string.Empty);
    }

    [Fact]
    public void Language_codes_are_case_insensitive_and_blank_values_are_dropped()
    {
        var text = LocalizedText.From(new Dictionary<string, string> { ["RU"] = " Ремонт ", ["en"] = "  " });

        text.Get("ru", "hy").ShouldBe("Ремонт");
        text.Values.Keys.ToArray().ShouldBe(new[] { "ru" });
    }

    [Fact]
    public void With_returns_a_new_value_and_leaves_the_original_unchanged()
    {
        var withEnglish = Plumbing.With("en", "Plumbing");

        withEnglish.Get("en", "hy").ShouldBe("Plumbing");
        Plumbing.Values.ContainsKey("en").ShouldBeFalse();
    }

    [Fact]
    public void With_a_blank_text_removes_that_translation()
    {
        Plumbing.With("ru", "").Values.ContainsKey("ru").ShouldBeFalse();
    }

    [Fact]
    public void Texts_with_the_same_translations_are_equal()
    {
        var same = LocalizedText.From(new Dictionary<string, string> { ["ru"] = "Сантехника", ["hy"] = "Սանտեխնիկա" });

        same.Equals(Plumbing).ShouldBeTrue();
        same.Equals((object)Plumbing).ShouldBeTrue();
        same.GetHashCode().ShouldBe(Plumbing.GetHashCode());
    }

    [Fact]
    public void Texts_with_different_translations_are_not_equal()
    {
        Plumbing.Equals(Plumbing.With("ru", "Другое")).ShouldBeFalse();
        Plumbing.Equals(Plumbing.With("en", "Plumbing")).ShouldBeFalse();
        Plumbing.Equals(null).ShouldBeFalse();
        Plumbing.Equals("Сантехника").ShouldBeFalse();
    }
}
