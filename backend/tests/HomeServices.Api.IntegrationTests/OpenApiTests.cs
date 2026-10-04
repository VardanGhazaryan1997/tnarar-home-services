using System.Net;
using System.Text.Json;
using HomeServices.Api.IntegrationTests.Infrastructure;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class OpenApiTests(ApiFactory factory)
{
    [Fact]
    public async Task OpenApi_document_is_published_in_development()
    {
        var response = await factory.CreateClient().GetAsync("/openapi/v1.json");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        document.GetProperty("openapi").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Swagger_UI_is_served_in_development()
    {
        var response = await factory.CreateClient().GetAsync("/swagger/index.html");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain("swagger-ui");
    }
}
