using HomeServices.Domain.Staff;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Infrastructure.IntegrationTests.Staff;

[Collection(PostgresCollection.Name)]
public class StaffInvitationStorageTests(PostgresFixture db)
{
    [Fact]
    public async Task An_invitation_is_found_by_its_token_hash()
    {
        var hash = $"hash:{Guid.NewGuid():N}";
        var staff = StaffUser.Invite($"{Guid.NewGuid():N}@example.com", "Invited", hash, db.Clock.Now.AddDays(7));
        await using (var context = db.CreateContext())
        {
            context.StaffUsers.Add(staff);
            await context.SaveChangesAsync();
        }

        await using (var context = db.CreateContext())
        {
            var found = await context.StaffUsers.SingleAsync(s => s.InviteTokenHash == hash);
            found.Id.ShouldBe(staff.Id);
            found.Status.ShouldBe(StaffStatus.Invited);
            found.PasswordHash.ShouldBeEmpty();
            found.InviteExpiresAt.ShouldBe(db.Clock.Now.AddDays(7));
        }
    }

    [Fact]
    public async Task Two_invitations_cannot_share_a_token_hash()
    {
        var hash = $"hash:{Guid.NewGuid():N}";
        await using var context = db.CreateContext();
        context.StaffUsers.Add(StaffUser.Invite($"{Guid.NewGuid():N}@example.com", "One", hash, db.Clock.Now));
        context.StaffUsers.Add(StaffUser.Invite($"{Guid.NewGuid():N}@example.com", "Two", hash, db.Clock.Now));

        await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
