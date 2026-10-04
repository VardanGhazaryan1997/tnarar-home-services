using HomeServices.Domain.Common;

namespace HomeServices.Domain.Tests.Common;

public class SlugTests
{
    [Theory]
    [InlineData("plumbing", "plumbing")]
    [InlineData(" Exterior-Cladding ", "exterior-cladding")]
    [InlineData("nor-nork-2", "nor-nork-2")]
    public void Valid_slugs_are_trimmed_and_lower_cased(string value, string expected)
    {
        Slug.Normalize(value, "x.slug_invalid").ShouldBe(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("two words")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    [InlineData("double--dash")]
    [InlineData("Երևան")]
    public void Invalid_slugs_throw_with_the_given_code(string value)
    {
        Should.Throw<DomainException>(() => Slug.Normalize(value, "city.slug_invalid")).Code.ShouldBe("city.slug_invalid");
    }

    [Fact]
    public void Slugs_longer_than_64_characters_are_rejected()
    {
        Should.Throw<DomainException>(() => Slug.Normalize(new string('a', 65), "x.slug_invalid"));
        Slug.Normalize(new string('a', 64), "x.slug_invalid").Length.ShouldBe(64);
    }

    [Theory]
    [InlineData("plumbing", true)]
    [InlineData(" Exterior-Cladding ", true)]
    [InlineData("two  words", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_checks_without_throwing(string? value, bool expected)
    {
        Slug.IsValid(value).ShouldBe(expected);
    }
}
