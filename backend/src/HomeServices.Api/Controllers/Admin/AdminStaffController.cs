using HomeServices.Api.Authorization;
using HomeServices.Application.Common;
using HomeServices.Application.Messaging;
using HomeServices.Application.Staff;
using HomeServices.Domain.Staff;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers.Admin;

/// <summary>
/// Back Office accounts: invite, change roles, suspend, reset 2FA. Super Admin accounts and the Super Admin
/// flag can only be changed by Super Admins, and the last active Super Admin always stays.
/// </summary>
[ApiController]
[Route("api/v1/admin/staff")]
public sealed class AdminStaffController : ControllerBase
{
    public sealed record InviteRequest(string Email, string FullName, IReadOnlyList<Guid> RoleIds, bool IsSuperAdmin = false);

    public sealed record UpdateRequest(string FullName, IReadOnlyList<Guid> RoleIds);

    /// <summary>Staff, by name. Optional filters: <paramref name="search"/> (name or email), <paramref name="status"/>, <paramref name="roleId"/>.</summary>
    [HttpGet]
    [HasPermission(Permissions.StaffView)]
    public Task<PagedResult<StaffMemberDto>> List(
        [FromServices] IQueryHandler<GetStaffList, PagedResult<StaffMemberDto>> handler,
        [FromQuery] string? search,
        [FromQuery] StaffStatus? status,
        [FromQuery] Guid? roleId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetStaffList(search, status, roleId, page ?? 1, pageSize ?? GetStaffList.DefaultPageSize), cancellationToken);

    /// <summary>A staff member with their effective permissions.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.StaffView)]
    public Task<StaffMemberDetailDto> Get(Guid id, [FromServices] IQueryHandler<GetStaffMember, StaffMemberDetailDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetStaffMember(id), cancellationToken);

    /// <summary>Invites someone. The response's <c>inviteToken</c> is shown only once: put it in the link you send them.</summary>
    [HttpPost]
    [HasPermission(Permissions.StaffManage)]
    public async Task<ActionResult<StaffInviteDto>> InviteAsync(
        InviteRequest request,
        [FromServices] ICommandHandler<InviteStaff, StaffInviteDto> handler,
        CancellationToken cancellationToken) =>
        StatusCode(
            StatusCodes.Status201Created,
            await handler.HandleAsync(new InviteStaff(request.Email, request.FullName, request.RoleIds, request.IsSuperAdmin), cancellationToken));

    /// <summary>A new invitation link (the old one stops working).</summary>
    [HttpPost("{id:guid}/invite")]
    [HasPermission(Permissions.StaffManage)]
    public Task<StaffInviteDto> RenewInvite(Guid id, [FromServices] ICommandHandler<RenewStaffInvite, StaffInviteDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new RenewStaffInvite(id), cancellationToken);

    /// <summary>Changes the name and replaces the roles.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.StaffManage)]
    public Task<StaffMemberDto> Update(
        Guid id,
        UpdateRequest request,
        [FromServices] ICommandHandler<UpdateStaffMember, StaffMemberDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new UpdateStaffMember(id, request.FullName, request.RoleIds), cancellationToken);

    /// <summary>Blocks sign-in and ends their sessions (within 15 minutes).</summary>
    [HttpPost("{id:guid}/suspend")]
    [HasPermission(Permissions.StaffManage)]
    public Task<StaffMemberDto> Suspend(Guid id, [FromServices] ICommandHandler<SetStaffSuspended, StaffMemberDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetStaffSuspended(id, Suspended: true), cancellationToken);

    [HttpPost("{id:guid}/activate")]
    [HasPermission(Permissions.StaffManage)]
    public Task<StaffMemberDto> Activate(Guid id, [FromServices] ICommandHandler<SetStaffSuspended, StaffMemberDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetStaffSuspended(id, Suspended: false), cancellationToken);

    /// <summary>Forgets their authenticator app and ends their sessions; they set up a new one at the next sign-in.</summary>
    [HttpPost("{id:guid}/reset-two-factor")]
    [HasPermission(Permissions.StaffManage)]
    public Task<StaffMemberDto> ResetTwoFactor(Guid id, [FromServices] ICommandHandler<ResetStaffTwoFactor, StaffMemberDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new ResetStaffTwoFactor(id), cancellationToken);

    /// <summary>Makes them a Super Admin (Super Admins only).</summary>
    [HttpPost("{id:guid}/super-admin")]
    [HasPermission(Permissions.RolesManage)]
    public Task<StaffMemberDto> GrantSuperAdmin(Guid id, [FromServices] ICommandHandler<SetStaffSuperAdmin, StaffMemberDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetStaffSuperAdmin(id, IsSuperAdmin: true), cancellationToken);

    /// <summary>Removes the Super Admin flag (Super Admins only; not from the last active one).</summary>
    [HttpDelete("{id:guid}/super-admin")]
    [HasPermission(Permissions.RolesManage)]
    public Task<StaffMemberDto> RevokeSuperAdmin(Guid id, [FromServices] ICommandHandler<SetStaffSuperAdmin, StaffMemberDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetStaffSuperAdmin(id, IsSuperAdmin: false), cancellationToken);
}
