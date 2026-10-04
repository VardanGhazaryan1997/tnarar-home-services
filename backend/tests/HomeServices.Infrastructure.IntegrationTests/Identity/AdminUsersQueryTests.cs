using HomeServices.Application.Users;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Partners;

namespace HomeServices.Infrastructure.IntegrationTests.Identity;

[Collection(PostgresCollection.Name)]
public class AdminUsersQueryTests(PostgresFixture db)
{
    [Fact]
    public async Task The_users_list_runs_on_PostgreSQL()
    {
        var name = $"Partner {Guid.NewGuid():N}";
        var user = User.Register(PhoneNumber.Parse($"+37498{Random.Shared.Next(100_000, 999_999)}"));
        user.UpdateProfile(name, null);
        user.AddRole(UserRoles.Partner);
        await using (var context = db.CreateContext())
        {
            context.Users.Add(user);
            context.PartnerProfiles.Add(PartnerProfile.Create(user.Id, PartnerType.Company, "Best Build"));
            await context.SaveChangesAsync();
        }

        await using (var context = db.CreateContext())
        {
            var handler = new GetAdminUsersHandler(context);

            var byName = await handler.HandleAsync(new GetAdminUsers(name.ToUpperInvariant(), UserRoles.Partner, UserStatus.Active), CancellationToken.None);
            var byPhone = await handler.HandleAsync(new GetAdminUsers(user.Phone.Value), CancellationToken.None);

            var item = byName.Items.ShouldHaveSingleItem();
            item.Id.ShouldBe(user.Id);
            item.PartnerStatus.ShouldBe("Draft");
            item.Roles.ShouldBe(new[] { "Customer", "Partner" });
            byPhone.Items.ShouldHaveSingleItem().Id.ShouldBe(user.Id);
        }
    }
}
