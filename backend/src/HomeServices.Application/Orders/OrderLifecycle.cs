using System.Linq.Expressions;
using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Commissions;
using HomeServices.Application.Messaging;
using HomeServices.Application.Notifications;
using HomeServices.Domain.Notifications;
using HomeServices.Domain.Offers;
using HomeServices.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Orders;

/// <summary>The partner starts the work.</summary>
public sealed record StartOrder(Guid Id) : ICommand<OrderDto>;

/// <summary>The partner marks the work (or the visit) as done; the customer confirms or it completes by itself later.</summary>
public sealed record RequestOrderCompletion(Guid Id) : ICommand<OrderDto>;

/// <summary>The customer confirms the order is done.</summary>
public sealed record ConfirmOrderCompletion(Guid Id) : ICommand<OrderDto>;

/// <summary>The customer says the order isn't done yet; back to in progress.</summary>
public sealed record RejectOrderCompletion(Guid Id, string Reason) : ICommand<OrderDto>;

/// <summary>Either side cancels the order, with a reason.</summary>
public sealed record CancelOrder(Guid Id, string Reason) : ICommand<OrderDto>;

/// <summary>Either side proposes extra work or a new schedule (see <see cref="ChangeProposal"/>).</summary>
public sealed record ProposeOrderChange(
    Guid Id,
    ChangeRequestKind Kind,
    string? Title,
    string? Description,
    int? Amount,
    DateOnly? NewStartDate,
    int? NewDurationDays,
    DateTimeOffset? NewVisitAt) : ICommand<OrderDto>;

/// <summary>The other side accepts a proposed change; it applies to the order.</summary>
public sealed record AcceptOrderChange(Guid Id, Guid ChangeId) : ICommand<OrderDto>;

/// <summary>The other side turns a proposed change down.</summary>
public sealed record RejectOrderChange(Guid Id, Guid ChangeId, string? Note) : ICommand<OrderDto>;

/// <summary>Whoever proposed a change takes it back.</summary>
public sealed record WithdrawOrderChange(Guid Id, Guid ChangeId) : ICommand<OrderDto>;

/// <summary>Completes orders marked as done whose customer didn't answer in time (the auto-complete job). Returns how many.</summary>
public sealed record CompleteUnansweredOrders : ICommand<int>;

internal static class ReasonRules
{
    /// <summary>A reason is required and at most <see cref="Order.ReasonMaxLength"/> characters.</summary>
    public static void Reason<T>(this AbstractValidator<T> validator, Expression<Func<T, string>> reason) =>
        validator.RuleFor(reason)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode("reason.required")
            .Must(value => value is null || value.Trim().Length <= Order.ReasonMaxLength).WithErrorCode("reason.too_long");
}

public sealed class RejectOrderCompletionValidator : AbstractValidator<RejectOrderCompletion>
{
    public RejectOrderCompletionValidator() => this.Reason(x => x.Reason);
}

public sealed class CancelOrderValidator : AbstractValidator<CancelOrder>
{
    public CancelOrderValidator() => this.Reason(x => x.Reason);
}

public sealed class ProposeOrderChangeValidator : AbstractValidator<ProposeOrderChange>
{
    public ProposeOrderChangeValidator()
    {
        RuleFor(x => x.Kind).IsInEnum().WithErrorCode("kind.invalid");
        RuleFor(x => x.Title)
            .Must(title => !string.IsNullOrWhiteSpace(title)).WithErrorCode("title.required")
            .When(x => x.Kind == ChangeRequestKind.ExtraWork);
        RuleFor(x => x.Title)
            .Must(title => title!.Trim().Length <= Offer.StageTitleMaxLength).WithErrorCode("title.too_long")
            .When(x => x.Title is not null);
        RuleFor(x => x.Amount)
            .NotNull().WithErrorCode("amount.required")
            .InclusiveBetween(1, Offer.MaxPrice).WithErrorCode("amount.invalid")
            .When(x => x.Kind == ChangeRequestKind.ExtraWork);
        RuleFor(x => x.Description)
            .Must(text => text!.Trim().Length <= Order.ReasonMaxLength).WithErrorCode("description.too_long")
            .When(x => x.Description is not null);
        RuleFor(x => x.NewDurationDays)
            .InclusiveBetween(1, Order.MaxDurationDays).WithErrorCode("new_duration_days.invalid")
            .When(x => x.NewDurationDays is not null);
    }
}

