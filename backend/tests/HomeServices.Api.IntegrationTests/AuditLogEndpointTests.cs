using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Auditing;
using HomeServices.Application.Common;
using HomeServices.Application.Identity;
using HomeServices.Application.Staff;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Staff;
using HomeServices.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AuditLogEndpointTests(ApiFactory factory)
{
    private HttpClient StaffClient(params string[] permissions)
    {
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", "Auditor", "hash");
        var token = factory.Services.GetRequiredService<IStaffTokenService>().CreateAccessToken(staff, permissions).Token;
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<User> GivenPortalUser()
    {
        var user = User.Register(PhoneNumber.Parse($"+37477{Random.Shared.Next(100_000, 999_999)}"));
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task A_users_profile_change_is_logged_with_the_user_as_actor()
    {
        var user = await GivenPortalUser();
        var portal = factory.CreateClient();
        portal.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.Services.GetRequiredService<ITokenService>().CreateAccessToken(user).Token);
        (await portal.PutAsJsonAsync("/api/v1/me", new { fullName = "Ani Petrosyan", email = (string?)null }))
            .StatusCode.ShouldBe(HttpStatusCode.OK);

        var response = await StaffClient(Permissions.AuditView)
            .GetAsync($"/api/v1/admin/audit-log?entityType=User&entityId={user.Id}&action=Updated");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = (await response.Content.ReadFromJsonAsync<PagedResult<AuditLogEntryDto>>())!;
        var entry = page.Items.ShouldHaveSingleItem();
        entry.ActorType.ShouldBe("User");
        entry.ActorId.ShouldBe(user.Id.ToString());
        entry.ActorName.ShouldBe("Ani Petrosyan");
        entry.Changes.ShouldContain(new AuditChangeDto("FullName", null, "Ani Petrosyan"));
    }

    [Fact]
    public async Task Records_created_outside_a_request_are_logged_as_system_changes()
    {
        var user = await GivenPortalUser();

        var page = (await StaffClient(Permissions.AuditView)
            .GetFromJsonAsync<PagedResult<AuditLogEntryDto>>($"/api/v1/admin/audit-log?entityType=User&entityId={user.Id}"))!;

        var entry = page.Items.ShouldHaveSingleItem();
        entry.Action.ShouldBe("Created");
        entry.ActorType.ShouldBe("System");
        entry.ActorId.ShouldBeNull();
    }

    [Fact]
    public async Task Staff_without_audit_view_get_403()
    {
        (await StaffClient(Permissions.OrdersView).GetAsync("/api/v1/admin/audit-log")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Invalid_paging_returns_400_with_the_error_code()
    {
        var response = await StaffClient(Permissions.AuditView).GetAsync("/api/v1/admin/audit-log?pageSize=0");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("page_size.invalid");
    }
}
