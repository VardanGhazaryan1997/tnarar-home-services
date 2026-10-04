using HomeServices.Domain.Staff;

namespace HomeServices.Domain.Tests.Staff;

public class StaffRefreshTokenTests
{
    [Fact]
    public void Belongs_to_a_staff_member_and_expires()
    {
        var now = new DateTimeOffset(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
        var staffId = Guid.NewGuid();

        var token = StaffRefreshToken.Issue(staffId, "hash", now, TimeSpan.FromHours(12));

        token.StaffUserId.ShouldBe(staffId);
        token.IsActive(now.AddHours(11)).ShouldBeTrue();
        token.IsActive(now.AddHours(12)).ShouldBeFalse();
    }
}
