using HomeServices.Domain.Common;
using HomeServices.Domain.Localization;

namespace HomeServices.Domain.Catalog;

/// <summary>
/// A region of Armenia (a marz, or Yerevan). Towns and villages belong to one; a partner can serve a whole region.
/// Regions are reference data from the seed.
/// </summary>
public sealed class Region : Entity, IAudited
{
    private Region()
    {
    }

    public string Slug { get; private set; } = string.Empty;

    public LocalizedText Name { get; private set; } = LocalizedText.Empty;

    public int SortOrder { get; private set; }

    public static Region Create(string slug, LocalizedText name, int sortOrder)
    {
        if (name.Values.Count == 0)
        {
            throw new DomainException("region.name_required", "A region needs a name in at least one language.");
        }

        return new Region { Slug = Common.Slug.Normalize(slug, "region.slug_invalid"), Name = name, SortOrder = sortOrder };
    }
}

/// <summary>Whether a <see cref="City"/> is a town or a village.</summary>
public enum SettlementKind
{
    City,
    Village,
}
