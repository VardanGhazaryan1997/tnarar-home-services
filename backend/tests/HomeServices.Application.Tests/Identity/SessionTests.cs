using HomeServices.Application.Errors;
using HomeServices.Application.Identity;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Identity;

public class SessionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly FakeClock _clock = new(Now);
    private readonly AuthSettings _settings = new();
    private readonly User _user = User.Register(PhoneNumber.Parse("+37491234567"));

    private RefreshSessionHandler RefreshHandler() =>
        new(_db, new FakeSecretHasher(), new FakeTokenService(_clock), _clock, Options.Create(_settings));

    private async Task<RefreshToken> GivenSession(string raw = "old-token", DateTimeOffset? issuedAt = null)
    {
        _db.Users.Add(_user);
        var token = RefreshToken.Issue(_user.Id, $"hash:{raw}", issuedAt ?? Now, TimeSpan.FromDays(_settings.RefreshTokenLifetimeDays));
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();
        return token;
    }

    [Fact]
    public async Task Refreshing_replaces_the_session_token_with_a_new_one()
    {
        var old = await GivenSession();
        _clock.Now = Now.AddDays(1);

        var session = await RefreshHandler().HandleAsync(new RefreshSession("old-token"), CancellationToken.None);

        session.RefreshToken.ShouldBe("refresh-1");
        session.AccessToken.ShouldBe($"access:{_user.Id}");
        session.IsNewUser.ShouldBeFalse();
        old.RevokedAt.ShouldBe(Now.AddDays(1));
        old.ReplacedByHash.ShouldBe("hash:refresh-1");
        (await _db.RefreshTokens.SingleAsync(t => t.TokenHash == "hash:refresh-1")).IsActive(_clock.Now).ShouldBeTrue();
    }

    [Fact]
    public async Task An_unknown_token_is_refused()
    {
        var exception = await Should.ThrowAsync<UnauthorizedException>(
            () => RefreshHandler().HandleAsync(new RefreshSession("never-issued"), CancellationToken.None));

        exception.Code.ShouldBe("session.invalid");
    }

    [Fact]
    public async Task An_expired_token_is_refused()
    {
        await GivenSession(issuedAt: Now.AddDays(-_settings.RefreshTokenLifetimeDays));

        var exception = await Should.ThrowAsync<UnauthorizedException>(
            () => RefreshHandler().HandleAsync(new RefreshSession("old-token"), CancellationToken.None));

        exception.Code.ShouldBe("session.expired");
    }

    [Fact]
    public async Task Reusing_an_already_replaced_token_ends_every_session_of_that_user()
    {
        await GivenSession();
        var second = RefreshToken.Issue(_user.Id, "hash:other-device", Now, TimeSpan.FromDays(30));
        _db.RefreshTokens.Add(second);
        await _db.SaveChangesAsync();
        await RefreshHandler().HandleAsync(new RefreshSession("old-token"), CancellationToken.None);

        var exception = await Should.ThrowAsync<UnauthorizedException>(
            () => RefreshHandler().HandleAsync(new RefreshSession("old-token"), CancellationToken.None));

        exception.Code.ShouldBe("session.revoked");
        (await _db.RefreshTokens.ToListAsync()).ShouldAllBe(t => t.IsRevoked);
    }

    [Fact]
    public async Task Blocked_users_cannot_refresh()
    {
        await GivenSession();
        _user.Block();
        await _db.SaveChangesAsync();

        var exception = await Should.ThrowAsync<ForbiddenException>(
            () => RefreshHandler().HandleAsync(new RefreshSession("old-token"), CancellationToken.None));

        exception.Code.ShouldBe("user.blocked");
    }

    [Fact]
    public async Task Signing_out_revokes_the_session_token()
    {
        var token = await GivenSession();

        var revoked = await new EndSessionHandler(_db, new FakeSecretHasher(), _clock).HandleAsync(new EndSession("old-token"), CancellationToken.None);

        revoked.ShouldBeTrue();
        token.IsRevoked.ShouldBeTrue();
    }

    [Fact]
    public async Task Signing_out_with_an_unknown_token_does_nothing()
    {
        var revoked = await new EndSessionHandler(_db, new FakeSecretHasher(), _clock).HandleAsync(new EndSession("unknown"), CancellationToken.None);

        revoked.ShouldBeFalse();
    }
}
