using HomeServices.Application.Staff;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Staff;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Staff;

public class PermissionTests
{
    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();

    private async Task<StaffUser> GivenStaff(bool superAdmin = false, bool suspended = false, params Role[] roles)
    {
        _db.Roles.AddRange(roles);
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", "Staff", "pw:x", superAdmin);
        foreach (var role in roles)
        {
            staff.AssignRole(role.Id);
        }

        if (suspended)
        {
            staff.Suspend();
        }

        _db.StaffUsers.Add(staff);
        await _db.SaveChangesAsync();
        return staff;
    }

    [Fact]
    public async Task Super_Admins_have_every_permission()
    {
        var staff = await GivenStaff(superAdmin: true);

        (await StaffPermissionResolver.ResolveAsync(_db, staff, CancellationToken.None)).ShouldBe(Permissions.All);
    }

    [Fact]
    public async Task Other_staff_have_the_union_of_their_roles_permissions()
    {
        var staff = await GivenStaff(roles:
        [
            Role.Create("Operator", "", [Permissions.PartnersApprove, Permissions.OrdersView]),
            Role.Create("Support", "", [Permissions.OrdersView, Permissions.SupportManage]),
        ]);
        _db.Roles.Add(Role.Create("Unassigned", "", [Permissions.AuditView]));
        await _db.SaveChangesAsync();

        var permissions = await StaffPermissionResolver.ResolveAsync(_db, staff, CancellationToken.None);

        permissions.ShouldBe(new[] { Permissions.OrdersView, Permissions.PartnersApprove, Permissions.SupportManage });
    }

    [Fact]
    public async Task Staff_without_roles_have_no_permissions()
    {
        var staff = await GivenStaff();

        (await StaffPermissionResolver.ResolveAsync(_db, staff, CancellationToken.None)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Sessions_carry_the_effective_permissions_in_the_access_token()
    {
        var staff = await GivenStaff(roles: [Role.Create("Finance", "", [Permissions.PaymentsView])]);
        staff.BeginTwoFactorSetup(FakeTotpService.Secret);
        await _db.SaveChangesAsync();
        var clock = new FakeClock(DateTimeOffset.UtcNow);
        var tokens = new FakeStaffTokenService(clock);
        var steps = new StaffTwoFactorSteps(_db, new FakeTotpService(), tokens, new FakeTokenService(clock), new FakeSecretHasher(), clock, Options.Create(new StaffAuthSettings()));

        var session = await new CompleteStaffTwoFactorSetupHandler(steps)
            .HandleAsync(new CompleteStaffTwoFactorSetup($"challenge:{staff.Id}", FakeTotpService.ValidCode), CancellationToken.None);

        tokens.LastPermissions.ShouldBe(new[] { Permissions.PaymentsView });
        session.Staff.Permissions.ShouldBe(new[] { Permissions.PaymentsView });
    }

    [Fact]
    public async Task The_last_active_Super_Admin_cannot_be_removed()
    {
        var onlyAdmin = await GivenStaff(superAdmin: true);
        await GivenStaff(superAdmin: true, suspended: true);

        var exception = await Should.ThrowAsync<DomainException>(
            () => new SuperAdminGuard(_db).EnsureNotLastActiveSuperAdminAsync(onlyAdmin.Id, CancellationToken.None));

        exception.Code.ShouldBe("staff.last_super_admin");
    }

    [Fact]
    public async Task A_Super_Admin_can_be_removed_while_another_active_one_exists()
    {
        var first = await GivenStaff(superAdmin: true);
        await GivenStaff(superAdmin: true);

        await new SuperAdminGuard(_db).EnsureNotLastActiveSuperAdminAsync(first.Id, CancellationToken.None);
    }

    [Fact]
    public async Task The_guard_ignores_staff_who_are_not_active_Super_Admins()
    {
        var regular = await GivenStaff();
        var suspendedAdmin = await GivenStaff(superAdmin: true, suspended: true);
        var guard = new SuperAdminGuard(_db);

        await guard.EnsureNotLastActiveSuperAdminAsync(regular.Id, CancellationToken.None);
        await guard.EnsureNotLastActiveSuperAdminAsync(suspendedAdmin.Id, CancellationToken.None);
        await guard.EnsureNotLastActiveSuperAdminAsync(Guid.NewGuid(), CancellationToken.None);
    }

    [Fact]
    public async Task The_permission_catalog_lists_every_permission_with_its_group()
    {
        var catalog = await new GetPermissionCatalogHandler().HandleAsync(new GetPermissionCatalog(), CancellationToken.None);

        catalog.Count.ShouldBe(Permissions.All.Count);
        catalog.ShouldContain(new PermissionDto("partners.approve", "partners", SuperAdminOnly: false));
        catalog.ShouldContain(new PermissionDto("roles.manage", "roles", SuperAdminOnly: true));
    }

    [Fact]
    public async Task Roles_are_listed_by_name_with_their_permissions()
    {
        _db.Roles.AddRange(Role.Create("Support", "Complaints", [Permissions.SupportManage], isSystem: true), Role.Create("Finance", "", [Permissions.PaymentsView]));
        await _db.SaveChangesAsync();

        var roles = await new GetRolesHandler(_db).HandleAsync(new GetRoles(), CancellationToken.None);

        roles.Select(r => r.Name).ShouldBe(new[] { "Finance", "Support" });
        roles[1].IsSystem.ShouldBeTrue();
        roles[1].Description.ShouldBe("Complaints");
        roles[1].Permissions.ShouldBe(new[] { Permissions.SupportManage });
    }
}
