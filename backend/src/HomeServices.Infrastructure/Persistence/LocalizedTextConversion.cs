using System.Text.Json;
using HomeServices.Domain.Localization;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HomeServices.Infrastructure.Persistence;

/// <summary>Stores <see cref="LocalizedText"/> as a JSON object: {"hy": "…", "ru": "…"}.</summary>
public sealed class LocalizedTextConverter() : ValueConverter<LocalizedText, string>(
    text => ToJson(text),
    json => FromJson(json))
{
    public static string ToJson(LocalizedText text) => JsonSerializer.Serialize(text.Values);

    public static LocalizedText FromJson(string json) =>
        LocalizedText.From(JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? []);
}

/// <summary>LocalizedText is immutable, so value equality is enough for change tracking.</summary>
public sealed class LocalizedTextComparer() : ValueComparer<LocalizedText>(
    (a, b) => object.Equals(a, b),
    text => text.GetHashCode(),
    text => text);
