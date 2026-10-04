using System.Text.Json;
using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Messaging;
using HomeServices.Application.Offers;
using HomeServices.Application.Partners;
using HomeServices.Application.Payments;
using HomeServices.Application.Requests;
using HomeServices.Application.Reviews;
using HomeServices.Domain.Offers;
using HomeServices.Domain.Orders;
using HomeServices.Domain.Payments;
using HomeServices.Domain.Reviews;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Orders;

/// <summary>
/// The signed-in user's orders, as customer and as partner, newest first. <see cref="As"/> keeps one side only.
/// </summary>
public sealed record GetMyOrders(OrderRole? As = null, OrderStatus? Status = null, int Page = 1, int PageSize = GetMyOrders.DefaultPageSize)
    : IQuery<PagedResult<OrderListItemDto>>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

/// <summary>One order of the signed-in user (either side); anyone else's is "not found".</summary>
public sealed record GetOrder(Guid Id) : IQuery<OrderDto>;

public sealed class GetMyOrdersValidator : AbstractValidator<GetMyOrders>
{
    public GetMyOrdersValidator()
    {
        RuleFor(x => x.As).IsInEnum().WithErrorCode("as.invalid");
        RuleFor(x => x.Status).IsInEnum().WithErrorCode("status.invalid");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetMyOrders.MaxPageSize).WithErrorCode("page_size.invalid");
    }
}

public sealed class GetMyOrdersHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language)
    : IQueryHandler<GetMyOrders, PagedResult<OrderListItemDto>>
{
    public async Task<PagedResult<OrderListItemDto>> HandleAsync(GetMyOrders query, CancellationToken cancellationToken)
    {
        var (userId, partnerId) = await OrderViews.MeAsync(db, currentUser, cancellationToken);
        var orders = query.As switch
        {
            OrderRole.Customer => db.Orders.AsNoTracking().Where(o => o.CustomerId == userId),
            OrderRole.Partner => db.Orders.AsNoTracking().Where(o => o.PartnerProfileId == partnerId),
            _ => db.Orders.AsNoTracking().Where(o => o.CustomerId == userId || o.PartnerProfileId == partnerId),
        };
        if (query.Status is { } status)
        {
            orders = orders.Where(o => o.Status == status);
        }

        var total = await orders.CountAsync(cancellationToken);
        var page = await orders
            .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var requestIds = page.Select(o => o.RequestId).Distinct().ToList();
        var requests = await db.ServiceRequests.AsNoTracking().Where(r => requestIds.Contains(r.Id)).ToListAsync(cancellationToken);
        var lookup = await RequestLookup.LoadAsync(db, language, requests, cancellationToken);
        var places = requests.ToDictionary(r => r.Id, lookup.Place);

        var customerIds = page.Select(o => o.CustomerId).Distinct().ToList();
        var customerNames = await db.Users.AsNoTracking()
            .Where(u => customerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);
        var partnerIds = page.Select(o => o.PartnerProfileId).Distinct().ToList();
        var partnerNames = await db.PartnerProfiles.AsNoTracking()
            .Where(p => partnerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.DisplayName, cancellationToken);

        var items = page
            .Select(o =>
            {
                var role = o.CustomerId == userId ? OrderRole.Customer : OrderRole.Partner;
                return new OrderListItemDto(
                    o.Id,
                    o.Kind.ToString(),
                    o.Status.ToString(),
                    role.ToString(),
                    places[o.RequestId],
                    RequestLookup.Excerpt(OrderViews.ReadTerms(o).Summary),
                    o.Price,
                    role == OrderRole.Customer ? partnerNames.GetValueOrDefault(o.PartnerProfileId) : customerNames.GetValueOrDefault(o.CustomerId),
                    o.StartDate,
                    o.VisitAt,
                    o.CreatedAt);
            })
            .ToList();
        return new PagedResult<OrderListItemDto>(items, query.Page, query.PageSize, total);
    }
}

