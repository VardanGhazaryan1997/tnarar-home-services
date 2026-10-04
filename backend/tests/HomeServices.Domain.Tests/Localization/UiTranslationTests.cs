using HomeServices.Domain.Localization;

namespace HomeServices.Domain.Tests.Localization;

public class UiTranslationTests
{
    [Fact]
    public void A_text_belongs_to_a_namespace_key_and_language()
    {
        var text = UiTranslation.Create(" Portal ", "home.hero-title", " RU ", " Добро пожаловать");

        text.Namespace.ShouldBe("portal");
        text.Key.ShouldBe("home.hero-title");
        text.LanguageCode.ShouldBe("ru");
        text.Value.ShouldBe(" Добро пожаловать");

        text.ChangeValue("Привет");
        text.Value.ShouldBe("Привет");
    }

    [Theory]
    [InlineData("home")]
    [InlineData("catalog.tabs.cities")]
    [InlineData("errors.district.slug_taken")]
    [InlineData("a1.B_2.c-3")]
    public void Dotted_keys_are_valid(string key)
    {
        UiTranslation.IsValidKey(key).ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(".home")]
    [InlineData("home.")]
    [InlineData("home..title")]
    [InlineData("home title")]
    [InlineData(null)]
    public void Malformed_keys_are_refused(string? key)
    {
        UiTranslation.IsValidKey(key).ShouldBeFalse();
        Should.Throw<DomainException>(() => UiTranslation.Create("portal", key!, "hy", "x")).Code.ShouldBe("translation.key_invalid");
    }

    [Fact]
    public void Keys_have_a_length_limit()
    {
        UiTranslation.IsValidKey(new string('k', UiTranslation.KeyMaxLength)).ShouldBeTrue();
        UiTranslation.IsValidKey(new string('k', UiTranslation.KeyMaxLength + 1)).ShouldBeFalse();
    }

    [Theory]
    [InlineData("1portal")]
    [InlineData("back office")]
    [InlineData("")]
    [InlineData(null)]
    public void Namespaces_are_short_lower_case_names(string? ns)
    {
        UiTranslation.IsValidNamespace(ns).ShouldBeFalse();
        Should.Throw<DomainException>(() => UiTranslation.NormalizeNamespace(ns!)).Code.ShouldBe("translation.namespace_invalid");
    }

    [Fact]
    public void Texts_are_not_empty_and_not_too_long()
    {
        Should.Throw<DomainException>(() => UiTranslation.Create("portal", "a", "hy", "")).Code.ShouldBe("translation.value_invalid");
        Should.Throw<DomainException>(() => UiTranslation.Create("portal", "a", "hy", new string('v', UiTranslation.ValueMaxLength + 1)))
            .Code.ShouldBe("translation.value_invalid");
        Should.Throw<DomainException>(() => UiTranslation.Create("portal", "a", "hy", "x").ChangeValue("")).Code.ShouldBe("translation.value_invalid");
    }

    [Fact]
    public void A_language_can_be_renamed_and_moved()
    {
        var french = Language.Create("fr", "French", "Français", 4);

        french.Update(" French (France) ", " Français ", 2);

        french.Name.ShouldBe("French (France)");
        french.NativeName.ShouldBe("Français");
        french.SortOrder.ShouldBe(2);
        Should.Throw<DomainException>(() => french.Update(new string('n', Language.NameMaxLength + 1), "x", 1)).Code.ShouldBe("language.name_too_long");
        Language.IsValidCode("zh-hans").ShouldBeTrue();
        Language.IsValidCode("french").ShouldBeFalse();
        Language.IsValidCode(null).ShouldBeFalse();
    }
}
