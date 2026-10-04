using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Identity;
using HomeServices.Application.Messaging;
using HomeServices.Domain;
using HomeServices.Domain.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Staff;

public sealed record StaffRoleRefDto(Guid Id, string Name);

/// <summary>A Back Office account as staff managers see it. <see cref="Status"/>: Active, Suspended or Invited.</summary>
public sealed record StaffMemberDto(
    Guid Id,
    string Email,
    string FullName,
    bool IsSuperAdmin,
    string Status,
    bool TwoFactorEnabled,
    IReadOnlyList<StaffRoleRefDto> Roles,
    DateTimeOffset? LastSignInAt,
    DateTimeOffset? InviteExpiresAt,
    DateTimeOffset CreatedAt);

/// <summary>
/// A staff member plus their effective permissions (everything for Super Admins, otherwise the union of their roles).
/// </summary>
public sealed record StaffMemberDetailDto(StaffMemberDto Member, IReadOnlyList<string> Permissions);

/// <summary>
/// An invitation. Send the person a link with <see cref="InviteToken"/> (e.g. <c>/accept-invite?token=…</c>);
/// it is shown only now and works once, until <see cref="ExpiresAt"/>.
/// </summary>
public sealed record StaffInviteDto(StaffMemberDto Member, string InviteToken, DateTimeOffset ExpiresAt);

public sealed record GetStaffList(
    string? Search = null,
    StaffStatus? Status = null,
    Guid? RoleId = null,
    int Page = 1,
    int PageSize = GetStaffList.DefaultPageSize) : IQuery<PagedResult<StaffMemberDto>>
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;
}

public sealed record GetStaffMember(Guid Id) : IQuery<StaffMemberDetailDto>;

/// <summary>Invites a new staff member. Only Super Admins can invite a Super Admin.</summary>
public sealed record InviteStaff(string Email, string FullName, IReadOnlyList<Guid> RoleIds, bool IsSuperAdmin = false) : ICommand<StaffInviteDto>;

/// <summary>A new invitation link for someone who hasn't joined yet; the old link stops working.</summary>
public sealed record RenewStaffInvite(Guid Id) : ICommand<StaffInviteDto>;

/// <summary>Changes the name and replaces the roles.</summary>
public sealed record UpdateStaffMember(Guid Id, string FullName, IReadOnlyList<Guid> RoleIds) : ICommand<StaffMemberDto>;

/// <summary>Suspends (ends their sessions) or reactivates a staff member. Nobody can suspend themselves.</summary>
public sealed record SetStaffSuspended(Guid Id, bool Suspended) : ICommand<StaffMemberDto>;

/// <summary>Grants or revokes Super Admin (Super Admins only). The last active Super Admin stays.</summary>
public sealed record SetStaffSuperAdmin(Guid Id, bool IsSuperAdmin) : ICommand<StaffMemberDto>;

/// <summary>Forgets the authenticator app (lost phone) and ends their sessions; they set up a new one at the next sign-in.</summary>
public sealed record ResetStaffTwoFactor(Guid Id) : ICommand<StaffMemberDto>;

/// <summary>The invited person chooses a password (no sign-in needed; the token proves the invitation).</summary>
public sealed record AcceptStaffInvite(string Token, string Password) : ICommand<bool>;

public sealed class GetStaffListValidator : AbstractValidator<GetStaffList>
{
    public GetStaffListValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithErrorCode("status.invalid");
        RuleFor(x => x.Search).MaximumLength(100).WithErrorCode("search.too_long");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetStaffList.MaxPageSize).WithErrorCode("page_size.invalid");
    }
}

public sealed class InviteStaffValidator : AbstractValidator<InviteStaff>
{
    public InviteStaffValidator(IAppDbContext db)
    {
        RuleFor(x => x.Email).Must(StaffRules.IsValidEmail).WithErrorCode("email.invalid");
        RuleFor(x => x.FullName).ValidStaffName();
        RuleFor(x => x.RoleIds).ExistingRoles(db);
    }
}

public sealed class UpdateStaffMemberValidator : AbstractValidator<UpdateStaffMember>
{
    public UpdateStaffMemberValidator(IAppDbContext db)
    {
        RuleFor(x => x.FullName).ValidStaffName();
        RuleFor(x => x.RoleIds).ExistingRoles(db);
    }
}