public sealed class GetOrderHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language)
    : IQueryHandler<GetOrder, OrderDto>
{
    public async Task<OrderDto> HandleAsync(GetOrder query, CancellationToken cancellationToken)
    {
        var (userId, partnerId) = await OrderViews.MeAsync(db, currentUser, cancellationToken);
        var order = await db.Orders.AsNoTracking()
            .Include(o => o.Stages)
            .Include(o => o.StatusChanges)
            .Include(o => o.ChangeRequests)
            .SingleOrDefaultAsync(o => o.Id == query.Id && (o.CustomerId == userId || o.PartnerProfileId == partnerId), cancellationToken)
            ?? throw OrderViews.NotFound();
        return await OrderViews.ToDtoAsync(db, language, order, userId, cancellationToken);
    }
}

internal static class OrderViews
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static NotFoundException NotFound() => new("Order not found.", "order.not_found");

    /// <summary>The signed-in Portal user and their partner profile id (null without one).</summary>
    public static async Task<(Guid UserId, Guid? PartnerId)> MeAsync(IAppDbContext db, ICurrentUser currentUser, CancellationToken cancellationToken)
    {
        var userId = MyPartnerProfile.UserId(currentUser);
        var partnerId = await db.PartnerProfiles.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => (Guid?)p.Id)
            .SingleOrDefaultAsync(cancellationToken);
        return (userId, partnerId);
    }

    /// <summary>The offer's terms as they stand now, to freeze on the order.</summary>
    public static string TermsJson(Offer offer) => JsonSerializer.Serialize(
        new OrderTermsDto(
            1,
            offer.Summary,
            OfferViews.Lines(offer),
            offer.Price,
            offer.MaterialsIncluded,
            offer.MaterialsNote,
            offer.StartDate,
            offer.DurationDays,
            offer.VisitAt,
            OfferViews.Stages(offer),
            offer.CreatedAt),
        Json);

    public static OrderTermsDto ReadTerms(Order order) => JsonSerializer.Deserialize<OrderTermsDto>(order.Terms, Json)!;

    public static Task<OrderDto> ToDtoAsync(IAppDbContext db, ICurrentLanguage language, Order order, Guid userId, CancellationToken cancellationToken) =>
        ToDtoAsync(db, language, order, order.CustomerId == userId ? OrderParty.Customer : OrderParty.Partner, cancellationToken);

    /// <summary>The order as <paramref name="viewer"/> (Customer, Partner or Staff) sees it, with what they can do next.</summary>
    public static async Task<OrderDto> ToDtoAsync(IAppDbContext db, ICurrentLanguage language, Order order, OrderParty viewer, CancellationToken cancellationToken)
    {
        var request = await db.ServiceRequests.AsNoTracking().SingleAsync(r => r.Id == order.RequestId, cancellationToken);
        var lookup = await RequestLookup.LoadAsync(db, language, [request], cancellationToken);
        var customer = await db.Users.AsNoTracking()
            .Where(u => u.Id == order.CustomerId)
            .Select(u => new { u.Id, u.FullName, u.Phone })
            .SingleAsync(cancellationToken);
        var partner = await db.PartnerProfiles.AsNoTracking()
            .Where(p => p.Id == order.PartnerProfileId)
            .Join(db.Users, p => p.UserId, u => u.Id, (p, u) => new { p.Id, p.DisplayName, p.Slug, u.Phone })
            .SingleAsync(cancellationToken);
        var payments = await db.Payments.AsNoTracking()
            .Where(p => p.OrderId == order.Id)
            .OrderByDescending(p => p.RecordedAt).ThenByDescending(p => p.Id)
            .ToListAsync(cancellationToken);
        var review = await db.Reviews.AsNoTracking().SingleOrDefaultAsync(r => r.OrderId == order.Id, cancellationToken);

        return new OrderDto(
            order.Id,
            order.Kind.ToString(),
            order.Status.ToString(),
            viewer.ToString(),
            order.RequestId,
            order.OfferId,
            order.ParentOrderId,
            lookup.Place(request),
            order.Price,
            ReadTerms(order),
            order.Stages.OrderBy(s => s.SortOrder).Select(s => new OrderStageDto(s.Id, s.Title, s.Purpose.ToString(), s.Amount)).ToList(),
            new OrderCustomerDto(customer.Id, customer.FullName, customer.Phone.Value),
            new OrderPartnerDto(partner.Id, partner.DisplayName, partner.Slug, partner.Phone.Value),
            order.StatusChanges.OrderBy(c => c.Sequence).Select(c => new OrderStatusChangeDto(c.Status.ToString(), c.ChangedAt, c.ChangedBy?.ToString(), c.Note)).ToList(),
            order.CreatedAt,
            order.StartDate,
            order.DurationDays,
            order.VisitAt,
            order.StartedAt,
            order.CompletionRequestedAt,
            order.AutoCompleteAt,
            order.CompletedAt,
            order.CancelledAt,
            order.CancelledBy?.ToString(),
            order.CancelReason,
            order.NeedsAttentionSince,
            order.ChangeRequests.OrderByDescending(c => c.ProposedAt).Select(c => ChangeDto(c, viewer)).ToList(),
            payments.Select(p => PaymentViews.ToDto(p, order, viewer)).ToList(),
            payments.Where(p => p.Status == PaymentStatus.Confirmed).Sum(p => p.Amount),
            review is null ? null : ReviewViews.ToDto(review),
            [.. Actions(order, viewer), .. MoreActions(order, viewer, payments, review)]);
    }

    private static OrderChangeRequestDto ChangeDto(OrderChangeRequest change, OrderParty viewer) => new(
        change.Id,
        change.Kind.ToString(),
        change.Status.ToString(),
        change.ProposedBy.ToString(),
        change.ProposedBy == viewer,
        change.Title,
        change.Description,
        change.Amount,
        change.NewStartDate,
        change.NewDurationDays,
        change.NewVisitAt,
        change.ResponseNote,
        change.ProposedAt,
        change.DecidedAt);

    /// <summary>Payment and review actions, with the same rules as <see cref="Payment"/> and <see cref="Review"/>.</summary>
    private static IEnumerable<string> MoreActions(Order order, OrderParty viewer, IReadOnlyList<Payment> payments, Review? review)
    {
        if (viewer is OrderParty.Customer or OrderParty.Partner && order.Status != OrderStatus.Cancelled
            && payments.Where(p => p.Counts).Sum(p => (long)p.Amount) < order.Price)
        {
            yield return OrderActions.RecordPayment;
        }

        if (viewer == OrderParty.Customer && order.Status == OrderStatus.Completed && review is null)
        {
            yield return OrderActions.Review;
        }

        if (viewer == OrderParty.Partner && review is { Reply: null })
        {
            yield return OrderActions.ReplyReview;
        }
    }

    /// <summary>The same rules as <see cref="Order"/>'s methods, from the viewer's side.</summary>
    public static IReadOnlyList<string> Actions(Order order, OrderParty viewer)
    {
        var actions = new List<string>();
        var changeable = order.Status is OrderStatus.Confirmed or OrderStatus.InProgress;
        if (viewer == OrderParty.Partner && order.Status == OrderStatus.Confirmed)
        {
            actions.Add(OrderActions.Start);
        }

        if (viewer == OrderParty.Partner && changeable)
        {
            actions.Add(OrderActions.RequestCompletion);
        }

        if (viewer == OrderParty.Customer && order.Status == OrderStatus.CompletionRequested)
        {
            actions.Add(OrderActions.ConfirmCompletion);
            actions.Add(OrderActions.RejectCompletion);
        }

        if (viewer is OrderParty.Customer or OrderParty.Partner && changeable)
        {
            var pending = order.PendingChange;
            if (pending is null)
            {
                actions.Add(OrderActions.ProposeChange);
            }
            else
            {
                actions.Add(pending.ProposedBy == viewer ? OrderActions.WithdrawChange : OrderActions.AnswerChange);
            }
        }

        if (order.IsOpen)
        {
            actions.Add(OrderActions.Cancel);
        }

        if (viewer == OrderParty.Staff && order.NeedsAttentionSince is not null)
        {
            actions.Add(OrderActions.Resolve);
        }

        return actions;
    }
}
