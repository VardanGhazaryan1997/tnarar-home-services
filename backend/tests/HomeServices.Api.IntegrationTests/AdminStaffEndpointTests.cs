using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Common;
using HomeServices.Application.Staff;
using HomeServices.Domain.Staff;
using HomeServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AdminStaffEndpointTests(ApiFactory factory)
{
    private const string StaffUrl = "/api/v1/admin/staff";
    private const string RolesUrl = "/api/v1/admin/roles";

    /// <summary>A saved staff member and a client signed in as them.</summary>
    private async Task<(HttpClient Client, StaffUser Staff)> StaffClientAsync(bool superAdmin, params string[] permissions)
    {
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", superAdmin ? "Boss" : "Manager", "hash", superAdmin);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.StaffUsers.Add(staff);
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.Services.GetRequiredService<IStaffTokenService>().CreateAccessToken(staff, permissions).Token);
        return (client, staff);
    }

    private async Task<Guid> RoleIdAsync(string name)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return (await scope.ServiceProvider.GetRequiredService<AppDbContext>().Roles.SingleAsync(r => r.Name == name)).Id;
    }

    private static async Task<string?> CodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();

    [Fact]
    public async Task An_invited_staff_member_chooses_a_password_and_signs_in()
    {
        var (manager, _) = await StaffClientAsync(false, Permissions.StaffManage, Permissions.StaffView);
        var email = $"{Guid.NewGuid():N}@example.com";

        var invited = await manager.PostAsJsonAsync(StaffUrl, new { email, fullName = "Ani Operator", roleIds = new[] { await RoleIdAsync("Operator") } });

        invited.StatusCode.ShouldBe(HttpStatusCode.Created);
        var invite = (await invited.Content.ReadFromJsonAsync<StaffInviteDto>())!;
        invite.Member.Status.ShouldBe("Invited");
        invite.Member.Roles.ShouldHaveSingleItem().Name.ShouldBe("Operator");

        var anonymous = factory.CreateClient();
        (await anonymous.PostAsJsonAsync("/api/v1/admin/auth/login", new { email, password = "Strong-Password-1" }))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.PostAsJsonAsync("/api/v1/admin/auth/accept-invite", new { token = invite.InviteToken, password = "Strong-Password-1" }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var signIn = await anonymous.PostAsJsonAsync("/api/v1/admin/auth/login", new { email, password = "Strong-Password-1" });
        signIn.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await signIn.Content.ReadFromJsonAsync<StaffSignInResult>())!.Status.ShouldBe(StaffSignInStatus.TwoFactorSetupRequired);

        var detail = await manager.GetFromJsonAsync<StaffMemberDetailDto>($"{StaffUrl}/{invite.Member.Id}");
        detail!.Member.Status.ShouldBe("Active");
        detail.Permissions.ShouldContain(Permissions.PartnersApprove);
    }

    [Fact]
    public async Task Bad_invitation_links_and_short_passwords_are_refused()
    {
        var anonymous = factory.CreateClient();

        var unknown = await anonymous.PostAsJsonAsync("/api/v1/admin/auth/accept-invite", new { token = "nope", password = "Strong-Password-1" });
        unknown.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await CodeAsync(unknown)).ShouldBe("staff.invite_invalid");

        var tooShort = await anonymous.PostAsJsonAsync("/api/v1/admin/auth/accept-invite", new { token = "nope", password = "short" });
        tooShort.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await tooShort.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").GetProperty("password")[0].GetString().ShouldBe("password.too_short");
    }

    [Fact]
    public async Task Staff_managers_list_suspend_and_reactivate_staff()
    {
        var (manager, _) = await StaffClientAsync(false, Permissions.StaffManage, Permissions.StaffView);
        var (_, colleague) = await StaffClientAsync(false);

        var list = await manager.GetFromJsonAsync<PagedResult<StaffMemberDto>>($"{StaffUrl}?search={colleague.Email}");
        list!.Items.ShouldHaveSingleItem().Id.ShouldBe(colleague.Id);

        var suspended = await manager.PostAsync($"{StaffUrl}/{colleague.Id}/suspend", null);
        suspended.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await suspended.Content.ReadFromJsonAsync<StaffMemberDto>())!.Status.ShouldBe("Suspended");
        (await (await manager.PostAsync($"{StaffUrl}/{colleague.Id}/activate", null)).Content.ReadFromJsonAsync<StaffMemberDto>())!
            .Status.ShouldBe("Active");
    }

    [Fact]
    public async Task Super_Admin_powers_stay_with_Super_Admins()
    {
        var (manager, _) = await StaffClientAsync(false, Permissions.StaffManage, Permissions.StaffView);
        var (boss, _) = await StaffClientAsync(true);
        var (_, colleague) = await StaffClientAsync(false);

        var invitingBoss = await manager.PostAsJsonAsync(StaffUrl, new { email = $"{Guid.NewGuid():N}@example.com", fullName = "New Boss", roleIds = Array.Empty<Guid>(), isSuperAdmin = true });
        invitingBoss.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await CodeAsync(invitingBoss)).ShouldBe("staff.super_admin_only");
        (await manager.PostAsync($"{StaffUrl}/{colleague.Id}/super-admin", null)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var granted = await boss.PostAsync($"{StaffUrl}/{colleague.Id}/super-admin", null);
        (await granted.Content.ReadFromJsonAsync<StaffMemberDto>())!.IsSuperAdmin.ShouldBeTrue();
        var revoked = await boss.DeleteAsync($"{StaffUrl}/{colleague.Id}/super-admin");
        (await revoked.Content.ReadFromJsonAsync<StaffMemberDto>())!.IsSuperAdmin.ShouldBeFalse();
    }

    [Fact]
    public async Task Super_Admins_create_change_and_delete_roles()
    {
        var (boss, _) = await StaffClientAsync(true);
        var name = $"Role {Guid.NewGuid():N}"[..30];

        var created = await boss.PostAsJsonAsync(RolesUrl, new { name, description = "Test", permissions = new[] { Permissions.ReviewsModerate } });
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var role = (await created.Content.ReadFromJsonAsync<RoleDto>())!;

        var updated = await boss.PutAsJsonAsync($"{RolesUrl}/{role.Id}", new { name, description = "Changed", permissions = new[] { Permissions.SupportManage } });
        (await updated.Content.ReadFromJsonAsync<RoleDto>())!.Permissions.ShouldBe(new[] { Permissions.SupportManage });

        var reserved = await boss.PutAsJsonAsync($"{RolesUrl}/{role.Id}", new { name, permissions = new[] { Permissions.RolesManage } });
        reserved.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        (await boss.DeleteAsync($"{RolesUrl}/{role.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Staff_without_the_permissions_get_403()
    {
        var (viewer, _) = await StaffClientAsync(false, Permissions.StaffView);
        var (allGrantable, _) = await StaffClientAsync(false, [.. Permissions.All.Where(Permissions.IsGrantable)]);

        (await viewer.PostAsJsonAsync(StaffUrl, new { email = "x@example.com", fullName = "X", roleIds = Array.Empty<Guid>() }))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await allGrantable.PostAsJsonAsync(RolesUrl, new { name = "Sneaky", permissions = Array.Empty<string>() }))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
