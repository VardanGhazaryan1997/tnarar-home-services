using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using HomeServices.Domain.Localization;

namespace HomeServices.Application.Translations;

/// <summary>
/// Converts between i18next JSON files (nested objects, e.g. <c>{ "home": { "title": "…" } }</c>; dotted
/// property names also work) and flat dotted keys ("home.title").
/// </summary>
public static class TranslationJson
{
    // Armenian and Russian stay readable in exported files (no \u escapes).
    private static readonly JsonSerializerOptions ReadableJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public sealed record FlattenResult(IReadOnlyDictionary<string, string> Texts, IReadOnlyList<string> Invalid);

    /// <summary>
    /// Every text in <paramref name="json"/> by dotted key. Leaves that aren't strings, empty texts, bad keys and
    /// texts that are too long are reported in <see cref="FlattenResult.Invalid"/> instead.
    /// </summary>
    public static FlattenResult Flatten(JsonElement json)
    {
        var texts = new Dictionary<string, string>(StringComparer.Ordinal);
        var invalid = new List<string>();
        if (json.ValueKind != JsonValueKind.Object)
        {
            invalid.Add("$");
            return new FlattenResult(texts, invalid);
        }

        Walk(json, prefix: null, texts, invalid);
        return new FlattenResult(texts, invalid);
    }

    /// <summary>Nested JSON for <paramref name="texts"/>, keys in order. A key that would sit under a text is skipped.</summary>
    public static string Nest(IEnumerable<KeyValuePair<string, string>> texts)
    {
        var root = new JsonObject();
        foreach (var (key, value) in texts.OrderBy(t => t.Key, StringComparer.Ordinal))
        {
            var parts = key.Split('.');
            var node = root;
            var placed = true;
            foreach (var part in parts[..^1])
            {
                if (node[part] is JsonObject existing)
                {
                    node = existing;
                }
                else if (node[part] is null)
                {
                    var created = new JsonObject();
                    node[part] = created;
                    node = created;
                }
                else
                {
                    placed = false;
                    break;
                }
            }

            if (placed && node[parts[^1]] is null)
            {
                node[parts[^1]] = value;
            }
        }

        return root.ToJsonString(ReadableJson);
    }

    /// <summary>True when one key would have to be both a text and a group of texts ("home" and "home.title").</summary>
    public static bool Conflicts(string key, IEnumerable<string> existingKeys) =>
        existingKeys.Any(existing => existing != key
            && (key.StartsWith(existing + ".", StringComparison.Ordinal) || existing.StartsWith(key + ".", StringComparison.Ordinal)));

    private static void Walk(JsonElement element, string? prefix, Dictionary<string, string> texts, List<string> invalid)
    {
        foreach (var property in element.EnumerateObject())
        {
            var key = prefix is null ? property.Name : $"{prefix}.{property.Name}";
            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    Walk(property.Value, key, texts, invalid);
                    break;
                case JsonValueKind.String when UiTranslation.IsValidKey(key)
                    && property.Value.GetString() is { Length: > 0 and <= UiTranslation.ValueMaxLength } text
                    && !Conflicts(key, texts.Keys):
                    texts[key] = text;
                    break;
                default:
                    invalid.Add(key);
                    break;
            }
        }
    }
}
