using HomeServices.Application.Abstractions;
using HomeServices.Domain.Staff;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Staff;

/// <summary>A staff member's effective permissions: everything for Super Admins, otherwise the union of their roles.</summary>
public static class StaffPermissionResolver
{
    public static async Task<IReadOnlyList<string>> ResolveAsync(IAppDbContext db, StaffUser staff, CancellationToken cancellationToken)
    {
        if (staff.IsSuperAdmin)
        {
            return Permissions.All;
        }

        var roleIds = staff.RoleIds;
        if (roleIds.Count == 0)
        {
            return [];
        }

        var rolePermissions = await db.Roles.AsNoTracking().Where(r => roleIds.Contains(r.Id)).ToListAsync(cancellationToken);
        return rolePermissions.SelectMany(r => r.Permissions).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
    }
}
