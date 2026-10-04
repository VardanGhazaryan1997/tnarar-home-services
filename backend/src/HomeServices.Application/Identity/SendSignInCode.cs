using System.Globalization;
using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Identity;

/// <summary>Step 1 of sign-in: text a one-time code to the phone number.</summary>
public sealed record SendSignInCode(string Phone) : ICommand<SignInCodeSent>;

public sealed record SignInCodeSent(int ExpiresInSeconds, int ResendAfterSeconds);

public sealed class SendSignInCodeValidator : AbstractValidator<SendSignInCode>
{
    public SendSignInCodeValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty().WithErrorCode("phone.required")
            .Must(p => PhoneNumber.TryParse(p, out _)).WithErrorCode("phone.invalid");
    }
}

public sealed class SendSignInCodeHandler(
    IAppDbContext db,
    ISmsSender sms,
    ISecretHasher hasher,
    IOtpGenerator generator,
    TimeProvider clock,
    IOptions<AuthSettings> options,
    ICurrentLanguage language) : ICommandHandler<SendSignInCode, SignInCodeSent>
{
    private static readonly Dictionary<string, string> Templates = new()
    {
        ["hy"] = "Ձեր մուտքի կոդը՝ {0}",
        ["ru"] = "Ваш код входа: {0}",
        ["en"] = "Your sign-in code: {0}",
    };

    public async Task<SignInCodeSent> HandleAsync(SendSignInCode command, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var phone = PhoneNumber.Parse(command.Phone);
        var now = clock.GetUtcNow();

        var sentLastHour = await db.OtpCodes
            .Where(o => o.Phone == phone && o.CreatedAt > now.AddHours(-1))
            .Select(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        if (sentLastHour.Any(createdAt => createdAt > now.AddSeconds(-settings.OtpResendCooldownSeconds)))
        {
            throw new TooManyRequestsException("Please wait before requesting another code.", "otp.resend_too_soon");
        }

        if (sentLastHour.Count >= settings.OtpMaxPerHour)
        {
            throw new TooManyRequestsException("Too many codes requested. Try again later.", "otp.too_many_requests");
        }

        var code = generator.Generate();
        db.OtpCodes.Add(OtpCode.Issue(phone, hasher.Hash(code), now, TimeSpan.FromMinutes(settings.OtpLifetimeMinutes)));
        await db.SaveChangesAsync(cancellationToken);

        await sms.SendAsync(phone, string.Format(CultureInfo.InvariantCulture, Template(), code), cancellationToken);

        return new SignInCodeSent(settings.OtpLifetimeMinutes * 60, settings.OtpResendCooldownSeconds);
    }

    private string Template() =>
        Templates.TryGetValue(language.Code, out var template) || Templates.TryGetValue(language.DefaultCode, out template)
            ? template
            : Templates["en"];
}
