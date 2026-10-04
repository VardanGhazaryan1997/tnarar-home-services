using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Messaging;
using HomeServices.Application.Notifications;
using HomeServices.Application.Orders;
using HomeServices.Domain.Notifications;
using HomeServices.Domain.Offers;
using HomeServices.Domain.Orders;
using HomeServices.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Payments;

/// <summary>
/// A payment record on an order. <see cref="Mine"/>: the viewer recorded it (they can withdraw it while pending; the
/// other side confirms or disputes it). <see cref="RecordedBy"/>: Customer or Partner.
/// </summary>
public sealed record PaymentDto(
    Guid Id,
    Guid? StageId,
    string? StageTitle,
    int Amount,
    string Method,
    DateOnly PaidOn,
    string? Note,
    string RecordedBy,
    bool Mine,
    string Status,
    DateTimeOffset RecordedAt,
    DateTimeOffset? AnsweredAt,
    string? DisputeReason,
    DateTimeOffset? ResolvedAt,
    string? ResolutionNote);

/// <summary>A payment in the Back Office list, with its order's parties.</summary>
public sealed record AdminPaymentDto(
    Guid Id,
    Guid OrderId,
    int OrderPrice,
    int Amount,
    string Method,
    DateOnly PaidOn,
    string? Note,
    string RecordedBy,
    string Status,
    DateTimeOffset RecordedAt,
    DateTimeOffset? AnsweredAt,
    string? DisputeReason,
    DateTimeOffset? ResolvedAt,
    string? ResolutionNote,
    Guid CustomerId,
    string? CustomerName,
    string CustomerPhone,
    Guid PartnerId,
    string PartnerName,
    string PartnerPhone);

/// <summary>The customer ("I paid") or the partner ("I received") records a payment on their order.</summary>
public sealed record RecordPayment(Guid OrderId, int Amount, PaymentMethod Method, DateOnly PaidOn, Guid? StageId = null, string? Note = null)
    : ICommand<OrderDto>;

/// <summary>The other side confirms the payment happened.</summary>
public sealed record ConfirmPayment(Guid Id) : ICommand<OrderDto>;

/// <summary>The other side disputes it, saying why; the team decides.</summary>
public sealed record DisputePayment(Guid Id, string Reason) : ICommand<OrderDto>;

/// <summary>Whoever recorded it takes it back while it waits.</summary>
public sealed record WithdrawPayment(Guid Id) : ICommand<OrderDto>;

/// <summary>Payments for the Back Office: with <c>Status = Disputed</c> it is the dispute queue, waiting longest first.</summary>
public sealed record GetAdminPayments(PaymentStatus? Status = null, Guid? OrderId = null, int Page = 1, int PageSize = GetAdminPayments.DefaultPageSize)
    : IQuery<PagedResult<AdminPaymentDto>>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

/// <summary>Staff decide a disputed payment: it counts as paid (<see cref="Counts"/>) or not.</summary>
public sealed record ResolvePayment(Guid Id, bool Counts, string? Note) : ICommand<AdminPaymentDto>;

public sealed class RecordPaymentValidator : AbstractValidator<RecordPayment>
{
    public RecordPaymentValidator()
    {
        RuleFor(x => x.Amount).InclusiveBetween(1, Offer.MaxPrice).WithErrorCode("amount.invalid");
        RuleFor(x => x.Method).IsInEnum().WithErrorCode("method.invalid");
        RuleFor(x => x.Note)
            .Must(note => note!.Trim().Length <= Payment.NoteMaxLength).WithErrorCode("note.too_long")
            .When(x => x.Note is not null);
    }
}

public sealed class DisputePaymentValidator : AbstractValidator<DisputePayment>
{
    public DisputePaymentValidator() =>
        RuleFor(x => x.Reason)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode("reason.required")
            .Must(value => value is null || value.Trim().Length <= Payment.ReasonMaxLength).WithErrorCode("reason.too_long");
}

public sealed class GetAdminPaymentsValidator : AbstractValidator<GetAdminPayments>
{
    public GetAdminPaymentsValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithErrorCode("status.invalid");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetAdminPayments.MaxPageSize).WithErrorCode("page_size.invalid");
    }
}

public sealed class ResolvePaymentValidator : AbstractValidator<ResolvePayment>
{
    public ResolvePaymentValidator() =>
        RuleFor(x => x.Note)
            .Must(note => note!.Trim().Length <= Payment.ReasonMaxLength).WithErrorCode("note.too_long")
            .When(x => x.Note is not null);
}

