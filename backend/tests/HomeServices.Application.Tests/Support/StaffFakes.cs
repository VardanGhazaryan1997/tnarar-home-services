using HomeServices.Application.Staff;
using HomeServices.Domain.Staff;

namespace HomeServices.Application.Tests.Support;

public sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"pw:{password}";

    public bool Verify(string hash, string password) => hash == $"pw:{password}";
}

/// <summary>Accepts <see cref="ValidCode"/>; the time step is the 30-second window of "now".</summary>
public sealed class FakeTotpService : ITotpService
{
    public const string ValidCode = "111111";
    public const string Secret = "JBSWY3DPEHPK3PXP";

    public string GenerateSecret() => Secret;

    public string BuildProvisioningUri(string accountName, string secret) => $"otpauth://totp/HomeServices:{accountName}?secret={secret}";

    public long? Verify(string secret, string code, DateTimeOffset now) =>
        code == ValidCode ? now.ToUnixTimeSeconds() / 30 : null;
}

public sealed class FakeStaffTokenService(TimeProvider clock) : IStaffTokenService
{
    private const string Prefix = "challenge:";

    public IReadOnlyCollection<string> LastPermissions { get; private set; } = [];

    public StaffAccessToken CreateAccessToken(StaffUser staff, IReadOnlyCollection<string> permissions)
    {
        LastPermissions = permissions;
        return new($"staff-access:{staff.Id}", clock.GetUtcNow().AddMinutes(15));
    }

    public string CreateChallengeToken(Guid staffUserId) => Prefix + staffUserId;

    public Task<Guid?> ReadChallengeTokenAsync(string challengeToken) =>
        Task.FromResult<Guid?>(
            challengeToken.StartsWith(Prefix, StringComparison.Ordinal) && Guid.TryParse(challengeToken[Prefix.Length..], out var id) ? id : null);
}
