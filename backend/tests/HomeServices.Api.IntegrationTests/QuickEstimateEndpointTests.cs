using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Estimates;
using HomeServices.Application.Staff;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Estimates;
using HomeServices.Domain.Staff;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class QuickEstimateEndpointTests(ApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    [Fact]
    public async Task Every_room_type_has_seeded_usual_work()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "en");

        var templates = (await client.GetFromJsonAsync<List<RoomTemplateDto>>("/api/v1/estimates/templates", Json))!;

        templates.Select(t => t.Type).ShouldBe(Enum.GetValues<RoomType>());
        templates.ShouldAllBe(t => t.Items.Count > 0);
        var bathroom = templates.Single(t => t.Type == RoomType.Bathroom);
        bathroom.Items.Select(i => i.Slug).ShouldContain("toilet-installation");
        bathroom.Items.Single(i => i.Slug == "toilet-installation").Quantity.ShouldBe(1m);
        templates.Single(t => t.Type == RoomType.Bedroom).Items.Single(i => i.Slug == "socket-installation").QuantityPerSquareMeter.ShouldBe(0.25m);
    }

    [Fact]
    public async Task A_quick_estimate_gives_a_price_range_for_a_room()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/estimates/quick", new { roomType = "Bedroom", area = 14m });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = (await response.Content.ReadFromJsonAsync<EstimateMeasurementDto>(Json))!;
        result.TotalMin.ShouldBeGreaterThan(0);
        result.TotalMin.ShouldBeLessThanOrEqualTo(result.TotalTypical);
        result.TotalTypical.ShouldBeLessThanOrEqualTo(result.TotalMax);
        result.UnpricedLines.ShouldBe(0);
        result.Rooms.Single().Lines.ShouldContain(l => l.Unit == WorkUnit.Point && l.Quantity == 4m); // 14 m² × 0.25 → 4 sockets

        var older = (await (await client.PostAsJsonAsync("/api/v1/estimates/quick", new { roomType = "Bedroom", area = 14m, oldBuilding = true }))
            .Content.ReadFromJsonAsync<EstimateMeasurementDto>(Json))!;
        older.TotalTypical.ShouldBeGreaterThan(result.TotalTypical);

        (await client.PostAsJsonAsync("/api/v1/estimates/quick", new { roomType = "Bedroom", area = 0m })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Staff_with_catalog_manage_edit_templates()
    {
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", "Catalog Manager", "hash");
        var token = factory.Services.GetRequiredService<IStaffTokenService>().CreateAccessToken(staff, [Permissions.CatalogManage]).Token;
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var all = (await client.GetFromJsonAsync<List<AdminRoomTemplateDto>>("/api/v1/admin/catalog/room-templates", Json))!;
        var garage = all.Single(t => t.Type == RoomType.Garage);
        try
        {
            var response = await client.PutAsJsonAsync("/api/v1/admin/catalog/room-templates/Garage", new { items = garage.Items.Take(1) });
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await response.Content.ReadFromJsonAsync<AdminRoomTemplateDto>(Json))!.Items.Count.ShouldBe(1);
        }
        finally
        {
            await client.PutAsJsonAsync("/api/v1/admin/catalog/room-templates/Garage", new { items = garage.Items });
        }

        (await factory.CreateClient().GetAsync("/api/v1/admin/catalog/room-templates")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
