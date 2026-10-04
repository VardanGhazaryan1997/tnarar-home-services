using System.Text.RegularExpressions;
using HomeServices.Domain.Common;

namespace HomeServices.Domain.Staff;

public enum StaffStatus
{
    Active = 1,
    Suspended = 2,

    /// <summary>Invited but hasn't chosen a password yet; can't sign in.</summary>
    Invited = 3,
}

/// <summary>
/// A Back Office account. Signs in with email + password + authenticator code (2FA is mandatory).
/// New staff are invited: they get a one-time link, choose a password, then set up 2FA at first sign-in.
/// Permissions come from roles; Super Admins have all of them.
/// </summary>
public sealed partial class StaffUser : AuditableEntity, IAudited
{
    public const int EmailMaxLength = 254;
    public const int FullNameMaxLength = 100;

    private readonly List<StaffUserRole> _roles = [];

    private StaffUser()
    {
    }

    public string Email { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    [AuditRedacted]
    public string PasswordHash { get; private set; } = string.Empty;

    public bool IsSuperAdmin { get; private set; }

    public StaffStatus Status { get; private set; }

    [AuditRedacted]
    public string? TotpSecret { get; private set; }

    [NotAudited]
    public string? PendingTotpSecret { get; private set; }

    [NotAudited]
    public long? LastTotpTimeStep { get; private set; }

    [NotAudited]
    public int FailedSignInCount { get; private set; }

    [NotAudited]
    public DateTimeOffset? LockedUntil { get; private set; }

    [NotAudited]
    public DateTimeOffset? LastSignInAt { get; private set; }

    /// <summary>Hash of the invitation link's token while the invitation is open.</summary>
    [AuditRedacted]
    public string? InviteTokenHash { get; private set; }

    public DateTimeOffset? InviteExpiresAt { get; private set; }

    public IReadOnlyCollection<StaffUserRole> Roles => _roles.AsReadOnly();

    public IReadOnlyList<Guid> RoleIds => _roles.Select(r => r.RoleId).ToList();

    public bool TwoFactorEnabled => TotpSecret is not null;

    public bool IsSuspended => Status == StaffStatus.Suspended;

    public bool IsInvited => Status == StaffStatus.Invited;

    public static StaffUser Create(string email, string fullName, string passwordHash, bool isSuperAdmin = false) =>
        new()
        {
            Email = NormalizeEmail(email),
            FullName = CleanName(fullName),
            PasswordHash = passwordHash,
            IsSuperAdmin = isSuperAdmin,
            Status = StaffStatus.Active,
        };

    /// <summary>A new staff member who will choose a password through the invitation link.</summary>
    public static StaffUser Invite(string email, string fullName, string inviteTokenHash, DateTimeOffset expiresAt, bool isSuperAdmin = false) =>
        new()
        {
            Email = NormalizeEmail(email),
            FullName = CleanName(fullName),
            IsSuperAdmin = isSuperAdmin,
            Status = StaffStatus.Invited,
            InviteTokenHash = inviteTokenHash,
            InviteExpiresAt = expiresAt,
        };

    /// <summary>A fresh invitation link; the previous one stops working.</summary>
    public void RenewInvite(string inviteTokenHash, DateTimeOffset expiresAt)
    {
        if (!IsInvited)
        {
            throw new DomainException("staff.not_invited", "Only invited staff who haven't joined yet get a new invitation.");
        }

        InviteTokenHash = inviteTokenHash;
        InviteExpiresAt = expiresAt;
    }

    /// <summary>The invited person chose a password; the account becomes active and the link stops working.</summary>
    public void AcceptInvite(string passwordHash, DateTimeOffset now)
    {
        if (!IsInvited || InviteTokenHash is null)
        {
            throw new DomainException("staff.not_invited", "This invitation is no longer valid.");
        }

        if (InviteExpiresAt is { } expires && now >= expires)
        {
            throw new DomainException("staff.invite_expired", "This invitation has expired. Ask for a new one.");
        }

        PasswordHash = passwordHash;
        Status = StaffStatus.Active;
        InviteTokenHash = null;
        InviteExpiresAt = null;
    }

    public void Rename(string fullName) => FullName = CleanName(fullName);

    /// <summary>Forgets the authenticator app (lost phone); a new one is set up at the next sign-in.</summary>
    public void ResetTwoFactor()
    {
        TotpSecret = null;
        PendingTotpSecret = null;
        LastTotpTimeStep = null;
    }

    public static string NormalizeEmail(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length > EmailMaxLength || !EmailPattern().IsMatch(normalized))
        {
            throw new DomainException("staff.email_invalid", "The email address is not valid.");
        }

        return normalized;
    }

