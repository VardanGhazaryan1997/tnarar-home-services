using HomeServices.Application.Abstractions;
using HomeServices.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Identity;

/// <summary>Ends the session that owns this refresh token. Returns false when the token is unknown.</summary>
public sealed record EndSession(string RefreshToken) : ICommand<bool>;

public sealed class EndSessionHandler(IAppDbContext db, ISecretHasher hasher, TimeProvider clock) : ICommandHandler<EndSession, bool>
{
    public async Task<bool> HandleAsync(EndSession command, CancellationToken cancellationToken)
    {
        var hash = hasher.Hash(command.RefreshToken);
        var token = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (token is null)
        {
            return false;
        }

        token.Revoke(clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
