using System.Text.RegularExpressions;
using FluentValidation;
using HomeServices.Application.Languages;
using HomeServices.Domain.Common;

namespace HomeServices.Application.Catalog;

/// <summary>Validation rules shared by the catalog's create and update commands.</summary>
public static partial class CatalogRules
{
    public const int NameMaxLength = 100;

    /// <summary>Lower-case latin letters, digits and dashes, e.g. "exterior-cladding".</summary>
    public static IRuleBuilderOptions<T, string> ValidSlug<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(Slug.IsValid).WithErrorCode("slug.invalid");

    /// <summary>
    /// Names per language code. The default language is required, because every other
    /// language falls back to it; other languages are optional.
    /// </summary>
    public static IRuleBuilderOptions<T, IReadOnlyDictionary<string, string>> LocalizedName<T>(
        this IRuleBuilder<T, IReadOnlyDictionary<string, string>> rule,
        ILanguageCatalog languages) =>
        rule.NotNull().WithErrorCode("name.required")
            .Must(name => name is null || name.Keys.All(code => LanguageCode().IsMatch(code.Trim().ToLowerInvariant())))
            .WithErrorCode("name.language_invalid")
            .Must(name => name is null || name.Values.All(text => (text?.Trim().Length ?? 0) <= NameMaxLength))
            .WithErrorCode("name.too_long")
            .MustAsync(async (name, cancellationToken) =>
                name is null || HasText(name, (await languages.GetAsync(cancellationToken)).DefaultCode))
            .WithErrorCode("name.default_language_required");

    public static IRuleBuilderOptions<T, int> ValidSortOrder<T>(this IRuleBuilder<T, int> rule) =>
        rule.GreaterThanOrEqualTo(0).WithErrorCode("sort_order.invalid");

    private static bool HasText(IReadOnlyDictionary<string, string> name, string language) =>
        name.Any(entry => string.Equals(entry.Key.Trim(), language, StringComparison.OrdinalIgnoreCase)
                          && !string.IsNullOrWhiteSpace(entry.Value));

    [GeneratedRegex("^[a-z]{2,3}(-[a-z0-9]{2,8})?$")]
    private static partial Regex LanguageCode();
}
