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
        categories.Count.ShouldBe(18);
        categories.Sum(c => c.Children.Count).ShouldBe(133);
        var plumbing = categories.Single(c => c.Slug == "plumbing");
        plumbing.Name.ShouldBe("Сантехника");
        plumbing.Icon.ShouldBe("pipe");
        plumbing.Children.Select(c => c.Slug).ShouldContain("plumbing-leaks");
        plumbing.Children.Single(c => c.Slug == "plumbing-leaks").Name.ShouldBe("Устранение протечек");
    }

    [Fact]
    public async Task Cities_are_listed_with_their_districts()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "en");

        var cities = (await client.GetFromJsonAsync<List<CityDto>>("/api/v1/cities"))!;

        var regions = (await client.GetFromJsonAsync<List<RegionDto>>("/api/v1/regions"))!;

        regions.Count.ShouldBe(11);
        regions[0].Name.ShouldBe("Yerevan");
        var yerevan = cities.Single(c => c.Slug == "yerevan");
        yerevan.Name.ShouldBe("Yerevan");
        yerevan.RegionId.ShouldBe(regions[0].Id);
        cities.Select(c => c.Name).ShouldContain("Ejmiatsin");
        cities.Select(c => c.Name).ShouldContain("Gyumri");
        cities.ShouldContain(c => c.Kind == "Village");
        cities.Single(c => c.Slug == "gyumri").RegionId.ShouldBe(regions.Single(r => r.Slug == "shirak").Id);
        yerevan.Districts.Count.ShouldBe(12);
        yerevan.Districts.ShouldContain(d => d.Name == "Kentron");
    }
}
