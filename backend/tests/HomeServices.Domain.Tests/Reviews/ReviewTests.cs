using HomeServices.Domain.Offers;
using HomeServices.Domain.Orders;
using HomeServices.Domain.Reviews;

namespace HomeServices.Domain.Tests.Reviews;

public class ReviewTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    private static Order GivenOrder(bool completed = true)
    {
        var order = Order.FromOffer(
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
        if (completed)
        {
            order.RequestCompletion(OrderParty.Partner, TimeSpan.FromDays(7), Now);
            order.ConfirmCompletion(OrderParty.Customer, Now);
        }

        return order;
    }

    [Fact]
    public void The_customer_reviews_a_completed_order()
    {
        var order = GivenOrder();

        var review = Review.Submit(order, OrderParty.Customer, 4, "  Good work  ", Now);

        review.OrderId.ShouldBe(order.Id);
        review.PartnerProfileId.ShouldBe(order.PartnerProfileId);
        review.CustomerId.ShouldBe(order.CustomerId);
        review.Rating.ShouldBe(4);
        review.Text.ShouldBe("Good work");
        review.SubmittedAt.ShouldBe(Now);
        review.IsHidden.ShouldBeFalse();
    }

    [Fact]
    public void Reviews_need_the_customer_a_completed_order_and_one_to_five_stars()
    {
        Should.Throw<DomainException>(() => Review.Submit(GivenOrder(completed: false), OrderParty.Customer, 5, null, Now)).Code.ShouldBe("review.order_not_completed");
        Should.Throw<DomainException>(() => Review.Submit(GivenOrder(), OrderParty.Partner, 5, null, Now)).Code.ShouldBe("review.wrong_party");
        Should.Throw<DomainException>(() => Review.Submit(GivenOrder(), OrderParty.Customer, 6, null, Now)).Code.ShouldBe("review.rating_invalid");
        Should.Throw<DomainException>(() => Review.Submit(GivenOrder(), OrderParty.Customer, 0, null, Now)).Code.ShouldBe("review.rating_invalid");
        Should.Throw<DomainException>(() => Review.Submit(GivenOrder(), OrderParty.Customer, 3, new string('a', Review.TextMaxLength + 1), Now)).Code.ShouldBe("review.text_too_long");
    }

    [Fact]
    public void The_partner_replies_once()
    {
        var review = Review.Submit(GivenOrder(), OrderParty.Customer, 2, null, Now);
        Should.Throw<DomainException>(() => review.AddReply(OrderParty.Customer, "Hi", Now)).Code.ShouldBe("review.wrong_party");
        Should.Throw<DomainException>(() => review.AddReply(OrderParty.Partner, " ", Now)).Code.ShouldBe("review.reply_required");

        review.AddReply(OrderParty.Partner, "Sorry, we'll fix it.", Now.AddHours(1));

        review.Reply.ShouldBe("Sorry, we'll fix it.");
        review.RepliedAt.ShouldBe(Now.AddHours(1));
        Should.Throw<DomainException>(() => review.AddReply(OrderParty.Partner, "Again", Now)).Code.ShouldBe("review.already_replied");
    }

    [Fact]
    public void Staff_hide_with_a_reason_and_restore()
    {
        var review = Review.Submit(GivenOrder(), OrderParty.Customer, 1, "Call me at 077…", Now);
        Should.Throw<DomainException>(() => review.Hide(null, Now)).Code.ShouldBe("review.reason_required");
        Should.Throw<DomainException>(() => review.Restore()).Code.ShouldBe("review.not_hidden");

        review.Hide("Contains a phone number", Now);
        review.IsHidden.ShouldBeTrue();
        review.HiddenAt.ShouldBe(Now);
        Should.Throw<DomainException>(() => review.Hide("Again", Now)).Code.ShouldBe("review.already_hidden");

        review.Restore();
        review.IsHidden.ShouldBeFalse();
        review.HiddenReason.ShouldBeNull();
    }
}
