namespace HomeServices.Application.Abstractions;

/// <summary>The language chosen for the current request (from Accept-Language) and the platform default.</summary>
public interface ICurrentLanguage
{
    /// <summary>The request language, e.g. "ru".</summary>
    string Code { get; }

    /// <summary>The default language, used when a text has no translation in <see cref="Code"/>.</summary>
    string DefaultCode { get; }
}