public sealed class AcceptStaffInviteValidator : AbstractValidator<AcceptStaffInvite>
{
    public AcceptStaffInviteValidator(IOptions<StaffAuthSettings> options)
    {
        RuleFor(x => x.Token).NotEmpty().WithErrorCode("token.required");
        RuleFor(x => x.Password)
            .Must(password => password is not null && password.Length >= options.Value.MinPasswordLength)
            .WithErrorCode("password.too_short");
        RuleFor(x => x.Password).MaximumLength(256).WithErrorCode("password.too_long");
    }
}

public sealed class GetStaffListHandler(IAppDbContext db) : IQueryHandler<GetStaffList, PagedResult<StaffMemberDto>>
{
    public async Task<PagedResult<StaffMemberDto>> HandleAsync(GetStaffList query, CancellationToken cancellationToken)
    {
        var staff = db.StaffUsers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            staff = staff.Where(s => s.Email.Contains(search) || s.FullName.ToLower().Contains(search));
        }

        if (query.Status is { } status)
        {
            staff = staff.Where(s => s.Status == status);
        }

        if (query.RoleId is { } roleId)
        {
            staff = staff.Where(s => s.Roles.Any(r => r.RoleId == roleId));
        }

        var total = await staff.CountAsync(cancellationToken);
        var page = await staff
            .OrderBy(s => s.FullName)
            .ThenBy(s => s.Email)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var roleNames = await StaffRules.RoleNamesAsync(db, cancellationToken);
        return new PagedResult<StaffMemberDto>(page.Select(s => StaffRules.ToDto(s, roleNames)).ToList(), query.Page, query.PageSize, total);
    }
}

public sealed class GetStaffMemberHandler(IAppDbContext db) : IQueryHandler<GetStaffMember, StaffMemberDetailDto>
{
    public async Task<StaffMemberDetailDto> HandleAsync(GetStaffMember query, CancellationToken cancellationToken)
    {
        var staff = await StaffRules.LoadAsync(db, query.Id, cancellationToken);
        var permissions = await StaffPermissionResolver.ResolveAsync(db, staff, cancellationToken);
        return new StaffMemberDetailDto(StaffRules.ToDto(staff, await StaffRules.RoleNamesAsync(db, cancellationToken)), permissions);
    }
}

public sealed class InviteStaffHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    ITokenService secrets,
    ISecretHasher hasher,
    TimeProvider clock,
    IOptions<StaffAuthSettings> options) : ICommandHandler<InviteStaff, StaffInviteDto>
{
    public async Task<StaffInviteDto> HandleAsync(InviteStaff command, CancellationToken cancellationToken)
    {
        var actor = await StaffRules.ActorAsync(db, currentUser, cancellationToken);
        if (command.IsSuperAdmin)
        {
            StaffRules.EnsureSuperAdmin(actor);
        }

        var email = StaffUser.NormalizeEmail(command.Email);
        if (await db.StaffUsers.AnyAsync(s => s.Email == email, cancellationToken))
        {
            throw new ConflictException("A staff member with this email already exists.", "staff.email_taken");
        }

        // A random, URL-safe secret; only its hash is stored.
        var token = secrets.CreateRefreshToken();
        var expiresAt = clock.GetUtcNow().AddDays(options.Value.InviteLifetimeDays);
        var staff = StaffUser.Invite(email, command.FullName, hasher.Hash(token), expiresAt, command.IsSuperAdmin);
        foreach (var roleId in command.RoleIds.Distinct())
        {
            staff.AssignRole(roleId);
        }

        db.StaffUsers.Add(staff);
        await db.SaveChangesAsync(cancellationToken);
        return new StaffInviteDto(StaffRules.ToDto(staff, await StaffRules.RoleNamesAsync(db, cancellationToken)), token, expiresAt);
    }
}

public sealed class RenewStaffInviteHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    ITokenService secrets,
    ISecretHasher hasher,
    TimeProvider clock,
    IOptions<StaffAuthSettings> options) : ICommandHandler<RenewStaffInvite, StaffInviteDto>
{
    public async Task<StaffInviteDto> HandleAsync(RenewStaffInvite command, CancellationToken cancellationToken)
    {
        var actor = await StaffRules.ActorAsync(db, currentUser, cancellationToken);
        var staff = await StaffRules.LoadAsync(db, command.Id, cancellationToken);
        StaffRules.EnsureMayManage(actor, staff);

        var token = secrets.CreateRefreshToken();
        var expiresAt = clock.GetUtcNow().AddDays(options.Value.InviteLifetimeDays);
        staff.RenewInvite(hasher.Hash(token), expiresAt);
        await db.SaveChangesAsync(cancellationToken);
        return new StaffInviteDto(StaffRules.ToDto(staff, await StaffRules.RoleNamesAsync(db, cancellationToken)), token, expiresAt);
    }
}

