using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Identity;
using HomeServices.Application.Messaging;
using HomeServices.Domain;
using HomeServices.Domain.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Staff;

/// <summary>Step 2 (first sign-in): confirm the authenticator app with a code; this enables 2FA and starts a session.</summary>
public sealed record CompleteStaffTwoFactorSetup(string ChallengeToken, string Code) : ICommand<StaffSession>;

/// <summary>Step 2 (later sign-ins): the authenticator code.</summary>
public sealed record VerifyStaffTwoFactor(string ChallengeToken, string Code) : ICommand<StaffSession>;

public sealed class CompleteStaffTwoFactorSetupValidator : AbstractValidator<CompleteStaffTwoFactorSetup>
{
    public CompleteStaffTwoFactorSetupValidator()
    {
        RuleFor(x => x.ChallengeToken).NotEmpty().WithErrorCode("challenge.required");
        RuleFor(x => x.Code).Matches("^[0-9]{6}$").WithErrorCode("totp.code_format");
    }
}

public sealed class VerifyStaffTwoFactorValidator : AbstractValidator<VerifyStaffTwoFactor>
{
    public VerifyStaffTwoFactorValidator()
    {
        RuleFor(x => x.ChallengeToken).NotEmpty().WithErrorCode("challenge.required");
        RuleFor(x => x.Code).Matches("^[0-9]{6}$").WithErrorCode("totp.code_format");
    }
}

public sealed class CompleteStaffTwoFactorSetupHandler(StaffTwoFactorSteps steps) : ICommandHandler<CompleteStaffTwoFactorSetup, StaffSession>
{
    public async Task<StaffSession> HandleAsync(CompleteStaffTwoFactorSetup command, CancellationToken cancellationToken)
    {
        var staff = await steps.LoadChallengedStaffAsync(command.ChallengeToken, cancellationToken);
        var secret = staff.PendingTotpSecret
            ?? throw new DomainException("staff.two_factor_setup_not_started", "Two-factor setup has not been started.");

        var step = steps.VerifyCode(secret, command.Code);
        staff.CompleteTwoFactorSetup(step);

        return await steps.StartSessionAsync(staff, cancellationToken);
    }
}

public sealed class VerifyStaffTwoFactorHandler(StaffTwoFactorSteps steps) : ICommandHandler<VerifyStaffTwoFactor, StaffSession>
{
    public async Task<StaffSession> HandleAsync(VerifyStaffTwoFactor command, CancellationToken cancellationToken)
    {
        var staff = await steps.LoadChallengedStaffAsync(command.ChallengeToken, cancellationToken);
        var secret = staff.TotpSecret
            ?? throw new DomainException("staff.two_factor_not_enabled", "Two-factor authentication is not set up yet.");

        staff.AcceptTotpTimeStep(steps.VerifyCode(secret, command.Code));

        return await steps.StartSessionAsync(staff, cancellationToken);
    }
}

/// <summary>Steps shared by both 2FA commands.</summary>
public sealed class StaffTwoFactorSteps(
    IAppDbContext db,
    ITotpService totp,
    IStaffTokenService tokens,
    ITokenService refreshTokens,
    ISecretHasher hasher,
    TimeProvider clock,
    IOptions<StaffAuthSettings> options)
{
    public async Task<StaffUser> LoadChallengedStaffAsync(string challengeToken, CancellationToken cancellationToken)
    {
        var staffId = await tokens.ReadChallengeTokenAsync(challengeToken)
            ?? throw new UnauthorizedException("Please sign in again.", "staff.challenge_invalid");

        var staff = await db.StaffUsers.SingleOrDefaultAsync(s => s.Id == staffId, cancellationToken)
            ?? throw new UnauthorizedException("Please sign in again.", "staff.challenge_invalid");

        return staff.IsSuspended
            ? throw new ForbiddenException("This account is suspended.", "staff.suspended")
            : staff;
    }

    public long VerifyCode(string secret, string code) =>
        totp.Verify(secret, code, clock.GetUtcNow())
        ?? throw new DomainException("staff.totp_invalid", "The authenticator code is not correct.");

    public async Task<StaffSession> StartSessionAsync(StaffUser staff, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var permissions = await StaffPermissionResolver.ResolveAsync(db, staff, cancellationToken);
        var (session, stored) = StaffSessionIssuer.Issue(staff, permissions, tokens, refreshTokens, hasher, now, options.Value);
        staff.RecordSignIn(now);
        db.StaffRefreshTokens.Add(stored);
        await db.SaveChangesAsync(cancellationToken);
        return session;
    }
}
