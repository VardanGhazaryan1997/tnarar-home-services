using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Messaging;
using HomeServices.Application.Notifications;
using HomeServices.Application.Requests;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Notifications;
using HomeServices.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Orders;

/// <summary>
/// Every order for the Back Office, newest first; <c>NeedsAttention = true</c> lists orders cancelled after work started,
/// waiting longest first. <see cref="Search"/> is a customer's or partner's phone, or part of a name.
/// </summary>
public sealed record GetAdminOrders(
    OrderStatus? Status = null,
    OrderKind? Kind = null,
    bool? NeedsAttention = null,
    string? Search = null,
    int Page = 1,
    int PageSize = GetAdminOrders.DefaultPageSize) : IQuery<PagedResult<AdminOrderListItemDto>>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

/// <summary>One order with both parties, the full history and every change, as staff see it.</summary>
public sealed record GetAdminOrder(Guid Id) : IQuery<OrderDto>;

/// <summary>Staff cancel an order (any open one), with a reason.</summary>
public sealed record CancelOrderByStaff(Guid Id, string Reason) : ICommand<OrderDto>;

/// <summary>Staff mark an order that needed attention as dealt with.</summary>
public sealed record ResolveOrder(Guid Id) : ICommand<OrderDto>;

public sealed class GetAdminOrdersValidator : AbstractValidator<GetAdminOrders>
{
    public GetAdminOrdersValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithErrorCode("status.invalid");
        RuleFor(x => x.Kind).IsInEnum().WithErrorCode("kind.invalid");
        RuleFor(x => x.Search).MaximumLength(100).WithErrorCode("search.too_long");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetAdminOrders.MaxPageSize).WithErrorCode("page_size.invalid");
    }
}

public sealed class CancelOrderByStaffValidator : AbstractValidator<CancelOrderByStaff>
{
    public CancelOrderByStaffValidator() => this.Reason(x => x.Reason);
}

public sealed class GetAdminOrdersHandler(IAppDbContext db, ICurrentLanguage language)
    : IQueryHandler<GetAdminOrders, PagedResult<AdminOrderListItemDto>>
{
    public async Task<PagedResult<AdminOrderListItemDto>> HandleAsync(GetAdminOrders query, CancellationToken cancellationToken)
    {
        var rows =
            from order in db.Orders.AsNoTracking()
            join customer in db.Users.AsNoTracking() on order.CustomerId equals customer.Id
            join partner in db.PartnerProfiles.AsNoTracking() on order.PartnerProfileId equals partner.Id
            join partnerUser in db.Users.AsNoTracking() on partner.UserId equals partnerUser.Id
            select new { Order = order, Customer = customer, Partner = partner, PartnerPhone = partnerUser.Phone };

        if (query.Status is { } status)
        {
            rows = rows.Where(r => r.Order.Status == status);
        }

        if (query.Kind is { } kind)
        {
            rows = rows.Where(r => r.Order.Kind == kind);
        }

        if (query.NeedsAttention is { } needsAttention)
        {
            rows = needsAttention ? rows.Where(r => r.Order.NeedsAttentionSince != null) : rows.Where(r => r.Order.NeedsAttentionSince == null);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            if (PhoneNumber.TryParse(search, out var phone))
            {
                rows = rows.Where(r => r.Customer.Phone == phone || r.PartnerPhone == phone);
            }
            else
            {
                var lowered = search.ToLowerInvariant();
                rows = rows.Where(r => r.Partner.DisplayName.ToLower().Contains(lowered) || (r.Customer.FullName != null && r.Customer.FullName.ToLower().Contains(lowered)));
            }
        }

        rows = query.NeedsAttention == true
            ? rows.OrderBy(r => r.Order.NeedsAttentionSince).ThenBy(r => r.Order.Id)
            : rows.OrderByDescending(r => r.Order.CreatedAt).ThenByDescending(r => r.Order.Id);

        var total = await rows.CountAsync(cancellationToken);
        var page = await rows
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new
            {
                r.Order,
                CustomerName = r.Customer.FullName,
                CustomerPhone = r.Customer.Phone,
                PartnerName = r.Partner.DisplayName,
                Pending = r.Order.ChangeRequests.Any(c => c.Status == ChangeRequestStatus.Pending),
            })
            .ToListAsync(cancellationToken);

        var requestIds = page.Select(r => r.Order.RequestId).Distinct().ToList();
        var requests = await db.ServiceRequests.AsNoTracking().Where(r => requestIds.Contains(r.Id)).ToListAsync(cancellationToken);
        var lookup = await RequestLookup.LoadAsync(db, language, requests, cancellationToken);
        var places = requests.ToDictionary(r => r.Id, lookup.Place);

        var items = page
            .Select(r => new AdminOrderListItemDto(
                r.Order.Id,
                r.Order.Kind.ToString(),
                r.Order.Status.ToString(),
                places[r.Order.RequestId],
                RequestLookup.Excerpt(OrderViews.ReadTerms(r.Order).Summary),
                r.Order.Price,
                r.Order.CustomerId,
                r.CustomerName,
                r.CustomerPhone.Value,
                r.Order.PartnerProfileId,
                r.PartnerName,
                r.Pending,
                r.Order.NeedsAttentionSince,
                r.Order.CreatedAt))
            .ToList();
        return new PagedResult<AdminOrderListItemDto>(items, query.Page, query.PageSize, total);
    }
}

internal static class AdminOrderCommand
{
    public static async Task<Order> LoadAsync(IAppDbContext db, Guid id, CancellationToken cancellationToken) =>
        await db.Orders.WithDetails().SingleOrDefaultAsync(o => o.Id == id, cancellationToken) ?? throw OrderViews.NotFound();
}

public sealed class GetAdminOrderHandler(IAppDbContext db, ICurrentLanguage language) : IQueryHandler<GetAdminOrder, OrderDto>
{
    public async Task<OrderDto> HandleAsync(GetAdminOrder query, CancellationToken cancellationToken)
    {
        var order = await db.Orders.AsNoTracking().WithDetails().SingleOrDefaultAsync(o => o.Id == query.Id, cancellationToken)
            ?? throw OrderViews.NotFound();
        return await OrderViews.ToDtoAsync(db, language, order, OrderParty.Staff, cancellationToken);
    }
}

public sealed class CancelOrderByStaffHandler(IAppDbContext db, ICurrentLanguage language, TimeProvider clock)
    : ICommandHandler<CancelOrderByStaff, OrderDto>
{
    public async Task<OrderDto> HandleAsync(CancelOrderByStaff command, CancellationToken cancellationToken)
    {
        var order = await AdminOrderCommand.LoadAsync(db, command.Id, cancellationToken);
        var now = clock.GetUtcNow();
        order.Cancel(OrderParty.Staff, command.Reason, now);
        await Notifier.ToOrderAsync(db, order, OrderParty.Staff, NoticeTo.Both, NotificationType.OrderCancelled, null, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await OrderViews.ToDtoAsync(db, language, order, OrderParty.Staff, cancellationToken);
    }
}

public sealed class ResolveOrderHandler(IAppDbContext db, ICurrentLanguage language) : ICommandHandler<ResolveOrder, OrderDto>
{
    public async Task<OrderDto> HandleAsync(ResolveOrder command, CancellationToken cancellationToken)
    {
        var order = await AdminOrderCommand.LoadAsync(db, command.Id, cancellationToken);
        order.Resolve();
        await db.SaveChangesAsync(cancellationToken);
        return await OrderViews.ToDtoAsync(db, language, order, OrderParty.Staff, cancellationToken);
    }
}
