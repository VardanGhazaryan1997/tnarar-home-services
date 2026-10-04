using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Messaging;
using HomeServices.Application.Notifications;
using HomeServices.Application.Orders;
using HomeServices.Application.Requests;
using HomeServices.Domain;
using HomeServices.Domain.Notifications;
using HomeServices.Domain.Offers;
using HomeServices.Domain.Orders;
using HomeServices.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Offers;

/// <summary>
/// The offers on a request. The customer who owns it gets every offer except withdrawn ones, for comparison:
/// waiting offers first, cheapest first. A partner who received it gets their own offers. Anyone else: not found.
/// </summary>
public sealed record GetRequestOffers(Guid RequestId) : IQuery<IReadOnlyList<OfferDto>>;

/// <summary>One offer, for the customer of its request or the partner who sent it.</summary>
public sealed record GetOffer(Guid Id) : IQuery<OfferDto>;

/// <summary>
/// The customer accepts an offer, which creates an order. Accepting a work offer closes the request and the other
/// waiting offers; accepting a visit leaves the request open for the work offer that follows. Accepting an
/// already accepted offer again returns the same order, so retries are safe.
/// </summary>
public sealed record AcceptOffer(Guid Id) : ICommand<OrderDto>;

/// <summary>The customer turns an offer down, optionally saying why (the partner sees the reason).</summary>
public sealed record RejectOffer(Guid Id, string? Reason) : ICommand<OfferDto>;

public sealed class RejectOfferValidator : AbstractValidator<RejectOffer>
{
    public RejectOfferValidator() =>
        RuleFor(x => x.Reason)
            .Must(reason => reason!.Trim().Length <= Offer.ReasonMaxLength).WithErrorCode("reason.too_long")
            .When(x => x.Reason is not null);
}

public sealed class GetRequestOffersHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : IQueryHandler<GetRequestOffers, IReadOnlyList<OfferDto>>
{
    public async Task<IReadOnlyList<OfferDto>> HandleAsync(GetRequestOffers query, CancellationToken cancellationToken)
    {
        var (userId, partnerId) = await OrderViews.MeAsync(db, currentUser, cancellationToken);
        var request = await db.ServiceRequests.AsNoTracking()
            .Where(r => r.Id == query.RequestId)
            .Select(r => new { r.CustomerId, Received = r.Recipients.Any(x => x.PartnerProfileId == partnerId) })
            .SingleOrDefaultAsync(cancellationToken);

        var offers = db.Offers.AsNoTracking().Include(o => o.Items).Include(o => o.Stages).Where(o => o.RequestId == query.RequestId);
        if (request?.CustomerId == userId)
        {
            offers = offers.Where(o => o.Status != OfferStatus.Withdrawn);
        }
        else if (request?.Received == true)
        {
            offers = offers.Where(o => o.PartnerProfileId == partnerId);
        }
        else
        {
            throw MyRequests.NotFound();
        }

        var now = clock.GetUtcNow();
        var list = (await offers.ToListAsync(cancellationToken))
            .OrderByDescending(o => o.IsOpen(now))
            .ThenBy(o => o.Price)
            .ThenBy(o => o.CreatedAt)
            .ToList();
        return await OfferViews.ToDtosAsync(db, list, now, cancellationToken);
    }
}

public sealed class GetOfferHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider clock) : IQueryHandler<GetOffer, OfferDto>
{
    public async Task<OfferDto> HandleAsync(GetOffer query, CancellationToken cancellationToken)
    {
        var (userId, partnerId) = await OrderViews.MeAsync(db, currentUser, cancellationToken);
        var offer = await OfferViews.LoadAsync(db, query.Id, cancellationToken);
        var isCustomer = await db.ServiceRequests.AnyAsync(r => r.Id == offer.RequestId && r.CustomerId == userId, cancellationToken);
        if (!isCustomer && offer.PartnerProfileId != partnerId)
        {
            throw OfferViews.NotFound();
        }

        return await OfferViews.ToDtoAsync(db, offer, clock.GetUtcNow(), cancellationToken);
    }
}

