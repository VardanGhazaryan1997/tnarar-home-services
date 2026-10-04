using HomeServices.Domain.Staff;

namespace HomeServices.Domain.Tests.Staff;

public class StaffUserTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    private static StaffUser NewStaff(bool superAdmin = false) => StaffUser.Create(" Admin@Example.com ", " Vardan ", "hash", superAdmin);

    [Fact]
    public void A_new_staff_member_is_active_without_two_factor_yet()
    {
        var staff = NewStaff(superAdmin: true);

        staff.Email.ShouldBe("admin@example.com");
        staff.FullName.ShouldBe("Vardan");
        staff.PasswordHash.ShouldBe("hash");
        staff.IsSuperAdmin.ShouldBeTrue();
        staff.Status.ShouldBe(StaffStatus.Active);
        staff.TwoFactorEnabled.ShouldBeFalse();
    }

    [Fact]
    public void Email_and_name_are_validated()
    {
        Should.Throw<DomainException>(() => StaffUser.Create("not-an-email", "Ani", "hash")).Code.ShouldBe("staff.email_invalid");
        Should.Throw<DomainException>(() => StaffUser.Create("ani@example.com", " ", "hash")).Code.ShouldBe("staff.name_invalid");
        Should.Throw<DomainException>(() => StaffUser.Create("ani@example.com", new string('a', 101), "hash")).Code.ShouldBe("staff.name_invalid");
    }

    [Fact]
    public void Repeated_wrong_passwords_lock_the_account_for_a_while()
    {
        var staff = NewStaff();

        staff.RecordFailedSignIn(Now, maxAttempts: 3, TimeSpan.FromMinutes(15));
        staff.RecordFailedSignIn(Now, maxAttempts: 3, TimeSpan.FromMinutes(15));
        staff.IsLockedOut(Now).ShouldBeFalse();
        staff.FailedSignInCount.ShouldBe(2);

        staff.RecordFailedSignIn(Now, maxAttempts: 3, TimeSpan.FromMinutes(15));

        staff.IsLockedOut(Now.AddMinutes(14)).ShouldBeTrue();
        staff.IsLockedOut(Now.AddMinutes(15)).ShouldBeFalse();
        staff.FailedSignInCount.ShouldBe(0);
    }

    [Fact]
    public void A_correct_password_resets_failed_attempts()
    {
        var staff = NewStaff();
        staff.RecordFailedSignIn(Now, 3, TimeSpan.FromMinutes(15));

        staff.RecordPasswordAccepted();

        staff.FailedSignInCount.ShouldBe(0);
        staff.LockedUntil.ShouldBeNull();
    }

    [Fact]
    public void Two_factor_becomes_active_only_after_setup_is_confirmed()
    {
        var staff = NewStaff();

        staff.BeginTwoFactorSetup("SECRET");
        staff.TwoFactorEnabled.ShouldBeFalse();
        staff.PendingTotpSecret.ShouldBe("SECRET");

        staff.CompleteTwoFactorSetup(timeStep: 100);

        staff.TwoFactorEnabled.ShouldBeTrue();
        staff.TotpSecret.ShouldBe("SECRET");
        staff.PendingTotpSecret.ShouldBeNull();
        staff.LastTotpTimeStep.ShouldBe(100);
    }

    [Fact]
    public void Setup_cannot_be_confirmed_before_it_starts_or_restarted_once_enabled()
    {
        var staff = NewStaff();

        Should.Throw<DomainException>(() => staff.CompleteTwoFactorSetup(1)).Code.ShouldBe("staff.two_factor_setup_not_started");

        staff.BeginTwoFactorSetup("SECRET");
        staff.CompleteTwoFactorSetup(1);

        Should.Throw<DomainException>(() => staff.BeginTwoFactorSetup("OTHER")).Code.ShouldBe("staff.two_factor_already_enabled");
    }

    [Fact]
    public void An_authenticator_code_cannot_be_reused()
    {
        var staff = NewStaff();
        staff.BeginTwoFactorSetup("SECRET");
        staff.CompleteTwoFactorSetup(100);

        Should.Throw<DomainException>(() => staff.AcceptTotpTimeStep(100)).Code.ShouldBe("staff.totp_already_used");
        Should.Throw<DomainException>(() => staff.AcceptTotpTimeStep(99));

        staff.AcceptTotpTimeStep(101);
        staff.LastTotpTimeStep.ShouldBe(101);
    }

    [Fact]
    public void The_first_code_is_accepted_when_none_was_used_before()
    {
        var staff = NewStaff();

        staff.AcceptTotpTimeStep(5);

        staff.LastTotpTimeStep.ShouldBe(5);
    }

    [Fact]
    public void Records_sign_in_password_changes_and_suspension()
    {
        var staff = NewStaff();

        staff.RecordSignIn(Now);
        staff.ChangePasswordHash("new-hash");
        staff.Suspend();

        staff.LastSignInAt.ShouldBe(Now);
        staff.PasswordHash.ShouldBe("new-hash");
        staff.IsSuspended.ShouldBeTrue();

        staff.Activate();
        staff.IsSuspended.ShouldBeFalse();
    }
}

public class StaffUserRoleTests
{
    [Fact]
    public void Roles_can_be_assigned_once_and_removed()
    {
        var staff = StaffUser.Create("ani@example.com", "Ani", "hash");
        var operatorRole = Guid.NewGuid();
        var financeRole = Guid.NewGuid();

        staff.AssignRole(operatorRole);
        staff.AssignRole(operatorRole);
        staff.AssignRole(financeRole);

        staff.RoleIds.ShouldBe(new[] { operatorRole, financeRole });
        staff.Roles.ShouldAllBe(r => r.StaffUserId == staff.Id);

        staff.RemoveRole(operatorRole);

        staff.RoleIds.ShouldBe(new[] { financeRole });
    }

    [Fact]
    public void Super_Admin_can_be_granted_and_revoked()
    {
        var staff = StaffUser.Create("ani@example.com", "Ani", "hash");

        staff.MakeSuperAdmin();
        staff.IsSuperAdmin.ShouldBeTrue();

        staff.RevokeSuperAdmin();
        staff.IsSuperAdmin.ShouldBeFalse();
    }
}
