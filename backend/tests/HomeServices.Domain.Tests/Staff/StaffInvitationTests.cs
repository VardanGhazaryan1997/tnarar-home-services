using HomeServices.Domain.Staff;

namespace HomeServices.Domain.Tests.Staff;

public class StaffInvitationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    private static StaffUser Invited() => StaffUser.Invite(" Ani@Example.com ", " Ani Operator ", "hash:token", Now.AddDays(7));

    [Fact]
    public void An_invited_member_has_no_password_until_they_accept()
    {
        var staff = Invited();

        staff.Email.ShouldBe("ani@example.com");
        staff.FullName.ShouldBe("Ani Operator");
        staff.Status.ShouldBe(StaffStatus.Invited);
        staff.IsInvited.ShouldBeTrue();
        staff.IsSuperAdmin.ShouldBeFalse();
        staff.PasswordHash.ShouldBeEmpty();
        staff.InviteTokenHash.ShouldBe("hash:token");
        staff.InviteExpiresAt.ShouldBe(Now.AddDays(7));
        StaffUser.Invite("boss@example.com", "Boss", "h", Now, isSuperAdmin: true).IsSuperAdmin.ShouldBeTrue();
    }

    [Fact]
    public void Accepting_sets_the_password_and_closes_the_invitation()
    {
        var staff = Invited();

        staff.AcceptInvite("pw:secret", Now.AddDays(1));

        staff.Status.ShouldBe(StaffStatus.Active);
        staff.PasswordHash.ShouldBe("pw:secret");
        staff.InviteTokenHash.ShouldBeNull();
        staff.InviteExpiresAt.ShouldBeNull();
        Should.Throw<DomainException>(() => staff.AcceptInvite("pw:again", Now)).Code.ShouldBe("staff.not_invited");
    }

    [Fact]
    public void An_expired_invitation_cannot_be_accepted_but_can_be_renewed()
    {
        var staff = Invited();

        Should.Throw<DomainException>(() => staff.AcceptInvite("pw:late", Now.AddDays(7))).Code.ShouldBe("staff.invite_expired");

        staff.RenewInvite("hash:new", Now.AddDays(14));
        staff.InviteTokenHash.ShouldBe("hash:new");
        staff.AcceptInvite("pw:secret", Now.AddDays(8));
        staff.Status.ShouldBe(StaffStatus.Active);
    }

    [Fact]
    public void Only_open_invitations_are_renewed()
    {
        var active = StaffUser.Create("ani@example.com", "Ani", "hash");

        Should.Throw<DomainException>(() => active.RenewInvite("hash:new", Now)).Code.ShouldBe("staff.not_invited");
    }

    [Fact]
    public void Suspending_cancels_an_invitation_and_activating_invites_again()
    {
        var staff = Invited();

        staff.Suspend();
        staff.Status.ShouldBe(StaffStatus.Suspended);
        staff.InviteTokenHash.ShouldBeNull();

        staff.Activate();
        staff.Status.ShouldBe(StaffStatus.Invited);
        Should.Throw<DomainException>(() => staff.AcceptInvite("pw:x", Now)).Code.ShouldBe("staff.not_invited");
    }

    [Fact]
    public void Names_can_change_within_limits()
    {
        var staff = Invited();

        staff.Rename("  Ani Petrosyan ");

        staff.FullName.ShouldBe("Ani Petrosyan");
        Should.Throw<DomainException>(() => staff.Rename(" ")).Code.ShouldBe("staff.name_invalid");
    }

    [Fact]
    public void Resetting_two_factor_forgets_the_authenticator()
    {
        var staff = StaffUser.Create("ani@example.com", "Ani", "hash");
        staff.BeginTwoFactorSetup("SECRET");
        staff.CompleteTwoFactorSetup(100);

        staff.ResetTwoFactor();

        staff.TwoFactorEnabled.ShouldBeFalse();
        staff.PendingTotpSecret.ShouldBeNull();
        staff.LastTotpTimeStep.ShouldBeNull();
        Should.NotThrow(() => staff.BeginTwoFactorSetup("NEW"));
    }

    [Fact]
    public void Built_in_roles_can_get_a_new_description()
    {
        var role = Role.Create("Operator", "Old", [Permissions.PartnersView], isSystem: true);

        role.Describe(" Reviews partners ");

        role.Name.ShouldBe("Operator");
        role.Description.ShouldBe("Reviews partners");
    }
}
