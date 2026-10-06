using HomeServices.Application.Abstractions;
using HomeServices.Application.Catalog;
using HomeServices.Application.Messaging;
using HomeServices.Application.Partners;
using HomeServices.Domain.Partners;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Demo;

/// <summary>
/// Gives every demo partner without prices a price list (staging and local development only): most of the work in
/// their services, priced around the usual market price, some as ranges, a few with materials. Then recalculates the
/// market ranges. Safe to run on every start: partners who already have prices are left alone. Returns how many demo
/// partners got prices.
/// </summary>
public sealed record SeedDemoPrices : ICommand<int>;

public sealed class SeedDemoPricesHandler(IAppDbContext db, IOptions<PricingSettings> settings, TimeProvider clock)
    : ICommandHandler<SeedDemoPrices, int>
{
    public async Task<int> HandleAsync(SeedDemoPrices command, CancellationToken cancellationToken)
    {
        var phones = DemoPartners.All.Select((_, index) => SeedDemoPartnersHandler.Phone(index)).ToList();
        var owners = await db.Users.AsNoTracking().Where(u => phones.Contains(u.Phone)).Select(u => u.Id).ToListAsync(cancellationToken);
        var profiles = await db.PartnerProfiles
            .Include(p => p.Services)
            .Where(p => owners.Contains(p.UserId) && !db.PartnerPrices.Any(price => price.PartnerProfileId == p.Id))
            .ToListAsync(cancellationToken);

        var priced = new HashSet<Guid>();
        var partners = 0;
        foreach (var profile in profiles)
        {
            var company = profile.Type == PartnerType.Company;
            var added = 0;
            foreach (var offered in await MyPriceList.OfferedItemsAsync(db, profile, cancellationToken))
            {
                var item = offered.Item;
                if (item.MarketTypical is not { } typical || DemoPrice.Next(profile.Id, item.Slug, 1) < 0.15)
                {
                    continue; // nobody prices everything
                }

                var (from, to, materials) = DemoPrice.For(profile.Id, item.Slug, typical, company);
                db.PartnerPrices.Add(PartnerPrice.Create(profile.Id, item.Id, from, to, materials));
                priced.Add(item.Id);
                added++;
            }

            partners += added > 0 ? 1 : 0;
        }

        if (partners == 0)
        {
            return 0;
        }

        await db.SaveChangesAsync(cancellationToken);
        await MarketPrices.RecalculateAsync(db, settings.Value, clock, priced, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return partners;
    }
}

/// <summary>Repeatable "random" demo prices: the same partner and work item always get the same price.</summary>
public static class DemoPrice
{
    /// <summary>A price around <paramref name="typical"/> (companies a little higher); a third are ranges, a tenth include materials.</summary>
    public static (int From, int? To, bool Materials) For(Guid partnerId, string slug, int typical, bool company)
    {
        var factor = 0.8 + (Next(partnerId, slug, 2) * 0.45) + (company ? 0.08 : 0);
        var from = Math.Max(MarketPrices.Tidy(typical * factor), 10);
        int? to = Next(partnerId, slug, 3) < 0.33 ? MarketPrices.Tidy(from * (1.2 + (Next(partnerId, slug, 4) * 0.3))) : null;
        var materials = Next(partnerId, slug, 5) < 0.1;
        return (from, to, materials);
    }

    /// <summary>A number in [0, 1) that depends only on the inputs (FNV-1a hash).</summary>
    public static double Next(Guid partnerId, string slug, int salt)
    {
        var hash = 2166136261u;
        foreach (var ch in $"{partnerId:N}/{slug}/{salt}")
        {
            hash = (hash ^ ch) * 16777619u;
        }

        return (hash % 10_000) / 10_000.0;
    }
}
