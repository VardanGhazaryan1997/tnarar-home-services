using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Catalog;
using HomeServices.Application.Staff;
using HomeServices.Domain.Staff;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AdminCatalogEndpointTests(ApiFactory factory)
{
    private const string Categories = "/api/v1/admin/catalog/categories";
    private const string Cities = "/api/v1/admin/catalog/cities";

    private HttpClient StaffClient(params string[] permissions)
    {
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", "Catalog Manager", "hash");
        var token = factory.Services.GetRequiredService<IStaffTokenService>().CreateAccessToken(staff, permissions).Token;
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private HttpClient CatalogManager() => StaffClient(Permissions.CatalogManage);

    private static string NewSlug(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..30];

    private static object CategoryBody(string slug, Guid? parentId = null) => new
    {
        slug,
        name = new Dictionary<string, string> { ["hy"] = "Ջեռուցում", ["en"] = "Heating" },
        icon = "fire",
        parentId,
        sortOrder = 5,
    };

    [Fact]
    public async Task Staff_without_catalog_manage_get_403()
    {
        var client = StaffClient(Permissions.PartnersView);

        (await client.GetAsync(Categories)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await client.PostAsJsonAsync(Cities, new { slug = "x", name = new { hy = "x" }, sortOrder = 1 })).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Categories_are_created_listed_updated_hidden_and_deleted()
    {
        var client = CatalogManager();
        var slug = NewSlug("heating");

        var createResponse = await client.PostAsJsonAsync(Categories, CategoryBody(slug));
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<AdminCategoryDto>())!;
        created.Name["en"].ShouldBe("Heating");

        var child = (await (await client.PostAsJsonAsync(Categories, CategoryBody(NewSlug("boilers"), created.Id)))
            .Content.ReadFromJsonAsync<AdminCategoryDto>())!;
        var tree = (await client.GetFromJsonAsync<List<AdminCategoryNodeDto>>(Categories))!;
        tree.Single(c => c.Id == created.Id).Children.Select(c => c.Id).ShouldBe(new[] { child.Id });

        var updated = (await (await client.PutAsJsonAsync($"{Categories}/{child.Id}", CategoryBody(NewSlug("boilers"), null)))
            .Content.ReadFromJsonAsync<AdminCategoryDto>())!;
        updated.ParentId.ShouldBeNull();

        var hidden = (await (await client.PostAsync($"{Categories}/{created.Id}/deactivate", null))
            .Content.ReadFromJsonAsync<AdminCategoryDto>())!;
        hidden.IsActive.ShouldBeFalse();
        var publicTree = (await factory.CreateClient().GetFromJsonAsync<List<CategoryDto>>("/api/v1/categories"))!;
        publicTree.ShouldNotContain(c => c.Id == created.Id);

        (await client.DeleteAsync($"{Categories}/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.GetFromJsonAsync<List<AdminCategoryNodeDto>>(Categories))!.ShouldNotContain(c => c.Id == created.Id);
    }

    [Fact]
    public async Task Invalid_categories_return_400_with_field_error_codes()
    {
        var response = await CatalogManager().PostAsJsonAsync(Categories, new
        {
            slug = "two words",
            name = new Dictionary<string, string> { ["en"] = "Heating" },
            sortOrder = -1,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        errors.GetProperty("slug")[0].GetString().ShouldBe("slug.invalid");
        errors.GetProperty("name")[0].GetString().ShouldBe("name.default_language_required");
        errors.GetProperty("sortOrder")[0].GetString().ShouldBe("sort_order.invalid");
    }

    [Fact]
    public async Task A_taken_slug_returns_409()
    {
        var response = await CatalogManager().PostAsJsonAsync(Categories, CategoryBody("plumbing"));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().ShouldBe("category.slug_taken");
    }

    [Fact]
    public async Task Unknown_categories_return_404()
    {
        var response = await CatalogManager().PostAsync($"{Categories}/{Guid.NewGuid()}/activate", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().ShouldBe("category.not_found");
    }

    [Fact]
    public async Task Cities_and_districts_are_managed()
    {
        var client = CatalogManager();
        var name = new Dictionary<string, string> { ["hy"] = "Գյումրի", ["en"] = "Gyumri" };

        var lori = (await client.GetFromJsonAsync<List<AdminRegionDto>>("/api/v1/admin/catalog/regions"))!.Single(r => r.Slug == "lori");
        var cityResponse = await client.PostAsJsonAsync(Cities, new { slug = NewSlug("town"), name, sortOrder = 10, regionId = lori.Id, kind = "Village" });
        cityResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var city = (await cityResponse.Content.ReadFromJsonAsync<AdminCityDto>())!;
        city.RegionId.ShouldBe(lori.Id);
        city.Kind.ShouldBe("Village");
        (await client.PostAsJsonAsync(Cities, new { slug = NewSlug("town"), name, sortOrder = 10, regionId = Guid.NewGuid() }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var districtResponse = await client.PostAsJsonAsync($"{Cities}/{city.Id}/districts", new { slug = "center", name, sortOrder = 1 });
        districtResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var district = (await districtResponse.Content.ReadFromJsonAsync<AdminDistrictDto>())!;

        (await client.PutAsJsonAsync($"{Cities}/{city.Id}/districts/{district.Id}", new { slug = "kentron", name, sortOrder = 2 }))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.PostAsync($"{Cities}/{city.Id}/districts/{district.Id}/deactivate", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.PutAsJsonAsync($"{Cities}/{city.Id}", new { slug = city.Slug, name, sortOrder = 11, regionId = lori.Id, kind = "City" })).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.PostAsync($"{Cities}/{city.Id}/deactivate", null)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var saved = (await client.GetFromJsonAsync<List<AdminCityDto>>(Cities))!.Single(c => c.Id == city.Id);
        saved.IsActive.ShouldBeFalse();
        saved.SortOrder.ShouldBe(11);
        saved.Kind.ShouldBe("City");
        saved.RegionId.ShouldBe(lori.Id);
        var savedDistrict = saved.Districts.ShouldHaveSingleItem();
        savedDistrict.Slug.ShouldBe("kentron");
        savedDistrict.IsActive.ShouldBeFalse();

        (await client.PostAsync($"{Cities}/{city.Id}/activate", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.PostAsync($"{Cities}/{city.Id}/districts/{district.Id}/activate", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        var publicCity = (await factory.CreateClient().GetFromJsonAsync<List<CityDto>>("/api/v1/cities"))!.Single(c => c.Id == city.Id);
        publicCity.Districts.ShouldHaveSingleItem().Slug.ShouldBe("kentron");
    }

    [Fact]
    public async Task Catalog_changes_are_in_the_audit_log()
    {
        var client = StaffClient(Permissions.CatalogManage, Permissions.AuditView);
        var created = (await (await client.PostAsJsonAsync(Categories, CategoryBody(NewSlug("audited"))))
            .Content.ReadFromJsonAsync<AdminCategoryDto>())!;

        var log = await client.GetFromJsonAsync<JsonElement>($"/api/v1/admin/audit-log?entityType=Category&entityId={created.Id}");

        var entry = log.GetProperty("items").EnumerateArray().ShouldHaveSingleItem();
        entry.GetProperty("action").GetString().ShouldBe("Created");
        entry.GetProperty("actorType").GetString().ShouldBe("Staff");
    }
}
