using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Domain.Offers;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Offers;

/// <summary>Loading offers and turning them into DTOs.</summary>
internal static class OfferViews
{
    public static NotFoundException NotFound() => new("Offer not found.", "offer.not_found");

    /// <summary>An offer with its lines and stages, tracked.</summary>
    public static async Task<Offer> LoadAsync(IAppDbContext db, Guid id, CancellationToken cancellationToken) =>
        await db.Offers
            .Include(o => o.Items)
            .Include(o => o.Stages)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw NotFound();

    public static async Task<IReadOnlyList<OfferDto>> ToDtosAsync(IAppDbContext db, IReadOnlyList<Offer> offers, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var partnerIds = offers.Select(o => o.PartnerProfileId).Distinct().ToList();
        var partners = await db.PartnerProfiles.AsNoTracking()
            .Where(p => partnerIds.Contains(p.Id))
            .Select(p => new OfferPartnerDto(p.Id, p.DisplayName, p.Slug))
            .ToDictionaryAsync(p => p.PartnerId, cancellationToken);
        return offers
            .Select(o => ToDto(o, partners.GetValueOrDefault(o.PartnerProfileId) ?? new OfferPartnerDto(o.PartnerProfileId, string.Empty, null), now))
            .ToList();
    }

    public static async Task<OfferDto> ToDtoAsync(IAppDbContext db, Offer offer, DateTimeOffset now, CancellationToken cancellationToken) =>
        (await ToDtosAsync(db, [offer], now, cancellationToken))[0];

    public static IReadOnlyList<OfferLineDto> Lines(Offer offer) =>
        offer.Items.OrderBy(i => i.SortOrder).Select(i => new OfferLineDto(i.Title, i.Included)).ToList();

    public static IReadOnlyList<PaymentStageDto> Stages(Offer offer) =>
        offer.Stages.OrderBy(s => s.SortOrder).Select(s => new PaymentStageDto(s.Title, s.Purpose.ToString(), s.Amount)).ToList();

    private static OfferDto ToDto(Offer offer, OfferPartnerDto partner, DateTimeOffset now) => new(
        offer.Id,
        offer.RequestId,
        offer.Kind.ToString(),
        offer.StatusAt(now).ToString(),
        partner,
        offer.Summary,
        Lines(offer),
        offer.Price,
        offer.MaterialsIncluded,
        offer.MaterialsNote,
        offer.StartDate,
        offer.DurationDays,
        offer.VisitAt,
        Stages(offer),
        offer.ExpiresAt,
        offer.CreatedAt,
        offer.DecidedAt,
        offer.RejectReason,
        offer.OrderId);
}

/// <summary>When a request ends (closed by an accepted work offer, or cancelled), offers still waiting on it close too.</summary>
internal static class WaitingOffers
{
    public static async Task CloseAsync(IAppDbContext db, Guid requestId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var waiting = await db.Offers
            .Where(o => o.RequestId == requestId && o.Status == OfferStatus.Sent)
            .ToListAsync(cancellationToken);
        foreach (var offer in waiting)
        {
            offer.Close(now);
        }
    }
}
