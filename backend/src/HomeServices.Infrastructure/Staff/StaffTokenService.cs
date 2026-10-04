using System.Security.Claims;
using HomeServices.Application.Staff;
using HomeServices.Domain.Staff;
using HomeServices.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HomeServices.Infrastructure.Staff;

/// <summary>Back Office access tokens (own audience) and the 5-minute token between the password and 2FA steps.</summary>
public sealed class StaffTokenService(IOptions<JwtSettings> options, TimeProvider clock) : IStaffTokenService
{
    public const string StaffClaim = "staff";
    public const string SuperAdminClaim = "super_admin";
    public const string PermissionClaim = "perm";
    private const string ChallengeAudience = "home-services-staff-2fa";

    public StaffAccessToken CreateAccessToken(StaffUser staff, IReadOnlyCollection<string> permissions)
    {
        var settings = options.Value;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, staff.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, staff.Email),
            new(StaffClaim, "true"),
        };
        if (staff.IsSuperAdmin)
        {
            claims.Add(new Claim(SuperAdminClaim, "true"));
        }

        claims.AddRange(permissions.Select(permission => new Claim(PermissionClaim, permission)));

        var expires = clock.GetUtcNow().AddMinutes(settings.StaffAccessTokenLifetimeMinutes);
        return new StaffAccessToken(Create(settings.StaffAudience, claims, expires), expires);
    }

    public string CreateChallengeToken(Guid staffUserId) =>
        Create(
            ChallengeAudience,
            [new Claim(JwtRegisteredClaimNames.Sub, staffUserId.ToString())],
            clock.GetUtcNow().AddMinutes(options.Value.StaffChallengeLifetimeMinutes));

    public async Task<Guid?> ReadChallengeTokenAsync(string challengeToken)
    {
        var settings = options.Value;
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(challengeToken, new TokenValidationParameters
        {
            ValidIssuer = settings.Issuer,
            ValidAudience = ChallengeAudience,
            IssuerSigningKey = JwtTokenService.SigningKey(settings),
            LifetimeValidator = (_, expires, _, _) => expires > clock.GetUtcNow().UtcDateTime,
            ClockSkew = TimeSpan.Zero,
        });

        return result.IsValid && result.Claims.TryGetValue(JwtRegisteredClaimNames.Sub, out var sub) && Guid.TryParse(sub as string, out var id)
            ? id
            : null;
    }

    private string Create(string audience, IEnumerable<Claim> claims, DateTimeOffset expires)
    {
        var settings = options.Value;
        var now = clock.GetUtcNow();
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(JwtTokenService.SigningKey(settings), SecurityAlgorithms.HmacSha256),
        });
    }
}
