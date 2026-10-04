using HomeServices.Domain.Staff;

namespace HomeServices.Domain.Tests.Staff;

public class PermissionsTests
{
    [Fact]
    public void Every_permission_is_listed_once()
    {
        Permissions.All.Distinct().Count().ShouldBe(Permissions.All.Count);
        Permissions.All.ShouldAllBe(p => Permissions.IsKnown(p));
    }

    [Fact]
    public void Super_Admin_only_permissions_are_known_but_not_grantable()
    {
        foreach (var permission in Permissions.SuperAdminOnly)
        {
            Permissions.IsKnown(permission).ShouldBeTrue();
            Permissions.IsGrantable(permission).ShouldBeFalse();
        }

        Permissions.IsGrantable(Permissions.PartnersApprove).ShouldBeTrue();
        Permissions.IsGrantable("made.up").ShouldBeFalse();
    }
}
