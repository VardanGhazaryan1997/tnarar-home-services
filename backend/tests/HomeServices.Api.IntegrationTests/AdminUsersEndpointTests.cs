using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Common;
using HomeServices.Application.Staff;
using HomeServices.Application.Users;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Staff;
using HomeServices.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AdminUsersEndpointTests(ApiFactory factory)
{
    private const string Url = "/api/v1/admin/users";

    private HttpClient StaffClient(params string[] permissions)
    {
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", "Support", "hash");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.Services.GetRequiredService<IStaffTokenService>().CreateAccessToken(staff, permissions).Token);
        return client;
    }

    private async Task<User> GivenUserAsync()
    {
        var user = User.Register(PhoneNumber.Parse($"+37496{Random.Shared.Next(100_000, 999_999)}"));
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Support_finds_a_user_by_phone()
    {
        var user = await GivenUserAsync();

        var page = await StaffClient(Permissions.UsersView).GetFromJsonAsync<PagedResult<AdminUserListItemDto>>($"{Url}?search={Uri.EscapeDataString(user.Phone.Value)}");

        page!.Items.ShouldHaveSingleItem().Id.ShouldBe(user.Id);
    }

    [Fact]
    public async Task Support_blocks_and_unblocks_a_user()
    {
        var user = await GivenUserAsync();
        var support = StaffClient(Permissions.UsersView, Permissions.UsersBlock);

        var blocked = await support.PostAsJsonAsync($"{Url}/{user.Id}/block", new { reason = "Spam requests" });

        blocked.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = (await blocked.Content.ReadFromJsonAsync<AdminUserDto>())!;
        dto.Status.ShouldBe("Blocked");
        dto.BlockReason.ShouldBe("Spam requests");
        (await support.GetFromJsonAsync<AdminUserDto>($"{Url}/{user.Id}"))!.Status.ShouldBe("Blocked");

        var unblocked = await support.PostAsync($"{Url}/{user.Id}/unblock", null);
        (await unblocked.Content.ReadFromJsonAsync<AdminUserDto>())!.Status.ShouldBe("Active");
    }

    [Fact]
    public async Task Blocking_needs_a_reason_and_the_permission()
    {
        var user = await GivenUserAsync();

        var noReason = await StaffClient(Permissions.UsersBlock).PostAsJsonAsync($"{Url}/{user.Id}/block", new { reason = " " });
        noReason.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await noReason.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").GetProperty("reason")[0].GetString().ShouldBe("reason.required");

        (await StaffClient(Permissions.UsersView).PostAsJsonAsync($"{Url}/{user.Id}/block", new { reason = "Spam" })).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await StaffClient(Permissions.PartnersView).GetAsync(Url)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Unknown_users_are_404()
    {
        var response = await StaffClient(Permissions.UsersView).GetAsync($"{Url}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().ShouldBe("user.not_found");
    }
}
