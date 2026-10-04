using HomeServices.Application.Errors;
using HomeServices.Application.Staff;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Staff;

public class StaffTwoFactorTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly FakeClock _clock = new(Now);
    private readonly StaffAuthSettings _settings = new();

    private StaffTwoFactorSteps Steps() =>
        new(_db, new FakeTotpService(), new FakeStaffTokenService(_clock), new FakeTokenService(_clock), new FakeSecretHasher(), _clock, Options.Create(_settings));

    private async Task<StaffUser> GivenStaff(bool setupStarted = true, bool enabled = false)
    {
        var staff = StaffUser.Create("admin@example.com", "Vardan", "pw:x");
        if (setupStarted || enabled)
        {
            staff.BeginTwoFactorSetup(FakeTotpService.Secret);
        }

        if (enabled)
        {
            staff.CompleteTwoFactorSetup(timeStep: 0);
        }

        _db.StaffUsers.Add(staff);
        await _db.SaveChangesAsync();
        return staff;
    }

    [Fact]
    public async Task Confirming_setup_with_a_valid_code_enables_two_factor_and_starts_a_session()
    {
        var staff = await GivenStaff();

        var session = await new CompleteStaffTwoFactorSetupHandler(Steps())
            .HandleAsync(new CompleteStaffTwoFactorSetup($"challenge:{staff.Id}", FakeTotpService.ValidCode), CancellationToken.None);

        staff.TwoFactorEnabled.ShouldBeTrue();
        staff.LastSignInAt.ShouldBe(Now);
        session.AccessToken.ShouldBe($"staff-access:{staff.Id}");
        session.RefreshToken.ShouldBe("refresh-1");
        session.RefreshTokenExpiresAt.ShouldBe(Now.AddHours(_settings.RefreshTokenLifetimeHours));
        session.Staff.Email.ShouldBe("admin@example.com");
        (await _db.StaffRefreshTokens.SingleAsync()).TokenHash.ShouldBe("hash:refresh-1");
    }

    [Fact]
    public async Task A_wrong_code_does_not_enable_two_factor()
    {
        var staff = await GivenStaff();

        var exception = await Should.ThrowAsync<DomainException>(() => new CompleteStaffTwoFactorSetupHandler(Steps())
            .HandleAsync(new CompleteStaffTwoFactorSetup($"challenge:{staff.Id}", "999999"), CancellationToken.None));

        exception.Code.ShouldBe("staff.totp_invalid");
        staff.TwoFactorEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task Setup_must_have_been_started()
    {
        var staff = await GivenStaff(setupStarted: false);

        var exception = await Should.ThrowAsync<DomainException>(() => new CompleteStaffTwoFactorSetupHandler(Steps())
            .HandleAsync(new CompleteStaffTwoFactorSetup($"challenge:{staff.Id}", FakeTotpService.ValidCode), CancellationToken.None));

        exception.Code.ShouldBe("staff.two_factor_setup_not_started");
    }

    [Fact]
    public async Task With_two_factor_enabled_a_valid_code_starts_a_session()
    {
        var staff = await GivenStaff(enabled: true);

        var session = await new VerifyStaffTwoFactorHandler(Steps())
            .HandleAsync(new VerifyStaffTwoFactor($"challenge:{staff.Id}", FakeTotpService.ValidCode), CancellationToken.None);

        session.Staff.Id.ShouldBe(staff.Id);
        staff.LastTotpTimeStep.ShouldBe(Now.ToUnixTimeSeconds() / 30);
    }

    [Fact]
    public async Task The_same_code_cannot_be_used_twice()
    {
        var staff = await GivenStaff(enabled: true);
        await new VerifyStaffTwoFactorHandler(Steps()).HandleAsync(new VerifyStaffTwoFactor($"challenge:{staff.Id}", FakeTotpService.ValidCode), CancellationToken.None);

        var exception = await Should.ThrowAsync<DomainException>(() => new VerifyStaffTwoFactorHandler(Steps())
            .HandleAsync(new VerifyStaffTwoFactor($"challenge:{staff.Id}", FakeTotpService.ValidCode), CancellationToken.None));

        exception.Code.ShouldBe("staff.totp_already_used");
    }

    [Fact]
    public async Task Verifying_requires_two_factor_to_be_set_up()
    {
        var staff = await GivenStaff();

        var exception = await Should.ThrowAsync<DomainException>(() => new VerifyStaffTwoFactorHandler(Steps())
            .HandleAsync(new VerifyStaffTwoFactor($"challenge:{staff.Id}", FakeTotpService.ValidCode), CancellationToken.None));

        exception.Code.ShouldBe("staff.two_factor_not_enabled");
    }

    [Theory]
    [InlineData("not-a-challenge")]
    [InlineData("challenge:00000000-0000-0000-0000-000000000001")]
    public async Task An_invalid_challenge_means_signing_in_again(string challenge)
    {
        await GivenStaff(enabled: true);

        var exception = await Should.ThrowAsync<UnauthorizedException>(() => new VerifyStaffTwoFactorHandler(Steps())
            .HandleAsync(new VerifyStaffTwoFactor(challenge, FakeTotpService.ValidCode), CancellationToken.None));

        exception.Code.ShouldBe("staff.challenge_invalid");
    }

    [Fact]
    public async Task Staff_suspended_between_the_two_steps_are_refused()
    {
        var staff = await GivenStaff(enabled: true);
        staff.Suspend();
        await _db.SaveChangesAsync();

        var exception = await Should.ThrowAsync<ForbiddenException>(() => new VerifyStaffTwoFactorHandler(Steps())
            .HandleAsync(new VerifyStaffTwoFactor($"challenge:{staff.Id}", FakeTotpService.ValidCode), CancellationToken.None));

        exception.Code.ShouldBe("staff.suspended");
    }

    [Fact]
    public void Codes_must_be_six_digits_and_the_challenge_present()
    {
        new VerifyStaffTwoFactorValidator().Validate(new VerifyStaffTwoFactor("", "12"))
            .Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "challenge.required", "totp.code_format" }, ignoreOrder: true);
        new CompleteStaffTwoFactorSetupValidator().Validate(new CompleteStaffTwoFactorSetup("", "abcdef"))
            .Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "challenge.required", "totp.code_format" }, ignoreOrder: true);
    }
}
