using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Partners;

/// <summary>
/// A partner's public price list (A7): their prices for work they still offer, in catalog order, each next to the
/// market range. Anyone can see it, like the rest of the public profile; 404 "partner.not_found" when it isn't public.
/// </summary>
public sealed record GetPublicPartnerPrices(string Slug) : IQuery<IReadOnlyList<PublicPriceDto>>;

/// <summary>
/// A partner's price for a work item, labour per unit in AMD (a single price, or <see cref="PriceFrom"/>–
/// <see cref="PriceTo"/>), with the market range. <see cref="Comparison"/>: "Below" the market's 25th percentile,
/// "Above" its 75th, "Typical" in between; null when the price includes materials or the work has no market price.
/// </summary>
public sealed record PublicPriceDto(
    Guid WorkItemId,
    string Name,
    WorkUnit Unit,
    Guid CategoryId,
    string CategoryName,
    Guid MainCategoryId,
    string MainCategoryName,
    int PriceFrom,
    int? PriceTo,
    bool IncludesMaterials,
    int? MarketMin,
    int? MarketTypical,
    int? MarketMax,
    string? Comparison);

public sealed class GetPublicPartnerPricesHandler(IAppDbContext db, ICurrentLanguage language)
    : IQueryHandler<GetPublicPartnerPrices, IReadOnlyList<PublicPriceDto>>
{
    public async Task<IReadOnlyList<PublicPriceDto>> HandleAsync(GetPublicPartnerPrices query, CancellationToken cancellationToken)
    {
        var slug = query.Slug.Trim().ToLowerInvariant();
        var partner = await PublicPartnerQueries.Visible(db)
            .Include(p => p.Services)
            .SingleOrDefaultAsync(p => p.Slug == slug, cancellationToken)
            ?? throw new NotFoundException("Partner not found.", "partner.not_found");

        var offered = await MyPriceList.OfferedItemsAsync(db, partner, cancellationToken);
        var prices = await db.PartnerPrices.AsNoTracking()
            .Where(p => p.PartnerProfileId == partner.Id)
            .ToDictionaryAsync(p => p.WorkItemId, cancellationToken);

        return offered
            .Where(o => prices.ContainsKey(o.Item.Id))
            .Select(o =>
            {
                var price = prices[o.Item.Id];
                return new PublicPriceDto(
                    o.Item.Id,
                    o.Item.Name.Get(language.Code, language.DefaultCode),
                    o.Item.Unit,
                    o.Subcategory.Id,
                    o.Subcategory.Name.Get(language.Code, language.DefaultCode),
                    o.Main.Id,
                    o.Main.Name.Get(language.Code, language.DefaultCode),
                    price.PriceFrom,
                    price.PriceTo,
                    price.IncludesMaterials,
                    o.Item.MarketMin,
                    o.Item.MarketTypical,
                    o.Item.MarketMax,
                    PriceComparison.Of(price.PriceFrom, price.PriceTo, price.IncludesMaterials, o.Item.MarketMin, o.Item.MarketMax));
            })
            .ToList();
    }
}

/// <summary>Where a partner's price sits in the market range (see <see cref="PublicPriceDto.Comparison"/>).</summary>
public static class PriceComparison
{
    public const string Below = "Below";
    public const string Typical = "Typical";
    public const string Above = "Above";

    /// <summary>Compares the middle of the partner's price with the market's 25th–75th percentile.</summary>
    public static string? Of(int priceFrom, int? priceTo, bool includesMaterials, int? marketMin, int? marketMax)
    {
        if (includesMaterials || marketMin is not { } min || marketMax is not { } max)
        {
            return null;
        }

        var middle = priceTo is { } to ? (priceFrom + to) / 2 : priceFrom;
        return middle < min ? Below : middle > max ? Above : Typical;
    }
}
