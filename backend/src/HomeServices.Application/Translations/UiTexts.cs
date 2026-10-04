using System.Security.Cryptography;
using System.Text;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Languages;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HomeServices.Application.Translations;

/// <summary>
/// The interface texts of one app in one language, as i18next JSON. Texts not yet translated come in the
/// default language. Unknown or inactive languages are 404 "language.not_found".
/// </summary>
public sealed record GetUiTexts(string Language, string Namespace) : IQuery<UiTextsDto>;

/// <summary>The JSON and its version tag (for HTTP ETag caching).</summary>
public sealed record UiTextsDto(string Json, string ETag);

/// <summary>Bumped on every change to texts or languages, so cached JSON is rebuilt.</summary>
public sealed class UiTextsVersion
{
    private long _value;

    public long Current => Interlocked.Read(ref _value);

    public void Bump() => Interlocked.Increment(ref _value);
}

public sealed class GetUiTextsHandler(IAppDbContext db, ILanguageCatalog languages, IMemoryCache cache, UiTextsVersion version)
    : IQueryHandler<GetUiTexts, UiTextsDto>
{
    // Other API instances see changes after this long at most.
    public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<UiTextsDto> HandleAsync(GetUiTexts query, CancellationToken cancellationToken)
    {
        var language = query.Language.Trim().ToLowerInvariant();
        var catalog = await languages.GetAsync(cancellationToken);
        if (!catalog.ActiveCodes.Contains(language) && language != catalog.DefaultCode)
        {
            throw new NotFoundException($"'{query.Language}' is not an available language.", "language.not_found");
        }

        if (!UiTranslation.IsValidNamespace(query.Namespace))
        {
            throw new NotFoundException($"'{query.Namespace}' is not a text namespace.", "translation.namespace_not_found");
        }

        var ns = UiTranslation.NormalizeNamespace(query.Namespace);
        var cacheKey = $"ui-texts:{version.Current}:{ns}:{language}:{catalog.DefaultCode}";
        if (cache.TryGetValue(cacheKey, out UiTextsDto? cached) && cached is not null)
        {
            return cached;
        }

        var rows = await db.UiTranslations.AsNoTracking()
            .Where(t => t.Namespace == ns && (t.LanguageCode == language || t.LanguageCode == catalog.DefaultCode))
            .Select(t => new { t.Key, t.LanguageCode, t.Value })
            .ToListAsync(cancellationToken);

        // Default-language keys are the full set; the requested language overrides them.
        var texts = rows.Where(r => r.LanguageCode == catalog.DefaultCode).ToDictionary(r => r.Key, r => r.Value, StringComparer.Ordinal);
        foreach (var row in rows.Where(r => r.LanguageCode == language && texts.ContainsKey(r.Key)))
        {
            texts[row.Key] = row.Value;
        }

        var json = TranslationJson.Nest(texts);
        var result = new UiTextsDto(json, $"\"{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json)))[..32]}\"");
        cache.Set(cacheKey, result, CacheDuration);
        return result;
    }
}