public sealed class AcceptOfferHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, TimeProvider clock)
    : ICommandHandler<AcceptOffer, OrderDto>
{
    public async Task<OrderDto> HandleAsync(AcceptOffer command, CancellationToken cancellationToken)
    {
        var (offer, request, userId) = await CustomerOffer.LoadAsync(db, currentUser, command.Id, cancellationToken);
        if (offer is { Status: OfferStatus.Accepted, OrderId: { } existingId })
        {
            var existing = await db.Orders.AsNoTracking()
                .Include(o => o.Stages)
                .Include(o => o.StatusChanges)
                .Include(o => o.ChangeRequests)
                .SingleAsync(o => o.Id == existingId, cancellationToken);
            return await OrderViews.ToDtoAsync(db, language, existing, userId, cancellationToken);
        }

        if (request.Status != RequestStatus.Open)
        {
            throw new DomainException("request.not_open", $"The request is {request.Status}.");
        }

        if (!await RequestMatching.Available(db).AnyAsync(p => p.Id == offer.PartnerProfileId, cancellationToken))
        {
            throw new DomainException("offer.partner_unavailable", "This partner can't take orders right now.");
        }

        var now = clock.GetUtcNow();
        Guid? parentOrderId = null;
        if (offer.Kind == OfferKind.Work)
        {
            // The work follows this partner's assessment visit on the same request, if there was one.
            parentOrderId = await db.Orders
                .Where(o => o.RequestId == request.Id && o.PartnerProfileId == offer.PartnerProfileId
                    && o.Kind == OrderKind.Visit && o.Status != OrderStatus.Cancelled)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => (Guid?)o.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var order = Order.FromOffer(offer, userId, parentOrderId, OrderViews.TermsJson(offer), now);
        offer.Accept(order.Id, now);
        if (offer.Kind == OfferKind.Work)
        {
            request.Close(now);
            await WaitingOffers.CloseAsync(db, request.Id, now, cancellationToken);
        }

        db.Orders.Add(order);
        await Notifier.ToOrderAsync(
            db,
            order,
            OrderParty.Customer,
            NoticeTo.Partner,
            NotificationType.OfferAccepted,
            new Dictionary<string, string?> { ["kind"] = order.Kind.ToString(), ["price"] = order.Price.ToString(System.Globalization.CultureInfo.InvariantCulture) },
            now,
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await OrderViews.ToDtoAsync(db, language, order, userId, cancellationToken);
    }
}

public sealed class RejectOfferHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider clock) : ICommandHandler<RejectOffer, OfferDto>
{
    public async Task<OfferDto> HandleAsync(RejectOffer command, CancellationToken cancellationToken)
    {
        var (offer, _, _) = await CustomerOffer.LoadAsync(db, currentUser, command.Id, cancellationToken);
        var now = clock.GetUtcNow();
        offer.Reject(command.Reason, now);
        await Notifier.ToPartnersAsync(db, [offer.PartnerProfileId], NotificationType.OfferRejected, _ => "/offers", null, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await OfferViews.ToDtoAsync(db, offer, now, cancellationToken);
    }
}

internal static class CustomerOffer
{
    /// <summary>An offer on one of the signed-in customer's requests, with the request, both tracked; anything else is "not found".</summary>
    public static async Task<(Offer Offer, ServiceRequest Request, Guid UserId)> LoadAsync(
        IAppDbContext db, ICurrentUser currentUser, Guid id, CancellationToken cancellationToken)
    {
        var (userId, _) = await OrderViews.MeAsync(db, currentUser, cancellationToken);
        var offer = await OfferViews.LoadAsync(db, id, cancellationToken);
        var request = await db.ServiceRequests
            .Include(r => r.Recipients)
            .SingleOrDefaultAsync(r => r.Id == offer.RequestId && r.CustomerId == userId, cancellationToken);
        return (offer, request ?? throw OfferViews.NotFound(), userId);
    }
}
