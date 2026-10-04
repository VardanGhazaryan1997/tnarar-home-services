namespace HomeServices.Domain.Identity;

/// <summary>A Portal user's sign-in session.</summary>
public sealed class RefreshToken : SessionToken
{
    private RefreshToken()
    {
    }

    public Guid UserId { get; private set; }

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTimeOffset now, TimeSpan lifetime) =>
        new() { UserId = userId, TokenHash = tokenHash, CreatedAt = now, ExpiresAt = now + lifetime };
}
