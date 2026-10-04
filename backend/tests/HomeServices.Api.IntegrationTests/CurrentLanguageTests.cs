using HomeServices.Api.IntegrationTests.Infrastructure;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class CurrentLanguageTests(ApiFactory factory)
{
    [Theory]
    [InlineData("ru", "ru")]
    [InlineData("de-DE", "hy")]
    public async Task Use_cases_see_the_negotiated_language(string acceptLanguage, string expected)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", acceptLanguage);

        (await client.GetStringAsync("/api/v1/test/language")).ShouldBe(expected);
    }
}
