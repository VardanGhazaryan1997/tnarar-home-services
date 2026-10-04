using System.Text;

namespace HomeServices.Domain.Common;

/// <summary>Writes Armenian and Russian text in Latin letters, for slugs ("Արամ Սանտեխնիկ" → "aram-santekhnik").</summary>
public static class Transliteration
{
    private static readonly Dictionary<char, string> Letters = new()
    {
        // Armenian (lower case; "ու" is handled separately).
        ['ա'] = "a", ['բ'] = "b", ['գ'] = "g", ['դ'] = "d", ['ե'] = "e", ['զ'] = "z", ['է'] = "e", ['ը'] = "y",
        ['թ'] = "t", ['ժ'] = "zh", ['ի'] = "i", ['լ'] = "l", ['խ'] = "kh", ['ծ'] = "ts", ['կ'] = "k", ['հ'] = "h",
        ['ձ'] = "dz", ['ղ'] = "gh", ['ճ'] = "ch", ['մ'] = "m", ['յ'] = "y", ['ն'] = "n", ['շ'] = "sh", ['ո'] = "o",
        ['չ'] = "ch", ['պ'] = "p", ['ջ'] = "j", ['ռ'] = "r", ['ս'] = "s", ['վ'] = "v", ['տ'] = "t", ['ր'] = "r",
        ['ց'] = "ts", ['ւ'] = "v", ['փ'] = "p", ['ք'] = "k", ['օ'] = "o", ['ֆ'] = "f", ['և'] = "ev",

        // Russian.
        ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d", ['е'] = "e", ['ё'] = "e", ['ж'] = "zh",
        ['з'] = "z", ['и'] = "i", ['й'] = "y", ['к'] = "k", ['л'] = "l", ['м'] = "m", ['н'] = "n", ['о'] = "o",
        ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t", ['у'] = "u", ['ф'] = "f", ['х'] = "kh", ['ц'] = "ts",
        ['ч'] = "ch", ['ш'] = "sh", ['щ'] = "shch", ['ъ'] = "", ['ы'] = "y", ['ь'] = "", ['э'] = "e", ['ю'] = "yu",
        ['я'] = "ya",
    };

    /// <summary>
    /// A slug made from <paramref name="text"/>: lower-case Latin letters and digits joined by single dashes,
    /// at most <paramref name="maxLength"/> characters. Other characters are dropped. Empty when nothing is left.
    /// </summary>
    public static string ToSlug(string? text, int maxLength = Slug.MaxLength)
    {
        var latin = ToLatin((text ?? string.Empty).ToLowerInvariant());
        var slug = new StringBuilder();
        foreach (var c in latin)
        {
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                slug.Append(c);
            }
            else if (slug.Length > 0 && slug[^1] != '-')
            {
                slug.Append('-');
            }
        }

        var result = slug.ToString().Trim('-');
        return result.Length <= maxLength ? result : result[..maxLength].TrimEnd('-');
    }

    private static string ToLatin(string text)
    {
        var latin = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == 'ո' && i + 1 < text.Length && text[i + 1] == 'ւ')
            {
                latin.Append('u');
                i++;
            }
            else
            {
                latin.Append(Letters.TryGetValue(text[i], out var letters) ? letters : text[i].ToString());
            }
        }

        return latin.ToString();
    }
}
