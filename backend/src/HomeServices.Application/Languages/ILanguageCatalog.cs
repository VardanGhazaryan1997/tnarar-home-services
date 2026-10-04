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
}
