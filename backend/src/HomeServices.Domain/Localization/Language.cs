using System.Text.RegularExpressions;
using HomeServices.Domain.Common;

namespace HomeServices.Domain.Localization;

/// <summary>
/// A language the platform can show. New languages are added as data from the
/// Back Office; they stay inactive until translated and activated.
/// </summary>
public sealed partial class Language : Entity, IAudited
{
    private Language()
    {
    }

    /// <summary>Lower-case BCP 47 code, e.g. "hy", "ru", "en", "zh-hans".</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>English name, e.g. "Armenian".</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Name in the language itself, e.g. "Հայերեն" — shown in the language switcher.</summary>
    public string NativeName { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public bool IsDefault { get; private set; }

    public int SortOrder { get; private set; }

    public const int NameMaxLength = 64;

    public static Language Create(string code, string name, string nativeName, int sortOrder)
    {
        var normalizedCode = code.Trim().ToLowerInvariant();
        if (!IsValidCode(normalizedCode))
        {
            throw new DomainException("language.code_invalid", $"'{code}' is not a valid language code.");
        }

        var language = new Language { Code = normalizedCode };
        language.Update(name, nativeName, sortOrder);
        return language;
    }

    public static bool IsValidCode(string? code) => code is not null && CodePattern().IsMatch(code.Trim().ToLowerInvariant());

    /// <summary>Changes the names and the position in the language switcher. The code never changes.</summary>
    public void Update(string name, string nativeName, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("language.name_required", "The language name is required.");
        }

        if (string.IsNullOrWhiteSpace(nativeName))
        {
            throw new DomainException("language.native_name_required", "The native language name is required.");
        }

        if (name.Trim().Length > NameMaxLength || nativeName.Trim().Length > NameMaxLength)
        {
            throw new DomainException("language.name_too_long", $"Names can be at most {NameMaxLength} characters.");
        }

        Name = name.Trim();
        NativeName = nativeName.Trim();
        SortOrder = sortOrder;
    }

    public void Activate() => IsActive = true;

    public void Deactivate()
    {
        if (IsDefault)
        {
            throw new DomainException("language.default_cannot_be_deactivated", "Choose another default language first.");
        }

        IsActive = false;
    }

    public void MakeDefault()
    {
        if (!IsActive)
        {
            throw new DomainException("language.inactive_cannot_be_default", "Activate the language before making it the default.");
        }

        IsDefault = true;
    }

    public void RemoveDefault() => IsDefault = false;

    [GeneratedRegex("^[a-z]{2,3}(-[a-z0-9]{2,8})?$")]
    private static partial Regex CodePattern();
}
