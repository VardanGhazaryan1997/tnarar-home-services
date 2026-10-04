using HomeServices.Application.Errors;
using HomeServices.Application.Staff;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Staff;

public class StaffSessionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly FakeClock _clock = new(Now);
    private readonly StaffAuthSettings _settings = new();
    private readonly StaffUser _staff = StaffUser.Create("admin@example.com", "Vardan", "pw:x");

    private RefreshStaffSessionHandler Refresh() =>
        new(_db, new FakeStaffTokenService(_clock), new FakeTokenService(_clock), new FakeSecretHasher(), _clock, Options.Create(_settings));

    private async Task<StaffRefreshToken> GivenSession(DateTimeOffset? issuedAt = null)
    {
        _db.StaffUsers.Add(_staff);
        var token = StaffRefreshToken.Issue(_staff.Id, "hash:old", issuedAt ?? Now, TimeSpan.FromHours(_settings.RefreshTokenLifetimeHours));
        _db.StaffRefreshTokens.Add(token);
        await _db.SaveChangesAsync();
        return token;
    }

    [Fact]
    public async Task Refreshing_rotates_the_token()
    {
        var old = await GivenSession();

        var session = await Refresh().HandleAsync(new RefreshStaffSession("old"), CancellationToken.None);

        session.RefreshToken.ShouldBe("refresh-1");
        session.AccessToken.ShouldBe($"staff-access:{_staff.Id}");
        old.ReplacedByHash.ShouldBe("hash:refresh-1");
    }

    [Fact]
    public async Task Unknown_and_expired_tokens_are_refused()
    {
        (await Should.ThrowAsync<UnauthorizedException>(() => Refresh().HandleAsync(new RefreshStaffSession("unknown"), CancellationToken.None)))
            .Code.ShouldBe("session.invalid");

        await GivenSession(issuedAt: Now.AddHours(-_settings.RefreshTokenLifetimeHours));
        (await Should.ThrowAsync<UnauthorizedException>(() => Refresh().HandleAsync(new RefreshStaffSession("old"), CancellationToken.None)))
            .Code.ShouldBe("session.expired");
    }

    [Fact]
    public async Task Reusing_a_replaced_token_ends_all_sessions()
    {
        await GivenSession();
        await Refresh().HandleAsync(new RefreshStaffSession("old"), CancellationToken.None);

        (await Should.ThrowAsync<UnauthorizedException>(() => Refresh().HandleAsync(new RefreshStaffSession("old"), CancellationToken.None)))
            .Code.ShouldBe("session.revoked");
        (await _db.StaffRefreshTokens.ToListAsync()).ShouldAllBe(t => t.IsRevoked);
    }

    [Fact]
    public async Task Suspended_staff_cannot_refresh()
    {
        await GivenSession();
        _staff.Suspend();
        await _db.SaveChangesAsync();

        (await Should.ThrowAsync<ForbiddenException>(() => Refresh().HandleAsync(new RefreshStaffSession("old"), CancellationToken.None)))
            .Code.ShouldBe("staff.suspended");
    }

    [Fact]
    public async Task Ending_a_session_revokes_it()
    {
        var token = await GivenSession();
        var handler = new EndStaffSessionHandler(_db, new FakeSecretHasher(), _clock);

        (await handler.HandleAsync(new EndStaffSession("old"), CancellationToken.None)).ShouldBeTrue();
        token.IsRevoked.ShouldBeTrue();
        (await handler.HandleAsync(new EndStaffSession("unknown"), CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task The_signed_in_staff_member_can_read_their_profile()
    {
        await GivenSession();

        var profile = await new GetStaffProfileHandler(_db, new FakeCurrentUser(_staff.Id)).HandleAsync(new GetStaffProfile(), CancellationToken.None);

        profile.Id.ShouldBe(_staff.Id);
        profile.Email.ShouldBe("admin@example.com");
        profile.FullName.ShouldBe("Vardan");
        profile.IsSuperAdmin.ShouldBeFalse();
        profile.Permissions.ShouldBeEmpty();
        await Should.ThrowAsync<UnauthorizedException>(
            () => new GetStaffProfileHandler(_db, new FakeCurrentUser(null)).HandleAsync(new GetStaffProfile(), CancellationToken.None));
    }

    [Fact]
    public async Task The_first_Super_Admin_is_created_only_when_no_staff_exists()
    {
        var handler = new BootstrapSuperAdminHandler(_db, new FakePasswordHasher(), Options.Create(_settings));

        (await handler.HandleAsync(new BootstrapSuperAdmin("owner@example.com", "Vardan", "Long-Enough-Password"), CancellationToken.None)).ShouldBeTrue();
        (await handler.HandleAsync(new BootstrapSuperAdmin("second@example.com", "Other", "Long-Enough-Password"), CancellationToken.None)).ShouldBeFalse();

        var owner = await _db.StaffUsers.SingleAsync();
        owner.Email.ShouldBe("owner@example.com");
        owner.IsSuperAdmin.ShouldBeTrue();
        owner.PasswordHash.ShouldBe("pw:Long-Enough-Password");
    }

    [Fact]
    public async Task The_bootstrap_password_must_be_long_enough()
    {
        var handler = new BootstrapSuperAdminHandler(_db, new FakePasswordHasher(), Options.Create(_settings));

        (await Should.ThrowAsync<DomainException>(() => handler.HandleAsync(new BootstrapSuperAdmin("owner@example.com", "Vardan", "short"), CancellationToken.None)))
            .Code.ShouldBe("staff.password_too_short");
    }
}
