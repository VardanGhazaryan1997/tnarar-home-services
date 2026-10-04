using HomeServices.Application.Identity;
using HomeServices.Domain.Staff;

namespace HomeServices.Application.Staff;

internal static class StaffSessionIssuer
{
    public static (StaffSession Session, StaffRefreshToken Stored) Issue(
        StaffUser staff,
        IReadOnlyList<string> permissions,
        IStaffTokenService tokens,
        ITokenService refreshTokens,
        ISecretHasher hasher,
        DateTimeOffset now,
        StaffAuthSettings settings)
    {
        var access = tokens.CreateAccessToken(staff, permissions);
        var rawRefresh = refreshTokens.CreateRefreshToken();
        var stored = StaffRefreshToken.Issue(staff.Id, hasher.Hash(rawRefresh), now, TimeSpan.FromHours(settings.RefreshTokenLifetimeHours));

        return (new StaffSession(access.Token, access.ExpiresAt, rawRefresh, stored.ExpiresAt, StaffProfileDto.From(staff, permissions)), stored);
    }
}
