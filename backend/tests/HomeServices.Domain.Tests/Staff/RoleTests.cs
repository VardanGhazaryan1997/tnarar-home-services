using HomeServices.Domain.Staff;

namespace HomeServices.Domain.Tests.Staff;

public class RoleTests
{
    [Fact]
    public void A_role_bundles_distinct_sorted_permissions()
    {
        var role = Role.Create(" Operator ", " Partners and orders ", [Permissions.OrdersView, Permissions.PartnersApprove, Permissions.OrdersView]);

        role.Name.ShouldBe("Operator");
        role.Description.ShouldBe("Partners and orders");
        role.Permissions.ShouldBe(new[] { Permissions.OrdersView, Permissions.PartnersApprove });
        role.IsSystem.ShouldBeFalse();
    }

    [Fact]
    public void Unknown_permissions_are_rejected()
    {
        Should.Throw<DomainException>(() => Role.Create("X", "", ["partners.delete_everything"])).Code.ShouldBe("role.permission_unknown");
    }

    [Fact]
    public void Super_Admin_only_permissions_cannot_be_put_in_a_role()
    {
        Should.Throw<DomainException>(() => Role.Create("X", "", [Permissions.RolesManage])).Code.ShouldBe("role.permission_super_admin_only");
        Should.Throw<DomainException>(() => Role.Create("X", "", [Permissions.PaymentSettingsManage])).Code.ShouldBe("role.permission_super_admin_only");
    }

    [Fact]
    public void Names_and_descriptions_are_validated()
    {
        Should.Throw<DomainException>(() => Role.Create(" ", "", [])).Code.ShouldBe("role.name_invalid");
        Should.Throw<DomainException>(() => Role.Create(new string('a', 65), "", [])).Code.ShouldBe("role.name_invalid");
        Should.Throw<DomainException>(() => Role.Create("X", new string('a', 257), [])).Code.ShouldBe("role.description_too_long");
    }

    [Fact]
    public void Custom_roles_can_be_renamed_and_re_permissioned()
    {
        var role = Role.Create("Night shift", "", [Permissions.OrdersView]);

        role.Rename("Evening shift", "Handles late orders");
        role.SetPermissions([Permissions.OrdersManage]);

        role.Name.ShouldBe("Evening shift");
        role.Permissions.ShouldBe(new[] { Permissions.OrdersManage });
    }

    [Fact]
    public void Built_in_roles_keep_their_name_but_permissions_can_change()
    {
        var role = Role.Create("Finance", "", [Permissions.PaymentsView], isSystem: true);

        Should.Throw<DomainException>(() => role.Rename("Money", "")).Code.ShouldBe("role.system_cannot_be_renamed");

        role.SetPermissions([Permissions.PaymentsView, Permissions.ReportsView]);
        role.Permissions.Count.ShouldBe(2);
    }
}
