using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Catalog;
using HomeServices.Application.Common;
using HomeServices.Application.Staff;
using HomeServices.Application.Stores;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AdminStoresEndpointTests(ApiFactory factory)
{
    private const string Admin = "/api/v1/admin/stores";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private HttpClient StaffClient(params string[] permissions)
    {
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", "Price Manager", "hash");
        var token = factory.Services.GetRequiredService<IStaffTokenService>().CreateAccessToken(staff, permissions).Token;
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static object StoreBody(string slug) => new { slug, name = "Demo Market", description = new { en = "Building materials" }, website = "https://demo.example", sortOrder = 1 };

    private static object ProductBody(Guid materialId, string code) => new
    {
        name = "Finishing putty 25 kg",
        brand = "Knauf",
        code,
        packSize = 25,
        packLabel = "25 kg bag",
        price = 5000,
        inStock = true,
        materialId,
        grade = "Standard",
    };

    [Fact]
    public async Task Only_staff_who_manage_prices_reach_the_store_endpoints()
    {
        (await factory.CreateClient().GetAsync(Admin)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await StaffClient(Permissions.CatalogManage).GetAsync(Admin)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await StaffClient(Permissions.CatalogManage).PostAsJsonAsync(Admin, StoreBody("nope"))).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await StaffClient(Permissions.MaterialsPricing).GetAsync(Admin)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var materials = (await StaffClient(Permissions.MaterialsPricing).GetFromJsonAsync<List<AdminMaterialDto>>($"{Admin}/materials", Json))!;
        materials.ShouldContain(m => m.Slug == "finishing-putty");
        (await StaffClient(Permissions.CatalogManage).GetAsync($"{Admin}/materials")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Staff_keep_a_store_its_branches_and_its_products()
    {
        var client = StaffClient(Permissions.MaterialsPricing, Permissions.CatalogManage);
        var slug = $"demo-{Guid.NewGuid():N}";

        var created = await client.PostAsJsonAsync(Admin, StoreBody(slug));
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var store = (await created.Content.ReadFromJsonAsync<AdminStoreDto>(Json))!;
        (store.Slug, store.Name, store.ProductCount).ShouldBe((slug, "Demo Market", 0));
        (await client.PostAsJsonAsync(Admin, StoreBody(slug))).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await client.PostAsJsonAsync(Admin, StoreBody("Not a slug"))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        Guid cityId;
        using (var scope = factory.Services.CreateScope())
        {
            cityId = (await scope.ServiceProvider.GetRequiredService<IAppDbContext>().Cities.AsNoTracking().OrderBy(c => c.SortOrder).FirstAsync()).Id;
        }

        var branches = await client.PutAsJsonAsync($"{Admin}/{store.Id}/branches", new { branches = new[] { new { name = "Komitas", cityId, address = "Komitas 1", openingHours = "Mon–Sat 9–19" } } });
        branches.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await branches.Content.ReadFromJsonAsync<AdminStoreDto>(Json))!.Branches.ShouldNotBeNull().Single().Name.ShouldBe("Komitas");

        var materials = (await client.GetFromJsonAsync<List<AdminMaterialDto>>("/api/v1/admin/catalog/materials", Json))!;
        var putty = materials.Single(m => m.Slug == "finishing-putty");
        var product = await client.PostAsJsonAsync($"{Admin}/{store.Id}/products", ProductBody(putty.Id, "P-25"));
        product.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created2 = (await product.Content.ReadFromJsonAsync<AdminStoreProductDto>(Json))!;
        (created2.Grade, created2.Unit, created2.UnitPrice).ShouldBe((MaterialGrade.Standard, MaterialUnit.Kilogram, 200m));
        (await client.PostAsJsonAsync($"{Admin}/{store.Id}/products", ProductBody(putty.Id, "P-25"))).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await client.PostAsJsonAsync($"{Admin}/{store.Id}/products", new { name = "x", packSize = 1, price = 100, inStock = true, salePrice = 50 })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var page = (await client.GetFromJsonAsync<PagedResult<AdminStoreProductDto>>($"{Admin}/{store.Id}/products?linked=true&search=putty", Json))!;
        page.TotalCount.ShouldBe(1);
        (await client.PostAsync($"{Admin}/{store.Id}/products/{created2.Id}/confirm", null)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var offers = (await client.GetFromJsonAsync<List<AdminMaterialOfferDto>>($"/api/v1/admin/catalog/materials/{putty.Id}/store-products", Json))!;
        offers.ShouldContain(o => o.StoreId == store.Id && o.Product.Id == created2.Id);

        var listed = (await client.GetFromJsonAsync<List<AdminStoreDto>>($"{Admin}?search={slug}", Json))!.Single();
        (listed.BranchCount, listed.ProductCount, listed.LinkedCount).ShouldBe((1, 1, 1));

        var refused = await client.DeleteAsync($"{Admin}/{store.Id}");
        refused.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await refused.Content.ReadAsStringAsync()).ShouldContain("store.has_products");
        (await client.DeleteAsync($"{Admin}/{store.Id}/products/{created2.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.DeleteAsync($"{Admin}/{store.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.GetAsync($"{Admin}/{store.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
