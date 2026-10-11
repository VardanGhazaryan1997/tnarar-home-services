using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Catalog;
using HomeServices.Application.Staff;
using HomeServices.Application.Stores;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class PublicStoresEndpointTests(ApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private HttpClient StaffClient()
    {
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", "Price Manager", "hash");
        var token = factory.Services.GetRequiredService<IStaffTokenService>().CreateAccessToken(staff, [Permissions.MaterialsPricing]).Token;
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static HttpClient Customer(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("Accept-Language", "en");
        return client;
    }

    private async Task<(Guid StoreId, Guid ProductId)> GivenStoreWithPuttyAsync(HttpClient staff, string slug, int price, bool active, bool inStock, Guid cityId)
    {
        var created = await staff.PostAsJsonAsync("/api/v1/admin/stores", new { slug, name = $"Store {slug}", sortOrder = 1, isActive = active });
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var store = (await created.Content.ReadFromJsonAsync<AdminStoreDto>(Json))!;
        (await staff.PutAsJsonAsync($"/api/v1/admin/stores/{store.Id}/branches", new { branches = new[] { new { name = "Main", cityId, address = "Main street 1" } } }))
            .StatusCode.ShouldBe(HttpStatusCode.OK);

        var materials = (await staff.GetFromJsonAsync<List<AdminMaterialDto>>("/api/v1/admin/stores/materials", Json))!;
        var putty = materials.Single(m => m.Slug == "finishing-putty");
        var product = await staff.PostAsJsonAsync($"/api/v1/admin/stores/{store.Id}/products", new { name = $"Putty {slug}", packSize = 25, packLabel = "25 kg bag", price, inStock, materialId = putty.Id, grade = "Standard" });
        product.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (store.Id, (await product.Content.ReadFromJsonAsync<AdminStoreProductDto>(Json))!.Id);
    }

    private async Task<Guid> FirstCityAsync()
    {
        using var scope = factory.Services.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<IAppDbContext>().Cities.AsNoTracking().OrderBy(c => c.SortOrder).FirstAsync()).Id;
    }

    [Fact]
    public async Task A_materials_offers_are_sorted_by_price_per_unit_and_leave_out_hidden_stores()
    {
        var staff = StaffClient();
        var tag = Guid.NewGuid().ToString("N")[..8];
        var cityId = await FirstCityAsync();
        await GivenStoreWithPuttyAsync(staff, $"dear-{tag}", price: 7500, active: true, inStock: true, cityId);
        await GivenStoreWithPuttyAsync(staff, $"cheap-{tag}", price: 5000, active: true, inStock: true, cityId);
        await GivenStoreWithPuttyAsync(staff, $"gone-{tag}", price: 1000, active: true, inStock: false, cityId);
        await GivenStoreWithPuttyAsync(staff, $"hidden-{tag}", price: 2500, active: false, inStock: true, cityId);
        var customer = Customer(factory.CreateClient());

        var material = (await customer.GetFromJsonAsync<PublicMaterialDetailDto>("/api/v1/materials/finishing-putty", Json))!;

        material.Name.ShouldNotBeNullOrEmpty();
        material.Unit.ShouldBe(MaterialUnit.Kilogram);
        var ours = material.Offers.Where(o => o.Store.Slug.EndsWith(tag, StringComparison.Ordinal)).ToList();
        ours.Select(o => o.Store.Slug).ToArray().ShouldBe(new[] { $"cheap-{tag}", $"dear-{tag}", $"gone-{tag}" });
        ours[0].Product.UnitPrice.ShouldBe(200m);
        ours[0].Product.PackLabel.ShouldBe("25 kg bag");
        ours[0].Store.Branches.Single().Address.ShouldBe("Main street 1");
        ours[2].Product.InStock.ShouldBeFalse();

        var inStock = (await customer.GetFromJsonAsync<PublicMaterialDetailDto>("/api/v1/materials/finishing-putty?inStock=true&grade=Standard", Json))!;
        inStock.Offers.Any(o => o.Store.Slug == $"gone-{tag}").ShouldBeFalse();
        inStock.Offers.Any(o => o.Store.Slug == $"cheap-{tag}").ShouldBeTrue();

        var premium = (await customer.GetFromJsonAsync<PublicMaterialDetailDto>("/api/v1/materials/finishing-putty?grade=Premium", Json))!;
        premium.Offers.Any(o => o.Store.Slug.EndsWith(tag, StringComparison.Ordinal)).ShouldBeFalse();

        var list = (await customer.GetFromJsonAsync<List<PublicMaterialDto>>("/api/v1/materials?search=putty", Json))!;
        var putty = list.Single(m => m.Slug == "finishing-putty");
        putty.StoreCount.ShouldBeGreaterThanOrEqualTo(3);
        putty.LowestUnitPrice.ShouldNotBeNull().ShouldBeLessThanOrEqualTo(200m);
    }

    [Fact]
    public async Task Bad_requests_and_unknown_things_get_clear_answers()
    {
        var customer = Customer(factory.CreateClient());

        (await customer.GetAsync("/api/v1/materials/finishing-putty?grade=Gold")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var missing = await customer.GetAsync("/api/v1/materials/no-such-material");
        missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await missing.Content.ReadAsStringAsync()).ShouldContain("material.not_found");
        var store = await customer.GetAsync("/api/v1/stores/no-such-store");
        store.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await store.Content.ReadAsStringAsync()).ShouldContain("store.not_found");
    }

    [Fact]
    public async Task Stores_are_listed_and_shown_with_their_products_without_signing_in()
    {
        var staff = StaffClient();
        var tag = Guid.NewGuid().ToString("N")[..8];
        await GivenStoreWithPuttyAsync(staff, $"shown-{tag}", price: 5000, active: true, inStock: true, await FirstCityAsync());
        await GivenStoreWithPuttyAsync(staff, $"off-{tag}", price: 5000, active: false, inStock: true, await FirstCityAsync());
        var customer = Customer(factory.CreateClient());

        var stores = (await customer.GetFromJsonAsync<List<PublicStoreDto>>("/api/v1/stores", Json))!;
        stores.Any(s => s.Slug == $"shown-{tag}").ShouldBeTrue();
        stores.Any(s => s.Slug == $"off-{tag}").ShouldBeFalse();

        var store = (await customer.GetFromJsonAsync<PublicStoreDto>($"/api/v1/stores/shown-{tag}", Json))!;
        store.Branches.Single().Name.ShouldBe("Main");
        var group = store.Materials.ShouldNotBeNull().Single();
        group.Slug.ShouldBe("finishing-putty");
        group.Products.Single().UnitPrice.ShouldBe(200m);

        (await customer.GetAsync($"/api/v1/stores/off-{tag}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
