namespace HomeServices.Application.Abstractions;

/// <summary>The language chosen for the current request (from Accept-Language) and the platform default.</summary>
public interface ICurrentLanguage
{
    /// <summary>The request language, e.g. "ru".</summary>
    string Code { get; }

    /// <summary>
    /// The language used when a text has no translation in <see cref="Code"/>: the default language, or English for
    /// languages such as Arabic, Persian and Hindi (see <c>LanguageCatalog.FallbackFor</c>).
    /// </summary>
    string DefaultCode { get; }
}
