using System.Globalization;

namespace HomeServices.Application.Languages;

/// <summary>Chooses a language for a request from its Accept-Language header.</summary>
public static class LanguageNegotiator
{
    /// <summary>
    /// Returns the first active language the client accepts (by preference weight), matching
    /// either the full tag ("zh-hans") or its base ("ru-RU" → "ru"); otherwise the default.
    /// </summary>
    public static string Pick(string? acceptLanguage, IReadOnlyCollection<string> activeLanguages, string defaultLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage))
        {
            return defaultLanguage;
        }

        var preferences = acceptLanguage
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Parse)
            .Where(p => p.Weight > 0)
            .OrderByDescending(p => p.Weight); // stable: equal weights keep header order

        foreach (var (tag, _) in preferences)
        {
            if (activeLanguages.Contains(tag))
            {
                return tag;
            }

            var baseLanguage = tag.Split('-')[0];
            if (activeLanguages.Contains(baseLanguage))
            {
                return baseLanguage;
            }
        }

        return defaultLanguage;
    }

    private static (string Tag, double Weight) Parse(string part)
    {
        var pieces = part.Split(';', StringSplitOptions.TrimEntries);
        var tag = pieces[0].ToLowerInvariant();
        var weight = 1.0;

        var quality = pieces.Skip(1).FirstOrDefault(p => p.StartsWith("q=", StringComparison.OrdinalIgnoreCase));
        if (quality is not null && double.TryParse(quality[2..], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            weight = parsed;
        }

        return (tag, weight);
    }
}
