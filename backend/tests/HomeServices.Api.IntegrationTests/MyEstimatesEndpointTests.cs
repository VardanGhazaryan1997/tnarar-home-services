using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Estimates;
using HomeServices.Application.Identity;
using HomeServices.Domain.Estimates;
using HomeServices.Domain.Identity;
using HomeServices.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class MyEstimatesEndpointTests(ApiFactory factory)
{
    private const string Url = "/api/v1/me/estimates";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private async Task<HttpClient> PortalUserAsync()
    {
        var user = User.Register(PhoneNumber.Parse($"+37455{Random.Shared.Next(100_000, 999_999)}"));
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.Services.GetRequiredService<ITokenService>().CreateAccessToken(user).Token);
        client.DefaultRequestHeaders.Add("Accept-Language", "en");
        return client;
    }

    private async Task<object> BedroomAsync(HttpClient client, string title = "Our flat")
    {
        var templates = (await client.GetFromJsonAsync<List<RoomTemplateDto>>("/api/v1/estimates/templates", Json))!;
        var lines = templates.Single(t => t.Type == RoomType.Bedroom).Items
            .Select(i => new { workItemId = i.WorkItemId, quantity = i.Quantity ?? (i.QuantityPerSquareMeter is null ? (decimal?)null : 3m) });
        return new
        {
            title,
            cityId = (Guid?)null,
            oldBuilding = false,
            rooms = new[]
            {
                new { name = "Bedroom", type = "Bedroom", length = 4.2m, width = 3.5m, area = (decimal?)null, height = 2.7m, openings = new[] { new { kind = "Door", width = 0.9m, height = 2m, count = 1 } }, lines },
            },
        };
    }

    [Fact]
    public async Task A_user_saves_lists_changes_and_deletes_an_estimate()
    {
        var client = await PortalUserAsync();

        var created = await client.PostAsJsonAsync(Url, await BedroomAsync(client), Json);
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var estimate = (await created.Content.ReadFromJsonAsync<EstimateDto>(Json))!;
        estimate.Rooms.Single().Lines.Count.ShouldBeGreaterThan(3);
        estimate.Measurement.TotalTypical.ShouldBeGreaterThan(0);
        estimate.Measurement.Rooms.Single().FloorArea.ShouldBe(14.7m);

        var list = (await client.GetFromJsonAsync<List<EstimateSummaryDto>>(Url, Json))!;
        list.Single().Id.ShouldBe(estimate.Id);
        list.Single().TotalTypical.ShouldBe(estimate.Measurement.TotalTypical);

        var updated = await client.PutAsJsonAsync($"{Url}/{estimate.Id}", new { title = "Renamed", oldBuilding = true, rooms = Array.Empty<object>() }, Json);
        updated.StatusCode.ShouldBe(HttpStatusCode.OK);
        var renamed = (await client.GetFromJsonAsync<EstimateDto>($"{Url}/{estimate.Id}", Json))!;
        (renamed.Title, renamed.OldBuilding, renamed.Rooms.Count).ShouldBe(("Renamed", true, 0));
        renamed.UpdatedAt.ShouldNotBeNull();

        (await client.DeleteAsync($"{Url}/{estimate.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.GetAsync($"{Url}/{estimate.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Estimates_are_private_and_need_signing_in()
    {
        var owner = await PortalUserAsync();
        var estimate = (await (await owner.PostAsJsonAsync(Url, await BedroomAsync(owner), Json)).Content.ReadFromJsonAsync<EstimateDto>(Json))!;
        var stranger = await PortalUserAsync();

        (await stranger.GetAsync($"{Url}/{estimate.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await stranger.DeleteAsync($"{Url}/{estimate.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await factory.CreateClient().GetAsync(Url)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_shared_link_shows_the_estimate_to_anyone_until_sharing_stops()
    {
        var owner = await PortalUserAsync();
        var estimate = (await (await owner.PostAsJsonAsync(Url, await BedroomAsync(owner, "Shared flat"), Json)).Content.ReadFromJsonAsync<EstimateDto>(Json))!;

        var shared = (await (await owner.PostAsync($"{Url}/{estimate.Id}/share", null)).Content.ReadFromJsonAsync<EstimateDto>(Json))!;
        shared.ShareToken.ShouldNotBeNullOrEmpty();

        var visitor = factory.CreateClient();
        var seen = (await visitor.GetFromJsonAsync<SharedEstimateDto>($"/api/v1/estimates/shared/{shared.ShareToken}", Json))!;
        seen.Title.ShouldBe("Shared flat");
        seen.Measurement.TotalTypical.ShouldBe(estimate.Measurement.TotalTypical);

        (await owner.DeleteAsync($"{Url}/{estimate.Id}/share")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await visitor.GetAsync($"/api/v1/estimates/shared/{shared.ShareToken}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Bad_estimates_are_refused()
    {
        var client = await PortalUserAsync();

        var noTitle = await client.PostAsJsonAsync(Url, new { title = "", rooms = Array.Empty<object>() }, Json);
        noTitle.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var badSize = await client.PostAsJsonAsync(
            Url,
            new { title = "x", rooms = new[] { new { name = "Hall", type = "Hallway", length = 500m, width = 2m } } },
            Json);
        badSize.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }
}
