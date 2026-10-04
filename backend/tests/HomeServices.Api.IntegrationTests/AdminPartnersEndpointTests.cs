using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Common;
using HomeServices.Application.Partners;
using HomeServices.Application.Staff;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Staff;
using HomeServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AdminPartnersEndpointTests(ApiFactory factory)
{
    private const string Url = "/api/v1/admin/partners";

    private HttpClient StaffClient(params string[] permissions)
    {
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", "Ani Reviewer", "hash");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.Services.GetRequiredService<IStaffTokenService>().CreateAccessToken(staff, permissions).Token);
        return client;
    }

    /// <summary>A partner with a complete profile, submitted for review.</summary>
    private async Task<PartnerProfile> GivenSubmittedProfileAsync(string displayName)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = User.Register(PhoneNumber.Parse($"+37494{Random.Shared.Next(100_000, 999_999)}"));
        var photo = StoredFile.Begin(FileOwnerType.User, user.Id.ToString(), "work.jpg", "image/jpeg", 100, DateTimeOffset.UtcNow);
        photo.MarkReady(100, DateTimeOffset.UtcNow);
        var category = await db.Categories.FirstAsync(c => c.Slug == "plumbing");
        var yerevan = await db.Cities.FirstAsync(c => c.Slug == "yerevan");

        var profile = PartnerProfile.Create(user.Id, PartnerType.Specialist, displayName);
        profile.UpdateDetails(PartnerType.Specialist, displayName, new string('a', 60), null, null);
        profile.SetServices([category.Id]);
        profile.SetAreas([(yerevan.Id, null)]);
        profile.AddMedia(PartnerMediaKind.WorkExample, photo.Id, null);
        profile.Submit(DateTimeOffset.UtcNow);
        db.AddRange(user, photo, profile);
        await db.SaveChangesAsync();
        return profile;
    }

    private static async Task<string?> CodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();

    [Fact]
    public async Task Reviewers_see_submitted_profiles_in_the_queue()
    {
        var name = $"Queue {Guid.NewGuid():N}";
        var profile = await GivenSubmittedProfileAsync(name);

        var response = await StaffClient(Permissions.PartnersView).GetAsync($"{Url}?status=UnderReview&search={name}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = (await response.Content.ReadFromJsonAsync<PagedResult<AdminPartnerListItemDto>>())!;
        page.Items.ShouldHaveSingleItem().Id.ShouldBe(profile.Id);
    }

    [Fact]
    public async Task A_reviewer_approves_a_profile_and_the_history_names_them()
    {
        var profile = await GivenSubmittedProfileAsync("Aram Plumbing");
        var reviewer = StaffClient(Permissions.PartnersView, Permissions.PartnersApprove);

        var detail = await reviewer.GetFromJsonAsync<AdminPartnerDto>($"{Url}/{profile.Id}");
        detail!.Profile.Status.ShouldBe("UnderReview");
        detail.Profile.WorkExamples.ShouldHaveSingleItem();

        var response = await reviewer.PostAsync($"{Url}/{profile.Id}/approve", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var approved = (await response.Content.ReadFromJsonAsync<AdminPartnerDto>())!;
        approved.Profile.Status.ShouldBe("Approved");
        approved.History.Select(h => h.ToStatus).ShouldBe(new[] { "Approved", "UnderReview" });
        approved.History[0].ActorId.ShouldNotBeNull();
    }

    [Fact]
    public async Task Sending_a_profile_back_needs_a_comment()
    {
        var profile = await GivenSubmittedProfileAsync("Aram Plumbing");
        var reviewer = StaffClient(Permissions.PartnersApprove);

        var missing = await reviewer.PostAsJsonAsync($"{Url}/{profile.Id}/request-changes", new { comment = " " });
        missing.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await missing.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").GetProperty("comment")[0].GetString().ShouldBe("comment.required");

        var sent = await reviewer.PostAsJsonAsync($"{Url}/{profile.Id}/request-changes", new { comment = "Add your license." });
        sent.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await sent.Content.ReadFromJsonAsync<AdminPartnerDto>())!.Profile.ReviewComment.ShouldBe("Add your license.");
    }

    [Fact]
    public async Task Rejected_suspended_and_reinstated_profiles_follow_the_rules()
    {
        var rejected = await GivenSubmittedProfileAsync("To reject");
        var approved = await GivenSubmittedProfileAsync("To suspend");
        var reviewer = StaffClient(Permissions.PartnersApprove);

        (await reviewer.PostAsJsonAsync($"{Url}/{rejected.Id}/reject", new { comment = "Not a business." })).StatusCode.ShouldBe(HttpStatusCode.OK);
        var tooLate = await reviewer.PostAsync($"{Url}/{rejected.Id}/approve", null);
        tooLate.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await CodeAsync(tooLate)).ShouldBe("partner.not_under_review");

        (await reviewer.PostAsync($"{Url}/{approved.Id}/approve", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await reviewer.PostAsJsonAsync($"{Url}/{approved.Id}/suspend", new { comment = "Complaints." })).StatusCode.ShouldBe(HttpStatusCode.OK);
        var reinstated = await reviewer.PostAsync($"{Url}/{approved.Id}/reinstate", null);
        (await reinstated.Content.ReadFromJsonAsync<AdminPartnerDto>())!.Profile.Status.ShouldBe("Approved");
    }

    [Fact]
    public async Task Viewing_and_deciding_need_their_own_permissions()
    {
        var profile = await GivenSubmittedProfileAsync("Aram Plumbing");
        var viewer = StaffClient(Permissions.PartnersView);
        var outsider = StaffClient(Permissions.CatalogManage);

        (await outsider.GetAsync(Url)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await outsider.GetAsync($"{Url}/{profile.Id}")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await viewer.PostAsync($"{Url}/{profile.Id}/approve", null)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await factory.CreateClient().GetAsync(Url)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unknown_profiles_are_404()
    {
        var response = await StaffClient(Permissions.PartnersView).GetAsync($"{Url}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await CodeAsync(response)).ShouldBe("partner.not_found");
    }
}
