using HomeServices.Domain.Identity;

namespace HomeServices.Application.Identity;

internal static class SessionIssuer
{
    /// <summary>Creates an access token and a stored (hashed) refresh token for <paramref name="user"/>.</summary>
    public static (AuthSession Session, RefreshToken Stored) Issue(
        User user,
        bool isNewUser,
        ITokenService tokens,
        ISecretHasher hasher,
        DateTimeOffset now,
        AuthSettings settings)
    {
        var access = tokens.CreateAccessToken(user);
        var rawRefresh = tokens.CreateRefreshToken();
        var stored = RefreshToken.Issue(user.Id, hasher.Hash(rawRefresh), now, TimeSpan.FromDays(settings.RefreshTokenLifetimeDays));

        var session = new AuthSession(access.Token, access.ExpiresAt, rawRefresh, stored.ExpiresAt, MyProfileDto.From(user), isNewUser);
        return (session, stored);
    }
}
