using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Partners;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Users;

/// <summary>
/// Portal users for the Back Office, newest first. <see cref="Search"/> matches the phone number (any format)
/// when it is one, otherwise the name or email.
/// </summary>
public sealed record GetAdminUsers(
    string? Search = null,
    UserRoles? Role = null,
    UserStatus? Status = null,
    int Page = 1,
    int PageSize = GetAdminUsers.DefaultPageSize) : IQuery<PagedResult<AdminUserListItemDto>>
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;
}

public sealed record GetAdminUser(Guid Id) : IQuery<AdminUserDto>;

/// <summary>Blocks a user: no sign-in, sessions end within 15 minutes, their partner profile is hidden. A reason is required.</summary>
public sealed record BlockUser(Guid Id, string Reason) : ICommand<AdminUserDto>;

public sealed record UnblockUser(Guid Id) : ICommand<AdminUserDto>;

/// <summary>The user's partner profile, if they have one.</summary>
public sealed record UserPartnerDto(Guid Id, string DisplayName, string Status, string? Slug);

public sealed record AdminUserListItemDto(
    Guid Id,
    string Phone,
    string? FullName,
    string? Email,
    IReadOnlyList<string> Roles,
    string Status,
    string? PartnerStatus,
    DateTimeOffset? LastSignInAt,
    DateTimeOffset CreatedAt);

public sealed record AdminUserDto(
    Guid Id,
    string Phone,
    string? FullName,
    string? Email,
    IReadOnlyList<string> Roles,
    string Status,
    string? BlockReason,
    UserPartnerDto? Partner,
    DateTimeOffset? LastSignInAt,
    DateTimeOffset CreatedAt);

public sealed class GetAdminUsersValidator : AbstractValidator<GetAdminUsers>
{
    public GetAdminUsersValidator()
    {
        RuleFor(x => x.Role)
            .Must(role => role is UserRoles.Customer or UserRoles.Partner).WithErrorCode("role.invalid")
            .When(x => x.Role is not null);
        RuleFor(x => x.Status).IsInEnum().WithErrorCode("status.invalid");
        RuleFor(x => x.Search).MaximumLength(100).WithErrorCode("search.too_long");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetAdminUsers.MaxPageSize).WithErrorCode("page_size.invalid");
    }
}

public sealed class BlockUserValidator : AbstractValidator<BlockUser>
{
    public BlockUserValidator()
    {
        RuleFor(x => x.Reason)
            .Must(reason => !string.IsNullOrWhiteSpace(reason)).WithErrorCode("reason.required")
            .Must(reason => reason is null || reason.Trim().Length <= User.BlockReasonMaxLength).WithErrorCode("reason.too_long");
    }
}

public sealed class GetAdminUsersHandler(IAppDbContext db) : IQueryHandler<GetAdminUsers, PagedResult<AdminUserListItemDto>>
{
    public async Task<PagedResult<AdminUserListItemDto>> HandleAsync(GetAdminUsers query, CancellationToken cancellationToken)
    {
        var users = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            if (PhoneNumber.TryParse(search, out var phone))
            {
                users = users.Where(u => u.Phone == phone);
            }
            else
            {
                var lowered = search.ToLowerInvariant();
                users = users.Where(u => (u.FullName != null && u.FullName.ToLower().Contains(lowered)) || (u.Email != null && u.Email.Contains(lowered)));
            }
        }

        if (query.Role is { } role)
        {
            users = users.Where(u => (u.Roles & role) == role);
        }

        if (query.Status is { } status)
        {
            users = users.Where(u => u.Status == status);
        }

        var total = await users.CountAsync(cancellationToken);
        var page = await (
                from user in users.OrderByDescending(u => u.CreatedAt).ThenByDescending(u => u.Id)
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                join profile in db.PartnerProfiles.AsNoTracking() on user.Id equals profile.UserId into profiles
                from profile in profiles.DefaultIfEmpty()
                select new { User = user, PartnerStatus = profile == null ? (PartnerStatus?)null : profile.Status })
            .ToListAsync(cancellationToken);

        var items = page
            .OrderByDescending(r => r.User.CreatedAt)
            .ThenByDescending(r => r.User.Id)
            .Select(r => new AdminUserListItemDto(
                r.User.Id,
                r.User.Phone.Value,
                r.User.FullName,
                r.User.Email,
                r.User.RoleNames,
                r.User.Status.ToString(),
                r.PartnerStatus?.ToString(),
                r.User.LastSignInAt,
                r.User.CreatedAt))
            .ToList();
        return new PagedResult<AdminUserListItemDto>(items, query.Page, query.PageSize, total);
    }
}

public sealed class GetAdminUserHandler(IAppDbContext db) : IQueryHandler<GetAdminUser, AdminUserDto>
{
    public async Task<AdminUserDto> HandleAsync(GetAdminUser query, CancellationToken cancellationToken) =>
        await AdminUserRules.ToDtoAsync(db, await AdminUserRules.LoadAsync(db, query.Id, cancellationToken), cancellationToken);
}

public sealed class BlockUserHandler(IAppDbContext db, TimeProvider clock) : ICommandHandler<BlockUser, AdminUserDto>
{
    public async Task<AdminUserDto> HandleAsync(BlockUser command, CancellationToken cancellationToken)
    {
        var user = await AdminUserRules.LoadAsync(db, command.Id, cancellationToken);
        user.Block(command.Reason);

        // Ends their sessions: the current access token still works until it expires (within 15 minutes).
        var now = clock.GetUtcNow();
        var active = await db.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAt == null).ToListAsync(cancellationToken);
        active.ForEach(t => t.Revoke(now));

        await db.SaveChangesAsync(cancellationToken);
        return await AdminUserRules.ToDtoAsync(db, user, cancellationToken);
    }
}

public sealed class UnblockUserHandler(IAppDbContext db) : ICommandHandler<UnblockUser, AdminUserDto>
{
    public async Task<AdminUserDto> HandleAsync(UnblockUser command, CancellationToken cancellationToken)
    {
        var user = await AdminUserRules.LoadAsync(db, command.Id, cancellationToken);
        user.Unblock();
        await db.SaveChangesAsync(cancellationToken);
        return await AdminUserRules.ToDtoAsync(db, user, cancellationToken);
    }
}

internal static class AdminUserRules
{
    public static async Task<User> LoadAsync(IAppDbContext db, Guid id, CancellationToken cancellationToken) =>
        await db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken)
        ?? throw new NotFoundException("User not found.", "user.not_found");

    public static async Task<AdminUserDto> ToDtoAsync(IAppDbContext db, User user, CancellationToken cancellationToken)
    {
        var partner = await db.PartnerProfiles.AsNoTracking()
            .Where(p => p.UserId == user.Id)
            .Select(p => new UserPartnerDto(p.Id, p.DisplayName, p.Status.ToString(), p.Slug))
            .SingleOrDefaultAsync(cancellationToken);

        return new AdminUserDto(
            user.Id,
            user.Phone.Value,
            user.FullName,
            user.Email,
            user.RoleNames,
            user.Status.ToString(),
            user.BlockReason,
            partner,
            user.LastSignInAt,
            user.CreatedAt);
    }
}
