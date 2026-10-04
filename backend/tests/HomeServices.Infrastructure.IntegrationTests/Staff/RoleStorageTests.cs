using HomeServices.Domain.Staff;
using HomeServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Infrastructure.IntegrationTests.Staff;

[Collection(PostgresCollection.Name)]
public class RoleStorageTests(PostgresFixture db)
{
    [Fact]
    public async Task Migrations_seed_the_built_in_roles()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(db.ConnectionStringFor("role_seed")).Options;
        await using var context = new AppDbContext(options);
        await context.Database.MigrateAsync();

        var roles = await context.Roles.OrderBy(r => r.Name).ToListAsync();

        roles.Select(r => r.Name).ShouldBe(new[] { "Administrator", "Content Manager", "Finance", "Operator", "Support" });
        roles.ShouldAllBe(r => r.IsSystem);
        roles.Single(r => r.Name == "Operator").Permissions.ShouldContain(Permissions.PartnersApprove);
        roles.ShouldAllBe(r => !r.Permissions.Contains(Permissions.RolesManage));
    }

    [Fact]
    public async Task Role_permissions_and_staff_assignments_are_saved()
    {
        var role = Role.Create($"Night shift {Guid.NewGuid():N}", "Late orders", [Permissions.OrdersView, Permissions.OrdersManage]);
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", "Ani", "hash");
        staff.AssignRole(role.Id);
        await using (var context = db.CreateContext())
        {
            context.Roles.Add(role);
            context.StaffUsers.Add(staff);
            await context.SaveChangesAsync();
        }

        await using var check = db.CreateContext();
        (await check.Roles.SingleAsync(r => r.Id == role.Id)).Permissions.ShouldBe(new[] { Permissions.OrdersManage, Permissions.OrdersView });
        (await check.StaffUsers.SingleAsync(s => s.Id == staff.Id)).RoleIds.ShouldBe(new[] { role.Id });
    }

    [Fact]
    public async Task Removing_a_role_from_staff_is_saved()
    {
        var role = Role.Create($"Temp {Guid.NewGuid():N}", "", [Permissions.AuditView]);
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", "Ani", "hash");
        staff.AssignRole(role.Id);
        await using (var context = db.CreateContext())
        {
            context.Roles.Add(role);
            context.StaffUsers.Add(staff);
            await context.SaveChangesAsync();
        }

        await using (var context = db.CreateContext())
        {
            var loaded = await context.StaffUsers.SingleAsync(s => s.Id == staff.Id);
            loaded.RemoveRole(role.Id);
            await context.SaveChangesAsync();
        }

        await using var check = db.CreateContext();
        (await check.StaffUsers.SingleAsync(s => s.Id == staff.Id)).RoleIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task Role_names_are_unique()
    {
        var name = $"Duplicate {Guid.NewGuid():N}";
        await using (var first = db.CreateContext())
        {
            first.Roles.Add(Role.Create(name, "first", []));
            await first.SaveChangesAsync();
        }

        await using var context = db.CreateContext();
        context.Roles.Add(Role.Create(name, "duplicate", []));

        await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
