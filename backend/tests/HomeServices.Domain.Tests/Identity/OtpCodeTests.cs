using HomeServices.Domain.Identity;

namespace HomeServices.Domain.Tests.Identity;

public class OtpCodeTests
{
    private static readonly PhoneNumber Phone = PhoneNumber.Parse("+37491234567");
    private static readonly DateTimeOffset Issued = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
    private const int MaxAttempts = 3;

    private static OtpCode NewCode() => OtpCode.Issue(Phone, "hash-of-123456", Issued, TimeSpan.FromMinutes(5));

    [Fact]
    public void Issued_codes_expire_after_their_lifetime()
    {
        var code = NewCode();

        code.Phone.ShouldBe(Phone);
        code.CreatedAt.ShouldBe(Issued);
        code.ExpiresAt.ShouldBe(Issued.AddMinutes(5));
        code.Attempts.ShouldBe(0);
        code.ConsumedAt.ShouldBeNull();
    }

    [Fact]
    public void The_right_code_within_its_lifetime_is_accepted_once()
    {
        var code = NewCode();

        code.Verify("hash-of-123456", Issued.AddMinutes(1), MaxAttempts).ShouldBe(OtpVerification.Valid);
        code.ConsumedAt.ShouldBe(Issued.AddMinutes(1));

        code.Verify("hash-of-123456", Issued.AddMinutes(2), MaxAttempts).ShouldBe(OtpVerification.AlreadyUsed);
    }

    [Fact]
    public void A_wrong_code_counts_as_an_attempt()
    {
        var code = NewCode();

        code.Verify("hash-of-000000", Issued.AddMinutes(1), MaxAttempts).ShouldBe(OtpVerification.Invalid);

        code.Attempts.ShouldBe(1);
        code.ConsumedAt.ShouldBeNull();
    }

    [Fact]
    public void After_too_many_wrong_attempts_even_the_right_code_is_refused()
    {
        var code = NewCode();

        code.Verify("wrong", Issued, MaxAttempts).ShouldBe(OtpVerification.Invalid);
        code.Verify("wrong", Issued, MaxAttempts).ShouldBe(OtpVerification.Invalid);
        code.Verify("wrong", Issued, MaxAttempts).ShouldBe(OtpVerification.TooManyAttempts);

        code.Verify("hash-of-123456", Issued, MaxAttempts).ShouldBe(OtpVerification.TooManyAttempts);
    }

    [Fact]
    public void An_expired_code_is_refused()
    {
        var code = NewCode();

        code.Verify("hash-of-123456", Issued.AddMinutes(5), MaxAttempts).ShouldBe(OtpVerification.Expired);
        code.ConsumedAt.ShouldBeNull();
    }
}