public sealed class RejectOrderChangeValidator : AbstractValidator<RejectOrderChange>
{
    public RejectOrderChangeValidator() =>
        RuleFor(x => x.Note)
            .Must(note => note!.Trim().Length <= Order.ReasonMaxLength).WithErrorCode("note.too_long")
            .When(x => x.Note is not null);
}

/// <summary>
/// Loads the signed-in user's order (either side), runs one domain action, tells whoever the notice names,
/// saves and returns the new view.
/// </summary>
internal static class OrderCommand
{
    public static IQueryable<Order> WithDetails(this IQueryable<Order> orders) =>
        orders.Include(o => o.Stages).Include(o => o.StatusChanges).Include(o => o.ChangeRequests);

    public static async Task<OrderDto> RunAsync(
        IAppDbContext db,
        ICurrentUser currentUser,
        ICurrentLanguage language,
        TimeProvider clock,
        Guid orderId,
        Action<Order, OrderParty, DateTimeOffset> act,
        OrderNotice? notice,
        CancellationToken cancellationToken)
    {
        var (userId, partnerId) = await OrderViews.MeAsync(db, currentUser, cancellationToken);
        var order = await db.Orders.WithDetails()
            .SingleOrDefaultAsync(o => o.Id == orderId && (o.CustomerId == userId || o.PartnerProfileId == partnerId), cancellationToken)
            ?? throw OrderViews.NotFound();
        var party = order.CustomerId == userId ? OrderParty.Customer : OrderParty.Partner;
        var now = clock.GetUtcNow();
        var wasCompleted = order.Status == OrderStatus.Completed;
        act(order, party, now);
        if (!wasCompleted)
        {
            await CommissionLogic.ChargeAsync(db, order, now, cancellationToken);
        }

        if (notice is not null)
        {
            await Notifier.ToOrderAsync(db, order, party, notice.To, notice.Type, notice.Values, now, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await OrderViews.ToDtoAsync(db, language, order, party, cancellationToken);
    }
}

public sealed class StartOrderHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, TimeProvider clock)
    : ICommandHandler<StartOrder, OrderDto>
{
    public Task<OrderDto> HandleAsync(StartOrder command, CancellationToken cancellationToken) =>
        OrderCommand.RunAsync(
            db, currentUser, language, clock, command.Id, (order, by, now) => order.Start(by, now), new(NotificationType.OrderStarted), cancellationToken);
}

public sealed class RequestOrderCompletionHandler(
    IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, IOptions<OrderSettings> settings, TimeProvider clock)
    : ICommandHandler<RequestOrderCompletion, OrderDto>
{
    public Task<OrderDto> HandleAsync(RequestOrderCompletion command, CancellationToken cancellationToken) =>
        OrderCommand.RunAsync(
            db,
            currentUser,
            language,
            clock,
            command.Id,
            (order, by, now) => order.RequestCompletion(by, settings.Value.AutoCompleteAfter, now),
            new(NotificationType.CompletionRequested),
            cancellationToken);
}

public sealed class ConfirmOrderCompletionHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, TimeProvider clock)
    : ICommandHandler<ConfirmOrderCompletion, OrderDto>
{
    public Task<OrderDto> HandleAsync(ConfirmOrderCompletion command, CancellationToken cancellationToken) =>
        OrderCommand.RunAsync(
            db, currentUser, language, clock, command.Id, (order, by, now) => order.ConfirmCompletion(by, now), new(NotificationType.OrderCompleted), cancellationToken);
}

public sealed class RejectOrderCompletionHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, TimeProvider clock)
    : ICommandHandler<RejectOrderCompletion, OrderDto>
{
    public Task<OrderDto> HandleAsync(RejectOrderCompletion command, CancellationToken cancellationToken) =>
        OrderCommand.RunAsync(
            db,
            currentUser,
            language,
            clock,
            command.Id,
            (order, by, now) => order.RejectCompletion(by, command.Reason, now),
            new(NotificationType.CompletionRejected),
            cancellationToken);
}