internal static class PaymentViews
{
    public static NotFoundException NotFound() => new("Payment not found.", "payment.not_found");

    public static PaymentDto ToDto(Payment p, Order order, OrderParty viewer) => new(
        p.Id,
        p.StageId,
        order.Stages.FirstOrDefault(s => s.Id == p.StageId)?.Title,
        p.Amount,
        p.Method.ToString(),
        p.PaidOn,
        p.Note,
        p.RecordedBy.ToString(),
        p.RecordedBy == viewer,
        p.Status.ToString(),
        p.RecordedAt,
        p.AnsweredAt,
        p.DisputeReason,
        p.ResolvedAt,
        p.ResolutionNote);

    public static Dictionary<string, string?> Values(Payment p) => new()
    {
        ["amount"] = p.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["status"] = p.Status.ToString(),
    };
}

public sealed class RecordPaymentHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, TimeProvider clock)
    : ICommandHandler<RecordPayment, OrderDto>
{
    public async Task<OrderDto> HandleAsync(RecordPayment command, CancellationToken cancellationToken)
    {
        var (userId, partnerId) = await OrderViews.MeAsync(db, currentUser, cancellationToken);
        var order = await db.Orders.WithDetails()
            .SingleOrDefaultAsync(o => o.Id == command.OrderId && (o.CustomerId == userId || o.PartnerProfileId == partnerId), cancellationToken)
            ?? throw OrderViews.NotFound();
        var party = order.CustomerId == userId ? OrderParty.Customer : OrderParty.Partner;
        var counted = await db.Payments
            .Where(p => p.OrderId == order.Id && (p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Confirmed || p.Status == PaymentStatus.Disputed))
            .SumAsync(p => p.Amount, cancellationToken);

        var now = clock.GetUtcNow();
        var payment = Payment.Record(order, party, userId, command.StageId, command.Amount, command.Method, command.PaidOn, command.Note, counted, now);
        db.Payments.Add(payment);
        await Notifier.ToOrderAsync(db, order, party, NoticeTo.OtherSide, NotificationType.PaymentRecorded, PaymentViews.Values(payment), now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await OrderViews.ToDtoAsync(db, language, order, party, cancellationToken);
    }
}

/// <summary>Loads a payment on one of the signed-in user's orders, runs the action, tells the other side and returns the order.</summary>
internal static class PaymentCommand
{
    public static async Task<OrderDto> RunAsync(
        IAppDbContext db,
        ICurrentUser currentUser,
        ICurrentLanguage language,
        TimeProvider clock,
        Guid paymentId,
        Action<Payment, OrderParty, DateTimeOffset> act,
        NotificationType? notice,
        CancellationToken cancellationToken)
    {
        var (userId, partnerId) = await OrderViews.MeAsync(db, currentUser, cancellationToken);
        var payment = await db.Payments.SingleOrDefaultAsync(p => p.Id == paymentId, cancellationToken) ?? throw PaymentViews.NotFound();
        var order = await db.Orders.WithDetails()
            .SingleOrDefaultAsync(o => o.Id == payment.OrderId && (o.CustomerId == userId || o.PartnerProfileId == partnerId), cancellationToken)
            ?? throw PaymentViews.NotFound();
        var party = order.CustomerId == userId ? OrderParty.Customer : OrderParty.Partner;
        var now = clock.GetUtcNow();
        act(payment, party, now);
        if (notice is { } type)
        {
            await Notifier.ToOrderAsync(db, order, party, NoticeTo.OtherSide, type, PaymentViews.Values(payment), now, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await OrderViews.ToDtoAsync(db, language, order, party, cancellationToken);
    }
}

public sealed class ConfirmPaymentHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, TimeProvider clock)
    : ICommandHandler<ConfirmPayment, OrderDto>
{
    public Task<OrderDto> HandleAsync(ConfirmPayment command, CancellationToken cancellationToken) =>
        PaymentCommand.RunAsync(
            db, currentUser, language, clock, command.Id, (payment, by, now) => payment.Confirm(by, now), NotificationType.PaymentAnswered, cancellationToken);
}

public sealed class DisputePaymentHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, TimeProvider clock)
    : ICommandHandler<DisputePayment, OrderDto>
{
    public Task<OrderDto> HandleAsync(DisputePayment command, CancellationToken cancellationToken) =>
        PaymentCommand.RunAsync(
            db, currentUser, language, clock, command.Id, (payment, by, now) => payment.Dispute(by, command.Reason, now), NotificationType.PaymentAnswered, cancellationToken);
}

public sealed class WithdrawPaymentHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, TimeProvider clock)
    : ICommandHandler<WithdrawPayment, OrderDto>
{
    public Task<OrderDto> HandleAsync(WithdrawPayment command, CancellationToken cancellationToken) =>
        PaymentCommand.RunAsync(db, currentUser, language, clock, command.Id, (payment, by, now) => payment.Withdraw(by, now), notice: null, cancellationToken);
}

