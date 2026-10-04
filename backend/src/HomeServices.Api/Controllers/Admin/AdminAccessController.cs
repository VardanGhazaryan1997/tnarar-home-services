using HomeServices.Api.Authorization;
using HomeServices.Application.Messaging;
using HomeServices.Application.Staff;
using HomeServices.Domain.Staff;
using HomeServices.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers.Admin;

/// <summary>Permissions and roles. Changing roles is for Super Admins (<c>roles.manage</c>); staff accounts are in AdminStaffController.</summary>
[ApiController]
[Route("api/v1/admin")]
public sealed class AdminAccessController : ControllerBase
{
    public sealed record RoleRequest(string Name, string? Description, IReadOnlyList<string> Permissions);

    /// <summary>Every permission, grouped by area.</summary>
    [HttpGet("permissions")]
    [Authorize(Policy = AuthPolicies.Staff)]
    public Task<IReadOnlyList<PermissionDto>> GetPermissions(
        [FromServices] IQueryHandler<GetPermissionCatalog, IReadOnlyList<PermissionDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetPermissionCatalog(), cancellationToken);

    /// <summary>All roles with their permissions.</summary>
    [HttpGet("roles")]
    [HasPermission(Permissions.StaffView)]
    public Task<IReadOnlyList<RoleDto>> GetRoles(
        [FromServices] IQueryHandler<GetRoles, IReadOnlyList<RoleDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetRoles(), cancellationToken);

    /// <summary>Creates a role. Permission codes come from GET permissions; Super-Admin-only ones can't be included.</summary>
    [HttpPost("roles")]
    [HasPermission(Permissions.RolesManage)]
    public async Task<ActionResult<RoleDto>> CreateRoleAsync(
        RoleRequest request,
        [FromServices] ICommandHandler<CreateRole, RoleDto> handler,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await handler.HandleAsync(new CreateRole(request.Name, request.Description, request.Permissions), cancellationToken));

    /// <summary>Changes a role. Built-in roles keep their name. Staff get the new permissions at their next token refresh.</summary>
    [HttpPut("roles/{id:guid}")]
    [HasPermission(Permissions.RolesManage)]
    public Task<RoleDto> UpdateRole(
        Guid id,
        RoleRequest request,
        [FromServices] ICommandHandler<UpdateRole, RoleDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new UpdateRole(id, request.Name, request.Description, request.Permissions), cancellationToken);

    /// <summary>Deletes a role nobody has (409 "role.in_use" otherwise). Built-in roles can't be deleted.</summary>
    [HttpDelete("roles/{id:guid}")]
    [HasPermission(Permissions.RolesManage)]
    public async Task<IActionResult> DeleteRoleAsync(
        Guid id,
        [FromServices] ICommandHandler<DeleteRole, bool> handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteRole(id), cancellationToken);
        return NoContent();
    }
}
