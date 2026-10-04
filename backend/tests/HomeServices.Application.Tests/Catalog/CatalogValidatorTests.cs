using HomeServices.Application.Catalog;
using HomeServices.Application.Tests.Support;

namespace HomeServices.Application.Tests.Catalog;

public class CatalogValidatorTests
{
    private static readonly Dictionary<string, string> ValidName = new() { ["hy"] = "Սանտեխնիկա", ["en"] = "Plumbing" };

    private static async Task<string[]> ErrorsFor(CreateCategory command, string defaultLanguage = "hy") =>
        (await new CreateCategoryValidator(new FakeLanguageCatalog(defaultLanguage)).ValidateAsync(command))
            .Errors.Select(e => e.ErrorCode).ToArray();

    private static CreateCategory Category(
        string slug = "plumbing",
        Dictionary<string, string>? name = null,
        string? icon = null,
        int sortOrder = 1) =>
        new(slug, name ?? ValidName, icon, null, sortOrder);

    [Fact]
    public async Task A_complete_category_is_valid()
    {
        (await ErrorsFor(Category())).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("two words")]
    [InlineData("")]
    [InlineData("-dash")]
    public async Task The_slug_must_be_valid(string slug)
    {
        (await ErrorsFor(Category(slug))).ShouldBe(new[] { "slug.invalid" });
    }

    [Fact]
    public async Task A_name_is_required()
    {
        (await ErrorsFor(new CreateCategory("plumbing", null!, null, null, 1))).ShouldBe(new[] { "name.required" });
    }

    [Fact]
    public async Task The_name_in_the_default_language_is_required()
    {
        (await ErrorsFor(Category(name: new() { ["en"] = "Plumbing" }))).ShouldBe(new[] { "name.default_language_required" });
        (await ErrorsFor(Category(name: new() { ["hy"] = "  " }))).ShouldBe(new[] { "name.default_language_required" });
    }

    [Fact]
    public async Task The_default_language_comes_from_the_language_settings()
    {
        (await ErrorsFor(Category(name: new() { ["en"] = "Plumbing" }), defaultLanguage: "en")).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("english")]
    [InlineData("e")]
    [InlineData("en_US")]
    public async Task Language_codes_must_look_like_language_codes(string code)
    {
        (await ErrorsFor(Category(name: new() { ["hy"] = "Սանտեխնիկա", [code] = "Plumbing" })))
            .ShouldBe(new[] { "name.language_invalid" });
    }

    [Fact]
    public async Task Regional_language_codes_are_accepted()
    {
        (await ErrorsFor(Category(name: new() { ["hy"] = "Սանտեխնիկա", ["en-GB"] = "Plumbing" }))).ShouldBeEmpty();
    }

    [Fact]
    public async Task Names_have_a_maximum_length()
    {
        (await ErrorsFor(Category(name: new() { ["hy"] = new string('ա', CatalogRules.NameMaxLength + 1) })))
            .ShouldBe(new[] { "name.too_long" });
    }

    [Fact]
    public async Task Icon_and_sort_order_are_checked()
    {
        (await ErrorsFor(Category(icon: new string('x', 33), sortOrder: -1))).ShouldBe(new[] { "icon.too_long", "sort_order.invalid" });
    }

    [Fact]
    public async Task Update_and_place_commands_use_the_same_rules()
    {
        var languages = new FakeLanguageCatalog();
        var noDefault = new Dictionary<string, string> { ["en"] = "x" };

        (await new UpdateCategoryValidator(languages).ValidateAsync(new UpdateCategory(Guid.NewGuid(), "bad slug", ValidName, null, null, 1)))
            .Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "slug.invalid" });
        (await new CreateCityValidator(languages).ValidateAsync(new CreateCity("masis", noDefault, 1)))
            .Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "name.default_language_required" });
        (await new UpdateCityValidator(languages).ValidateAsync(new UpdateCity(Guid.NewGuid(), "masis", ValidName, -1)))
            .Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "sort_order.invalid" });
        (await new AddDistrictValidator(languages).ValidateAsync(new AddDistrict(Guid.NewGuid(), "x y", ValidName, 1)))
            .Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "slug.invalid" });
        (await new UpdateDistrictValidator(languages).ValidateAsync(new UpdateDistrict(Guid.NewGuid(), Guid.NewGuid(), "ok", ValidName, 1)))
            .IsValid.ShouldBeTrue();
    }
}