internal static class AdminPayments
{
    /// <summary>The Back Office rows for these payments, in the given order.</summary>
    public static async Task<List<AdminPaymentDto>> LoadAsync(IAppDbContext db, IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        var rows = await (
            from payment in db.Payments.AsNoTracking()
            join order in db.Orders.AsNoTracking() on payment.OrderId equals order.Id
            join customer in db.Users.AsNoTracking() on order.CustomerId equals customer.Id
            join partner in db.PartnerProfiles.AsNoTracking() on order.PartnerProfileId equals partner.Id
            join partnerUser in db.Users.AsNoTracking() on partner.UserId equals partnerUser.Id
            where ids.Contains(payment.Id)
            select new
            {
                Payment = payment,
                OrderPrice = order.Price,
                CustomerId = customer.Id,
                CustomerName = customer.FullName,
                CustomerPhone = customer.Phone,
                PartnerId = partner.Id,
                PartnerName = partner.DisplayName,
                PartnerPhone = partnerUser.Phone,
            }).ToListAsync(cancellationToken);

        return ids
            .Select(id => rows.Single(r => r.Payment.Id == id))
            .Select(r => new AdminPaymentDto(
                r.Payment.Id,
                r.Payment.OrderId,
                r.OrderPrice,
                r.Payment.Amount,
                r.Payment.Method.ToString(),
                r.Payment.PaidOn,
                r.Payment.Note,
                r.Payment.RecordedBy.ToString(),
                r.Payment.Status.ToString(),
                r.Payment.RecordedAt,
                r.Payment.AnsweredAt,
                r.Payment.DisputeReason,
                r.Payment.ResolvedAt,
                r.Payment.ResolutionNote,
                r.CustomerId,
                r.CustomerName,
                r.CustomerPhone.Value,
                r.PartnerId,
                r.PartnerName,
                r.PartnerPhone.Value))
            .ToList();
    }
}

public sealed class GetAdminPaymentsHandler(IAppDbContext db) : IQueryHandler<GetAdminPayments, PagedResult<AdminPaymentDto>>
{
    public async Task<PagedResult<AdminPaymentDto>> HandleAsync(GetAdminPayments query, CancellationToken cancellationToken)
    {
        var payments = db.Payments.AsNoTracking();
        if (query.Status is { } status)
        {
            payments = payments.Where(p => p.Status == status);
        }

        if (query.OrderId is { } orderId)
        {
            payments = payments.Where(p => p.OrderId == orderId);
        }

        payments = query.Status == PaymentStatus.Disputed
            ? payments.OrderBy(p => p.AnsweredAt).ThenBy(p => p.Id)
            : payments.OrderByDescending(p => p.RecordedAt).ThenByDescending(p => p.Id);

        var total = await payments.CountAsync(cancellationToken);
        var ids = await payments.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).Select(p => p.Id).ToListAsync(cancellationToken);
        var items = await AdminPayments.LoadAsync(db, ids, cancellationToken);
        return new PagedResult<AdminPaymentDto>(items, query.Page, query.PageSize, total);
    }
}

public sealed class ResolvePaymentHandler(IAppDbContext db, TimeProvider clock) : ICommandHandler<ResolvePayment, AdminPaymentDto>
{
    public async Task<AdminPaymentDto> HandleAsync(ResolvePayment command, CancellationToken cancellationToken)
    {
        var payment = await db.Payments.SingleOrDefaultAsync(p => p.Id == command.Id, cancellationToken) ?? throw PaymentViews.NotFound();
        var order = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == payment.OrderId, cancellationToken);
        var now = clock.GetUtcNow();
        payment.Resolve(command.Counts, command.Note, now);
        await Notifier.ToOrderAsync(db, order, OrderParty.Staff, NoticeTo.Both, NotificationType.PaymentResolved, PaymentViews.Values(payment), now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return (await AdminPayments.LoadAsync(db, [payment.Id], cancellationToken))[0];
    }
}
