using HomeServices.Application.Languages;

namespace HomeServices.Application.Tests.Languages;

public class LanguageFallbackTests
{
    [Theory]
    [InlineData("ar", "en")]
    [InlineData("fa", "en")]
    [InlineData("hi", "en")]
    [InlineData("ru", "hy")]
    [InlineData("en", "hy")]
    [InlineData("hy", "hy")]
    public void Arabic_Persian_and_Hindi_fall_back_to_English_others_to_the_default(string language, string expected)
    {
        var catalog = new LanguageCatalog(["hy", "ru", "en", "ar", "fa", "hi"], "hy");

        catalog.FallbackFor(language).ShouldBe(expected);
    }

    [Fact]
    public void Without_English_everything_falls_back_to_the_default()
    {
        var catalog = new LanguageCatalog(["hy", "ar"], "hy");

        catalog.FallbackFor("ar").ShouldBe("hy");
    }
}
