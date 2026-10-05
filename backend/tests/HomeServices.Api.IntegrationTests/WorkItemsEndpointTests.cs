using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Catalog;
using HomeServices.Application.Staff;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Staff;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class WorkItemsEndpointTests(ApiFactory factory)
{
    private const string Admin = "/api/v1/admin/catalog/work-items";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private HttpClient StaffClient(params string[] permissions)
    {
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", "Catalog Manager", "hash");
        var token = factory.Services.GetRequiredService<IStaffTokenService>().CreateAccessToken(staff, permissions).Token;
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string NewSlug(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..30];

    private static object Body(Guid categoryId, string slug, string unit = "SquareMeter", string surface = "Wall") => new
    {
        categoryId,
        slug,
        name = new Dictionary<string, string> { ["hy"] = "Պատերի սվաղում", ["en"] = "Wall plastering", ["ar"] = "لياسة الجدران" },
        unit,
        surface,
        sortOrder = 1,
    };

    private async Task<CategoryDto> SeededMain(string slug) =>
        (await factory.CreateClient().GetFromJsonAsync<List<CategoryDto>>("/api/v1/categories"))!.Single(c => c.Slug == slug);

    [Fact]
    public async Task Staff_without_catalog_manage_get_403()
    {
        var client = StaffClient(Permissions.PartnersView);

        (await client.GetAsync(Admin)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await client.PostAsJsonAsync(Admin, Body(Guid.NewGuid(), "x"))).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Work_items_are_created_listed_publicly_updated_hidden_and_deleted()
    {
        var client = StaffClient(Permissions.CatalogManage);
        var renovation = await SeededMain("renovation");
        var plastering = renovation.Children.Single(c => c.Slug == "renovation-plastering");
        var slug = NewSlug("wall-plaster");

        var createResponse = await client.PostAsJsonAsync(Admin, Body(plastering.Id, slug));
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<AdminWorkItemDto>(Json))!;
        created.Unit.ShouldBe(WorkUnit.SquareMeter);
        created.Name["ar"].ShouldBe("لياسة الجدران");

        try
        {
            (await client.GetFromJsonAsync<List<AdminWorkItemDto>>($"{Admin}?categoryId={renovation.Id}&search={slug}", Json))!
                .Select(w => w.Id).ShouldBe(new[] { created.Id });

            var english = factory.CreateClient();
            english.DefaultRequestHeaders.Add("Accept-Language", "en");
            var publicItem = (await english.GetFromJsonAsync<List<WorkItemDto>>("/api/v1/work-items?category=renovation", Json))!
                .Single(w => w.Id == created.Id);
            publicItem.Name.ShouldBe("Wall plastering");
            publicItem.Surface.ShouldBe(WorkSurface.Wall);
            var raw = await english.GetStringAsync("/api/v1/work-items?category=renovation-plastering");
            raw.ShouldContain("\"unit\":\"SquareMeter\"");

            var updated = (await (await client.PutAsJsonAsync($"{Admin}/{created.Id}", Body(plastering.Id, slug, "RunningMeter", "Floor")))
                .Content.ReadFromJsonAsync<AdminWorkItemDto>(Json))!;
            updated.Unit.ShouldBe(WorkUnit.RunningMeter);
            updated.Surface.ShouldBe(WorkSurface.Floor);

            (await client.PostAsync($"{Admin}/{created.Id}/deactivate", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
            (await english.GetFromJsonAsync<List<WorkItemDto>>("/api/v1/work-items?category=renovation", Json))!
                .ShouldNotContain(w => w.Id == created.Id);
            (await (await client.PostAsync($"{Admin}/{created.Id}/activate", null)).Content.ReadFromJsonAsync<AdminWorkItemDto>(Json))!
                .IsActive.ShouldBeTrue();
        }
        finally
        {
            (await client.DeleteAsync($"{Admin}/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        (await client.GetFromJsonAsync<List<AdminWorkItemDto>>($"{Admin}?search={slug}", Json))!.ShouldBeEmpty();
    }

    [Fact]
    public async Task Invalid_work_items_are_rejected()
    {
        var client = StaffClient(Permissions.CatalogManage);
        var renovation = await SeededMain("renovation");
        var plastering = renovation.Children.Single(c => c.Slug == "renovation-plastering");

        (await client.PostAsJsonAsync(Admin, Body(plastering.Id, "bad slug"))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync(Admin, Body(renovation.Id, NewSlug("main")))).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        var slug = NewSlug("dup");
        var first = (await (await client.PostAsJsonAsync(Admin, Body(plastering.Id, slug))).Content.ReadFromJsonAsync<AdminWorkItemDto>(Json))!;
        try
        {
            (await client.PostAsJsonAsync(Admin, Body(plastering.Id, slug))).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        }
        finally
        {
            await client.DeleteAsync($"{Admin}/{first.Id}");
        }

        (await client.PutAsJsonAsync($"{Admin}/{Guid.NewGuid()}", Body(plastering.Id, NewSlug("x")))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task The_starter_catalog_has_priced_work_items_in_every_language()
    {
        var all = (await StaffClient(Permissions.CatalogManage).GetFromJsonAsync<List<AdminWorkItemDto>>(Admin, Json))!;
        all.Count(w => w.PriceTypical is not null).ShouldBeGreaterThanOrEqualTo(287);

        var plastering = all.Single(w => w.Slug == "wall-plastering");
        plastering.Unit.ShouldBe(WorkUnit.SquareMeter);
        plastering.Surface.ShouldBe(WorkSurface.Wall);
        (plastering.PriceMin, plastering.PriceTypical, plastering.PriceMax).ShouldBe((2500, 3500, 5000));
        plastering.Name.Keys.ShouldBe(new[] { "hy", "ru", "en", "ar", "fa", "hi" }, ignoreOrder: true);

        var arabic = factory.CreateClient();
        arabic.DefaultRequestHeaders.Add("Accept-Language", "ar");
        var items = (await arabic.GetFromJsonAsync<List<WorkItemDto>>("/api/v1/work-items?category=renovation-plastering", Json))!;
        items[0].Slug.ShouldBe("wall-plastering");
        items[0].Name.ShouldBe(plastering.Name["ar"]);
        items[0].PriceTypical.ShouldBe(3500);
    }

    [Fact]
    public async Task Prices_are_validated_and_saved()
    {
        var client = StaffClient(Permissions.CatalogManage);
        var plastering = (await SeededMain("renovation")).Children.Single(c => c.Slug == "renovation-plastering");
        var slug = NewSlug("priced");

        var incomplete = await client.PostAsJsonAsync(Admin, new { categoryId = plastering.Id, slug, name = new { hy = "Գին" }, unit = "Piece", surface = "None", sortOrder = 1, priceMin = 100 });
        incomplete.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await incomplete.Content.ReadAsStringAsync()).ShouldContain("price.incomplete");

        var created = (await (await client.PostAsJsonAsync(
                Admin,
                new { categoryId = plastering.Id, slug, name = new { hy = "Գին" }, unit = "Piece", surface = "None", sortOrder = 1, priceMin = 100, priceTypical = 200, priceMax = 300 }))
            .Content.ReadFromJsonAsync<AdminWorkItemDto>(Json))!;
        try
        {
            (created.PriceMin, created.PriceTypical, created.PriceMax).ShouldBe((100, 200, 300));
        }
        finally
        {
            await client.DeleteAsync($"{Admin}/{created.Id}");
        }
    }
}
