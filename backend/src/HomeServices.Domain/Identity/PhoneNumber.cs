using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace HomeServices.Domain.Identity;

/// <summary>
/// A phone number in E.164 form (+37491234567). Accepts the ways Armenian users
/// usually type numbers: "091 23 45 67", "+374 91 234567", "0037491234567".
/// </summary>
public sealed partial record PhoneNumber
{
    private const string ArmenianPrefix = "+374";

    private PhoneNumber(string value) => Value = value;

    public string Value { get; }

    public static PhoneNumber Parse(string input) =>
        TryParse(input, out var phone)
            ? phone
            : throw new DomainException("phone.invalid", $"'{input}' is not a valid phone number.");

    public static bool TryParse(string? input, [NotNullWhen(true)] out PhoneNumber? phone)
    {
        phone = null;
        var digits = Separators().Replace(input ?? string.Empty, string.Empty);

        if (digits.StartsWith("00", StringComparison.Ordinal))
        {
            digits = "+" + digits[2..];
        }
        else if (digits.Length == 9 && digits[0] == '0')
        {
            digits = ArmenianPrefix + digits[1..]; // local format: 0XX XXX XXX
        }
        else if (digits.Length == 11 && digits.StartsWith("374", StringComparison.Ordinal))
        {
            digits = "+" + digits;
        }

        var valid = E164().IsMatch(digits)
            && (!digits.StartsWith(ArmenianPrefix, StringComparison.Ordinal) || digits.Length == 12);
        if (valid)
        {
            phone = new PhoneNumber(digits);
        }

        return valid;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"[\s\-().]")]
    private static partial Regex Separators();

    [GeneratedRegex(@"^\+[1-9]\d{7,14}$")]
    private static partial Regex E164();
}
