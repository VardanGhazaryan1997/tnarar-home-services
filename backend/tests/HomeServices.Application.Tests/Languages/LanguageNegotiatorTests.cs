using HomeServices.Application.Languages;

namespace HomeServices.Application.Tests.Languages;

public class LanguageNegotiatorTests
{
    private static readonly string[] Active = ["hy", "ru", "en"];

    [Theory]
    [InlineData("ru", "ru")]
    [InlineData("ru-RU", "ru")]
    [InlineData("EN-us", "en")]
    [InlineData("fr-FR,fr;q=0.9,en;q=0.8", "en")]
    [InlineData("en;q=0.5,ru;q=0.9", "ru")]
    [InlineData("ru;q=0,en", "en")]
    [InlineData("*", "hy")]
    [InlineData("fr", "hy")]
    [InlineData("", "hy")]
    [InlineData(null, "hy")]
    [InlineData("ru;q=abc", "ru")]
    [InlineData(" , ru ", "ru")]
    public void Picks_the_best_active_language_from_the_Accept_Language_header(string? header, string expected)
    {
        LanguageNegotiator.Pick(header, Active, defaultLanguage: "hy").ShouldBe(expected);
    }

    [Fact]
    public void A_newly_activated_language_is_picked_without_code_changes()
    {
        LanguageNegotiator.Pick("fr-CA", ["hy", "fr"], "hy").ShouldBe("fr");
    }
}
