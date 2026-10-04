using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Identity;
using HomeServices.Application.Partners;
using HomeServices.Application.Staff;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Staff;
using HomeServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class PartnerProfileEndpointTests(ApiFactory factory)
{
    private const string Url = "/api/v1/me/partner-profile";

    private async Task<(HttpClient Client, User User)> PortalUserAsync()
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
        return (client, user);
    }

    private async Task<(Guid CategoryId, Guid CityId, Guid DistrictId)> SeededPlacesAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = await db.Categories.FirstAsync(c => c.Slug == "plumbing");
        var yerevan = await db.Cities.Include(c => c.Districts).FirstAsync(c => c.Slug == "yerevan");
        return (category.Id, yerevan.Id, yerevan.Districts.First(d => d.Slug == "kentron").Id);
    }

    private async Task<Guid> GivenReadyFileAsync(User owner, string contentType)
    {
        var file = StoredFile.Begin(FileOwnerType.User, owner.Id.ToString(), "upload", contentType, 100, DateTimeOffset.UtcNow);
        file.MarkReady(100, DateTimeOffset.UtcNow);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Files.Add(file);
        await db.SaveChangesAsync();
        return file.Id;
    }

    private async Task<object> ValidBodyAsync()
    {
        var (categoryId, cityId, districtId) = await SeededPlacesAsync();
        return new
        {
            type = "Specialist",
            displayName = "Aram Plumbing",
            about = new string('a', 80),
            yearsOfExperience = 12,
            categoryIds = new[] { categoryId },
            areas = new object[] { new { cityId, districtId }, new { cityId, districtId = (Guid?)null } },
        };
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) => await response.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task A_user_creates_a_profile_adds_work_and_submits_it()
    {
        var (client, user) = await PortalUserAsync();
        (await client.GetAsync(Url)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var saved = await client.PutAsJsonAsync(Url, await ValidBodyAsync());
        saved.StatusCode.ShouldBe(HttpStatusCode.OK);
        var draft = (await saved.Content.ReadFromJsonAsync<PartnerProfileDto>())!;
        draft.Status.ShouldBe("Draft");
        draft.Areas.ShouldHaveSingleItem().DistrictId.ShouldBeNull(); // the whole city covers Kentron
        draft.MissingForSubmit.ShouldBe(new[] { "work_examples" });

        var photoId = await GivenReadyFileAsync(user, "image/jpeg");
        var withPhoto = await client.PostAsJsonAsync($"{Url}/media", new { kind = "WorkExample", fileId = photoId, caption = "Bathroom" });
        withPhoto.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await withPhoto.Content.ReadFromJsonAsync<PartnerProfileDto>())!.CanSubmit.ShouldBeTrue();

        var submitted = await client.PostAsync($"{Url}/submit", null);
        submitted.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await submitted.Content.ReadFromJsonAsync<PartnerProfileDto>())!.Status.ShouldBe("UnderReview");

        var locked = await client.PutAsJsonAsync(Url, await ValidBodyAsync());
        locked.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await JsonAsync(locked)).GetProperty("code").GetString().ShouldBe("partner.not_editable");
    }

    [Fact]
    public async Task Media_can_be_removed()
    {
        var (client, user) = await PortalUserAsync();
        await client.PutAsJsonAsync(Url, await ValidBodyAsync());
        var licenseId = await GivenReadyFileAsync(user, "application/pdf");
        var added = (await (await client.PostAsJsonAsync($"{Url}/media", new { kind = "Document", fileId = licenseId })).Content
            .ReadFromJsonAsync<PartnerProfileDto>())!;

        var response = await client.DeleteAsync($"{Url}/media/{added.Documents.Single().Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<PartnerProfileDto>())!.Documents.ShouldBeEmpty();
    }

    [Fact]
    public async Task Incomplete_profiles_cannot_be_submitted()
    {
        var (client, _) = await PortalUserAsync();
        await client.PutAsJsonAsync(Url, await ValidBodyAsync());

        var response = await client.PostAsync($"{Url}/submit", null);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await JsonAsync(response)).GetProperty("code").GetString().ShouldBe("partner.incomplete");
    }

    [Fact]
    public async Task Invalid_profiles_return_400_with_field_codes()
    {
        var (client, _) = await PortalUserAsync();
        var someoneElsesFile = await GivenReadyFileAsync((await PortalUserAsync()).User, "image/jpeg");

        var response = await client.PutAsJsonAsync(Url, new
        {
            type = "Specialist",
            displayName = "A",
            categoryIds = new[] { Guid.NewGuid() },
            areas = new[] { new { cityId = Guid.NewGuid() } },
            avatarFileId = someoneElsesFile,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var errors = (await JsonAsync(response)).GetProperty("errors");
        errors.GetProperty("displayName")[0].GetString().ShouldBe("display_name.length");
        errors.GetProperty("categoryIds")[0].GetString().ShouldBe("categories.invalid");
        errors.GetProperty("areas")[0].GetString().ShouldBe("areas.invalid");
        errors.GetProperty("avatarFileId")[0].GetString().ShouldBe("avatar.invalid");
    }

    [Fact]
    public async Task An_unknown_partner_type_is_reported_on_its_field()
    {
        var (client, _) = await PortalUserAsync();

        var response = await client.PutAsJsonAsync(Url, new { type = "Plumber", displayName = "Aram", categoryIds = Array.Empty<Guid>(), areas = Array.Empty<object>() });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await JsonAsync(response)).GetProperty("errors").GetProperty("type")[0].GetString().ShouldBe("value.invalid");
    }

    [Fact]
    public async Task Signed_out_users_and_staff_tokens_get_401()
    {
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", "Operator", "hash");
        var staffClient = factory.CreateClient();
        staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.Services.GetRequiredService<IStaffTokenService>().CreateAccessToken(staff, []).Token);

        (await factory.CreateClient().GetAsync(Url)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await staffClient.GetAsync(Url)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
