namespace HomeServices.Application.Identity;

/// <summary>Sign-in rules (configuration section "Auth").</summary>
public sealed class AuthSettings
{
    public const string SectionName = "Auth";

    public int OtpLifetimeMinutes { get; set; } = 5;

    public int OtpMaxAttempts { get; set; } = 5;

    public int OtpResendCooldownSeconds { get; set; } = 60;

    public int OtpMaxPerHour { get; set; } = 5;

    public int RefreshTokenLifetimeDays { get; set; } = 30;

    /// <summary>
    /// Staging only: every SMS sign-in code is this value (e.g. "111111") so testers need no SMS.
    /// Empty means random codes. The API refuses to start in Production with this set.
    /// </summary>
    public string? FixedOtpCode { get; set; }

    /// <summary>Key for hashing SMS codes and refresh tokens. Required; at least 32 characters.</summary>
    public string SecretHashKey { get; set; } = string.Empty;
}
