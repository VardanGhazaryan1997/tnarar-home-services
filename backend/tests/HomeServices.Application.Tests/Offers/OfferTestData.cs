using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Offers;
using HomeServices.Application.Orders;
using HomeServices.Application.Tests.Requests;
using HomeServices.Domain.Offers;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Tests.Offers;

/// <summary>The request test data plus offer and order handlers wired to it.</summary>
public sealed class OfferTestData
{
    public RequestTestData Requests { get; } = new();

    public static DateTimeOffset Now => RequestTestData.Now;

    public ICurrentUser Customer => Requests.Customer;

    public static ICurrentUser As(PartnerProfile partner) => RequestTestData.As(partner);

    public SendOffer Work(Guid requestId, int price = 100_000) => new(
        requestId,
        OfferKind.Work,
        "Replace the kitchen tap and the pipes under the sink.",
        [new OfferLine("Remove the old tap", true), new OfferLine("Tiling", false)],
        price,
        true,
        "Tap included",
        new DateOnly(2026, 10, 8),
        2,
        null,
        [new OfferStageTerms("Deposit", PaymentPurpose.Deposit, price / 2), new OfferStageTerms(null, PaymentPurpose.Final, price - (price / 2))],
        null);

    public SendOffer Visit(Guid requestId, int fee = 0) => new(
        requestId, OfferKind.Visit, "I'll come and measure the bathroom.", null, fee, false, null, null, null, Now.AddDays(1), null, null);

    public Task<OfferDto> SendAsync(PartnerProfile partner, SendOffer command) =>
        new SendOfferHandler(Requests.Db, As(partner), Requests.Clock).HandleAsync(command, CancellationToken.None);

    public Task<OfferDto> WithdrawAsync(PartnerProfile partner, Guid offerId) =>
        new WithdrawOfferHandler(Requests.Db, As(partner), Requests.Clock).HandleAsync(new WithdrawOffer(offerId), CancellationToken.None);

    public Task<OrderDto> AcceptAsync(Guid offerId, ICurrentUser? user = null) =>
        new AcceptOfferHandler(Requests.Db, user ?? Customer, Requests.Language, Requests.Clock).HandleAsync(new AcceptOffer(offerId), CancellationToken.None);

    public Task<OfferDto> RejectAsync(Guid offerId, string? reason = null, ICurrentUser? user = null) =>
        new RejectOfferHandler(Requests.Db, user ?? Customer, Requests.Clock).HandleAsync(new RejectOffer(offerId, reason), CancellationToken.None);

    public Task<IReadOnlyList<OfferDto>> OffersAsync(Guid requestId, ICurrentUser? user = null) =>
        new GetRequestOffersHandler(Requests.Db, user ?? Customer, Requests.Clock).HandleAsync(new GetRequestOffers(requestId), CancellationToken.None);

    public Task<OfferDto> GetAsync(Guid offerId, ICurrentUser user) =>
        new GetOfferHandler(Requests.Db, user, Requests.Clock).HandleAsync(new GetOffer(offerId), CancellationToken.None);

    public Task<PagedResult<MyOfferListItemDto>> MyOffersAsync(PartnerProfile partner, GetMyOffers? query = null) =>
        new GetMyOffersHandler(Requests.Db, As(partner), Requests.Language, Requests.Clock).HandleAsync(query ?? new GetMyOffers(), CancellationToken.None);

    public Task<PagedResult<OrderListItemDto>> OrdersAsync(ICurrentUser user, GetMyOrders? query = null) =>
        new GetMyOrdersHandler(Requests.Db, user, Requests.Language).HandleAsync(query ?? new GetMyOrders(), CancellationToken.None);

    public Task<OrderDto> OrderAsync(Guid orderId, ICurrentUser user) =>
        new GetOrderHandler(Requests.Db, user, Requests.Language).HandleAsync(new GetOrder(orderId), CancellationToken.None);

    public Offer StoredOffer(Guid id) => Requests.Db.Offers.AsNoTracking().Include(o => o.Stages).Single(o => o.Id == id);

    public ServiceRequest StoredRequest(Guid id) => Requests.Db.ServiceRequests.AsNoTracking().Include(r => r.Recipients).Single(r => r.Id == id);
}
