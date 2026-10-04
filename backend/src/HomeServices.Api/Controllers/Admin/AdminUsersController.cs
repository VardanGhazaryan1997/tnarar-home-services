using HomeServices.Api.Authorization;
using HomeServices.Application.Common;
using HomeServices.Application.Messaging;
using HomeServices.Application.Users;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Staff;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers.Admin;

/// <summary>Portal users: search, view, block and unblock.</summary>
[ApiController]
[Route("api/v1/admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    public sealed record BlockRequest(string Reason);

    /// <summary>
    /// Users, newest first. <paramref name="search"/> matches the phone (any format), name or email;
    /// <paramref name="role"/> is Customer or Partner; <paramref name="status"/> is Active or Blocked.
    /// </summary>
    [HttpGet]
    [HasPermission(Permissions.UsersView)]
    public Task<PagedResult<AdminUserListItemDto>> List(
        [FromServices] IQueryHandler<GetAdminUsers, PagedResult<AdminUserListItemDto>> handler,
        [FromQuery] string? search,
        [FromQuery] UserRoles? role,
        [FromQuery] UserStatus? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminUsers(search, role, status, page ?? 1, pageSize ?? GetAdminUsers.DefaultPageSize), cancellationToken);

    /// <summary>A user with their partner profile (if any) and why they're blocked (if they are).</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.UsersView)]
    public Task<AdminUserDto> Get(Guid id, [FromServices] IQueryHandler<GetAdminUser, AdminUserDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminUser(id), cancellationToken);

    /// <summary>Stops sign-in, ends sessions (within 15 minutes) and hides their partner profile. <c>reason</c> is required.</summary>
    [HttpPost("{id:guid}/block")]
    [HasPermission(Permissions.UsersBlock)]
    public Task<AdminUserDto> Block(Guid id, BlockRequest request, [FromServices] ICommandHandler<BlockUser, AdminUserDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new BlockUser(id, request.Reason), cancellationToken);

    [HttpPost("{id:guid}/unblock")]
    [HasPermission(Permissions.UsersBlock)]
    public Task<AdminUserDto> Unblock(Guid id, [FromServices] ICommandHandler<UnblockUser, AdminUserDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new UnblockUser(id), cancellationToken);
}
