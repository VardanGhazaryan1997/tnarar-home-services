using HomeServices.Application.Identity;
using HomeServices.Domain.Identity;
using HomeServices.Infrastructure.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HomeServices.Infrastructure.IntegrationTests.Identity;

public class IdentityServicesTests
{
    private static readonly JwtSettings Jwt = new() { SigningKey = new string('k', 32), AccessTokenLifetimeMinutes = 15 };

    [Fact]
    public async Task Access_tokens_are_signed_and_carry_the_user_id_phone_and_roles()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Register(PhoneNumber.Parse("+37491234567"));
        user.AddRole(UserRoles.Partner);
        var sut = new JwtTokenService(Options.Create(Jwt), new FixedClock(now));

        var token = sut.CreateAccessToken(user);

        token.ExpiresAt.ShouldBe(now.AddMinutes(15));
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, new TokenValidationParameters
        {
            ValidIssuer = Jwt.Issuer,
            ValidAudience = Jwt.Audience,
            IssuerSigningKey = JwtTokenService.SigningKey(Jwt),
        });
        result.IsValid.ShouldBeTrue();
        result.Claims["sub"].ShouldBe(user.Id.ToString());
        result.Claims[JwtTokenService.PhoneClaim].ShouldBe("+37491234567");
        var jwt = (JsonWebToken)result.SecurityToken;
        jwt.Claims.Where(c => c.Type == JwtTokenService.RoleClaim).Select(c => c.Value).ShouldBe(new[] { "Customer", "Partner" }, ignoreOrder: true);
    }

    [Fact]
    public async Task Tokens_signed_with_another_key_are_rejected()
    {
        var token = new JwtTokenService(Options.Create(Jwt), TimeProvider.System).CreateAccessToken(User.Register(PhoneNumber.Parse("+37491234567")));

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, new TokenValidationParameters
        {
            ValidIssuer = Jwt.Issuer,
            ValidAudience = Jwt.Audience,
            IssuerSigningKey = JwtTokenService.SigningKey(new JwtSettings { SigningKey = new string('x', 32) }),
        });

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Refresh_tokens_are_long_random_and_url_safe()
    {
        var sut = new JwtTokenService(Options.Create(Jwt), TimeProvider.System);

        var first = sut.CreateRefreshToken();
        var second = sut.CreateRefreshToken();

        first.Length.ShouldBeGreaterThanOrEqualTo(80);
        first.ShouldNotBe(second);
        first.ShouldNotContain("+");
        first.ShouldNotContain("/");
    }

    [Fact]
    public void Secrets_hash_the_same_way_every_time_but_differently_per_key()
    {
        var hasher = new HmacSecretHasher(Options.Create(new AuthSettings { SecretHashKey = new string('a', 32) }));
        var otherKey = new HmacSecretHasher(Options.Create(new AuthSettings { SecretHashKey = new string('b', 32) }));

        hasher.Hash("123456").ShouldBe(hasher.Hash("123456"));
        hasher.Hash("123456").ShouldNotBe(hasher.Hash("123457"));
        hasher.Hash("123456").ShouldNotBe(otherKey.Hash("123456"));
        hasher.Hash("123456").Length.ShouldBe(64);
    }

    [Fact]
    public void Sign_in_codes_are_six_random_digits()
    {
        var generator = new RandomOtpGenerator();

        var codes = Enumerable.Range(0, 50).Select(_ => generator.Generate()).ToList();

        codes.ShouldAllBe(c => c.Length == 6 && c.All(char.IsAsciiDigit));
        codes.Distinct().Count().ShouldBeGreaterThan(1);
    }

    [Fact]
    public async Task The_development_SMS_sender_keeps_messages_for_tests()
    {
        var sms = new FakeSmsSender(NullLogger<FakeSmsSender>.Instance);
        var phone = PhoneNumber.Parse("+37491234567");

        await sms.SendAsync(phone, "first", CancellationToken.None);
        await sms.SendAsync(phone, "second", CancellationToken.None);

        sms.Sent.Count.ShouldBe(2);
        sms.LastMessageTo(phone).ShouldBe("second");
        sms.LastMessageTo(PhoneNumber.Parse("+37499000000")).ShouldBeNull();
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
