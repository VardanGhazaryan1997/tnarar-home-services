using System.Text;
using HomeServices.Domain.Staff;
using HomeServices.Infrastructure.Identity;
using HomeServices.Infrastructure.Staff;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HomeServices.Infrastructure.IntegrationTests.Staff;

public class StaffServicesTests
{
    // RFC 6238 test secret "12345678901234567890" (ASCII) in Base32.
    private const string RfcSecret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
    private static readonly JwtSettings Jwt = new() { SigningKey = new string('k', 32) };

    [Theory]
    [InlineData(59, "287082")]
    [InlineData(1111111109, "081804")]
    [InlineData(1111111111, "050471")]
    [InlineData(1234567890, "005924")]
    [InlineData(2000000000, "279037")]
    public void Codes_match_the_RFC_6238_test_vectors(long unixSeconds, string expected)
    {
        new TotpService().GenerateCode(RfcSecret, DateTimeOffset.FromUnixTimeSeconds(unixSeconds)).ShouldBe(expected);
    }

    [Fact]
    public void Accepts_the_current_code_and_one_step_either_side_for_clock_drift()
    {
        var sut = new TotpService();
        var now = DateTimeOffset.FromUnixTimeSeconds(1234567890);
        var step = TotpService.TimeStep(now);

        sut.Verify(RfcSecret, sut.GenerateCode(RfcSecret, now), now).ShouldBe(step);
        sut.Verify(RfcSecret, sut.GenerateCode(RfcSecret, now.AddSeconds(-30)), now).ShouldBe(step - 1);
        sut.Verify(RfcSecret, sut.GenerateCode(RfcSecret, now.AddSeconds(30)), now).ShouldBe(step + 1);
        sut.Verify(RfcSecret, sut.GenerateCode(RfcSecret, now.AddSeconds(90)), now).ShouldBeNull();
        sut.Verify(RfcSecret, "000000", now).ShouldBeNull();
    }

    [Fact]
    public void Generates_random_Base32_secrets_that_round_trip()
    {
        var sut = new TotpService();

        var secret = sut.GenerateSecret();

        secret.Length.ShouldBe(32);
        secret.ShouldNotBe(sut.GenerateSecret());
        TotpService.ToBase32(TotpService.FromBase32(secret)).ShouldBe(secret);
        TotpService.ToBase32(Encoding.ASCII.GetBytes("12345678901234567890")).ShouldBe(RfcSecret);
        TotpService.ToBase32([0xFF]).ShouldBe("74");
        TotpService.FromBase32("gezd gnbv====").ShouldBe(Encoding.ASCII.GetBytes("12345"));
    }

    [Fact]
    public void Rejects_secrets_that_are_not_Base32()
    {
        Should.Throw<FormatException>(() => TotpService.FromBase32("ABC1"));
    }

    [Fact]
    public void Builds_an_otpauth_link_for_authenticator_apps()
    {
        var uri = new TotpService().BuildProvisioningUri("admin@example.com", "SECRET");

        uri.ShouldStartWith("otpauth://totp/Home%20Services%3Aadmin%40example.com?secret=SECRET");
        uri.ShouldContain("issuer=Home%20Services");
        uri.ShouldContain("period=30");
    }

    [Fact]
    public void Passwords_are_hashed_with_a_salt_and_verified()
    {
        var sut = new AspNetPasswordHasher();

        var hash = sut.Hash("Correct-Horse-Battery");

        hash.ShouldNotContain("Correct-Horse-Battery");
        sut.Hash("Correct-Horse-Battery").ShouldNotBe(hash);
        sut.Verify(hash, "Correct-Horse-Battery").ShouldBeTrue();
        sut.Verify(hash, "wrong").ShouldBeFalse();
    }

    [Fact]
    public async Task Staff_access_tokens_use_their_own_audience_and_carry_staff_claims()
    {
        var staff = StaffUser.Create("admin@example.com", "Vardan", "hash", isSuperAdmin: true);
        var token = new StaffTokenService(Options.Create(Jwt), TimeProvider.System)
            .CreateAccessToken(staff, [Permissions.PartnersApprove, Permissions.OrdersView]);

        var asStaff = await Validate(token.Token, Jwt.StaffAudience);
        var asPortal = await Validate(token.Token, Jwt.Audience);

        asStaff.IsValid.ShouldBeTrue();
        asStaff.Claims["sub"].ShouldBe(staff.Id.ToString());
        asStaff.Claims[StaffTokenService.StaffClaim].ShouldBe("true");
        asStaff.Claims[StaffTokenService.SuperAdminClaim].ShouldBe("true");
        asPortal.IsValid.ShouldBeFalse();
        ((JsonWebToken)asStaff.SecurityToken).Claims
            .Where(c => c.Type == StaffTokenService.PermissionClaim)
            .Select(c => c.Value)
            .ShouldBe(new[] { Permissions.PartnersApprove, Permissions.OrdersView }, ignoreOrder: true);
    }

    [Fact]
    public async Task Regular_staff_tokens_have_no_super_admin_claim()
    {
        var staff = StaffUser.Create("operator@example.com", "Ani", "hash");
        var token = new StaffTokenService(Options.Create(Jwt), TimeProvider.System).CreateAccessToken(staff, []);

        (await Validate(token.Token, Jwt.StaffAudience)).Claims.ContainsKey(StaffTokenService.SuperAdminClaim).ShouldBeFalse();
    }

    [Fact]
    public async Task Challenge_tokens_carry_the_staff_id_until_they_expire()
    {
        var clock = new MovableClock(DateTimeOffset.UtcNow);
        var sut = new StaffTokenService(Options.Create(Jwt), clock);
        var staffId = Guid.NewGuid();

        var challenge = sut.CreateChallengeToken(staffId);

        (await sut.ReadChallengeTokenAsync(challenge)).ShouldBe(staffId);
        clock.Now = clock.Now.AddMinutes(Jwt.StaffChallengeLifetimeMinutes + 1);
        (await sut.ReadChallengeTokenAsync(challenge)).ShouldBeNull();
    }

    [Fact]
    public async Task Access_tokens_and_garbage_are_not_accepted_as_challenges()
    {
        var sut = new StaffTokenService(Options.Create(Jwt), TimeProvider.System);
        var accessToken = sut.CreateAccessToken(StaffUser.Create("a@example.com", "A", "h"), []).Token;

        (await sut.ReadChallengeTokenAsync(accessToken)).ShouldBeNull();
        (await sut.ReadChallengeTokenAsync("garbage")).ShouldBeNull();
    }

    private static Task<TokenValidationResult> Validate(string token, string audience) =>
        new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Jwt.Issuer,
            ValidAudience = audience,
            IssuerSigningKey = JwtTokenService.SigningKey(Jwt),
        });

    private sealed class MovableClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
