using HomeServices.Application.Languages;
using HomeServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HomeServices.Infrastructure.Localization;

/// <summary>Reads active languages from the database and caches them for a few minutes.</summary>
public sealed class CachedLanguageCatalog(AppDbContext db, IMemoryCache cache) : ILanguageCatalog
{
    internal const string CacheKey = "language-catalog";
    internal static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<LanguageCatalog> GetAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey, out LanguageCatalog? cached) && cached is not null)
        {
            return cached;
        }

        var languages = await db.Languages
            .AsNoTracking()
            .Where(l => l.IsActive)
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.Code)
            .Select(l => new { l.Code, l.IsDefault })
            .ToListAsync(cancellationToken);

        var catalog = new LanguageCatalog(
            languages.Select(l => l.Code).ToList(),
            languages.FirstOrDefault(l => l.IsDefault)?.Code ?? LanguageCatalog.FallbackLanguage);

        cache.Set(CacheKey, catalog, CacheDuration);
        return catalog;
    }

    public void Invalidate() => cache.Remove(CacheKey);
}
