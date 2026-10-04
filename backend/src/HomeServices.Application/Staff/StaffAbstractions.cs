using HomeServices.Domain.Staff;

namespace HomeServices.Application.Staff;

/// <summary>Slow, salted password hashing (ASP.NET Core Identity's PBKDF2 in Infrastructure).</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string hash, string password);
}

/// <summary>Authenticator-app codes (RFC 6238: 6 digits, 30-second steps).</summary>
public interface ITotpService
{
    /// <summary>A new random Base32 secret for an authenticator app.</summary>
    string GenerateSecret();

    /// <summary>otpauth:// link the Back Office shows as a QR code.</summary>
    string BuildProvisioningUri(string accountName, string secret);

    /// <summary>The matching time step if <paramref name="code"/> is valid now (±1 step for clock drift); otherwise null.</summary>
    long? Verify(string secret, string code, DateTimeOffset now);
}

/// <summary>Staff access tokens and the short-lived token that links the password step to the 2FA step.</summary>
public interface IStaffTokenService
{
    /// <summary>Access token carrying the staff member's effective permissions (applied again at each refresh).</summary>
    StaffAccessToken CreateAccessToken(StaffUser staff, IReadOnlyCollection<string> permissions);

    string CreateChallengeToken(Guid staffUserId);

    /// <summary>The staff id inside a valid, unexpired challenge token; otherwise null.</summary>
    Task<Guid?> ReadChallengeTokenAsync(string challengeToken);
}

public sealed record StaffAccessToken(string Token, DateTimeOffset ExpiresAt);
