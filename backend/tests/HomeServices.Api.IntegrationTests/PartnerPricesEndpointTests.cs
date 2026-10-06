using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Identity;
using HomeServices.Application.Partners;
using HomeServices.Domain.Identity;
using HomeServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class PartnerPricesEndpointTests(ApiFactory factory)
{
    private const string Url = "/api/v1/me/partner-profile/prices";

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
        return client;
    }

    private async Task<HttpClient> PlumberAsync()
    {
        var client = await PortalUserAsync();
        Guid plumbing, yerevan;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            plumbing = (await db.Categories.FirstAsync(c => c.Slug == "plumbing")).Id;
            yerevan = (await db.Cities.FirstAsync(c => c.Slug == "yerevan")).Id;
        }

        var saved = await client.PutAsJsonAsync("/api/v1/me/partner-profile", new
        {
            type = "Specialist",
            displayName = "Aram Plumbing",
            about = new string('a', 80),
            yearsOfExperience = 12,
            categoryIds = new[] { plumbing },
            areas = new object[] { new { cityId = yerevan, districtId = (Guid?)null } },
        });
        saved.StatusCode.ShouldBe(HttpStatusCode.OK);
        return client;
    }

    [Fact]
    public async Task Signing_in_is_required_and_a_partner_profile_too()
    {
        (await factory.CreateClient().GetAsync(Url)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await (await PortalUserAsync()).GetAsync(Url)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_plumber_prices_plumbing_work_and_staff_see_the_count()
    {
        var client = await PlumberAsync();
        client.DefaultRequestHeaders.Add("Accept-Language", "en");

        var list = (await client.GetFromJsonAsync<MyPriceListDto>(Url, Json))!;
        list.Items.ShouldNotBeEmpty();
        list.Items.ShouldAllBe(i => i.MainCategoryName == "Plumbing");
        var toilet = list.Items.Single(i => i.Slug == "toilet-installation");
        toilet.MarketTypical.ShouldBe(15000);
        toilet.CategoryName.ShouldNotBeNullOrEmpty();

        var response = await client.PutAsJsonAsync(Url, new { prices = new[] { new { workItemId = toilet.WorkItemId, priceFrom = 14000, priceTo = 18000, includesMaterials = false } } });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var saved = (await response.Content.ReadFromJsonAsync<MyPriceListDto>(Json))!;
        saved.PricedCount.ShouldBe(1);
        (await client.GetFromJsonAsync<MyPriceListDto>(Url, Json))!.Items.Single(i => i.Slug == "toilet-installation").PriceTo.ShouldBe(18000);
    }

    [Fact]
    public async Task Invalid_price_lists_are_rejected()
    {
        var client = await PlumberAsync();
        var list = (await client.GetFromJsonAsync<MyPriceListDto>(Url, Json))!;
        var item = list.Items[0].WorkItemId;
        Guid wallPlastering;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            wallPlastering = (await scope.ServiceProvider.GetRequiredService<AppDbContext>().WorkItems.FirstAsync(w => w.Slug == "wall-plastering")).Id;
        }

        var invalid = await client.PutAsJsonAsync(Url, new { prices = new[] { new { workItemId = item, priceFrom = 500, priceTo = (int?)100, includesMaterials = false } } });
        invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await invalid.Content.ReadAsStringAsync()).ShouldContain("partner_price.invalid");

        var notOffered = await client.PutAsJsonAsync(Url, new { prices = new[] { new { workItemId = wallPlastering, priceFrom = 500, priceTo = (int?)null, includesMaterials = false } } });
        notOffered.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await notOffered.Content.ReadAsStringAsync()).ShouldContain("partner_price.not_offered");
    }
}
