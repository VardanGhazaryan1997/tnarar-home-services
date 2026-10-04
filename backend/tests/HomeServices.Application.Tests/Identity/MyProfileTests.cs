using HomeServices.Application.Errors;
using HomeServices.Application.Identity;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Identity;

namespace HomeServices.Application.Tests.Identity;

public class MyProfileTests
{
    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();

    private async Task<User> GivenUser()
    {
        var user = User.Register(PhoneNumber.Parse("+37491234567"));
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Returns_the_signed_in_users_profile()
    {
        var user = await GivenUser();

        var me = await new GetMyProfileHandler(_db, new FakeCurrentUser(user.Id)).HandleAsync(new GetMyProfile(), CancellationToken.None);

        me.Id.ShouldBe(user.Id);
        me.PhoneNumber.ShouldBe("+37491234567");
        me.FullName.ShouldBeNull();
        me.Email.ShouldBeNull();
        me.Roles.ShouldBe(new[] { "Customer" });
        me.IsProfileComplete.ShouldBeFalse();
    }

    [Fact]
    public async Task Updates_name_and_email()
    {
        var user = await GivenUser();

        var me = await new UpdateMyProfileHandler(_db, new FakeCurrentUser(user.Id))
            .HandleAsync(new UpdateMyProfile("Ani Petrosyan", "ani@example.com"), CancellationToken.None);

        me.FullName.ShouldBe("Ani Petrosyan");
        me.Email.ShouldBe("ani@example.com");
        me.IsProfileComplete.ShouldBeTrue();
        user.FullName.ShouldBe("Ani Petrosyan");
    }

    [Fact]
    public async Task Requires_a_signed_in_user()
    {
        await Should.ThrowAsync<UnauthorizedException>(
            () => new GetMyProfileHandler(_db, new FakeCurrentUser(null)).HandleAsync(new GetMyProfile(), CancellationToken.None));
        await Should.ThrowAsync<UnauthorizedException>(
            () => new GetMyProfileHandler(_db, new FakeCurrentUser(Guid.NewGuid())).HandleAsync(new GetMyProfile(), CancellationToken.None));
    }

    [Theory]
    [InlineData("", null, "name.required")]
    [InlineData("Ani", "nope", "email.invalid")]
    public void Input_is_validated(string name, string? email, string expectedCode)
    {
        new UpdateMyProfileValidator().Validate(new UpdateMyProfile(name, email))
            .Errors.ShouldContain(e => e.ErrorCode == expectedCode);
    }

    [Fact]
    public void A_name_longer_than_allowed_is_rejected()
    {
        new UpdateMyProfileValidator().Validate(new UpdateMyProfile(new string('a', User.FullNameMaxLength + 1), null))
            .Errors.ShouldContain(e => e.ErrorCode == "name.too_long");
    }
}
