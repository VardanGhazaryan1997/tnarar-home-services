using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Common;
using HomeServices.Application.Partners;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Partners;
using HomeServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class PublicPartnersEndpointTests(ApiFactory factory)
{
    private const string Url = "/api/v1/partners";

    private async Task<PartnerProfile> GivenPartnerAsync(string displayName, bool approve = true)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = User.Register(PhoneNumber.Parse($"+37493{Random.Shared.Next(100_000, 999_999)}"));
        var photo = StoredFile.Begin(FileOwnerType.User, user.Id.ToString(), "work.jpg", "image/jpeg", 100, DateTimeOffset.UtcNow);
        photo.MarkReady(100, DateTimeOffset.UtcNow);
        var plumbing = await db.Categories.FirstAsync(c => c.Slug == "plumbing");
        var yerevan = await db.Cities.Include(c => c.Districts).FirstAsync(c => c.Slug == "yerevan");

        var profile = PartnerProfile.Create(user.Id, PartnerType.Specialist, displayName);
        profile.UpdateDetails(PartnerType.Specialist, displayName, new string('a', 60), 8, null);
        profile.SetServices([plumbing.Id]);
        profile.SetAreas([(yerevan.Id, yerevan.Districts.Single(d => d.Slug == "kentron").Id)]);
        profile.AddMedia(PartnerMediaKind.WorkExample, photo.Id, "Bathroom");
        profile.Submit(DateTimeOffset.UtcNow);
        if (approve)
        {
            profile.Approve(DateTimeOffset.UtcNow);
        }

        db.AddRange(user, photo, profile);
        await db.SaveChangesAsync();
        return profile;
    }

    [Fact]
    public async Task Visitors_find_approved_partners_by_category_and_place()
    {
        var name = $"Public {Guid.NewGuid():N}";
        var partner = await GivenPartnerAsync(name);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru");

        var response = await client.GetAsync($"{Url}?category=plumbing&city=yerevan&district=kentron&search={name}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = (await response.Content.ReadFromJsonAsync<PagedResult<PublicPartnerCardDto>>())!;
        var card = page.Items.ShouldHaveSingleItem();
        card.Slug.ShouldBe(partner.Slug);
        card.Categories.ShouldHaveSingleItem().Name.ShouldBe("Сантехника");
        card.Cover!.Caption.ShouldBe("Bathroom");
        var elsewhere = await client.GetFromJsonAsync<PagedResult<PublicPartnerCardDto>>($"{Url}?category=plumbing&city=yerevan&district=arabkir&search={name}");
        elsewhere!.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Visitors_open_a_public_profile_by_slug()
    {
        var partner = await GivenPartnerAsync("Aram Plumbing");

        var response = await factory.CreateClient().GetAsync($"{Url}/{partner.Slug}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var profile = (await response.Content.ReadFromJsonAsync<PublicPartnerDto>())!;
        profile.DisplayName.ShouldBe("Aram Plumbing");
        profile.Id.ShouldBe(partner.Id);
        profile.Areas.ShouldHaveSingleItem().DistrictSlug.ShouldBe("kentron");
        profile.WorkExamples.ShouldHaveSingleItem().Url.ShouldNotBeNullOrEmpty();
        (await response.Content.ReadAsStringAsync()).ShouldNotContain("phone", Case.Insensitive);
    }

    [Fact]
    public async Task Profiles_waiting_for_review_are_not_public()
    {
        var partner = await GivenPartnerAsync("Not yet", approve: false);

        var response = await factory.CreateClient().GetAsync($"{Url}/{partner.Slug}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().ShouldBe("partner.not_found");
    }

    [Fact]
    public async Task A_district_without_a_city_is_a_400()
    {
        var response = await factory.CreateClient().GetAsync($"{Url}?district=kentron");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").GetProperty("district")[0].GetString()
            .ShouldBe("district.needs_city");
    }
}
