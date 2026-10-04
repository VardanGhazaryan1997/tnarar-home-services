using HomeServices.Domain.Staff;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Infrastructure.IntegrationTests.Staff;

[Collection(PostgresCollection.Name)]
public class StaffStorageTests(PostgresFixture db)
{
    private static string UniqueEmail() => $"staff-{Guid.NewGuid():N}@example.com";

    [Fact]
    public async Task Staff_accounts_are_saved_with_two_factor_state()
    {
        var staff = StaffUser.Create(UniqueEmail(), "Vardan", "hash", isSuperAdmin: true);
        staff.BeginTwoFactorSetup("SECRET");
        staff.CompleteTwoFactorSetup(42);
        await using (var context = db.CreateContext())
        {
            context.StaffUsers.Add(staff);
            await context.SaveChangesAsync();
        }

        await using var check = db.CreateContext();
        var saved = await check.StaffUsers.SingleAsync(s => s.Id == staff.Id);
        saved.TwoFactorEnabled.ShouldBeTrue();
        saved.LastTotpTimeStep.ShouldBe(42);
        saved.IsSuperAdmin.ShouldBeTrue();
    }

    [Fact]
    public async Task Staff_emails_are_unique()
    {
        var email = UniqueEmail();
        await using var context = db.CreateContext();
        context.StaffUsers.Add(StaffUser.Create(email, "One", "hash"));
        await context.SaveChangesAsync();

        context.StaffUsers.Add(StaffUser.Create(email, "Two", "hash"));

        await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Staff_sessions_must_belong_to_a_staff_member()
    {
        await using var context = db.CreateContext();
        context.StaffRefreshTokens.Add(StaffRefreshToken.Issue(Guid.NewGuid(), $"orphan-{Guid.NewGuid()}", DateTimeOffset.UtcNow, TimeSpan.FromHours(1)));

        await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
