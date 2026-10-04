using FluentValidation;
using HomeServices.Application.Languages;
using HomeServices.Domain.Localization;

namespace HomeServices.Application.Common;

/// <summary>Validation for texts sent per language (<c>{ "hy": "…", "ru": "…" }</c>).</summary>
public static class LocalizedRules
{
    /// <summary>
    /// Language codes must be valid and texts at most <paramref name="maxLength"/> long. With <paramref name="defaultRequired"/>,
    /// the default language needs a text (other languages fall back to it). Error codes start with <paramref name="field"/>,
    /// e.g. "title.required", "title.language_invalid", "title.too_long", "title.default_language_required".
    /// </summary>
    public static IRuleBuilderOptions<T, IReadOnlyDictionary<string, string>> LocalizedText<T>(
        this IRuleBuilder<T, IReadOnlyDictionary<string, string>> rule,
        ILanguageCatalog languages,
        string field,
        int maxLength,
        bool defaultRequired = true) =>
        rule.NotNull().WithErrorCode($"{field}.required")
            .Must(texts => texts is null || texts.Keys.All(Language.IsValidCode)).WithErrorCode($"{field}.language_invalid")
            .Must(texts => texts is null || texts.Values.All(text => (text?.Trim().Length ?? 0) <= maxLength)).WithErrorCode($"{field}.too_long")
            .MustAsync(async (texts, cancellationToken) =>
                !defaultRequired || texts is null || HasText(texts, (await languages.GetAsync(cancellationToken)).DefaultCode))
            .WithErrorCode($"{field}.default_language_required");

    private static bool HasText(IReadOnlyDictionary<string, string> texts, string language) =>
        texts.Any(entry => string.Equals(entry.Key.Trim(), language, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(entry.Value));
}
