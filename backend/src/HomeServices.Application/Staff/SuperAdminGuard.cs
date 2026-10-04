using HomeServices.Application.Abstractions;
using HomeServices.Domain;
using HomeServices.Domain.Staff;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Staff;

/// <summary>
/// The platform must always have at least one active Super Admin. Call before suspending a staff
/// member or revoking their Super Admin flag (staff management, task T32).
/// </summary>
public sealed class SuperAdminGuard(IAppDbContext db)
{
    public async Task EnsureNotLastActiveSuperAdminAsync(Guid staffUserId, CancellationToken cancellationToken)
    {
        var target = await db.StaffUsers.AsNoTracking().SingleOrDefaultAsync(s => s.Id == staffUserId, cancellationToken);
        if (target is not { IsSuperAdmin: true, Status: StaffStatus.Active })
        {
            return;
        }

        var othersActive = await db.StaffUsers.AnyAsync(
            s => s.Id != staffUserId && s.IsSuperAdmin && s.Status == StaffStatus.Active,
            cancellationToken);

        if (!othersActive)
        {
            throw new DomainException("staff.last_super_admin", "The last active Super Admin can't be removed or suspended.");
        }
    }
}