public sealed class UpdateStaffMemberHandler(IAppDbContext db, ICurrentUser currentUser) : ICommandHandler<UpdateStaffMember, StaffMemberDto>
{
    public async Task<StaffMemberDto> HandleAsync(UpdateStaffMember command, CancellationToken cancellationToken)
    {
        var actor = await StaffRules.ActorAsync(db, currentUser, cancellationToken);
        var staff = await StaffRules.LoadAsync(db, command.Id, cancellationToken);
        StaffRules.EnsureMayManage(actor, staff);

        staff.Rename(command.FullName);
        var wanted = command.RoleIds.Distinct().ToList();
        foreach (var roleId in staff.RoleIds.Except(wanted).ToList())
        {
            staff.RemoveRole(roleId);
        }

        foreach (var roleId in wanted)
        {
            staff.AssignRole(roleId);
        }

        await db.SaveChangesAsync(cancellationToken);
        return StaffRules.ToDto(staff, await StaffRules.RoleNamesAsync(db, cancellationToken));
    }
}

public sealed class SetStaffSuspendedHandler(IAppDbContext db, ICurrentUser currentUser, SuperAdminGuard guard, TimeProvider clock)
    : ICommandHandler<SetStaffSuspended, StaffMemberDto>
{
    public async Task<StaffMemberDto> HandleAsync(SetStaffSuspended command, CancellationToken cancellationToken)
    {
        var actor = await StaffRules.ActorAsync(db, currentUser, cancellationToken);
        var staff = await StaffRules.LoadAsync(db, command.Id, cancellationToken);
        StaffRules.EnsureMayManage(actor, staff);

        if (command.Suspended)
        {
            if (staff.Id == actor.Id)
            {
                throw new DomainException("staff.cannot_suspend_self", "You can't suspend your own account.");
            }

            await guard.EnsureNotLastActiveSuperAdminAsync(staff.Id, cancellationToken);
            staff.Suspend();
            await StaffRules.EndSessionsAsync(db, staff.Id, clock.GetUtcNow(), cancellationToken);
        }
        else if (staff.IsSuspended)
        {
            staff.Activate();
        }

        await db.SaveChangesAsync(cancellationToken);
        return StaffRules.ToDto(staff, await StaffRules.RoleNamesAsync(db, cancellationToken));
    }
}

public sealed class SetStaffSuperAdminHandler(IAppDbContext db, ICurrentUser currentUser, SuperAdminGuard guard)
    : ICommandHandler<SetStaffSuperAdmin, StaffMemberDto>
{
    public async Task<StaffMemberDto> HandleAsync(SetStaffSuperAdmin command, CancellationToken cancellationToken)
    {
        var actor = await StaffRules.ActorAsync(db, currentUser, cancellationToken);
        StaffRules.EnsureSuperAdmin(actor);
        var staff = await StaffRules.LoadAsync(db, command.Id, cancellationToken);

        if (command.IsSuperAdmin)
        {
            staff.MakeSuperAdmin();
        }
        else
        {
            await guard.EnsureNotLastActiveSuperAdminAsync(staff.Id, cancellationToken);
            staff.RevokeSuperAdmin();
        }

        await db.SaveChangesAsync(cancellationToken);
        return StaffRules.ToDto(staff, await StaffRules.RoleNamesAsync(db, cancellationToken));
    }
}

public sealed class ResetStaffTwoFactorHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : ICommandHandler<ResetStaffTwoFactor, StaffMemberDto>
{
    public async Task<StaffMemberDto> HandleAsync(ResetStaffTwoFactor command, CancellationToken cancellationToken)
    {
        var actor = await StaffRules.ActorAsync(db, currentUser, cancellationToken);
        var staff = await StaffRules.LoadAsync(db, command.Id, cancellationToken);
        StaffRules.EnsureMayManage(actor, staff);

        staff.ResetTwoFactor();
        await StaffRules.EndSessionsAsync(db, staff.Id, clock.GetUtcNow(), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return StaffRules.ToDto(staff, await StaffRules.RoleNamesAsync(db, cancellationToken));
    }
}

public sealed class AcceptStaffInviteHandler(IAppDbContext db, IPasswordHasher passwords, ISecretHasher hasher, TimeProvider clock)
    : ICommandHandler<AcceptStaffInvite, bool>
{
    public async Task<bool> HandleAsync(AcceptStaffInvite command, CancellationToken cancellationToken)
    {
        var hash = hasher.Hash(command.Token);
        var staff = await db.StaffUsers.SingleOrDefaultAsync(s => s.InviteTokenHash == hash, cancellationToken)
            ?? throw new NotFoundException("This invitation link is not valid.", "staff.invite_invalid");

        staff.AcceptInvite(passwords.Hash(command.Password), clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

internal static class StaffRules
{
    public static bool IsValidEmail(string? email)
    {
        try
        {
            StaffUser.NormalizeEmail(email ?? string.Empty);
            return true;
        }
        catch (DomainException)
        {
            return false;
        }
    }

    public static IRuleBuilderOptions<T, string> ValidStaffName<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithErrorCode("name.required")
            .Must(name => name is null || name.Trim().Length <= StaffUser.FullNameMaxLength).WithErrorCode("name.too_long");

    public static IRuleBuilderOptions<T, IReadOnlyList<Guid>> ExistingRoles<T>(this IRuleBuilder<T, IReadOnlyList<Guid>> rule, IAppDbContext db) =>
        rule
            .NotNull().WithErrorCode("roles.required")
            .MustAsync(async (ids, ct) =>
            {
                if (ids is null)
                {
                    return true;
                }

                var wanted = ids.Distinct().ToList();
                return await db.Roles.CountAsync(r => wanted.Contains(r.Id), ct) == wanted.Count;
            })
            .WithErrorCode("roles.invalid");

    /// <summary>The staff member making the request, as stored now (their Super Admin flag decides what they may do).</summary>
    public static async Task<StaffUser> ActorAsync(IAppDbContext db, ICurrentUser currentUser, CancellationToken cancellationToken)
    {
        var actor = currentUser.IsStaff && Guid.TryParse(currentUser.UserId, out var id)
            ? await db.StaffUsers.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
            : null;
        return actor is { IsSuspended: false }
            ? actor
            : throw new UnauthorizedException("Please sign in again.", "session.invalid");
    }

    public static async Task<StaffUser> LoadAsync(IAppDbContext db, Guid id, CancellationToken cancellationToken) =>
        await db.StaffUsers.SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
        ?? throw new NotFoundException("Staff member not found.", "staff.not_found");

    public static void EnsureSuperAdmin(StaffUser actor)
    {
        if (!actor.IsSuperAdmin)
        {
            throw new ForbiddenException("Only a Super Admin can do this.", "staff.super_admin_only");
        }
    }

    /// <summary>Super Admin accounts can only be changed by Super Admins.</summary>
    public static void EnsureMayManage(StaffUser actor, StaffUser target)
    {
        if (target.IsSuperAdmin)
        {
            EnsureSuperAdmin(actor);
        }
    }

    /// <summary>Revokes their refresh tokens: they're signed out when the current access token expires (within 15 minutes).</summary>
    public static async Task EndSessionsAsync(IAppDbContext db, Guid staffId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var active = await db.StaffRefreshTokens.Where(t => t.StaffUserId == staffId && t.RevokedAt == null).ToListAsync(cancellationToken);
        active.ForEach(t => t.Revoke(now));
    }

    public static async Task<Dictionary<Guid, string>> RoleNamesAsync(IAppDbContext db, CancellationToken cancellationToken) =>
        await db.Roles.AsNoTracking().ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

    public static StaffMemberDto ToDto(StaffUser staff, IReadOnlyDictionary<Guid, string> roleNames) =>
        new(
            staff.Id,
            staff.Email,
            staff.FullName,
            staff.IsSuperAdmin,
            staff.Status.ToString(),
            staff.TwoFactorEnabled,
            staff.RoleIds
                .Where(roleNames.ContainsKey)
                .Select(id => new StaffRoleRefDto(id, roleNames[id]))
                .OrderBy(r => r.Name, StringComparer.Ordinal)
                .ToList(),
            staff.LastSignInAt,
            staff.InviteExpiresAt,
            staff.CreatedAt);
}
