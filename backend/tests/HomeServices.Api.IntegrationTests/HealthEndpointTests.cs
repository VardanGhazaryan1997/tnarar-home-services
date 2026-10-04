using System.Net;
using HomeServices.Api.IntegrationTests.Infrastructure;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class HealthEndpointTests(ApiFactory factory)
{
    [Fact]
    public async Task Health_returns_200_Healthy()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }

    [Fact]
    public async Task Unknown_route_returns_404()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/no-such-route");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
