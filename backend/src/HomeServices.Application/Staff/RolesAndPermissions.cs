using HomeServices.Application.Abstractions;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Staff;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Staff;

/// <summary>All permissions, grouped by area (the part before the dot), for the role editor.</summary>
public sealed record GetPermissionCatalog : IQuery<IReadOnlyList<PermissionDto>>;

public sealed record PermissionDto(string Code, string Group, bool SuperAdminOnly);

public sealed record GetRoles : IQuery<IReadOnlyList<RoleDto>>;

public sealed record RoleDto(Guid Id, string Name, string Description, bool IsSystem, IReadOnlyList<string> Permissions);

public sealed class GetPermissionCatalogHandler : IQueryHandler<GetPermissionCatalog, IReadOnlyList<PermissionDto>>
{
    public Task<IReadOnlyList<PermissionDto>> HandleAsync(GetPermissionCatalog query, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PermissionDto>>(
            Permissions.All
                .Select(code => new PermissionDto(code, code.Split('.')[0], Permissions.SuperAdminOnly.Contains(code)))
                .ToList());
}

public sealed class GetRolesHandler(IAppDbContext db) : IQueryHandler<GetRoles, IReadOnlyList<RoleDto>>
{
    public async Task<IReadOnlyList<RoleDto>> HandleAsync(GetRoles query, CancellationToken cancellationToken)
    {
        var roles = await db.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(cancellationToken);
        return roles.Select(r => new RoleDto(r.Id, r.Name, r.Description, r.IsSystem, r.Permissions)).ToList();
    }
}
