using System.Net;
using System.Net.Http.Json;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Catalog;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class CatalogEndpointTests(ApiFactory factory)
{
    [Fact]
    public async Task Categories_are_listed_in_the_request_language()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "ru");

        var response = await client.GetAsync("/api/v1/categories");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var categories = (await response.Content.ReadFromJsonAsync<List<CategoryDto>>())!;
        categories.Count.ShouldBe(7);
        categories.Single(c => c.Slug == "plumbing").Name.ShouldBe("Сантехника");
        categories.Single(c => c.Slug == "plumbing").Icon.ShouldBe("pipe");
    }

    [Fact]
    public async Task Cities_are_listed_with_their_districts()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "en");

        var cities = (await client.GetFromJsonAsync<List<CityDto>>("/api/v1/cities"))!;

        cities.Select(c => c.Name).ShouldBe(new[] { "Yerevan", "Ejmiatsin", "Abovyan", "Ashtarak", "Masis" });
        cities[0].Districts.Count.ShouldBe(12);
        cities[0].Districts.ShouldContain(d => d.Name == "Kentron");
    }
}
