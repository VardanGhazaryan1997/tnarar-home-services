using HomeServices.Domain.Identity;

namespace HomeServices.Domain.Tests.Identity;

public class UserTests
{
    private static readonly PhoneNumber Phone = PhoneNumber.Parse("+37491234567");
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_new_user_is_an_active_customer_without_a_name_yet()
    {
        var user = User.Register(Phone);

        user.Phone.ShouldBe(Phone);
        user.Roles.ShouldBe(UserRoles.Customer);
        user.Status.ShouldBe(UserStatus.Active);
        user.FullName.ShouldBeNull();
        user.IsProfileComplete.ShouldBeFalse();
    }

    [Fact]
    public void Completing_the_profile_sets_a_trimmed_name_and_optional_email()
    {
        var user = User.Register(Phone);

        user.UpdateProfile("  Ani Petrosyan ", " Ani@Example.com ");

        user.FullName.ShouldBe("Ani Petrosyan");
        user.Email.ShouldBe("ani@example.com");
        user.IsProfileComplete.ShouldBeTrue();
    }

    [Fact]
    public void Email_is_optional()
    {
        var user = User.Register(Phone);

        user.UpdateProfile("Ani", null);
        user.Email.ShouldBeNull();

        user.UpdateProfile("Ani", "  ");
        user.Email.ShouldBeNull();
    }

    [Fact]
    public void Profile_values_are_validated()
    {
        var user = User.Register(Phone);

        Should.Throw<DomainException>(() => user.UpdateProfile(" ", null)).Code.ShouldBe("user.name_required");
        Should.Throw<DomainException>(() => user.UpdateProfile(new string('a', 101), null)).Code.ShouldBe("user.name_too_long");
        Should.Throw<DomainException>(() => user.UpdateProfile("Ani", "not-an-email")).Code.ShouldBe("user.email_invalid");
    }

    [Fact]
    public void Roles_can_be_added_without_losing_existing_ones()
    {
        var user = User.Register(Phone);

        user.AddRole(UserRoles.Partner);

        user.HasRole(UserRoles.Customer).ShouldBeTrue();
        user.HasRole(UserRoles.Partner).ShouldBeTrue();
        user.RoleNames.ShouldBe(new[] { "Customer", "Partner" });
    }

    [Fact]
    public void Remembers_the_last_sign_in()
    {
        var user = User.Register(Phone);

        user.RecordSignIn(Now);

        user.LastSignInAt.ShouldBe(Now);
    }

    [Fact]
    public void Can_be_blocked_and_unblocked()
    {
        var user = User.Register(Phone);

        user.Block();
        user.IsBlocked.ShouldBeTrue();

        user.Unblock();
        user.IsBlocked.ShouldBeFalse();
    }
}
