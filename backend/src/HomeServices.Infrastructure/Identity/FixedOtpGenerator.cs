using HomeServices.Application.Identity;

namespace HomeServices.Infrastructure.Identity;

/// <summary>
/// Staging only: always returns the configured code (Auth:FixedOtpCode), so testers can sign in without an SMS.
/// Limits on attempts, resends and code lifetime still apply.
/// </summary>
public sealed class FixedOtpGenerator : IOtpGenerator
{
    private readonly string _code;

    public FixedOtpGenerator(string code)
    {
        if (!IsValidCode(code))
        {
            throw new ArgumentException("A fixed sign-in code must be exactly 6 digits.", nameof(code));
        }

        _code = code;
    }

    public static bool IsValidCode(string code) => code.Length == 6 && code.All(char.IsAsciiDigit);

    public string Generate() => _code;
}
