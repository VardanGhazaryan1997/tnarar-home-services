using HomeServices.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Infrastructure.IntegrationTests.Identity;

[Collection(PostgresCollection.Name)]
public class IdentityStorageTests(PostgresFixture db)
{
    private static PhoneNumber UniquePhone() => PhoneNumber.Parse($"+3749{Random.Shared.Next(1_000_000, 9_999_999)}");

    [Fact]
    public async Task Users_are_saved_with_their_phone_roles_and_profile()
    {
        var user = User.Register(UniquePhone());
        user.UpdateProfile("Ani", "ani@example.com");
        user.AddRole(UserRoles.Partner);
        await using (var context = db.CreateContext())
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        await using var check = db.CreateContext();
        var saved = await check.Users.SingleAsync(u => u.Phone == user.Phone);
        saved.FullName.ShouldBe("Ani");
        saved.HasRole(UserRoles.Partner).ShouldBeTrue();
        saved.HasRole(UserRoles.Customer).ShouldBeTrue();
    }

    [Fact]
    public async Task A_phone_number_belongs_to_one_account()
    {
        var phone = UniquePhone();
        await using var context = db.CreateContext();
        context.Users.Add(User.Register(phone));
        await context.SaveChangesAsync();

        context.Users.Add(User.Register(phone));

        await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Codes_and_sessions_are_stored_by_hash()
    {
        var user = User.Register(UniquePhone());
        var now = DateTimeOffset.UtcNow;
        await using (var context = db.CreateContext())
        {
            context.Users.Add(user);
            context.OtpCodes.Add(OtpCode.Issue(user.Phone, "otp-hash", now, TimeSpan.FromMinutes(5)));
            context.RefreshTokens.Add(RefreshToken.Issue(user.Id, $"session-{user.Id}", now, TimeSpan.FromDays(30)));
            await context.SaveChangesAsync();
        }

        await using var check = db.CreateContext();
        (await check.OtpCodes.SingleAsync(o => o.Phone == user.Phone)).CodeHash.ShouldBe("otp-hash");
        (await check.RefreshTokens.SingleAsync(t => t.UserId == user.Id)).TokenHash.ShouldBe($"session-{user.Id}");
    }

    [Fact]
    public async Task A_session_must_belong_to_an_existing_user()
    {
        await using var context = db.CreateContext();
        context.RefreshTokens.Add(RefreshToken.Issue(Guid.NewGuid(), $"orphan-{Guid.NewGuid()}", DateTimeOffset.UtcNow, TimeSpan.FromDays(1)));

        await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