    public bool IsLockedOut(DateTimeOffset now) => LockedUntil is { } until && now < until;

    /// <summary>Counts a wrong password; after <paramref name="maxAttempts"/> in a row the account locks for <paramref name="lockout"/>.</summary>
    public void RecordFailedSignIn(DateTimeOffset now, int maxAttempts, TimeSpan lockout)
    {
        FailedSignInCount++;
        if (FailedSignInCount >= maxAttempts)
        {
            LockedUntil = now + lockout;
            FailedSignInCount = 0;
        }
    }

    public void RecordPasswordAccepted()
    {
        FailedSignInCount = 0;
        LockedUntil = null;
    }

    /// <summary>Starts authenticator enrolment; the secret becomes active once a valid code confirms it.</summary>
    public void BeginTwoFactorSetup(string secret)
    {
        if (TwoFactorEnabled)
        {
            throw new DomainException("staff.two_factor_already_enabled", "Two-factor authentication is already set up.");
        }

        PendingTotpSecret = secret;
    }

    public void CompleteTwoFactorSetup(long timeStep)
    {
        if (PendingTotpSecret is null)
        {
            throw new DomainException("staff.two_factor_setup_not_started", "Two-factor setup has not been started.");
        }

        TotpSecret = PendingTotpSecret;
        PendingTotpSecret = null;
        LastTotpTimeStep = timeStep;
    }

    /// <summary>Accepts an authenticator code's time step; a code can't be used twice.</summary>
    public void AcceptTotpTimeStep(long timeStep)
    {
        if (LastTotpTimeStep is { } last && timeStep <= last)
        {
            throw new DomainException("staff.totp_already_used", "This code was already used. Wait for the next one.");
        }

        LastTotpTimeStep = timeStep;
    }

    public void RecordSignIn(DateTimeOffset at) => LastSignInAt = at;

    public void ChangePasswordHash(string passwordHash) => PasswordHash = passwordHash;

    public void AssignRole(Guid roleId)
    {
        if (_roles.All(r => r.RoleId != roleId))
        {
            _roles.Add(new StaffUserRole(Id, roleId));
        }
    }

    public void RemoveRole(Guid roleId) => _roles.RemoveAll(r => r.RoleId == roleId);

    public void MakeSuperAdmin() => IsSuperAdmin = true;

    /// <summary>Callers must first check this isn't the last active Super Admin (SuperAdminGuard).</summary>
    public void RevokeSuperAdmin() => IsSuperAdmin = false;

    /// <summary>Blocks sign-in. An open invitation is cancelled.</summary>
    public void Suspend()
    {
        Status = StaffStatus.Suspended;
        InviteTokenHash = null;
        InviteExpiresAt = null;
    }

    /// <summary>Lifts a suspension. Someone who never accepted their invitation is invited again (send a new link).</summary>
    public void Activate() => Status = PasswordHash.Length == 0 ? StaffStatus.Invited : StaffStatus.Active;

    private static string CleanName(string fullName)
    {
        var name = (fullName ?? string.Empty).Trim();
        return name.Length is > 0 and <= FullNameMaxLength
            ? name
            : throw new DomainException("staff.name_invalid", "A name of up to 100 characters is required.");
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
