using System.Globalization;
using System.Security.Cryptography;
using HomeServices.Application.Identity;

namespace HomeServices.Infrastructure.Identity;

/// <summary>Cryptographically random 6-digit codes, e.g. "048213".</summary>
public sealed class RandomOtpGenerator : IOtpGenerator
{
    public string Generate() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
}
