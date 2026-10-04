using HomeServices.Domain.Common;

namespace HomeServices.Domain.Staff;

/// <summary>Links a staff member to a role.</summary>
public sealed class StaffUserRole : IAudited
{
    private StaffUserRole()
    {
    }

    internal StaffUserRole(Guid staffUserId, Guid roleId)
    {
        StaffUserId = staffUserId;
        RoleId = roleId;
    }

    public Guid StaffUserId { get; private set; }

    public Guid RoleId { get; private set; }
}