public sealed class CancelOrderHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, TimeProvider clock)
    : ICommandHandler<CancelOrder, OrderDto>
{
    public Task<OrderDto> HandleAsync(CancelOrder command, CancellationToken cancellationToken) =>
        OrderCommand.RunAsync(
            db, currentUser, language, clock, command.Id, (order, by, now) => order.Cancel(by, command.Reason, now), new(NotificationType.OrderCancelled), cancellationToken);
}

public sealed class ProposeOrderChangeHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, TimeProvider clock)
    : ICommandHandler<ProposeOrderChange, OrderDto>
{
    public Task<OrderDto> HandleAsync(ProposeOrderChange command, CancellationToken cancellationToken) =>
        OrderCommand.RunAsync(
            db,
            currentUser,
            language,
            clock,
            command.Id,
            (order, by, now) => order.ProposeChange(
                by,
                new ChangeProposal(command.Kind, command.Title, command.Description, command.Amount, command.NewStartDate, command.NewDurationDays, command.NewVisitAt),
                now),
            new(NotificationType.ChangeProposed, Values: new Dictionary<string, string?> { ["kind"] = command.Kind.ToString() }),
            cancellationToken);
}

public sealed class AcceptOrderChangeHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, TimeProvider clock)
    : ICommandHandler<AcceptOrderChange, OrderDto>
{
    public Task<OrderDto> HandleAsync(AcceptOrderChange command, CancellationToken cancellationToken) =>
        OrderCommand.RunAsync(
            db,
            currentUser,
            language,
            clock,
            command.Id,
            (order, by, now) => order.AcceptChange(by, command.ChangeId, now),
            new(NotificationType.ChangeAnswered, Values: new Dictionary<string, string?> { ["status"] = nameof(ChangeRequestStatus.Accepted) }),
            cancellationToken);
}

public sealed class RejectOrderChangeHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, TimeProvider clock)
    : ICommandHandler<RejectOrderChange, OrderDto>
{
    public Task<OrderDto> HandleAsync(RejectOrderChange command, CancellationToken cancellationToken) =>
        OrderCommand.RunAsync(
            db,
            currentUser,
            language,
            clock,
            command.Id,
            (order, by, now) => order.RejectChange(by, command.ChangeId, command.Note, now),
            new(NotificationType.ChangeAnswered, Values: new Dictionary<string, string?> { ["status"] = nameof(ChangeRequestStatus.Rejected) }),
            cancellationToken);
}

public sealed class WithdrawOrderChangeHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, TimeProvider clock)
    : ICommandHandler<WithdrawOrderChange, OrderDto>
{
    public Task<OrderDto> HandleAsync(WithdrawOrderChange command, CancellationToken cancellationToken) =>
        OrderCommand.RunAsync(
            db, currentUser, language, clock, command.Id, (order, by, now) => order.WithdrawChange(by, command.ChangeId, now), notice: null, cancellationToken);
}

public sealed class CompleteUnansweredOrdersHandler(IAppDbContext db, TimeProvider clock) : ICommandHandler<CompleteUnansweredOrders, int>
{
    public async Task<int> HandleAsync(CompleteUnansweredOrders command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var due = await db.Orders
            .Include(o => o.StatusChanges)
            .Include(o => o.ChangeRequests)
            .Where(o => o.Status == OrderStatus.CompletionRequested && o.AutoCompleteAt <= now)
            .ToListAsync(cancellationToken);
        var completed = due.Where(o => o.CompleteIfUnanswered(now)).ToList();
        foreach (var order in completed)
        {
            await CommissionLogic.ChargeAsync(db, order, now, cancellationToken);
            await Notifier.ToOrderAsync(
                db, order, OrderParty.System, NoticeTo.Both, NotificationType.OrderCompleted, new Dictionary<string, string?> { ["auto"] = "true" }, now, cancellationToken);
        }

        if (completed.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return completed.Count;
    }
}
