using HomeServices.Domain.Offers;
using HomeServices.Domain.Orders;

namespace HomeServices.Domain.Tests.Orders;

public class OrderLifecycleTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Week = TimeSpan.FromDays(7);

    private static Order WorkOrder() => Order.FromOffer(
        Offer.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OfferKind.Work,
            new OfferTerms(
                "Replace the kitchen tap and the pipes.",
                [new OfferLine("Remove the old tap", true)],
                100_000,
                true,
                null,
                new DateOnly(2026, 10, 8),
                2,
                null,
                [new OfferStageTerms("Deposit", PaymentPurpose.Deposit, 30_000), new OfferStageTerms(null, PaymentPurpose.Final, 70_000)]),
            Now.AddDays(7),
            Now),
        Guid.NewGuid(),
        null,
        "{}",
        Now);

    private static Order VisitOrder() => Order.FromOffer(
        Offer.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OfferKind.Visit,
            new OfferTerms("I'll come and measure the bathroom.", [], 0, false, null, null, null, Now.AddDays(1), []),
            Now.AddDays(7),
            Now),
        Guid.NewGuid(),
        null,
        "{}",
        Now);

    private static ChangeProposal ExtraWork(int amount = 20_000) =>
        new(ChangeRequestKind.ExtraWork, "Replace the siphon", "It is cracked.", amount, null, null, null);

    private static ChangeProposal Schedule(DateOnly? start = null, int? days = null, DateTimeOffset? visitAt = null) =>
        new(ChangeRequestKind.Schedule, null, null, null, start, days, visitAt);

    [Fact]
    public void The_partner_starts_and_finishes_and_the_customer_confirms()
    {
        var order = WorkOrder();

        order.Start(OrderParty.Partner, Now.AddDays(1));
        order.Status.ShouldBe(OrderStatus.InProgress);
        order.StartedAt.ShouldBe(Now.AddDays(1));

        order.RequestCompletion(OrderParty.Partner, Week, Now.AddDays(2));
        order.Status.ShouldBe(OrderStatus.CompletionRequested);
        order.CompletionRequestedAt.ShouldBe(Now.AddDays(2));
        order.AutoCompleteAt.ShouldBe(Now.AddDays(9));

        order.ConfirmCompletion(OrderParty.Customer, Now.AddDays(3));
        order.Status.ShouldBe(OrderStatus.Completed);
        order.CompletedAt.ShouldBe(Now.AddDays(3));
        order.AutoCompleteAt.ShouldBeNull();
        order.IsOpen.ShouldBeFalse();
        order.StatusChanges.Select(c => (c.Status, c.ChangedBy, c.Sequence)).ShouldBe(new[]
        {
            (OrderStatus.Confirmed, (OrderParty?)OrderParty.Customer, 1),
            (OrderStatus.InProgress, OrderParty.Partner, 2),
            (OrderStatus.CompletionRequested, OrderParty.Partner, 3),
            (OrderStatus.Completed, OrderParty.Customer, 4),
        });
    }

    [Fact]
    public void Each_step_belongs_to_one_side_and_one_status()
    {
        var order = WorkOrder();

        Should.Throw<DomainException>(() => order.Start(OrderParty.Customer, Now)).Code.ShouldBe("order.wrong_party");
        Should.Throw<DomainException>(() => order.RequestCompletion(OrderParty.Customer, Week, Now)).Code.ShouldBe("order.wrong_party");
        Should.Throw<DomainException>(() => order.ConfirmCompletion(OrderParty.Customer, Now)).Code.ShouldBe("order.completion_not_requested");
        Should.Throw<DomainException>(() => order.RejectCompletion(OrderParty.Customer, "No", Now)).Code.ShouldBe("order.completion_not_requested");

        order.Start(OrderParty.Partner, Now);
        Should.Throw<DomainException>(() => order.Start(OrderParty.Partner, Now)).Code.ShouldBe("order.cannot_start");
        order.RequestCompletion(OrderParty.Partner, Week, Now);
        Should.Throw<DomainException>(() => order.RequestCompletion(OrderParty.Partner, Week, Now)).Code.ShouldBe("order.cannot_request_completion");
        Should.Throw<DomainException>(() => order.ConfirmCompletion(OrderParty.Partner, Now)).Code.ShouldBe("order.wrong_party");
    }

    [Fact]
    public void The_customer_can_say_it_is_not_finished_with_a_reason()
    {
        var order = WorkOrder();
        order.RequestCompletion(OrderParty.Partner, Week, Now);
        order.StartedAt.ShouldBe(Now);

        Should.Throw<DomainException>(() => order.RejectCompletion(OrderParty.Customer, " ", Now)).Code.ShouldBe("order.reason_required");
        Should.Throw<DomainException>(() => order.RejectCompletion(OrderParty.Customer, new string('a', 1001), Now)).Code.ShouldBe("order.reason_too_long");
        order.RejectCompletion(OrderParty.Customer, "  The tap still drips. ", Now.AddHours(1));

        order.Status.ShouldBe(OrderStatus.InProgress);
        order.CompletionRequestedAt.ShouldBeNull();
        order.AutoCompleteAt.ShouldBeNull();
        var last = order.StatusChanges.MaxBy(c => c.Sequence)!;
        last.Note.ShouldBe("The tap still drips.");
        last.ChangedBy.ShouldBe(OrderParty.Customer);
    }

    [Fact]
    public void An_unanswered_order_completes_by_itself_once_due()
    {
        var order = VisitOrder();
        order.CompleteIfUnanswered(Now.AddDays(30)).ShouldBeFalse();
        order.RequestCompletion(OrderParty.Partner, Week, Now);

        order.CompleteIfUnanswered(Now.AddDays(7).AddMinutes(-1)).ShouldBeFalse();
        order.CompleteIfUnanswered(Now.AddDays(7)).ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.Completed);
        order.StatusChanges.MaxBy(c => c.Sequence)!.ChangedBy.ShouldBe(OrderParty.System);
        order.CompleteIfUnanswered(Now.AddDays(8)).ShouldBeFalse();
    }

    [Fact]
    public void Cancelling_before_work_starts_just_cancels()
    {
        var order = WorkOrder();
        order.ProposeChange(OrderParty.Customer, ExtraWork(), Now);

        Should.Throw<DomainException>(() => order.Cancel(OrderParty.System, "x", Now)).Code.ShouldBe("order.wrong_party");
        Should.Throw<DomainException>(() => order.Cancel(OrderParty.Customer, null, Now)).Code.ShouldBe("order.reason_required");
        order.Cancel(OrderParty.Customer, "Found someone closer", Now.AddHours(2));

        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.CancelledAt.ShouldBe(Now.AddHours(2));
        order.CancelledBy.ShouldBe(OrderParty.Customer);
        order.CancelReason.ShouldBe("Found someone closer");
        order.NeedsAttentionSince.ShouldBeNull();
        order.ChangeRequests.ShouldHaveSingleItem().Status.ShouldBe(ChangeRequestStatus.Closed);
        Should.Throw<DomainException>(() => order.Cancel(OrderParty.Partner, "Again", Now)).Code.ShouldBe("order.cannot_cancel");
        Should.Throw<DomainException>(() => order.Resolve()).Code.ShouldBe("order.not_flagged");
    }

    [Fact]
    public void Cancelling_after_work_started_flags_the_order_for_the_team_until_resolved()
    {
        var order = WorkOrder();
        order.Start(OrderParty.Partner, Now);
        order.RequestCompletion(OrderParty.Partner, Week, Now);

        order.Cancel(OrderParty.Partner, "The customer stopped answering", Now.AddDays(1));

        order.NeedsAttentionSince.ShouldBe(Now.AddDays(1));
        order.AutoCompleteAt.ShouldBeNull();
        order.Resolve();
        order.NeedsAttentionSince.ShouldBeNull();

        var byStaff = WorkOrder();
        byStaff.Start(OrderParty.Partner, Now);
        byStaff.Cancel(OrderParty.Staff, "Duplicate", Now);
        byStaff.NeedsAttentionSince.ShouldBeNull();
        byStaff.CancelledBy.ShouldBe(OrderParty.Staff);
    }

    [Fact]
    public void Accepted_extra_work_raises_the_price_and_adds_a_payment_before_the_final_one()
    {
        var order = WorkOrder();

        var change = order.ProposeChange(OrderParty.Partner, ExtraWork(), Now);
        change.Status.ShouldBe(ChangeRequestStatus.Pending);
        change.ProposedBy.ShouldBe(OrderParty.Partner);
        change.Title.ShouldBe("Replace the siphon");
        order.PendingChange.ShouldBe(change);
        Should.Throw<DomainException>(() => order.ProposeChange(OrderParty.Customer, Schedule(days: 3), Now)).Code.ShouldBe("change.pending_exists");
        Should.Throw<DomainException>(() => order.AcceptChange(OrderParty.Partner, change.Id, Now)).Code.ShouldBe("change.wrong_party");

        order.AcceptChange(OrderParty.Customer, change.Id, Now.AddHours(1));

        order.Price.ShouldBe(120_000);
        change.Status.ShouldBe(ChangeRequestStatus.Accepted);
        change.DecidedAt.ShouldBe(Now.AddHours(1));
        order.Stages.OrderBy(s => s.SortOrder).Select(s => (s.Title, s.Purpose, s.Amount)).ShouldBe(new[]
        {
            ("Deposit", PaymentPurpose.Deposit, 30_000),
            ("Replace the siphon", PaymentPurpose.Stage, 20_000),
            ((string?)null, PaymentPurpose.Final, 70_000),
        });
        Should.Throw<DomainException>(() => order.AcceptChange(OrderParty.Customer, change.Id, Now)).Code.ShouldBe("change.not_pending");
        Should.Throw<DomainException>(() => order.AcceptChange(OrderParty.Customer, Guid.NewGuid(), Now)).Code.ShouldBe("change.not_found");
    }

    [Fact]
    public void A_visit_has_no_extra_work()
    {
        var order = VisitOrder();
        Should.Throw<DomainException>(() => order.ProposeChange(OrderParty.Partner, ExtraWork(), Now)).Code.ShouldBe("change.extra_work_on_visit");
    }

    [Fact]
    public void A_new_schedule_moves_the_dates_once_accepted()
    {
        var order = WorkOrder();
        var change = order.ProposeChange(OrderParty.Customer, Schedule(new DateOnly(2026, 10, 12)), Now);
        order.AcceptChange(OrderParty.Partner, change.Id, Now);
        order.StartDate.ShouldBe(new DateOnly(2026, 10, 12));
        order.DurationDays.ShouldBe(2);

        var longer = order.ProposeChange(OrderParty.Partner, Schedule(days: 4), Now);
        order.AcceptChange(OrderParty.Customer, longer.Id, Now);
        order.StartDate.ShouldBe(new DateOnly(2026, 10, 12));
        order.DurationDays.ShouldBe(4);

        var visit = VisitOrder();
        var moved = visit.ProposeChange(OrderParty.Customer, Schedule(visitAt: Now.AddDays(3)), Now);
        visit.AcceptChange(OrderParty.Partner, moved.Id, Now);
        visit.VisitAt.ShouldBe(Now.AddDays(3));
    }

    [Fact]
    public void Proposals_are_checked()
    {
        var order = WorkOrder();
        void Fails(ChangeProposal proposal, string code) =>
            Should.Throw<DomainException>(() => order.ProposeChange(OrderParty.Customer, proposal, Now)).Code.ShouldBe(code);

        Fails(ExtraWork() with { Title = " " }, "change.title_invalid");
        Fails(ExtraWork() with { Title = new string('a', 101) }, "change.title_invalid");
        Fails(ExtraWork(0), "change.amount_invalid");
        Fails(ExtraWork() with { Amount = null }, "change.amount_invalid");
        Fails(ExtraWork() with { Description = new string('a', 1001) }, "change.description_too_long");
        Fails(Schedule(), "change.schedule_empty");
        Fails(Schedule(days: 0), "change.duration_invalid");
        Fails(Schedule(days: 366), "change.duration_invalid");
        Fails(Schedule(new DateOnly(2026, 10, 4)), "change.start_date_past");
        Fails(new ChangeProposal((ChangeRequestKind)9, null, null, null, null, null, null), "change.kind_invalid");
        Should.Throw<DomainException>(() => order.ProposeChange(OrderParty.Staff, Schedule(days: 3), Now)).Code.ShouldBe("order.wrong_party");
        Should.Throw<DomainException>(() => VisitOrder().ProposeChange(OrderParty.Customer, Schedule(visitAt: Now), Now)).Code.ShouldBe("change.visit_time_invalid");
        Should.Throw<DomainException>(() => VisitOrder().ProposeChange(OrderParty.Customer, Schedule(days: 2), Now)).Code.ShouldBe("change.visit_time_invalid");

        var trimmed = order.ProposeChange(OrderParty.Customer, ExtraWork() with { Title = "  Siphon ", Description = "  " }, Now);
        trimmed.Title.ShouldBe("Siphon");
        trimmed.Description.ShouldBeNull();
    }

    [Fact]
    public void Changes_can_be_turned_down_or_withdrawn_by_the_right_side()
    {
        var order = WorkOrder();
        var change = order.ProposeChange(OrderParty.Customer, Schedule(days: 3), Now);

        Should.Throw<DomainException>(() => order.WithdrawChange(OrderParty.Partner, change.Id, Now)).Code.ShouldBe("change.wrong_party");
        Should.Throw<DomainException>(() => order.RejectChange(OrderParty.Partner, change.Id, new string('a', 1001), Now)).Code.ShouldBe("change.note_too_long");
        order.RejectChange(OrderParty.Partner, change.Id, " Too long for me ", Now);
        change.Status.ShouldBe(ChangeRequestStatus.Rejected);
        change.ResponseNote.ShouldBe("Too long for me");
        order.DurationDays.ShouldBe(2);

        var again = order.ProposeChange(OrderParty.Customer, Schedule(days: 3), Now);
        order.WithdrawChange(OrderParty.Customer, again.Id, Now);
        again.Status.ShouldBe(ChangeRequestStatus.Withdrawn);
        order.PendingChange.ShouldBeNull();
    }

    [Fact]
    public void Nothing_changes_once_the_partner_marked_the_order_done()
    {
        var order = WorkOrder();
        var change = order.ProposeChange(OrderParty.Customer, Schedule(days: 3), Now);
        order.RequestCompletion(OrderParty.Partner, Week, Now);

        change.Status.ShouldBe(ChangeRequestStatus.Closed);
        Should.Throw<DomainException>(() => order.ProposeChange(OrderParty.Customer, Schedule(days: 3), Now)).Code.ShouldBe("order.cannot_change");
        Should.Throw<DomainException>(() => order.WithdrawChange(OrderParty.Customer, change.Id, Now)).Code.ShouldBe("change.not_pending");
    }

    [Fact]
    public void Extra_work_cannot_push_the_price_over_the_limit()
    {
        var order = WorkOrder();
        var change = order.ProposeChange(OrderParty.Partner, ExtraWork(Offer.MaxPrice), Now);
        Should.Throw<DomainException>(() => order.AcceptChange(OrderParty.Customer, change.Id, Now)).Code.ShouldBe("change.price_too_high");
    }
}
