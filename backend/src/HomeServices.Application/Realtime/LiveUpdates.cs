using HomeServices.Application.Abstractions;
using HomeServices.Domain.Offers;
using HomeServices.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Realtime;

/// <summary>
/// A request's status changed: cancelled (by the customer or staff) or closed by an accepted work offer.
/// <see cref="OrderId"/>: the order that accepted offer created, so the Portal can say "Accepted".
/// </summary>
public sealed record RequestUpdateDto(Guid RequestId, string Status, Guid? OrderId, DateTimeOffset At);

/// <summary>An offer was sent, withdrawn, accepted or rejected. <see cref="OrderId"/>: the order an accepted offer created.</summary>
public sealed record OfferUpdateDto(Guid OfferId, Guid RequestId, Guid PartnerId, string Status, Guid? OrderId, DateTimeOffset At);

/// <summary>
/// Pushes status changes to the signed-in Portal apps of the given users (SignalR in the API), so open pages update
/// without a refresh. Best effort, after the change is saved: failures are not errors.
/// </summary>
public interface ILiveUpdates
{
    Task RequestUpdatedAsync(IReadOnlyCollection<Guid> userIds, RequestUpdateDto update, CancellationToken cancellationToken);

    Task OfferUpdatedAsync(IReadOnlyCollection<Guid> userIds, OfferUpdateDto update, CancellationToken cancellationToken);
}

/// <summary>Who hears about a change, and the pushes the handlers send once it is saved.</summary>
internal static class LiveUpdate
{
    /// <summary>A request changed: its customer and every partner who received it.</summary>
    public static async Task RequestAsync(IAppDbContext db, ILiveUpdates live, ServiceRequest request, Guid? orderId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var partnerIds = request.Recipients.Select(r => r.PartnerProfileId).ToList();
        var users = await PartnerUsersAsync(db, partnerIds, cancellationToken);
        users.Add(request.CustomerId);
        await live.RequestUpdatedAsync(users, new RequestUpdateDto(request.Id, request.Status.ToString(), orderId, now), cancellationToken);
    }

    /// <summary>An offer changed: the request's customer and the partner who sent it.</summary>
    public static async Task OfferAsync(IAppDbContext db, ILiveUpdates live, Offer offer, Guid customerId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var users = await PartnerUsersAsync(db, [offer.PartnerProfileId], cancellationToken);
        users.Add(customerId);
        await live.OfferUpdatedAsync(
            users, new OfferUpdateDto(offer.Id, offer.RequestId, offer.PartnerProfileId, offer.StatusAt(now).ToString(), offer.OrderId, now), cancellationToken);
    }

    private static async Task<HashSet<Guid>> PartnerUsersAsync(IAppDbContext db, IReadOnlyCollection<Guid> partnerIds, CancellationToken cancellationToken) =>
        (await db.PartnerProfiles.AsNoTracking()
            .Where(p => partnerIds.Contains(p.Id))
            .Select(p => p.UserId)
            .ToListAsync(cancellationToken))
        .ToHashSet();
}

/// <summary>The orders accepted work offers created: a closed request with one was "accepted" (D4); without, closed some other way.</summary>
internal static class AcceptedOrders
{
    public static async Task<IReadOnlyDictionary<Guid, Guid>> ForAsync(IAppDbContext db, IReadOnlyCollection<Guid> requestIds, CancellationToken cancellationToken) =>
        (await db.Offers.AsNoTracking()
            .Where(o => requestIds.Contains(o.RequestId) && o.Kind == OfferKind.Work && o.Status == OfferStatus.Accepted && o.OrderId != null)
            .Select(o => new { o.RequestId, OrderId = o.OrderId!.Value })
            .ToListAsync(cancellationToken))
        .GroupBy(o => o.RequestId)
        .ToDictionary(g => g.Key, g => g.First().OrderId);
}
