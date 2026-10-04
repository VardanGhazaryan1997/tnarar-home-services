using System.Net;
using System.Net.Http.Json;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Languages;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class LanguagesEndpointTests(ApiFactory factory)
{
    [Fact]
    public async Task Lists_the_active_languages_for_the_language_switcher()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/languages");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var languages = (await response.Content.ReadFromJsonAsync<List<LanguageDto>>())!;
        languages.Select(l => l.Code).ShouldBe(new[] { "hy", "ru", "en" });
        languages[0].ShouldBe(new LanguageDto("hy", "Armenian", "Հայերեն", IsDefault: true));
    }

    [Theory]
    [InlineData("ru-RU,ru;q=0.9,en;q=0.8", "ru")]
    [InlineData("en", "en")]
    [InlineData("fr", "hy")]
    [InlineData(null, "hy")]
    public async Task Responds_in_the_language_negotiated_from_Accept_Language(string? acceptLanguage, string expected)
    {
        var client = factory.CreateClient();
        if (acceptLanguage is not null)
        {
            client.DefaultRequestHeaders.Add("Accept-Language", acceptLanguage);
        }

        var response = await client.GetAsync("/api/v1/languages");

        response.Content.Headers.ContentLanguage.ShouldBe(new[] { expected });
    }
}
