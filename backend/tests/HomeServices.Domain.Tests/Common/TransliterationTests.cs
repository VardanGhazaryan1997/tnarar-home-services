using HomeServices.Domain.Common;

namespace HomeServices.Domain.Tests.Common;

public class TransliterationTests
{
    [Theory]
    [InlineData("Արամ Սանտեխնիկ", "aram-santekhnik")]
    [InlineData("Ջուր և Լույս", "jur-ev-luys")]
    [InlineData("Խաչատրյան ՍՊԸ", "khachatryan-spy")]
    [InlineData("Сантехник Иван", "santekhnik-ivan")]
    [InlineData("Щётка & Ёлка", "shchetka-elka")]
    [InlineData("Объявление", "obyavlenie")]
    [InlineData("  Best   Build -- LLC! ", "best-build-llc")]
    [InlineData("Mix Արամ и Co 24/7", "mix-aram-i-co-24-7")]
    public void Names_become_readable_Latin_slugs(string text, string slug)
    {
        Transliteration.ToSlug(text).ShouldBe(slug);
        Slug.IsValid(slug).ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("☺☺☺")]
    [InlineData(null)]
    public void Nothing_usable_gives_an_empty_slug(string? text)
    {
        Transliteration.ToSlug(text).ShouldBeEmpty();
    }

    [Fact]
    public void Long_names_are_cut_without_a_trailing_dash()
    {
        Transliteration.ToSlug("abcd efgh", maxLength: 5).ShouldBe("abcd");
        Transliteration.ToSlug(new string('a', 100)).Length.ShouldBe(Slug.MaxLength);
    }
}
