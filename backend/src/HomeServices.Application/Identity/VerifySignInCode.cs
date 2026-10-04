using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Messaging;
using HomeServices.Domain;
using HomeServices.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Identity;

/// <summary>Step 2 of sign-in: check the code; create the account on first sign-in; start a session.</summary>
public sealed record VerifySignInCode(string Phone, string Code) : ICommand<AuthSession>;

public sealed class VerifySignInCodeValidator : AbstractValidator<VerifySignInCode>
{
    public VerifySignInCodeValidator()
    {
        RuleFor(x => x.Phone).Must(p => PhoneNumber.TryParse(p, out _)).WithErrorCode("phone.invalid");
        RuleFor(x => x.Code).Matches("^[0-9]{6}$").WithErrorCode("otp.code_format");
    }
}

public sealed class VerifySignInCodeHandler(
    IAppDbContext db,
    ISecretHasher hasher,
    ITokenService tokens,
    TimeProvider clock,
    IOptions<AuthSettings> options) : ICommandHandler<VerifySignInCode, AuthSession>
{
    public async Task<AuthSession> HandleAsync(VerifySignInCode command, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var phone = PhoneNumber.Parse(command.Phone);
        var now = clock.GetUtcNow();

        var otp = await db.OtpCodes
            .Where(o => o.Phone == phone)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var result = otp?.Verify(hasher.Hash(command.Code), now, settings.OtpMaxAttempts) ?? OtpVerification.Invalid;
        if (result != OtpVerification.Valid)
        {
            await db.SaveChangesAsync(cancellationToken); // keep the used-up attempt
            throw result switch
            {
                OtpVerification.Expired => new DomainException("otp.expired", "The code has expired. Request a new one."),
                OtpVerification.TooManyAttempts => new DomainException("otp.too_many_attempts", "Too many wrong codes. Request a new one."),
                _ => new DomainException("otp.invalid", "The code is not correct."),
            };
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.Phone == phone, cancellationToken);
        var isNewUser = user is null;
        if (user is null)
        {
            user = User.Register(phone);
            db.Users.Add(user);
        }
        else if (user.IsBlocked)
        {
            await db.SaveChangesAsync(cancellationToken); // the code is still used up
            throw new ForbiddenException("This account is blocked.", "user.blocked");
        }

        user.RecordSignIn(now);
        var (session, stored) = SessionIssuer.Issue(user, isNewUser, tokens, hasher, now, settings);
        db.RefreshTokens.Add(stored);
        await db.SaveChangesAsync(cancellationToken);

        return session;
    }
}
