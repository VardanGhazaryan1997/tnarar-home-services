using System.Text.RegularExpressions;

namespace HomeServices.Domain.Common;

/// <summary>URL-friendly identifiers: lower-case latin letters, digits and single dashes, e.g. "exterior-cladding".</summary>
public static partial class Slug
{
    public const int MaxLength = 64;

    /// <summary>Trims and lower-cases <paramref name="value"/>, or throws a DomainException with <paramref name="errorCode"/>.</summary>
    public static string Normalize(string value, string errorCode)
    {
        if (!IsValid(value))
        {
            throw new DomainException(errorCode, $"'{value}' is not a valid slug.");
        }

        return value.Trim().ToLowerInvariant();
    }

    /// <summary>True when <paramref name="value"/> is a valid slug after trimming and lower-casing.</summary>
    public static bool IsValid(string? value)
    {
        var slug = value?.Trim().ToLowerInvariant() ?? string.Empty;
        return slug.Length <= MaxLength && Pattern().IsMatch(slug);
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex Pattern();
}
