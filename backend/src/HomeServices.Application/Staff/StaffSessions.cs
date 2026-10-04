using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Identity;
using HomeServices.Application.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Staff;

/// <summary>Exchanges a staff refresh token for a new session (rotation; reuse ends every session of that staff member).</summary>
public sealed record RefreshStaffSession(string RefreshToken) : ICommand<StaffSession>;

/// <summary>Ends the staff session that owns this refresh token.</summary>
public sealed record EndStaffSession(string RefreshToken) : ICommand<bool>;

public sealed record GetStaffProfile : IQuery<StaffProfileDto>;

public sealed class RefreshStaffSessionHandler(
    IAppDbContext db,
    IStaffTokenService tokens,
    ITokenService refreshTokens,
    ISecretHasher hasher,
    TimeProvider clock,
    IOptions<StaffAuthSettings> options) : ICommandHandler<RefreshStaffSession, StaffSession>
{
    public async Task<StaffSession> HandleAsync(RefreshStaffSession command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var hash = hasher.Hash(command.RefreshToken);

        var token = await db.StaffRefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
            ?? throw new UnauthorizedException("Please sign in again.", "session.invalid");

        if (token.IsRevoked)
        {
            var active = await db.StaffRefreshTokens.Where(t => t.StaffUserId == token.StaffUserId && t.RevokedAt == null).ToListAsync(cancellationToken);
            active.ForEach(t => t.Revoke(now));
            await db.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("This session was ended. Please sign in again.", "session.revoked");
        }

        if (!token.IsActive(now))
        {
            throw new UnauthorizedException("Your session has expired. Please sign in again.", "session.expired");
        }

        var staff = await db.StaffUsers.SingleAsync(s => s.Id == token.StaffUserId, cancellationToken);
        if (staff.IsSuspended)
        {
            throw new ForbiddenException("This account is suspended.", "staff.suspended");
        }

        var permissions = await StaffPermissionResolver.ResolveAsync(db, staff, cancellationToken);
        var (session, replacement) = StaffSessionIssuer.Issue(staff, permissions, tokens, refreshTokens, hasher, now, options.Value);
        token.Revoke(now, replacement.TokenHash);
        db.StaffRefreshTokens.Add(replacement);
        await db.SaveChangesAsync(cancellationToken);
        return session;
    }
}

public sealed class EndStaffSessionHandler(IAppDbContext db, ISecretHasher hasher, TimeProvider clock) : ICommandHandler<EndStaffSession, bool>
{
    public async Task<bool> HandleAsync(EndStaffSession command, CancellationToken cancellationToken)
    {
        var hash = hasher.Hash(command.RefreshToken);
        var token = await db.StaffRefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (token is null)
        {
            return false;
        }

        token.Revoke(clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class GetStaffProfileHandler(IAppDbContext db, ICurrentUser currentUser) : IQueryHandler<GetStaffProfile, StaffProfileDto>
{
    public async Task<StaffProfileDto> HandleAsync(GetStaffProfile query, CancellationToken cancellationToken)
    {
        var staff = Guid.TryParse(currentUser.UserId, out var id)
            ? await db.StaffUsers.SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
            : null;

        return staff is null
            ? throw new UnauthorizedException("Please sign in.")
            : StaffProfileDto.From(staff, await StaffPermissionResolver.ResolveAsync(db, staff, cancellationToken));
    }
}
