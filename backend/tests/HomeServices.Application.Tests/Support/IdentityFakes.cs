using HomeServices.Application.Abstractions;
using HomeServices.Application.Identity;
using HomeServices.Domain.Identity;

namespace HomeServices.Application.Tests.Support;

public sealed class FakeClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

public sealed class FakeSmsSender : ISmsSender
{
    public List<(PhoneNumber To, string Message)> Sent { get; } = [];

    public Task SendAsync(PhoneNumber to, string message, CancellationToken cancellationToken)
    {
        Sent.Add((to, message));
        return Task.CompletedTask;
    }
}

/// <summary>Readable "hashes" so tests can see what was stored.</summary>
public sealed class FakeSecretHasher : ISecretHasher
{
    public string Hash(string secret) => $"hash:{secret}";
}

public sealed class FakeOtpGenerator(string code = "123456") : IOtpGenerator
{
    public string Generate() => code;
}

public sealed class FakeTokenService(TimeProvider clock) : ITokenService
{
    private int _refreshCounter;

    public AccessToken CreateAccessToken(User user) => new($"access:{user.Id}", clock.GetUtcNow().AddMinutes(15));

    public string CreateRefreshToken() => $"refresh-{++_refreshCounter}";
}

public sealed class FakeCurrentUser(Guid? userId, bool isStaff = false) : ICurrentUser
{
    public string? UserId { get; } = userId?.ToString();

    public bool IsStaff { get; } = isStaff;
}
