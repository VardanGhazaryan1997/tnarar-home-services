using HomeServices.Domain.Identity;

namespace HomeServices.Domain.Tests.Identity;

public class RefreshTokenTests
{
    private static readonly DateTimeOffset Issued = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Is_active_until_it_expires()
    {
        var userId = Guid.NewGuid();
        var token = RefreshToken.Issue(userId, "hash", Issued, TimeSpan.FromDays(30));

        token.UserId.ShouldBe(userId);
        token.TokenHash.ShouldBe("hash");
        token.ExpiresAt.ShouldBe(Issued.AddDays(30));
        token.IsActive(Issued.AddDays(29)).ShouldBeTrue();
        token.IsActive(Issued.AddDays(30)).ShouldBeFalse();
    }

    [Fact]
    public void Revoking_ends_it_and_records_its_replacement()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), "old", Issued, TimeSpan.FromDays(30));

        token.Revoke(Issued.AddHours(1), replacedByHash: "new");

        token.IsActive(Issued.AddHours(2)).ShouldBeFalse();
        token.RevokedAt.ShouldBe(Issued.AddHours(1));
        token.ReplacedByHash.ShouldBe("new");
        token.IsRevoked.ShouldBeTrue();
    }

    [Fact]
    public void Revoking_twice_keeps_the_first_revocation()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), "old", Issued, TimeSpan.FromDays(30));
        token.Revoke(Issued.AddHours(1));

        token.Revoke(Issued.AddHours(5), "later");

        token.RevokedAt.ShouldBe(Issued.AddHours(1));
        token.ReplacedByHash.ShouldBeNull();
    }
}
