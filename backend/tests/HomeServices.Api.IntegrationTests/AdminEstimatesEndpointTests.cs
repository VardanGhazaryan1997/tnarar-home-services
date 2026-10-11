using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Common;
using HomeServices.Application.Estimates;
using HomeServices.Application.Identity;
using HomeServices.Application.Staff;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Staff;
using HomeServices.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AdminEstimatesEndpointTests(ApiFactory factory)
{
    private const string Url = "/api/v1/admin/estimates";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private HttpClient StaffClient(params string[] permissions)
    {
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", "Operator", "hash");
        var token = factory.Services.GetRequiredService<IStaffTokenService>().CreateAccessToken(staff, permissions).Token;
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<EstimateDto> SavedEstimateAsync(string title)
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
        var response = await client.PostAsJsonAsync(
            "/api/v1/me/estimates",
            new { title, oldBuilding = false, rooms = new[] { new { name = "Hall", type = "Hallway", area = 6m } } },
            Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EstimateDto>(Json))!;
    }

    [Fact]
    public async Task Staff_with_requests_view_see_estimates_and_stats()
    {
        var title = $"Flat {Guid.NewGuid():N}"[..20];
        var estimate = await SavedEstimateAsync(title);
        var client = StaffClient(Permissions.RequestsView);

        var list = (await client.GetFromJsonAsync<PagedResult<AdminEstimateListItemDto>>($"{Url}?search={title}", Json))!;
        list.Items.Single().Id.ShouldBe(estimate.Id);
        list.Items.Single().Owner.ShouldNotBeNull();

        var detail = (await client.GetFromJsonAsync<AdminEstimateDto>($"{Url}/{estimate.Id}", Json))!;
        detail.Rooms.Single().Name.ShouldBe("Hall");
        detail.Requests.ShouldBeEmpty();

        var stats = (await client.GetFromJsonAsync<EstimateStatsDto>($"{Url}/stats", Json))!;
        stats.Total.ShouldBeGreaterThanOrEqualTo(1);

        (await client.GetAsync($"{Url}/{Guid.NewGuid()}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Other_staff_and_portal_users_are_refused()
    {
        (await StaffClient(Permissions.CatalogManage).GetAsync(Url)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await factory.CreateClient().GetAsync(Url)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
