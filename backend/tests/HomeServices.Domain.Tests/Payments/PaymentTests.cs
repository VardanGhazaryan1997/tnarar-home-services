using HomeServices.Domain.Offers;
using HomeServices.Domain.Orders;
using HomeServices.Domain.Payments;

namespace HomeServices.Domain.Tests.Payments;

public class PaymentTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 5);

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

    private static Payment Record(Order order, OrderParty by = OrderParty.Customer, int amount = 30_000, int alreadyCounted = 0, Guid? stageId = null) =>
        Payment.Record(order, by, Guid.NewGuid(), stageId, amount, PaymentMethod.Cash, Today, "  At the door  ", alreadyCounted, Now);

    [Fact]
    public void A_record_waits_for_the_other_side()
    {
        var order = WorkOrder();
        var stage = order.Stages.First();

        var payment = Record(order, stageId: stage.Id);

        payment.Status.ShouldBe(PaymentStatus.Pending);
        payment.StageId.ShouldBe(stage.Id);
        payment.Note.ShouldBe("At the door");
        payment.RecordedAt.ShouldBe(Now);
        payment.Counts.ShouldBeTrue();

        Should.Throw<DomainException>(() => payment.Confirm(OrderParty.Customer, Now)).Code.ShouldBe("payment.wrong_party");
        Should.Throw<DomainException>(() => payment.Confirm(OrderParty.Staff, Now)).Code.ShouldBe("payment.wrong_party");
        payment.Confirm(OrderParty.Partner, Now.AddHours(1));
        payment.Status.ShouldBe(PaymentStatus.Confirmed);
        payment.AnsweredAt.ShouldBe(Now.AddHours(1));
        Should.Throw<DomainException>(() => payment.Withdraw(OrderParty.Customer, Now)).Code.ShouldBe("payment.not_pending");
    }

    [Theory]
    [InlineData(0, 0, "payment.amount_invalid")]
    [InlineData(70_001, 30_000, "payment.exceeds_price")]
    public void Amounts_must_fit_the_price(int amount, int alreadyCounted, string code) =>
        Should.Throw<DomainException>(() => Record(WorkOrder(), amount: amount, alreadyCounted: alreadyCounted)).Code.ShouldBe(code);

    [Fact]
    public void Records_need_a_party_a_known_stage_a_past_date_and_an_open_order()
    {
        var order = WorkOrder();
        Should.Throw<DomainException>(() => Record(order, OrderParty.Staff)).Code.ShouldBe("payment.wrong_party");
        Should.Throw<DomainException>(() => Record(order, stageId: Guid.NewGuid())).Code.ShouldBe("payment.stage_invalid");
        Should.Throw<DomainException>(() =>
            Payment.Record(order, OrderParty.Partner, Guid.NewGuid(), null, 1, PaymentMethod.Card, Today.AddDays(3), null, 0, Now)).Code.ShouldBe("payment.date_future");
        Should.Throw<DomainException>(() =>
            Payment.Record(order, OrderParty.Partner, Guid.NewGuid(), null, 1, (PaymentMethod)99, Today, null, 0, Now)).Code.ShouldBe("payment.method_invalid");

        order.Cancel(OrderParty.Customer, "Changed my mind", Now);
        Should.Throw<DomainException>(() => Record(order)).Code.ShouldBe("payment.order_cancelled");
    }

    [Fact]
    public void A_dispute_needs_a_reason_and_staff_decide_it()
    {
        var payment = Record(WorkOrder(), OrderParty.Partner);
        Should.Throw<DomainException>(() => payment.Dispute(OrderParty.Customer, " ", Now)).Code.ShouldBe("payment.reason_required");
        Should.Throw<DomainException>(() => payment.Resolve(true, null, Now)).Code.ShouldBe("payment.not_disputed");

        payment.Dispute(OrderParty.Customer, " I paid less ", Now);
        payment.Status.ShouldBe(PaymentStatus.Disputed);
        payment.DisputeReason.ShouldBe("I paid less");
        payment.Counts.ShouldBeTrue();

        payment.Resolve(false, "No proof", Now.AddDays(1));
        payment.Status.ShouldBe(PaymentStatus.Rejected);
        payment.ResolutionNote.ShouldBe("No proof");
        payment.ResolvedAt.ShouldBe(Now.AddDays(1));
        payment.Counts.ShouldBeFalse();
    }

    [Fact]
    public void Only_the_recorder_withdraws_and_a_withdrawn_record_stops_counting()
    {
        var payment = Record(WorkOrder());
        Should.Throw<DomainException>(() => payment.Withdraw(OrderParty.Partner, Now)).Code.ShouldBe("payment.wrong_party");

        payment.Withdraw(OrderParty.Customer, Now);

        payment.Status.ShouldBe(PaymentStatus.Withdrawn);
        payment.Counts.ShouldBeFalse();
    }
}
