using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Identity;
using HomeServices.Application.Staff;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Staff;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AdminAccessTests(ApiFactory factory)
{
    private HttpClient ClientWithStaffToken(bool superAdmin = false, params string[] permissions)
    {
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", "Staff", "hash", superAdmin);
        var token = factory.Services.GetRequiredService<IStaffTokenService>().CreateAccessToken(staff, permissions).Token;
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Staff_with_the_permission_can_list_roles()
    {
        var response = await ClientWithStaffToken(permissions: Permissions.StaffView).GetAsync("/api/v1/admin/roles");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var roles = (await response.Content.ReadFromJsonAsync<List<RoleDto>>())!;
        roles.Select(r => r.Name).ShouldContain("Operator");
        roles.Single(r => r.Name == "Finance").Permissions.ShouldContain(Permissions.CommissionsManage);
    }

    [Fact]
    public async Task Staff_without_the_permission_get_403()
    {
        var response = await ClientWithStaffToken(permissions: Permissions.ContentManage).GetAsync("/api/v1/admin/roles");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Super_Admins_pass_every_permission_check()
    {
        var response = await ClientWithStaffToken(superAdmin: true).GetAsync("/api/v1/admin/roles");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Portal_tokens_are_rejected_by_permission_protected_endpoints()
    {
        var user = User.Register(PhoneNumber.Parse("+37491234567"));
        var portalToken = factory.Services.GetRequiredService<ITokenService>().CreateAccessToken(user).Token;
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", portalToken);

        (await client.GetAsync("/api/v1/admin/roles")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Any_signed_in_staff_member_can_read_the_permission_catalog()
    {
        var response = await ClientWithStaffToken().GetAsync("/api/v1/admin/permissions");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<List<PermissionDto>>())!.Count.ShouldBe(Permissions.All.Count);
    }
}
