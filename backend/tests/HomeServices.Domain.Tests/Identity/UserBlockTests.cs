using HomeServices.Domain.Identity;

namespace HomeServices.Domain.Tests.Identity;

public class UserBlockTests
{
    private static User NewUser() => User.Register(PhoneNumber.Parse("+37477123456"));

    [Fact]
    public void Blocking_keeps_the_reason_until_unblocked()
    {
        var user = NewUser();

        user.Block("  Fake reviews ");

        user.IsBlocked.ShouldBeTrue();
        user.BlockReason.ShouldBe("Fake reviews");

        user.Unblock();
        user.IsBlocked.ShouldBeFalse();
        user.BlockReason.ShouldBeNull();
    }

    [Fact]
    public void A_blank_reason_is_stored_as_none_and_long_ones_are_refused()
    {
        var user = NewUser();

        user.Block(" ");
        user.BlockReason.ShouldBeNull();

        Should.Throw<DomainException>(() => user.Block(new string('r', User.BlockReasonMaxLength + 1))).Code.ShouldBe("user.block_reason_too_long");
    }
}
