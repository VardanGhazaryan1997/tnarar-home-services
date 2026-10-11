using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Catalog;
using HomeServices.Application.Estimates;
using HomeServices.Application.Staff;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Staff;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class MaterialsEndpointTests(ApiFactory factory)
{
    private const string Admin = "/api/v1/admin/catalog";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private HttpClient StaffClient(params string[] permissions)
    {
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", "Catalog Manager", "hash");
        var token = factory.Services.GetRequiredService<IStaffTokenService>().CreateAccessToken(staff, permissions).Token;
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Staff_see_the_seeded_materials_and_what_work_uses()
    {
        var client = StaffClient(Permissions.CatalogManage);
        (await StaffClient(Permissions.PartnersView).GetAsync($"{Admin}/materials")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var materials = (await client.GetFromJsonAsync<List<AdminMaterialDto>>($"{Admin}/materials", Json))!;
        var putty = materials.Single(m => m.Slug == "finishing-putty");
        putty.Unit.ShouldBe(MaterialUnit.Kilogram);
        putty.WorkItemCount.ShouldBeGreaterThan(0);

        var workItems = (await client.GetFromJsonAsync<List<AdminWorkItemDto>>($"{Admin}/work-items?search=wall-putty", Json))!;
        var used = (await client.GetFromJsonAsync<List<WorkItemMaterialInput>>($"{Admin}/work-items/{workItems.Single(w => w.Slug == "wall-putty").Id}/materials", Json))!;
        used.ShouldContain(m => m.MaterialId == putty.Id && m.QuantityPerUnit == 1.2m && m.WastePercent == 10);

        var inUse = await client.DeleteAsync($"{Admin}/materials/{putty.Id}");
        inUse.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await inUse.Content.ReadAsStringAsync()).ShouldContain("material.in_use");
    }

    [Fact]
    public async Task A_quick_estimate_can_count_materials()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/v1/estimates/quick",
            new { roomType = "Bedroom", area = 12, materials = "Standard" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = (await response.Content.ReadFromJsonAsync<EstimateMeasurementDto>(Json))!;
        result.MaterialGrade.ShouldBe(MaterialGrade.Standard);
        result.Materials.ShouldNotBeNull().ShouldNotBeEmpty();
        result.MaterialsTotal.ShouldBe(result.Materials!.Sum(m => m.Cost));
    }
}
