using HomeServices.Domain.Localization;

namespace HomeServices.Domain.Tests.Localization;

public class LanguageTests
{
    [Fact]
    public void A_new_language_starts_inactive_and_not_default()
    {
        var language = Language.Create("fr", "French", "Français", sortOrder: 4);

        language.Code.ShouldBe("fr");
        language.Name.ShouldBe("French");
        language.NativeName.ShouldBe("Français");
        language.SortOrder.ShouldBe(4);
        language.IsActive.ShouldBeFalse();
        language.IsDefault.ShouldBeFalse();
    }

    [Theory]
    [InlineData(" HY ", "hy")]
    [InlineData("zh-Hans", "zh-hans")]
    [InlineData("ast", "ast")]
    public void Codes_are_normalised_to_lower_case(string code, string expected)
    {
        Language.Create(code, "Name", "Native", 1).Code.ShouldBe(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("armenian")]
    [InlineData("hy_AM")]
    [InlineData("12")]
    public void Invalid_codes_are_rejected(string code)
    {
        var exception = Should.Throw<DomainException>(() => Language.Create(code, "Name", "Native", 1));

        exception.Code.ShouldBe("language.code_invalid");
    }

    [Fact]
    public void Names_are_required_and_trimmed()
    {
        Should.Throw<DomainException>(() => Language.Create("fr", " ", "Français", 1)).Code.ShouldBe("language.name_required");
        Should.Throw<DomainException>(() => Language.Create("fr", "French", "", 1)).Code.ShouldBe("language.native_name_required");
        Language.Create("fr", " French ", " Français ", 1).NativeName.ShouldBe("Français");
    }

    [Fact]
    public void Can_be_activated_and_deactivated()
    {
        var language = Language.Create("fr", "French", "Français", 4);

        language.Activate();
        language.IsActive.ShouldBeTrue();

        language.Deactivate();
        language.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Only_an_active_language_can_become_the_default()
    {
        var language = Language.Create("fr", "French", "Français", 4);

        Should.Throw<DomainException>(language.MakeDefault).Code.ShouldBe("language.inactive_cannot_be_default");

        language.Activate();
        language.MakeDefault();

        language.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void The_default_language_cannot_be_deactivated_until_another_becomes_default()
    {
        var language = Language.Create("hy", "Armenian", "Հայերեն", 1);
        language.Activate();
        language.MakeDefault();

        Should.Throw<DomainException>(language.Deactivate).Code.ShouldBe("language.default_cannot_be_deactivated");

        language.RemoveDefault();
        language.Deactivate();

        language.IsActive.ShouldBeFalse();
    }
}
