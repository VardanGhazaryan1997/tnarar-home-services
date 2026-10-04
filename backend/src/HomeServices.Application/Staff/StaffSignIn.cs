using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Staff;

/// <summary>Step 1 of Back Office sign-in: email + password.</summary>
public sealed record StaffSignIn(string Email, string Password) : ICommand<StaffSignInResult>;

public sealed class StaffSignInValidator : AbstractValidator<StaffSignIn>
{
    public StaffSignInValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithErrorCode("email.required");
        RuleFor(x => x.Password).NotEmpty().WithErrorCode("password.required");
    }
}

public sealed class StaffSignInHandler(
    IAppDbContext db,
    IPasswordHasher passwords,
    ITotpService totp,
    IStaffTokenService tokens,
    TimeProvider clock,
    IOptions<StaffAuthSettings> options) : ICommandHandler<StaffSignIn, StaffSignInResult>
{
    public async Task<StaffSignInResult> HandleAsync(StaffSignIn command, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var now = clock.GetUtcNow();
        var email = command.Email.Trim().ToLowerInvariant();

        var staff = await db.StaffUsers.SingleOrDefaultAsync(s => s.Email == email, cancellationToken);

        // Same answer for unknown email, wrong password and an invitation not yet accepted (no password), so emails can't be probed.
        if (staff is null || staff.IsInvited)
        {
            throw InvalidCredentials();
        }

        if (staff.IsLockedOut(now))
        {
            throw new TooManyRequestsException("Too many failed attempts. Try again later.", "staff.locked_out");
        }

        if (!passwords.Verify(staff.PasswordHash, command.Password))
        {
            staff.RecordFailedSignIn(now, settings.MaxFailedSignIns, TimeSpan.FromMinutes(settings.LockoutMinutes));
            await db.SaveChangesAsync(cancellationToken);
            throw InvalidCredentials();
        }

        if (staff.IsSuspended)
        {
            throw new ForbiddenException("This account is suspended.", "staff.suspended");
        }

        staff.RecordPasswordAccepted();
        var challenge = tokens.CreateChallengeToken(staff.Id);

        if (staff.TwoFactorEnabled)
        {
            await db.SaveChangesAsync(cancellationToken);
            return new StaffSignInResult(StaffSignInStatus.TwoFactorRequired, challenge, null, null);
        }

        var secret = staff.PendingTotpSecret ?? totp.GenerateSecret();
        if (staff.PendingTotpSecret is null)
        {
            staff.BeginTwoFactorSetup(secret);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new StaffSignInResult(StaffSignInStatus.TwoFactorSetupRequired, challenge, secret, totp.BuildProvisioningUri(staff.Email, secret));
    }

    private static UnauthorizedException InvalidCredentials() =>
        new("The email or password is not correct.", "staff.invalid_credentials");
}
