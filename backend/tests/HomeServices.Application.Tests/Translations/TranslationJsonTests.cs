using System.Text.Json;
using HomeServices.Application.Translations;

namespace HomeServices.Application.Tests.Translations;

public class TranslationJsonTests
{
    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    [Fact]
    public void Nested_and_dotted_files_flatten_to_dotted_keys()
    {
        var result = TranslationJson.Flatten(Json("""{ "home": { "title": "Բարի գալուստ", "cta": { "start": "Սկսել" } }, "nav.menu": "Մենյու" }"""));

        result.Texts.ShouldBe(new Dictionary<string, string>
        {
            ["home.title"] = "Բարի գալուստ",
            ["home.cta.start"] = "Սկսել",
            ["nav.menu"] = "Մենյու",
        });
        result.Invalid.ShouldBeEmpty();
    }

    [Fact]
    public void Values_that_are_not_texts_are_reported()
    {
        var result = TranslationJson.Flatten(Json("""{ "count": 3, "list": ["a"], "empty": "", "flag": true, "bad key": "x", "ok": "fine", "ok.sub": "clash" }"""));

        result.Texts.Keys.ShouldBe(new[] { "ok" });
        result.Invalid.ShouldBe(new[] { "count", "list", "empty", "flag", "bad key", "ok.sub" }, ignoreOrder: true);
    }

    [Fact]
    public void A_file_must_be_an_object()
    {
        TranslationJson.Flatten(Json("""["a"]""")).Invalid.ShouldBe(new[] { "$" });
    }

    [Fact]
    public void Texts_nest_back_into_readable_JSON()
    {
        var json = TranslationJson.Nest(new Dictionary<string, string>
        {
            ["home.title"] = "Բարի գալուստ",
            ["home.cta.start"] = "Սկսել",
            ["about"] = "Մեր մասին",
        });

        json.ShouldBe("""{"about":"Մեր մասին","home":{"cta":{"start":"Սկսել"},"title":"Բարի գալուստ"}}""");
    }

    [Fact]
    public void A_key_under_a_text_is_left_out_when_nesting()
    {
        TranslationJson.Nest(new Dictionary<string, string> { ["home.title"] = "Title", ["home"] = "Home" }).ShouldBe("""{"home":"Home"}""");
    }

    [Theory]
    [InlineData("home", true)]
    [InlineData("home.title.big", true)]
    [InlineData("homepage", false)]
    [InlineData("nav.home", false)]
    [InlineData("home.title", false)]
    public void Keys_conflict_when_one_would_contain_the_other(string key, bool conflicts)
    {
        TranslationJson.Conflicts(key, ["home.title", "nav.home"]).ShouldBe(conflicts);
    }
}
