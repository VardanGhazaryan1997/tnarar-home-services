using HomeServices.Application.Errors;
using HomeServices.Application.Identity;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Identity;

public class VerifySignInCodeTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
    private static readonly PhoneNumber Phone = PhoneNumber.Parse("+37491234567");
    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly FakeClock _clock = new(Now);
    private readonly AuthSettings _settings = new();

    private VerifySignInCodeHandler Handler() =>
        new(_db, new FakeSecretHasher(), new FakeTokenService(_clock), _clock, Options.Create(_settings));

    private async Task GivenCodeSent(string code = "123456", DateTimeOffset? at = null)
    {
        _db.OtpCodes.Add(OtpCode.Issue(Phone, $"hash:{code}", at ?? Now, TimeSpan.FromMinutes(_settings.OtpLifetimeMinutes)));
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task First_sign_in_creates_a_customer_account_and_starts_a_session()
    {
        await GivenCodeSent();

        var session = await Handler().HandleAsync(new VerifySignInCode("091 234 567", "123456"), CancellationToken.None);

        var user = await _db.Users.SingleAsync();
        user.Phone.ShouldBe(Phone);
        user.LastSignInAt.ShouldBe(Now);
        session.IsNewUser.ShouldBeTrue();
        session.User.Id.ShouldBe(user.Id);
        session.User.Roles.ShouldBe(new[] { "Customer" });
        session.User.IsProfileComplete.ShouldBeFalse();
        session.AccessToken.ShouldBe($"access:{user.Id}");
        session.RefreshToken.ShouldBe("refresh-1");
        session.RefreshTokenExpiresAt.ShouldBe(Now.AddDays(_settings.RefreshTokenLifetimeDays));

        var stored = await _db.RefreshTokens.SingleAsync();
        stored.TokenHash.ShouldBe("hash:refresh-1");
        stored.UserId.ShouldBe(user.Id);
    }

    [Fact]
    public async Task Existing_users_sign_in_to_their_account()
    {
        var existing = User.Register(Phone);
        existing.UpdateProfile("Ani", null);
        _db.Users.Add(existing);
        await GivenCodeSent();

        var session = await Handler().HandleAsync(new VerifySignInCode("+37491234567", "123456"), CancellationToken.None);

        session.IsNewUser.ShouldBeFalse();
        session.User.FullName.ShouldBe("Ani");
        (await _db.Users.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task A_wrong_code_is_rejected_and_uses_up_an_attempt()
    {
        await GivenCodeSent();

        var exception = await Should.ThrowAsync<DomainException>(
            () => Handler().HandleAsync(new VerifySignInCode("+37491234567", "000000"), CancellationToken.None));

        exception.Code.ShouldBe("otp.invalid");
        (await _db.OtpCodes.SingleAsync()).Attempts.ShouldBe(1);
        (await _db.Users.AnyAsync()).ShouldBeFalse();
    }

    [Fact]
    public async Task Too_many_wrong_codes_lock_the_code()
    {
        await GivenCodeSent();
        for (var i = 0; i < _settings.OtpMaxAttempts; i++)
        {
            await Should.ThrowAsync<DomainException>(() => Handler().HandleAsync(new VerifySignInCode("+37491234567", "000000"), CancellationToken.None));
        }

        var exception = await Should.ThrowAsync<DomainException>(
            () => Handler().HandleAsync(new VerifySignInCode("+37491234567", "123456"), CancellationToken.None));

        exception.Code.ShouldBe("otp.too_many_attempts");
    }

    [Fact]
    public async Task An_expired_code_is_rejected()
    {
        await GivenCodeSent(at: Now.AddMinutes(-_settings.OtpLifetimeMinutes));

        var exception = await Should.ThrowAsync<DomainException>(
            () => Handler().HandleAsync(new VerifySignInCode("+37491234567", "123456"), CancellationToken.None));

        exception.Code.ShouldBe("otp.expired");
    }

    [Fact]
    public async Task Without_a_sent_code_verification_fails()
    {
        var exception = await Should.ThrowAsync<DomainException>(
            () => Handler().HandleAsync(new VerifySignInCode("+37491234567", "123456"), CancellationToken.None));

        exception.Code.ShouldBe("otp.invalid");
    }

    [Fact]
    public async Task Only_the_latest_code_counts()
    {
        await GivenCodeSent("111111", Now.AddMinutes(-2));
        await GivenCodeSent("222222", Now.AddMinutes(-1));

        await Should.ThrowAsync<DomainException>(() => Handler().HandleAsync(new VerifySignInCode("+37491234567", "111111"), CancellationToken.None));
        var session = await Handler().HandleAsync(new VerifySignInCode("+37491234567", "222222"), CancellationToken.None);

        session.AccessToken.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task A_used_code_cannot_be_used_again()
    {
        await GivenCodeSent();
        await Handler().HandleAsync(new VerifySignInCode("+37491234567", "123456"), CancellationToken.None);

        var exception = await Should.ThrowAsync<DomainException>(
            () => Handler().HandleAsync(new VerifySignInCode("+37491234567", "123456"), CancellationToken.None));

        exception.Code.ShouldBe("otp.invalid");
    }

    [Fact]
    public async Task Blocked_users_cannot_sign_in()
    {
        var blocked = User.Register(Phone);
        blocked.Block();
        _db.Users.Add(blocked);
        await GivenCodeSent();

        var exception = await Should.ThrowAsync<ForbiddenException>(
            () => Handler().HandleAsync(new VerifySignInCode("+37491234567", "123456"), CancellationToken.None));

        exception.Code.ShouldBe("user.blocked");
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("abcdef")]
    [InlineData("")]
    public void The_code_must_be_six_digits(string code)
    {
        new VerifySignInCodeValidator().Validate(new VerifySignInCode("+37491234567", code))
            .Errors.ShouldContain(e => e.ErrorCode == "otp.code_format");
    }

    [Fact]
    public void The_phone_number_is_validated()
    {
        new VerifySignInCodeValidator().Validate(new VerifySignInCode("12", "123456"))
            .Errors.ShouldContain(e => e.ErrorCode == "phone.invalid");
    }
}
