using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Orders;
using HomeServices.Application.Tests.Offers;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Orders;
using HomeServices.Domain.Partners;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Orders;

public class OrderLifecycleTests
{
    private readonly OfferTestData _data = new();

    private OrderSettings Settings { get; } = new() { AutoCompleteDays = 7 };

    private ICurrentLanguage Language => _data.Requests.Language;

    private FakeClock Clock => _data.Requests.Clock;

    private IAppDbContext Db => _data.Requests.Db;

    private async Task<(OrderDto Order, PartnerProfile Partner)> GivenWorkOrderAsync()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var offer = await _data.SendAsync(aram, _data.Work(request.Id));
        return (await _data.AcceptAsync(offer.Id), aram);
    }

    private Task<OrderDto> StartAsync(Guid id, ICurrentUser user) =>
        new StartOrderHandler(Db, user, Language, Clock).HandleAsync(new StartOrder(id), CancellationToken.None);

    private Task<OrderDto> RequestCompletionAsync(Guid id, ICurrentUser user) =>
        new RequestOrderCompletionHandler(Db, user, Language, Options.Create(Settings), Clock).HandleAsync(new RequestOrderCompletion(id), CancellationToken.None);

    private Task<OrderDto> ConfirmAsync(Guid id, ICurrentUser user) =>
        new ConfirmOrderCompletionHandler(Db, user, Language, Clock).HandleAsync(new ConfirmOrderCompletion(id), CancellationToken.None);

    private Task<OrderDto> RejectCompletionAsync(Guid id, ICurrentUser user, string reason) =>
        new RejectOrderCompletionHandler(Db, user, Language, Clock).HandleAsync(new RejectOrderCompletion(id, reason), CancellationToken.None);

    private Task<OrderDto> CancelAsync(Guid id, ICurrentUser user, string reason) =>
        new CancelOrderHandler(Db, user, Language, Clock).HandleAsync(new CancelOrder(id, reason), CancellationToken.None);

    private Task<OrderDto> ProposeAsync(Guid id, ICurrentUser user, ProposeOrderChange? command = null) =>
        new ProposeOrderChangeHandler(Db, user, Language, Clock).HandleAsync(
            command ?? new ProposeOrderChange(id, ChangeRequestKind.ExtraWork, "Replace the siphon", "It is cracked.", 20_000, null, null, null),
            CancellationToken.None);

    private Task<OrderDto> AcceptChangeAsync(Guid id, Guid changeId, ICurrentUser user) =>
        new AcceptOrderChangeHandler(Db, user, Language, Clock).HandleAsync(new AcceptOrderChange(id, changeId), CancellationToken.None);

    private Task<OrderDto> RejectChangeAsync(Guid id, Guid changeId, ICurrentUser user, string? note) =>
        new RejectOrderChangeHandler(Db, user, Language, Clock).HandleAsync(new RejectOrderChange(id, changeId, note), CancellationToken.None);

    private Task<OrderDto> WithdrawChangeAsync(Guid id, Guid changeId, ICurrentUser user) =>
        new WithdrawOrderChangeHandler(Db, user, Language, Clock).HandleAsync(new WithdrawOrderChange(id, changeId), CancellationToken.None);

    private Task<int> CompleteUnansweredAsync() =>
        new CompleteUnansweredOrdersHandler(Db, Clock).HandleAsync(new CompleteUnansweredOrders(), CancellationToken.None);

    [Fact]
    public async Task The_order_goes_from_start_to_completion_with_the_right_actions_for_each_side()
    {
        var (order, aram) = await GivenWorkOrderAsync();
        var partner = OfferTestData.As(aram);
        order.Actions.ShouldBe(new[] { OrderActions.ProposeChange, OrderActions.Cancel, OrderActions.RecordPayment });
        (await _data.OrderAsync(order.Id, partner)).Actions
            .ShouldBe(new[] { OrderActions.Start, OrderActions.RequestCompletion, OrderActions.ProposeChange, OrderActions.Cancel, OrderActions.RecordPayment });

        Clock.Now = OfferTestData.Now.AddDays(1);
        var started = await StartAsync(order.Id, partner);
        started.Status.ShouldBe("InProgress");
        started.StartedAt.ShouldBe(Clock.Now);
        started.MyRole.ShouldBe("Partner");

        var done = await RequestCompletionAsync(order.Id, partner);
        done.Status.ShouldBe("CompletionRequested");
        done.AutoCompleteAt.ShouldBe(Clock.Now.AddDays(7));
        done.Actions.ShouldBe(new[] { OrderActions.Cancel, OrderActions.RecordPayment });
        (await _data.OrderAsync(order.Id, _data.Customer)).Actions
            .ShouldBe(new[] { OrderActions.ConfirmCompletion, OrderActions.RejectCompletion, OrderActions.Cancel, OrderActions.RecordPayment });

        var completed = await ConfirmAsync(order.Id, _data.Customer);
        completed.Status.ShouldBe("Completed");
        completed.CompletedAt.ShouldBe(Clock.Now);
        completed.Actions.ShouldBe(new[] { OrderActions.RecordPayment, OrderActions.Review });
        completed.History.Select(h => (h.Status, h.By)).ShouldBe(new[]
        {
            ("Confirmed", (string?)"Customer"),
            ("InProgress", "Partner"),
            ("CompletionRequested", "Partner"),
            ("Completed", "Customer"),
        });
        (await _data.OrdersAsync(_data.Customer, new GetMyOrders(Status: OrderStatus.Completed))).Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task The_wrong_side_gets_a_rule_error_and_strangers_get_not_found()
    {
        var (order, _) = await GivenWorkOrderAsync();

        (await Should.ThrowAsync<DomainException>(() => StartAsync(order.Id, _data.Customer))).Code.ShouldBe("order.wrong_party");
        (await Should.ThrowAsync<DomainException>(() => ConfirmAsync(order.Id, _data.Customer))).Code.ShouldBe("order.completion_not_requested");
        var stranger = new FakeCurrentUser(_data.Requests.Partners.OtherUser.Id);
        (await Should.ThrowAsync<NotFoundException>(() => StartAsync(order.Id, stranger))).Code.ShouldBe("order.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => CancelAsync(Guid.NewGuid(), _data.Customer, "x"))).Code.ShouldBe("order.not_found");
    }

    [Fact]
    public async Task The_customer_can_send_it_back_and_unanswered_orders_complete_by_themselves()
    {
        var (order, aram) = await GivenWorkOrderAsync();
        var partner = OfferTestData.As(aram);
        await RequestCompletionAsync(order.Id, partner);

        var back = await RejectCompletionAsync(order.Id, _data.Customer, "The tap still drips");
        back.Status.ShouldBe("InProgress");
        back.History[^1].ShouldBe(new OrderStatusChangeDto("InProgress", Clock.Now, "Customer", "The tap still drips"));

        await RequestCompletionAsync(order.Id, partner);
        Clock.Now = OfferTestData.Now.AddDays(6);
        (await CompleteUnansweredAsync()).ShouldBe(0);
        Clock.Now = OfferTestData.Now.AddDays(7);
        (await CompleteUnansweredAsync()).ShouldBe(1);

        var completed = await _data.OrderAsync(order.Id, _data.Customer);
        completed.Status.ShouldBe("Completed");
        completed.History[^1].By.ShouldBe("System");
        (await CompleteUnansweredAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Cancelling_keeps_the_reason_and_flags_orders_cancelled_after_work_started()
    {
        var (early, _) = await GivenWorkOrderAsync();
        var cancelled = await CancelAsync(early.Id, _data.Customer, "  Found someone closer ");
        cancelled.Status.ShouldBe("Cancelled");
        cancelled.CancelledBy.ShouldBe("Customer");
        cancelled.CancelReason.ShouldBe("Found someone closer");
        cancelled.NeedsAttentionSince.ShouldBeNull();
        cancelled.Actions.ShouldBeEmpty();

        var (late, aram) = await GivenWorkOrderAsync();
        await StartAsync(late.Id, OfferTestData.As(aram));
        var flagged = await CancelAsync(late.Id, OfferTestData.As(aram), "The customer stopped answering");
        flagged.NeedsAttentionSince.ShouldBe(Clock.Now);
        flagged.History[^1].Note.ShouldBe("The customer stopped answering");
    }

    [Fact]
    public async Task Extra_work_is_proposed_by_one_side_and_accepted_by_the_other()
    {
        var (order, aram) = await GivenWorkOrderAsync();
        var partner = OfferTestData.As(aram);

        var proposed = await ProposeAsync(order.Id, partner);
        var change = proposed.ChangeRequests.ShouldHaveSingleItem();
        change.Status.ShouldBe("Pending");
        change.ProposedBy.ShouldBe("Partner");
        change.Mine.ShouldBeTrue();
        change.Amount.ShouldBe(20_000);
        proposed.Actions.ShouldContain(OrderActions.WithdrawChange);
        proposed.Actions.ShouldNotContain(OrderActions.ProposeChange);

        var asCustomer = await _data.OrderAsync(order.Id, _data.Customer);
        asCustomer.ChangeRequests.ShouldHaveSingleItem().Mine.ShouldBeFalse();
        asCustomer.Actions.ShouldContain(OrderActions.AnswerChange);
        (await Should.ThrowAsync<DomainException>(() => AcceptChangeAsync(order.Id, change.Id, partner))).Code.ShouldBe("change.wrong_party");

        var accepted = await AcceptChangeAsync(order.Id, change.Id, _data.Customer);
        accepted.Price.ShouldBe(120_000);
        accepted.Terms.Price.ShouldBe(100_000);
        accepted.Stages.Select(s => (s.Purpose, s.Amount)).ShouldBe(new[] { ("Deposit", 50_000), ("Stage", 20_000), ("Final", 50_000) });
        accepted.ChangeRequests.ShouldHaveSingleItem().Status.ShouldBe("Accepted");
        (await _data.OrdersAsync(_data.Customer)).Items.ShouldHaveSingleItem().Price.ShouldBe(120_000);
    }

    [Fact]
    public async Task Schedule_changes_can_be_turned_down_or_withdrawn()
    {
        var (order, aram) = await GivenWorkOrderAsync();
        var partner = OfferTestData.As(aram);
        var later = new ProposeOrderChange(order.Id, ChangeRequestKind.Schedule, null, "I'm away that week.", null, new DateOnly(2026, 10, 15), 3, null);

        var proposed = await ProposeAsync(order.Id, _data.Customer, later);
        var rejected = await RejectChangeAsync(order.Id, proposed.ChangeRequests[0].Id, partner, "Fully booked then");
        rejected.ChangeRequests[0].Status.ShouldBe("Rejected");
        rejected.ChangeRequests[0].ResponseNote.ShouldBe("Fully booked then");
        rejected.StartDate.ShouldBe(new DateOnly(2026, 10, 8));

        Clock.Now = OfferTestData.Now.AddHours(1);
        var again = await ProposeAsync(order.Id, _data.Customer, later);
        var newest = again.ChangeRequests[0];
        newest.Status.ShouldBe("Pending");
        (await WithdrawChangeAsync(order.Id, newest.Id, _data.Customer)).ChangeRequests[0].Status.ShouldBe("Withdrawn");

        Clock.Now = OfferTestData.Now.AddHours(2);
        var third = await ProposeAsync(order.Id, _data.Customer, later);
        var moved = await AcceptChangeAsync(order.Id, third.ChangeRequests[0].Id, partner);
        moved.StartDate.ShouldBe(new DateOnly(2026, 10, 15));
        moved.DurationDays.ShouldBe(3);
        moved.ChangeRequests.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Proposals_are_validated()
    {
        var validator = new ProposeOrderChangeValidator();
        var id = Guid.NewGuid();

        var noTitle = await validator.ValidateAsync(new ProposeOrderChange(id, ChangeRequestKind.ExtraWork, " ", null, null, null, null, null));
        noTitle.Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "title.required", "amount.required" }, ignoreOrder: true);
        var tooMuch = await validator.ValidateAsync(new ProposeOrderChange(id, (ChangeRequestKind)7, new string('a', 101), new string('a', 1001), null, null, 0, null));
        tooMuch.Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "kind.invalid", "title.too_long", "description.too_long", "new_duration_days.invalid" }, ignoreOrder: true);
        (await validator.ValidateAsync(new ProposeOrderChange(id, ChangeRequestKind.ExtraWork, "Siphon", null, -5, null, null, null)))
            .Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("amount.invalid");
        (await validator.ValidateAsync(new ProposeOrderChange(id, ChangeRequestKind.Schedule, null, null, null, null, 3, null))).IsValid.ShouldBeTrue();

        (await new CancelOrderValidator().ValidateAsync(new CancelOrder(id, " "))).Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("reason.required");
        (await new RejectOrderCompletionValidator().ValidateAsync(new RejectOrderCompletion(id, new string('a', 1001))))
            .Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("reason.too_long");
        (await new RejectOrderChangeValidator().ValidateAsync(new RejectOrderChange(id, id, new string('a', 1001))))
            .Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("note.too_long");
        (await new RejectOrderChangeValidator().ValidateAsync(new RejectOrderChange(id, id, null))).IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Changes_are_stored_with_the_order()
    {
        var (order, aram) = await GivenWorkOrderAsync();
        await ProposeAsync(order.Id, OfferTestData.As(aram));

        var stored = await Db.Orders.AsNoTracking().Include(o => o.ChangeRequests).SingleAsync(o => o.Id == order.Id);
        stored.ChangeRequests.ShouldHaveSingleItem().Title.ShouldBe("Replace the siphon");
    }
}
