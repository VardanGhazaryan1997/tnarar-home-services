using System.Security.Cryptography;
using System.Text;
using HomeServices.Domain.Common;

namespace HomeServices.Domain.Identity;

public enum OtpVerification
{
    Valid = 1,
    Invalid = 2,
    Expired = 3,
    TooManyAttempts = 4,
    AlreadyUsed = 5,
}

/// <summary>A one-time sign-in code sent by SMS. Only its hash is stored.</summary>
public sealed class OtpCode : Entity
{
    private OtpCode()
    {
    }

    public PhoneNumber Phone { get; private set; } = null!;

    public string CodeHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public static OtpCode Issue(PhoneNumber phone, string codeHash, DateTimeOffset now, TimeSpan lifetime) =>
        new() { Phone = phone, CodeHash = codeHash, CreatedAt = now, ExpiresAt = now + lifetime };

    /// <summary>Checks a submitted code (as a hash). A wrong code uses up one attempt.</summary>
    public OtpVerification Verify(string submittedHash, DateTimeOffset now, int maxAttempts)
    {
        if (ConsumedAt is not null)
        {
            return OtpVerification.AlreadyUsed;
        }

        if (now >= ExpiresAt)
        {
            return OtpVerification.Expired;
        }

        if (Attempts >= maxAttempts)
        {
            return OtpVerification.TooManyAttempts;
        }

        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(submittedHash), Encoding.UTF8.GetBytes(CodeHash)))
        {
            Attempts++;
            return Attempts >= maxAttempts ? OtpVerification.TooManyAttempts : OtpVerification.Invalid;
        }

        ConsumedAt = now;
        return OtpVerification.Valid;
    }
}
