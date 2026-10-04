using HomeServices.Domain.Identity;

namespace HomeServices.Domain.Tests.Identity;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("+37491234567", "+37491234567")]
    [InlineData("+374 91 23-45-67", "+37491234567")]
    [InlineData("091 234 567", "+37491234567")]
    [InlineData("(091) 23 45 67", "+37491234567")]
    [InlineData("37491234567", "+37491234567")]
    [InlineData("0037491234567", "+37491234567")]
    [InlineData("+79161234567", "+79161234567")]
    public void Accepts_common_ways_of_writing_a_number_and_stores_E164(string input, string expected)
    {
        PhoneNumber.Parse(input).Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("+3749123456")]
    [InlineData("+374912345678")]
    [InlineData("phone")]
    [InlineData("+0123456789")]
    public void Rejects_invalid_numbers(string input)
    {
        Should.Throw<DomainException>(() => PhoneNumber.Parse(input)).Code.ShouldBe("phone.invalid");
        PhoneNumber.TryParse(input, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryParse_returns_the_number_when_valid()
    {
        PhoneNumber.TryParse("091234567", out var phone).ShouldBeTrue();
        phone!.Value.ShouldBe("+37491234567");
    }

    [Fact]
    public void Numbers_with_the_same_digits_are_equal()
    {
        PhoneNumber.Parse("091 234 567").ShouldBe(PhoneNumber.Parse("+37491234567"));
        PhoneNumber.Parse("+37491234567").ToString().ShouldBe("+37491234567");
    }
}
