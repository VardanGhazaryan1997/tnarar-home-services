using System.Text.RegularExpressions;
using HomeServices.Domain.Common;

namespace HomeServices.Domain.Localization;

/// <summary>
/// One interface text in one language, e.g. namespace "portal", key "home.title", language "ru".
/// The keys of the default language are the full set; other languages may miss some (they fall back).
/// </summary>
public sealed partial class UiTranslation : AuditableEntity
{
    public const int NamespaceMaxLength = 32;
    public const int KeyMaxLength = 200;
    public const int ValueMaxLength = 4000;

    private UiTranslation()
    {
    }

    /// <summary>Which app the text belongs to: "portal" or "backoffice" (more can be added).</summary>
    public string Namespace { get; private set; } = string.Empty;

    /// <summary>Dotted path, e.g. "catalog.tabs.cities".</summary>
    public string Key { get; private set; } = string.Empty;

    public string LanguageCode { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;

    public static UiTranslation Create(string ns, string key, string languageCode, string value) =>
        new()
        {
            Namespace = NormalizeNamespace(ns),
            Key = ValidKey(key),
            LanguageCode = languageCode.Trim().ToLowerInvariant(),
            Value = ValidValue(value),
        };

    public static bool IsValidNamespace(string? ns) => ns is not null && NamespacePattern().IsMatch(ns.Trim().ToLowerInvariant());

    public static bool IsValidKey(string? key) => key is not null && key.Length <= KeyMaxLength && KeyPattern().IsMatch(key);

    public static string NormalizeNamespace(string ns) =>
        IsValidNamespace(ns) ? ns.Trim().ToLowerInvariant() : throw new DomainException("translation.namespace_invalid", $"'{ns}' is not a valid namespace.");

    public void ChangeValue(string value) => Value = ValidValue(value);

    private static string ValidKey(string key) =>
        IsValidKey(key) ? key : throw new DomainException("translation.key_invalid", $"'{key}' is not a valid key.");

    // Values are kept as written (leading spaces can matter in UI text); empty means "not translated" and isn't stored.
    private static string ValidValue(string value) =>
        string.IsNullOrEmpty(value) || value.Length > ValueMaxLength
            ? throw new DomainException("translation.value_invalid", $"A text of 1–{ValueMaxLength} characters is required.")
            : value;

    [GeneratedRegex("^[a-z][a-z0-9-]{0,31}$")]
    private static partial Regex NamespacePattern();

    // Segments of letters, digits, "_" and "-", joined by dots (i18next nesting); "-" is common in keys like "page-title".
    [GeneratedRegex("^[A-Za-z0-9_-]+(\\.[A-Za-z0-9_-]+)*$")]
    private static partial Regex KeyPattern();
}
