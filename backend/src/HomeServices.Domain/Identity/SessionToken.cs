using HomeServices.Domain.Common;

namespace HomeServices.Domain.Identity;

/// <summary>
/// Shared behaviour of long-lived sign-in sessions (Portal users and staff). Each use replaces
/// the token with a new one (rotation); only the hash is stored.
/// </summary>
public abstract class SessionToken : Entity
{
    public string TokenHash { get; protected set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; protected set; }

    public DateTimeOffset ExpiresAt { get; protected set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public string? ReplacedByHash { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public bool IsActive(DateTimeOffset now) => !IsRevoked && now < ExpiresAt;

    public void Revoke(DateTimeOffset now, string? replacedByHash = null)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAt = now;
        ReplacedByHash = replacedByHash;
    }
}
