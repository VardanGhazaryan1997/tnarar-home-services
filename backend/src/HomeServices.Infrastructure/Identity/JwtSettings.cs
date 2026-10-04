namespace HomeServices.Infrastructure.Identity;

/// <summary>Access-token settings (configuration section "Jwt").</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "home-services";

    public string Audience { get; set; } = "home-services-portal";

    /// <summary>HMAC signing key. Required; at least 32 characters. Never commit a production key.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    /// <summary>Audience of Back Office tokens; Portal tokens are not accepted there, and vice versa.</summary>
    public string StaffAudience { get; set; } = "home-services-backoffice";

    public int StaffAccessTokenLifetimeMinutes { get; set; } = 15;

    /// <summary>How long a staff member has to enter the authenticator code after the password.</summary>
    public int StaffChallengeLifetimeMinutes { get; set; } = 5;
}
