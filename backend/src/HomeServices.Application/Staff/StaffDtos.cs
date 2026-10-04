using HomeServices.Domain.Staff;

namespace HomeServices.Application.Staff;

public sealed record StaffProfileDto(Guid Id, string Email, string FullName, bool IsSuperAdmin, IReadOnlyList<string> Permissions)
{
    public static StaffProfileDto From(StaffUser staff, IReadOnlyList<string> permissions) =>
        new(staff.Id, staff.Email, staff.FullName, staff.IsSuperAdmin, permissions);
}

public static class StaffSignInStatus
{
    public const string TwoFactorRequired = "two_factor_required";
    public const string TwoFactorSetupRequired = "two_factor_setup_required";
}

/// <summary>
/// Result of the password step. The client sends <see cref="ChallengeToken"/> with the authenticator code.
/// On first sign-in it also gets the secret / QR link to set up the authenticator app.
/// </summary>
public sealed record StaffSignInResult(string Status, string ChallengeToken, string? SetupSecret, string? SetupUri);

public sealed record StaffSession(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    StaffProfileDto Staff);
