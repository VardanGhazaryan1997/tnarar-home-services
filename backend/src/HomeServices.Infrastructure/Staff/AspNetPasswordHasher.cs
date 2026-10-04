using HomeServices.Application.Staff;
using Microsoft.AspNetCore.Identity;

namespace HomeServices.Infrastructure.Staff;

/// <summary>ASP.NET Core Identity's password hasher (PBKDF2, salted, versioned format).</summary>
public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private static readonly object User = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(User, password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(User, hash, password) is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
}
