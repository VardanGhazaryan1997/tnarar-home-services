using System.Text;

namespace HomeServices.Infrastructure.Persistence;

/// <summary>Converts .NET names to PostgreSQL-style snake_case: PartnerProfileId → partner_profile_id.</summary>
public static class SnakeCase
{
    public static string Convert(string name)
    {
        var builder = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];
            if (char.IsUpper(current) && i > 0 && name[i - 1] != '_' && StartsNewWord(name, i))
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(current));
        }

        return builder.ToString();
    }

    // "createdAt" → before 'A'; "HTTPStatus" → before 'S' (end of an acronym).
    private static bool StartsNewWord(string name, int i) =>
        !char.IsUpper(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1]));
}
