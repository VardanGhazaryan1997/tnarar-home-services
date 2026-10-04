namespace HomeServices.Domain.Localization;

/// <summary>
/// Text in several languages, keyed by language code (stored as JSONB).
/// Immutable: <see cref="With"/> returns a new value.
/// </summary>
public sealed class LocalizedText : IEquatable<LocalizedText>
{
    private readonly Dictionary<string, string> _values;

    private LocalizedText(Dictionary<string, string> values) => _values = values;

    public static LocalizedText Empty { get; } = new([]);

    public IReadOnlyDictionary<string, string> Values => _values;

    /// <summary>Builds a value, lower-casing codes, trimming text and dropping blank entries.</summary>
    public static LocalizedText From(IReadOnlyDictionary<string, string> values)
    {
        var cleaned = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (language, text) in values)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                cleaned[Normalize(language)] = text.Trim();
            }
        }

        return new LocalizedText(cleaned);
    }

    /// <summary>Returns a copy with the translation set — or removed when <paramref name="text"/> is blank.</summary>
    public LocalizedText With(string language, string text)
    {
        var copy = new Dictionary<string, string>(_values, StringComparer.Ordinal) { [Normalize(language)] = text };
        return From(copy);
    }

    /// <summary>The text in <paramref name="language"/>, else in <paramref name="fallbackLanguage"/>, else any available text.</summary>
    public string Get(string language, string fallbackLanguage)
    {
        if (_values.TryGetValue(Normalize(language), out var text) || _values.TryGetValue(Normalize(fallbackLanguage), out text))
        {
            return text;
        }

        return _values.OrderBy(v => v.Key, StringComparer.Ordinal).Select(v => v.Value).FirstOrDefault() ?? string.Empty;
    }

    public bool Equals(LocalizedText? other) =>
        other is not null
        && other._values.Count == _values.Count
        && _values.All(v => other._values.TryGetValue(v.Key, out var text) && text == v.Value);

    public override bool Equals(object? obj) => obj is LocalizedText other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var (language, text) in _values.OrderBy(v => v.Key, StringComparer.Ordinal))
        {
            hash.Add(language);
            hash.Add(text);
        }

        return hash.ToHashCode();
    }

    private static string Normalize(string language) => language.Trim().ToLowerInvariant();
}
