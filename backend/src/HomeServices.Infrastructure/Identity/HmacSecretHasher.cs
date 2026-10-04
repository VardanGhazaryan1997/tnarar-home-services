using System.Security.Cryptography;
using System.Text;
using HomeServices.Application.Identity;
using Microsoft.Extensions.Options;

namespace HomeServices.Infrastructure.Identity;

/// <summary>HMAC-SHA256 with a server-side key: a leaked database alone can't be used to guess codes or tokens.</summary>
public sealed class HmacSecretHasher(IOptions<AuthSettings> options) : ISecretHasher
{
    public string Hash(string secret)
    {
        var key = Encoding.UTF8.GetBytes(options.Value.SecretHashKey);
        var hash = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexStringLower(hash);
    }
}
