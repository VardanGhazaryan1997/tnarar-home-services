using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Staff;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Staff;

public class StaffManagementTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly FakeClock _clock = new(Now);
    private readonly FakeSecretHasher _hasher = new();
    private readonly StaffAuthSettings _settings = new();
    private readonly FakeTokenService _tokens;
    private readonly Role _operator;
    private readonly Role _finance;
    private readonly StaffUser _superAdmin;
    private readonly StaffUser _manager;

    public StaffManagementTests()
    {
        _tokens = new FakeTokenService(_clock);
        _operator = Role.Create("Operator", "Reviews partners", [Permissions.PartnersView, Permissions.PartnersApprove], isSystem: true);
        _finance = Role.Create("Finance", "Money", [Permissions.PaymentsView]);
        _superAdmin = StaffUser.Create("boss@example.com", "Boss", "pw:x", isSuperAdmin: true);
        _manager = StaffUser.Create("manager@example.com", "Manager", "pw:x");
        _db.Roles.AddRange(_operator, _finance);
        _db.StaffUsers.AddRange(_superAdmin, _manager);
        _db.SaveChanges();
    }

    private static ICurrentUser As(StaffUser staff) => new FakeCurrentUser(staff.Id, isStaff: true);

    private ICurrentUser SuperAdmin => As(_superAdmin);

    private ICurrentUser Manager => As(_manager);

    private Task<StaffInviteDto> InviteAsync(InviteStaff command, ICurrentUser? actor = null) =>
        new InviteStaffHandler(_db, actor ?? Manager, _tokens, _hasher, _clock, Options.Create(_settings))
            .HandleAsync(command, CancellationToken.None);

    private Task<StaffInviteDto> RenewAsync(Guid id, ICurrentUser? actor = null) =>
        new RenewStaffInviteHandler(_db, actor ?? Manager, _tokens, _hasher, _clock, Options.Create(_settings))
            .HandleAsync(new RenewStaffInvite(id), CancellationToken.None);

    private Task<bool> AcceptAsync(string token, string password = "Long-Enough-Password") =>
        new AcceptStaffInviteHandler(_db, new FakePasswordHasher(), _hasher, _clock).HandleAsync(new AcceptStaffInvite(token, password), CancellationToken.None);

    private Task<StaffMemberDto> UpdateAsync(UpdateStaffMember command, ICurrentUser? actor = null) =>
        new UpdateStaffMemberHandler(_db, actor ?? Manager).HandleAsync(command, CancellationToken.None);

    private Task<StaffMemberDto> SuspendAsync(Guid id, bool suspended = true, ICurrentUser? actor = null) =>
        new SetStaffSuspendedHandler(_db, actor ?? Manager, new SuperAdminGuard(_db), _clock).HandleAsync(new SetStaffSuspended(id, suspended), CancellationToken.None);

    private Task<StaffMemberDto> SuperAdminFlagAsync(Guid id, bool value, ICurrentUser? actor = null) =>
        new SetStaffSuperAdminHandler(_db, actor ?? SuperAdmin, new SuperAdminGuard(_db)).HandleAsync(new SetStaffSuperAdmin(id, value), CancellationToken.None);

    private Task<StaffMemberDto> ResetTwoFactorAsync(Guid id, ICurrentUser? actor = null) =>
        new ResetStaffTwoFactorHandler(_db, actor ?? Manager, _clock).HandleAsync(new ResetStaffTwoFactor(id), CancellationToken.None);

    private async Task<StaffUser> GivenActiveStaffAsync(string email, params Role[] roles)
    {
        var staff = StaffUser.Create(email, email.Split('@')[0], "pw:x");
        foreach (var role in roles)
        {
            staff.AssignRole(role.Id);
        }

        _db.StaffUsers.Add(staff);
        _db.StaffRefreshTokens.Add(StaffRefreshToken.Issue(staff.Id, $"hash:{email}", Now, TimeSpan.FromHours(12)));
        await _db.SaveChangesAsync();
        return staff;
    }

    [Fact]
    public async Task Inviting_creates_an_account_waiting_for_its_password()
    {
        var invite = await InviteAsync(new InviteStaff(" Ani@Example.com ", "Ani", [_operator.Id, _operator.Id]));

        invite.InviteToken.ShouldBe("refresh-1");
        invite.ExpiresAt.ShouldBe(Now.AddDays(7));
        invite.Member.Email.ShouldBe("ani@example.com");
        invite.Member.Status.ShouldBe("Invited");
        invite.Member.InviteExpiresAt.ShouldBe(Now.AddDays(7));
        invite.Member.Roles.ShouldBe(new[] { new StaffRoleRefDto(_operator.Id, "Operator") });
        var saved = await _db.StaffUsers.SingleAsync(s => s.Email == "ani@example.com");
        saved.InviteTokenHash.ShouldBe("hash:refresh-1");
    }

    [Fact]
    public async Task An_email_can_be_used_by_one_staff_member_only()
    {
        var error = await Should.ThrowAsync<ConflictException>(() => InviteAsync(new InviteStaff("MANAGER@example.com", "Again", [])));

        error.Code.ShouldBe("staff.email_taken");
    }

    [Fact]
    public async Task Only_Super_Admins_invite_Super_Admins()
    {
        (await Should.ThrowAsync<ForbiddenException>(() => InviteAsync(new InviteStaff("new@example.com", "New", [], IsSuperAdmin: true))))
            .Code.ShouldBe("staff.super_admin_only");

        (await InviteAsync(new InviteStaff("new@example.com", "New", [], IsSuperAdmin: true), SuperAdmin)).Member.IsSuperAdmin.ShouldBeTrue();
    }

    [Fact]
    public async Task The_invited_person_chooses_a_password_once()
    {
        var invite = await InviteAsync(new InviteStaff("ani@example.com", "Ani", []));

        (await AcceptAsync(invite.InviteToken)).ShouldBeTrue();

        var saved = await _db.StaffUsers.SingleAsync(s => s.Email == "ani@example.com");
        saved.Status.ShouldBe(StaffStatus.Active);
        saved.PasswordHash.ShouldBe("pw:Long-Enough-Password");
        (await Should.ThrowAsync<NotFoundException>(() => AcceptAsync(invite.InviteToken))).Code.ShouldBe("staff.invite_invalid");
    }

    [Fact]
    public async Task Expired_invitations_are_renewed_with_a_new_link()
    {
        var invite = await InviteAsync(new InviteStaff("ani@example.com", "Ani", []));
        _clock.Now = Now.AddDays(8);
        (await Should.ThrowAsync<DomainException>(() => AcceptAsync(invite.InviteToken))).Code.ShouldBe("staff.invite_expired");

        var renewed = await RenewAsync(invite.Member.Id);

        renewed.InviteToken.ShouldNotBe(invite.InviteToken);
        renewed.ExpiresAt.ShouldBe(Now.AddDays(15));
        (await Should.ThrowAsync<NotFoundException>(() => AcceptAsync(invite.InviteToken))).Code.ShouldBe("staff.invite_invalid");
        (await AcceptAsync(renewed.InviteToken)).ShouldBeTrue();
        (await Should.ThrowAsync<DomainException>(() => RenewAsync(invite.Member.Id))).Code.ShouldBe("staff.not_invited");
    }

    [Fact]
    public async Task Name_and_roles_are_updated()
    {
        var staff = await GivenActiveStaffAsync("ani@example.com", _operator);

        var dto = await UpdateAsync(new UpdateStaffMember(staff.Id, " Ani Petrosyan ", [_finance.Id]));

        dto.FullName.ShouldBe("Ani Petrosyan");
        dto.Roles.ShouldBe(new[] { new StaffRoleRefDto(_finance.Id, "Finance") });
        _db.ChangeTracker.Clear();
        (await _db.StaffUsers.SingleAsync(s => s.Id == staff.Id)).RoleIds.ShouldBe(new[] { _finance.Id });
    }

    [Fact]
    public async Task Super_Admin_accounts_are_changed_only_by_Super_Admins()
    {
        var otherBoss = StaffUser.Create("boss2@example.com", "Boss 2", "pw:x", isSuperAdmin: true);
        _db.StaffUsers.Add(otherBoss);
        await _db.SaveChangesAsync();

        (await Should.ThrowAsync<ForbiddenException>(() => UpdateAsync(new UpdateStaffMember(otherBoss.Id, "X", [])))).Code.ShouldBe("staff.super_admin_only");
        (await Should.ThrowAsync<ForbiddenException>(() => SuspendAsync(otherBoss.Id))).Code.ShouldBe("staff.super_admin_only");
        (await Should.ThrowAsync<ForbiddenException>(() => ResetTwoFactorAsync(otherBoss.Id))).Code.ShouldBe("staff.super_admin_only");
        (await SuspendAsync(otherBoss.Id, actor: SuperAdmin)).Status.ShouldBe("Suspended");
    }

    [Fact]
    public async Task Suspending_ends_sessions_and_activating_lifts_it()
    {
        var staff = await GivenActiveStaffAsync("ani@example.com");

        (await SuspendAsync(staff.Id)).Status.ShouldBe("Suspended");
        (await _db.StaffRefreshTokens.SingleAsync(t => t.StaffUserId == staff.Id)).IsRevoked.ShouldBeTrue();

        (await SuspendAsync(staff.Id, suspended: false)).Status.ShouldBe("Active");
        (await SuspendAsync(_manager.Id, suspended: false)).Status.ShouldBe("Active");
    }

    [Fact]
    public async Task Nobody_suspends_themselves_or_the_last_Super_Admin()
    {
        (await Should.ThrowAsync<DomainException>(() => SuspendAsync(_manager.Id))).Code.ShouldBe("staff.cannot_suspend_self");
        (await Should.ThrowAsync<DomainException>(() => SuperAdminFlagAsync(_superAdmin.Id, false))).Code.ShouldBe("staff.last_super_admin");
    }

    [Fact]
    public async Task Super_Admins_grant_and_revoke_the_flag()
    {
        (await Should.ThrowAsync<ForbiddenException>(() => SuperAdminFlagAsync(_manager.Id, true, Manager))).Code.ShouldBe("staff.super_admin_only");

        (await SuperAdminFlagAsync(_manager.Id, true)).IsSuperAdmin.ShouldBeTrue();
        (await SuperAdminFlagAsync(_superAdmin.Id, false)).IsSuperAdmin.ShouldBeFalse();
    }

    [Fact]
    public async Task Resetting_two_factor_ends_sessions()
    {
        var staff = await GivenActiveStaffAsync("ani@example.com");
        staff.BeginTwoFactorSetup("SECRET");
        staff.CompleteTwoFactorSetup(1);
        await _db.SaveChangesAsync();

        var dto = await ResetTwoFactorAsync(staff.Id);

        dto.TwoFactorEnabled.ShouldBeFalse();
        (await _db.StaffRefreshTokens.SingleAsync(t => t.StaffUserId == staff.Id)).IsRevoked.ShouldBeTrue();
    }

    [Fact]
    public async Task The_list_is_searched_filtered_and_paged()
    {
        var ani = await GivenActiveStaffAsync("ani@example.com", _operator);
        var suspended = await GivenActiveStaffAsync("zara@example.com");
        suspended.Suspend();
        await _db.SaveChangesAsync();
        var handler = new GetStaffListHandler(_db);

        var all = await handler.HandleAsync(new GetStaffList(), CancellationToken.None);
        all.TotalCount.ShouldBe(4);
        all.Items.Select(s => s.FullName).ShouldBe(new[] { "ani", "Boss", "Manager", "zara" }, ignoreOrder: true);

        (await handler.HandleAsync(new GetStaffList(Search: "ANI"), CancellationToken.None)).Items.ShouldHaveSingleItem().Id.ShouldBe(ani.Id);
        (await handler.HandleAsync(new GetStaffList(Search: "boss@"), CancellationToken.None)).Items.ShouldHaveSingleItem().Id.ShouldBe(_superAdmin.Id);
        (await handler.HandleAsync(new GetStaffList(Status: StaffStatus.Suspended), CancellationToken.None)).Items.ShouldHaveSingleItem().Id.ShouldBe(suspended.Id);
        (await handler.HandleAsync(new GetStaffList(RoleId: _operator.Id), CancellationToken.None)).Items.ShouldHaveSingleItem().Id.ShouldBe(ani.Id);
        (await handler.HandleAsync(new GetStaffList(Page: 2, PageSize: 3), CancellationToken.None)).Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task A_member_comes_with_their_effective_permissions()
    {
        var ani = await GivenActiveStaffAsync("ani@example.com", _operator, _finance);
        var handler = new GetStaffMemberHandler(_db);

        var detail = await handler.HandleAsync(new GetStaffMember(ani.Id), CancellationToken.None);

        detail.Member.Roles.Select(r => r.Name).ShouldBe(new[] { "Finance", "Operator" });
        detail.Permissions.ShouldBe(new[] { Permissions.PartnersApprove, Permissions.PartnersView, Permissions.PaymentsView });
        (await handler.HandleAsync(new GetStaffMember(_superAdmin.Id), CancellationToken.None)).Permissions.ShouldBe(Permissions.All);
        (await Should.ThrowAsync<NotFoundException>(() => handler.HandleAsync(new GetStaffMember(Guid.NewGuid()), CancellationToken.None)))
            .Code.ShouldBe("staff.not_found");
    }

    [Fact]
    public async Task Unknown_or_suspended_actors_are_signed_out()
    {
        var ani = await GivenActiveStaffAsync("ani@example.com");
        ani.Suspend();
        await _db.SaveChangesAsync();

        await Should.ThrowAsync<UnauthorizedException>(() => UpdateAsync(new UpdateStaffMember(_manager.Id, "X", []), As(ani)));
        await Should.ThrowAsync<UnauthorizedException>(() => UpdateAsync(new UpdateStaffMember(_manager.Id, "X", []), new FakeCurrentUser(Guid.NewGuid(), isStaff: true)));
        await Should.ThrowAsync<UnauthorizedException>(() => UpdateAsync(new UpdateStaffMember(_manager.Id, "X", []), new FakeCurrentUser(_manager.Id)));
    }

    [Fact]
    public async Task Invited_staff_cannot_sign_in_before_choosing_a_password()
    {
        await InviteAsync(new InviteStaff("ani@example.com", "Ani", []));
        var signIn = new StaffSignInHandler(_db, new FakePasswordHasher(), new FakeTotpService(), new FakeStaffTokenService(_clock), _clock, Options.Create(_settings));

        (await Should.ThrowAsync<UnauthorizedException>(() => signIn.HandleAsync(new StaffSignIn("ani@example.com", ""), CancellationToken.None)))
            .Code.ShouldBe("staff.invalid_credentials");
    }

    [Fact]
    public async Task Invitations_are_validated()
    {
        var validator = new InviteStaffValidator(_db);

        (await validator.ValidateAsync(new InviteStaff("ani@example.com", "Ani", [_operator.Id]))).IsValid.ShouldBeTrue();
        (await validator.ValidateAsync(new InviteStaff("not-an-email", " ", [Guid.NewGuid()])))
            .Errors.Select(e => $"{e.PropertyName}:{e.ErrorCode}")
            .ShouldBe(new[] { "Email:email.invalid", "FullName:name.required", "RoleIds:roles.invalid" }, ignoreOrder: true);
        (await validator.ValidateAsync(new InviteStaff("ani@example.com", new string('a', 101), null!)))
            .Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "name.too_long", "roles.required" }, ignoreOrder: true);
        (await new UpdateStaffMemberValidator(_db).ValidateAsync(new UpdateStaffMember(Guid.NewGuid(), "Ani", [_finance.Id]))).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Passwords_and_list_queries_are_validated()
    {
        var accept = new AcceptStaffInviteValidator(Options.Create(_settings));
        accept.Validate(new AcceptStaffInvite("token", "Long-Enough-Password")).IsValid.ShouldBeTrue();
        accept.Validate(new AcceptStaffInvite("", "short")).Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "token.required", "password.too_short" }, ignoreOrder: true);
        accept.Validate(new AcceptStaffInvite("token", new string('p', 257))).Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("password.too_long");

        new GetStaffListValidator().Validate(new GetStaffList("s", (StaffStatus)9, null, 0, 201)).Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "status.invalid", "page.invalid", "page_size.invalid" }, ignoreOrder: true);
    }
}
