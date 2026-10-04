using System.Text.RegularExpressions;
using HomeServices.Domain.Common;

namespace HomeServices.Domain.Identity;

/// <summary>A Portal account: customers, independent specialists and company managers. Signs in by phone.</summary>
public sealed partial class User : AuditableEntity, IAudited
{
    public const int FullNameMaxLength = 100;
    public const int EmailMaxLength = 254;
    public const int BlockReasonMaxLength = 500;

    private User()
    {
    }

    public PhoneNumber Phone { get; private set; } = null!;

    public string? FullName { get; private set; }

    public string? Email { get; private set; }

    public UserRoles Roles { get; private set; }

    public UserStatus Status { get; private set; }

    [NotAudited]
    public DateTimeOffset? LastSignInAt { get; private set; }

    /// <summary>Why staff blocked the account (shown in the Back Office), while it is blocked.</summary>
    public string? BlockReason { get; private set; }

    public bool IsProfileComplete => FullName is not null;

    public bool IsBlocked => Status == UserStatus.Blocked;

    public IReadOnlyList<string> RoleNames =>
        Enum.GetValues<UserRoles>().Where(r => r != UserRoles.None && HasRole(r)).Select(r => r.ToString()).ToList();

    /// <summary>First successful sign-in creates the account as a customer.</summary>
    public static User Register(PhoneNumber phone) =>
        new() { Phone = phone, Roles = UserRoles.Customer, Status = UserStatus.Active };

    public void UpdateProfile(string fullName, string? email)
    {
        var name = fullName.Trim();
        if (name.Length == 0)
        {
            throw new DomainException("user.name_required", "Your name is required.");
        }

        if (name.Length > FullNameMaxLength)
        {
            throw new DomainException("user.name_too_long", $"The name can be at most {FullNameMaxLength} characters.");
        }

        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        if (normalizedEmail is not null && (normalizedEmail.Length > EmailMaxLength || !EmailPattern().IsMatch(normalizedEmail)))
        {
            throw new DomainException("user.email_invalid", "The email address is not valid.");
        }

        FullName = name;
        Email = normalizedEmail;
    }

    public bool HasRole(UserRoles role) => (Roles & role) == role;

    public void AddRole(UserRoles role) => Roles |= role;

    public void RecordSignIn(DateTimeOffset at) => LastSignInAt = at;

    /// <summary>Stops sign-in and hides the user's partner profile. The caller ends their sessions.</summary>
    public void Block(string? reason = null)
    {
        var clean = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (clean?.Length > BlockReasonMaxLength)
        {
            throw new DomainException("user.block_reason_too_long", $"The reason can be at most {BlockReasonMaxLength} characters.");
        }

        Status = UserStatus.Blocked;
        BlockReason = clean;
    }

    public void Unblock()
    {
        Status = UserStatus.Active;
        BlockReason = null;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
