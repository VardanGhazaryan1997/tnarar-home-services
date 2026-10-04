using HomeServices.Application.Errors;
using HomeServices.Application.Identity;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Identity;

public class SendSignInCodeTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly FakeClock _clock = new(Now);
    private readonly FakeSmsSender _sms = new();
    private readonly AuthSettings _settings = new();

    private SendSignInCodeHandler Handler(string language = "hy") =>
        new(_db, _sms, new FakeSecretHasher(), new FakeOtpGenerator("482913"), _clock, Options.Create(_settings), new FakeCurrentLanguage(language));

    [Fact]
    public async Task Stores_only_the_hash_and_texts_the_code_to_the_number()
    {
        var result = await Handler().HandleAsync(new SendSignInCode("091 234 567"), CancellationToken.None);

        var stored = await _db.OtpCodes.SingleAsync();
        stored.Phone.ShouldBe(PhoneNumber.Parse("+37491234567"));
        stored.CodeHash.ShouldBe("hash:482913");
        stored.ExpiresAt.ShouldBe(Now.AddMinutes(_settings.OtpLifetimeMinutes));

        _sms.Sent.ShouldHaveSingleItem().To.Value.ShouldBe("+37491234567");
        _sms.Sent[0].Message.ShouldContain("482913");

        result.ExpiresInSeconds.ShouldBe(_settings.OtpLifetimeMinutes * 60);
        result.ResendAfterSeconds.ShouldBe(_settings.OtpResendCooldownSeconds);
    }

    [Theory]
    [InlineData("hy", "Ձեր մուտքի կոդը")]
    [InlineData("ru", "Ваш код входа")]
    [InlineData("en", "Your sign-in code")]
    [InlineData("fr", "Ձեր մուտքի կոդը")]
    public async Task The_SMS_is_in_the_request_language(string language, string expectedText)
    {
        await Handler(language).HandleAsync(new SendSignInCode("+37491234567"), CancellationToken.None);

        _sms.Sent.ShouldHaveSingleItem().Message.ShouldStartWith(expectedText);
    }

    [Fact]
    public async Task A_new_code_cannot_be_requested_before_the_cooldown_ends()
    {
        await Handler().HandleAsync(new SendSignInCode("+37491234567"), CancellationToken.None);
        _clock.Now = Now.AddSeconds(_settings.OtpResendCooldownSeconds - 1);

        var exception = await Should.ThrowAsync<TooManyRequestsException>(
            () => Handler().HandleAsync(new SendSignInCode("+37491234567"), CancellationToken.None));

        exception.Code.ShouldBe("otp.resend_too_soon");
        _sms.Sent.Count.ShouldBe(1);
    }

    [Fact]
    public async Task At_most_a_few_codes_per_hour_are_sent_to_one_number()
    {
        for (var i = 0; i < _settings.OtpMaxPerHour; i++)
        {
            _clock.Now = Now.AddMinutes(i * 2);
            await Handler().HandleAsync(new SendSignInCode("+37491234567"), CancellationToken.None);
        }

        _clock.Now = Now.AddMinutes(_settings.OtpMaxPerHour * 2);
        var exception = await Should.ThrowAsync<TooManyRequestsException>(
            () => Handler().HandleAsync(new SendSignInCode("+37491234567"), CancellationToken.None));

        exception.Code.ShouldBe("otp.too_many_requests");
    }

    [Fact]
    public async Task Limits_are_per_number()
    {
        await Handler().HandleAsync(new SendSignInCode("+37491234567"), CancellationToken.None);

        await Handler().HandleAsync(new SendSignInCode("+37499111222"), CancellationToken.None);

        _sms.Sent.Count.ShouldBe(2);
    }

    [Theory]
    [InlineData("", "phone.required")]
    [InlineData("12", "phone.invalid")]
    public void The_phone_number_is_validated(string phone, string expectedCode)
    {
        var result = new SendSignInCodeValidator().Validate(new SendSignInCode(phone));

        result.Errors.ShouldContain(e => e.ErrorCode == expectedCode);
    }
}
