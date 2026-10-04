using HomeServices.Domain.Identity;

namespace HomeServices.Application.Identity;

public sealed record MyProfileDto(
    Guid Id,
    string PhoneNumber,
    string? FullName,
    string? Email,
    IReadOnlyList<string> Roles,
    bool IsProfileComplete)
{
    public static MyProfileDto From(User user) =>
        new(user.Id, user.Phone.Value, user.FullName, user.Email, user.RoleNames, user.IsProfileComplete);
}

/// <summary>Result of a successful sign-in or refresh. The API puts the refresh token in an httpOnly cookie.</summary>
public sealed record AuthSession(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    MyProfileDto User,
    bool IsNewUser);
