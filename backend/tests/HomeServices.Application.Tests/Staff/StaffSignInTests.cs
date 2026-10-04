using HomeServices.Application.Errors;
using HomeServices.Application.Staff;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Staff;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Staff;

public class StaffSignInTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly FakeClock _clock = new(Now);
    private readonly StaffAuthSettings _settings = new() { MaxFailedSignIns = 3, LockoutMinutes = 15 };

    private StaffSignInHandler Handler() =>
        new(_db, new FakePasswordHasher(), new FakeTotpService(), new FakeStaffTokenService(_clock), _clock, Options.Create(_settings));

    private async Task<StaffUser> GivenStaff(bool twoFactor = false, bool suspended = false)
    {
        var staff = StaffUser.Create("admin@example.com", "Vardan", "pw:Correct-Horse-1", isSuperAdmin: true);
        if (twoFactor)
        {
            staff.BeginTwoFactorSetup("SECRET");
            staff.CompleteTwoFactorSetup(1);
        }

        if (suspended)
        {
            staff.Suspend();
        }

        _db.StaffUsers.Add(staff);
        await _db.SaveChangesAsync();
        return staff;
    }

    [Fact]
    public async Task First_sign_in_asks_to_set_up_the_authenticator_app()
    {
        var staff = await GivenStaff();

        var result = await Handler().HandleAsync(new StaffSignIn(" Admin@Example.com ", "Correct-Horse-1"), CancellationToken.None);

        result.Status.ShouldBe(StaffSignInStatus.TwoFactorSetupRequired);
        result.ChallengeToken.ShouldBe($"challenge:{staff.Id}");
        result.SetupSecret.ShouldBe(FakeTotpService.Secret);
        result.SetupUri!.ShouldStartWith("otpauth://totp/");
        staff.PendingTotpSecret.ShouldBe(FakeTotpService.Secret);
    }

    [Fact]
    public async Task Signing_in_again_before_finishing_setup_shows_the_same_secret()
    {
        var staff = await GivenStaff();
        staff.BeginTwoFactorSetup("EXISTING");
        await _db.SaveChangesAsync();

        var result = await Handler().HandleAsync(new StaffSignIn("admin@example.com", "Correct-Horse-1"), CancellationToken.None);

        result.SetupSecret.ShouldBe("EXISTING");
    }

    [Fact]
    public async Task With_two_factor_enabled_it_asks_for_the_code()
    {
        await GivenStaff(twoFactor: true);

        var result = await Handler().HandleAsync(new StaffSignIn("admin@example.com", "Correct-Horse-1"), CancellationToken.None);

        result.Status.ShouldBe(StaffSignInStatus.TwoFactorRequired);
        result.SetupSecret.ShouldBeNull();
        result.SetupUri.ShouldBeNull();
    }

    [Theory]
    [InlineData("nobody@example.com", "Correct-Horse-1")]
    [InlineData("admin@example.com", "wrong")]
    public async Task Unknown_email_and_wrong_password_get_the_same_answer(string email, string password)
    {
        await GivenStaff();

        var exception = await Should.ThrowAsync<UnauthorizedException>(
            () => Handler().HandleAsync(new StaffSignIn(email, password), CancellationToken.None));

        exception.Code.ShouldBe("staff.invalid_credentials");
    }

    [Fact]
    public async Task Too_many_wrong_passwords_lock_the_account_even_for_the_right_password()
    {
        var staff = await GivenStaff();
        for (var i = 0; i < _settings.MaxFailedSignIns; i++)
        {
            await Should.ThrowAsync<UnauthorizedException>(() => Handler().HandleAsync(new StaffSignIn("admin@example.com", "wrong"), CancellationToken.None));
        }

        var exception = await Should.ThrowAsync<TooManyRequestsException>(
            () => Handler().HandleAsync(new StaffSignIn("admin@example.com", "Correct-Horse-1"), CancellationToken.None));

        exception.Code.ShouldBe("staff.locked_out");
        staff.LockedUntil.ShouldBe(Now.AddMinutes(15));
    }

    [Fact]
    public async Task The_lock_lifts_after_the_lockout_period()
    {
        await GivenStaff();
        for (var i = 0; i < _settings.MaxFailedSignIns; i++)
        {
            await Should.ThrowAsync<UnauthorizedException>(() => Handler().HandleAsync(new StaffSignIn("admin@example.com", "wrong"), CancellationToken.None));
        }

        _clock.Now = Now.AddMinutes(16);
        var result = await Handler().HandleAsync(new StaffSignIn("admin@example.com", "Correct-Horse-1"), CancellationToken.None);

        result.Status.ShouldBe(StaffSignInStatus.TwoFactorSetupRequired);
    }

    [Fact]
    public async Task Suspended_staff_cannot_sign_in()
    {
        await GivenStaff(suspended: true);

        var exception = await Should.ThrowAsync<ForbiddenException>(
            () => Handler().HandleAsync(new StaffSignIn("admin@example.com", "Correct-Horse-1"), CancellationToken.None));

        exception.Code.ShouldBe("staff.suspended");
    }

    [Fact]
    public void Email_and_password_are_required()
    {
        var errors = new StaffSignInValidator().Validate(new StaffSignIn("", "")).Errors;

        errors.ShouldContain(e => e.ErrorCode == "email.required");
        errors.ShouldContain(e => e.ErrorCode == "password.required");
    }
}
