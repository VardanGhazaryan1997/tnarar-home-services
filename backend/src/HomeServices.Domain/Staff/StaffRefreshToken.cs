using HomeServices.Domain.Identity;

namespace HomeServices.Domain.Staff;

/// <summary>A staff member's Back Office sign-in session.</summary>
public sealed class StaffRefreshToken : SessionToken
{
    private StaffRefreshToken()
    {
    }

    public Guid StaffUserId { get; private set; }

    public static StaffRefreshToken Issue(Guid staffUserId, string tokenHash, DateTimeOffset now, TimeSpan lifetime) =>
        new() { StaffUserId = staffUserId, TokenHash = tokenHash, CreatedAt = now, ExpiresAt = now + lifetime };
}
