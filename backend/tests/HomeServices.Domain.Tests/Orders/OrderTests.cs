using HomeServices.Domain.Offers;
using HomeServices.Domain.Orders;

namespace HomeServices.Domain.Tests.Orders;

public class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Customer = Guid.NewGuid();

    private static Offer WorkOffer() => Offer.Create(
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
        Now);

    private static Offer VisitOffer() => Offer.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        OfferKind.Visit,
        new OfferTerms("I'll come and measure the bathroom.", [], 0, false, null, null, null, Now.AddDays(1), []),
        Now.AddDays(7),
        Now);

    [Fact]
    public void An_order_copies_the_accepted_offer_and_starts_confirmed()
    {
        var offer = WorkOffer();
        var parent = Guid.NewGuid();

        var order = Order.FromOffer(offer, Customer, parent, "{\"summary\":\"x\"}", Now);

        order.Kind.ShouldBe(OrderKind.Work);
        order.Status.ShouldBe(OrderStatus.Confirmed);
        order.RequestId.ShouldBe(offer.RequestId);
        order.OfferId.ShouldBe(offer.Id);
        order.PartnerProfileId.ShouldBe(offer.PartnerProfileId);
        order.CustomerId.ShouldBe(Customer);
        order.ParentOrderId.ShouldBe(parent);
        order.Price.ShouldBe(100_000);
        order.StartDate.ShouldBe(new DateOnly(2026, 10, 8));
        order.DurationDays.ShouldBe(2);
        order.Terms.ShouldBe("{\"summary\":\"x\"}");
        order.Stages.Select(s => (s.Title, s.Purpose, s.Amount, s.SortOrder))
            .ShouldBe(new[] { ("Deposit", PaymentPurpose.Deposit, 30_000, 1), ((string?)null, PaymentPurpose.Final, 70_000, 2) });
        var change = order.StatusChanges.ShouldHaveSingleItem();
        change.Status.ShouldBe(OrderStatus.Confirmed);
        change.ChangedAt.ShouldBe(Now);
        change.Sequence.ShouldBe(1);
        change.OrderId.ShouldBe(order.Id);
    }

    [Fact]
    public void A_visit_order_has_the_visit_time_and_no_parent()
    {
        var order = Order.FromOffer(VisitOffer(), Customer, null, "{}", Now);

        order.Kind.ShouldBe(OrderKind.Visit);
        order.VisitAt.ShouldBe(Now.AddDays(1));
        order.Price.ShouldBe(0);
        order.Stages.ShouldBeEmpty();
        Should.Throw<DomainException>(() => Order.FromOffer(VisitOffer(), Customer, Guid.NewGuid(), "{}", Now)).Code.ShouldBe("order.parent_invalid");
    }

    [Fact]
    public void An_order_needs_a_customer_and_terms()
    {
        Should.Throw<DomainException>(() => Order.FromOffer(WorkOffer(), Guid.Empty, null, "{}", Now)).Code.ShouldBe("order.customer_required");
        Should.Throw<DomainException>(() => Order.FromOffer(WorkOffer(), Customer, null, " ", Now)).Code.ShouldBe("order.terms_required");
    }
}
