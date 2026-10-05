namespace HomeServices.Application.Languages;

/// <summary>Active language codes and the default, cached so every request doesn't hit the database.</summary>
public interface ILanguageCatalog
{
    Task<LanguageCatalog> GetAsync(CancellationToken cancellationToken);

    /// <summary>Forgets the cached list after languages change (other API instances catch up within minutes).</summary>
    void Invalidate();
}

public sealed record LanguageCatalog(IReadOnlyList<string> ActiveCodes, string DefaultCode)
{
    /// <summary>Used when no default language is configured.</summary>
    public const string FallbackLanguage = "hy";

    /// <summary>Languages whose speakers are better served by English than by the default language when a text is missing.</summary>
    public static readonly IReadOnlySet<string> EnglishFallbackLanguages = new HashSet<string>(StringComparer.Ordinal) { "ar", "fa", "hi" };

    /// <summary>
    /// The language to show when a text has no translation in <paramref name="language"/>: English for
    /// <see cref="EnglishFallbackLanguages"/> (when English is active), otherwise the default language.
    /// </summary>
    public string FallbackFor(string language) =>
        EnglishFallbackLanguages.Contains(language) && ActiveCodes.Contains("en") ? "en" : DefaultCode;
}
