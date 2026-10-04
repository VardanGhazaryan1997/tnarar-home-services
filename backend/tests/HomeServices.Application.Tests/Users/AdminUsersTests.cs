using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Tests.Support;
using HomeServices.Application.Users;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Partners;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Tests.Users;

public class AdminUsersTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly FakeClock _clock = new(Now);

    private User GivenUser(string phone, string? name = null, string? email = null, DateTimeOffset? createdAt = null, bool partner = false)
    {
        var user = User.Register(PhoneNumber.Parse(phone));
        if (name is not null)
        {
            user.UpdateProfile(name, email);
        }

        if (partner)
        {
            user.AddRole(UserRoles.Partner);
            _db.PartnerProfiles.Add(PartnerProfile.Create(user.Id, PartnerType.Specialist, name ?? "Partner"));
        }

        user.MarkCreated(createdAt ?? Now, null);
        _db.Users.Add(user);
        _db.SaveChanges();
        return user;
    }

    private Task<PagedResult<AdminUserListItemDto>> ListAsync(GetAdminUsers query) =>
        new GetAdminUsersHandler(_db).HandleAsync(query, CancellationToken.None);

    [Fact]
    public async Task Users_are_listed_newest_first_with_their_roles_and_partner_status()
    {
        var older = GivenUser("+37477000001", "Ani Petrosyan", "ani@example.com", Now.AddDays(-2));
        var newer = GivenUser("+37477000002", "Aram Plumbing", createdAt: Now.AddDays(-1), partner: true);

        var page = await ListAsync(new GetAdminUsers());

        page.TotalCount.ShouldBe(2);
        page.Items.Select(i => i.Id).ShouldBe(new[] { newer.Id, older.Id });
        page.Items[0].Roles.ShouldBe(new[] { "Customer", "Partner" });
        page.Items[0].PartnerStatus.ShouldBe("Draft");
        var ani = page.Items[1];
        ani.Phone.ShouldBe("+37477000001");
        ani.FullName.ShouldBe("Ani Petrosyan");
        ani.Email.ShouldBe("ani@example.com");
        ani.Roles.ShouldBe(new[] { "Customer" });
        ani.Status.ShouldBe("Active");
        ani.PartnerStatus.ShouldBeNull();
        ani.CreatedAt.ShouldBe(Now.AddDays(-2));
    }

    [Fact]
    public async Task Users_are_found_by_phone_name_or_email_and_filtered()
    {
        var ani = GivenUser("+37477000001", "Ani Petrosyan", "ani@example.com");
        var aram = GivenUser("+37477000002", "Aram", "aram@mail.am", partner: true);
        var blocked = GivenUser("+37477000003");
        blocked.Block("Spam");
        await _db.SaveChangesAsync();

        (await ListAsync(new GetAdminUsers(Search: "077 000 001"))).Items.ShouldHaveSingleItem().Id.ShouldBe(ani.Id);
        (await ListAsync(new GetAdminUsers(Search: "PETROS"))).Items.ShouldHaveSingleItem().Id.ShouldBe(ani.Id);
        (await ListAsync(new GetAdminUsers(Search: "mail.am"))).Items.ShouldHaveSingleItem().Id.ShouldBe(aram.Id);
        (await ListAsync(new GetAdminUsers(Role: UserRoles.Partner))).Items.ShouldHaveSingleItem().Id.ShouldBe(aram.Id);
        (await ListAsync(new GetAdminUsers(Status: UserStatus.Blocked))).Items.ShouldHaveSingleItem().Id.ShouldBe(blocked.Id);
        (await ListAsync(new GetAdminUsers(Page: 2, PageSize: 2))).Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task A_user_comes_with_their_partner_profile()
    {
        var aram = GivenUser("+37477000002", "Aram", partner: true);

        var dto = await new GetAdminUserHandler(_db).HandleAsync(new GetAdminUser(aram.Id), CancellationToken.None);

        dto.Phone.ShouldBe("+37477000002");
        dto.Partner.ShouldNotBeNull();
        dto.Partner.DisplayName.ShouldBe("Aram");
        dto.Partner.Status.ShouldBe("Draft");
        dto.Partner.Slug.ShouldStartWith("aram-");
        (await Should.ThrowAsync<NotFoundException>(() => new GetAdminUserHandler(_db).HandleAsync(new GetAdminUser(Guid.NewGuid()), CancellationToken.None)))
            .Code.ShouldBe("user.not_found");
    }

    [Fact]
    public async Task Blocking_ends_sessions_and_unblocking_clears_the_reason()
    {
        var ani = GivenUser("+37477000001", "Ani");
        _db.RefreshTokens.Add(RefreshToken.Issue(ani.Id, "hash:1", Now, TimeSpan.FromDays(30)));
        await _db.SaveChangesAsync();

        var blocked = await new BlockUserHandler(_db, _clock).HandleAsync(new BlockUser(ani.Id, "Fake reviews"), CancellationToken.None);

        blocked.Status.ShouldBe("Blocked");
        blocked.BlockReason.ShouldBe("Fake reviews");
        blocked.Partner.ShouldBeNull();
        (await _db.RefreshTokens.SingleAsync()).IsRevoked.ShouldBeTrue();

        var unblocked = await new UnblockUserHandler(_db).HandleAsync(new UnblockUser(ani.Id), CancellationToken.None);
        unblocked.Status.ShouldBe("Active");
        unblocked.BlockReason.ShouldBeNull();
        (await Should.ThrowAsync<NotFoundException>(() => new UnblockUserHandler(_db).HandleAsync(new UnblockUser(Guid.NewGuid()), CancellationToken.None)))
            .Code.ShouldBe("user.not_found");
    }

    [Fact]
    public void Queries_and_block_reasons_are_validated()
    {
        new GetAdminUsersValidator().Validate(new GetAdminUsers(Role: UserRoles.Partner)).IsValid.ShouldBeTrue();
        new GetAdminUsersValidator().Validate(new GetAdminUsers(new string('s', 101), UserRoles.None, (UserStatus)9, 0, 201))
            .Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "search.too_long", "role.invalid", "status.invalid", "page.invalid", "page_size.invalid" }, ignoreOrder: true);

        var block = new BlockUserValidator();
        block.Validate(new BlockUser(Guid.NewGuid(), "Spam")).IsValid.ShouldBeTrue();
        block.Validate(new BlockUser(Guid.NewGuid(), " ")).Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("reason.required");
        block.Validate(new BlockUser(Guid.NewGuid(), new string('r', User.BlockReasonMaxLength + 1))).Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("reason.too_long");
    }
}
