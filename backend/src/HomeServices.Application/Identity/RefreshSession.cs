using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Identity;

/// <summary>
/// Exchanges a refresh token for a new session (rotation). Presenting a token that was
/// already replaced means it was copied: every session of that user is ended.
/// </summary>
public sealed record RefreshSession(string RefreshToken) : ICommand<AuthSession>;

public sealed class RefreshSessionHandler(
    IAppDbContext db,
    ISecretHasher hasher,
    ITokenService tokens,
    TimeProvider clock,
    IOptions<AuthSettings> options) : ICommandHandler<RefreshSession, AuthSession>
{
    public async Task<AuthSession> HandleAsync(RefreshSession command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var hash = hasher.Hash(command.RefreshToken);

        var token = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
            ?? throw new UnauthorizedException("Please sign in again.", "session.invalid");

        if (token.IsRevoked)
        {
            var active = await db.RefreshTokens.Where(t => t.UserId == token.UserId && t.RevokedAt == null).ToListAsync(cancellationToken);
            active.ForEach(t => t.Revoke(now));
            await db.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("This session was ended. Please sign in again.", "session.revoked");
        }

        if (!token.IsActive(now))
        {
            throw new UnauthorizedException("Your session has expired. Please sign in again.", "session.expired");
        }

        var user = await db.Users.SingleAsync(u => u.Id == token.UserId, cancellationToken);
        if (user.IsBlocked)
        {
            throw new ForbiddenException("This account is blocked.", "user.blocked");
        }

        var (session, replacement) = SessionIssuer.Issue(user, isNewUser: false, tokens, hasher, now, options.Value);
        token.Revoke(now, replacement.TokenHash);
        db.RefreshTokens.Add(replacement);
        await db.SaveChangesAsync(cancellationToken);

        return session;
    }
}
