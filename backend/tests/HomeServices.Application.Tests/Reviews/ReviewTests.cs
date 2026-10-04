using HomeServices.Application.Abstractions;
using HomeServices.Application.Orders;
using HomeServices.Application.Partners;
using HomeServices.Application.Reviews;
using HomeServices.Application.Tests.Offers;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Notifications;
using HomeServices.Domain.Partners;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Reviews;

public class ReviewTests
{
    private readonly OfferTestData _data = new();

    private IAppDbContext Db => _data.Requests.Db;

    private FakeClock Clock => _data.Requests.Clock;

    private ICurrentLanguage Language => _data.Requests.Language;

    private async Task<(OrderDto Order, PartnerProfile Partner)> GivenOrderAsync(bool completed = true)
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var offer = await _data.SendAsync(aram, _data.Work(request.Id));
        var order = await _data.AcceptAsync(offer.Id);
        if (completed)
        {
            await new RequestOrderCompletionHandler(Db, OfferTestData.As(aram), Language, Options.Create(new OrderSettings()), Clock)
                .HandleAsync(new RequestOrderCompletion(order.Id), CancellationToken.None);
            order = await new ConfirmOrderCompletionHandler(Db, _data.Customer, Language, Clock).HandleAsync(new ConfirmOrderCompletion(order.Id), CancellationToken.None);
        }

        return (order, aram);
    }

    private Task<OrderDto> ReviewAsync(ICurrentUser user, Guid orderId, int rating = 5, string? text = "Quick and tidy work") =>
        new SubmitReviewHandler(Db, user, Language, Clock).HandleAsync(new SubmitReview(orderId, rating, text), CancellationToken.None);

    private Task<OrderDto> ReplyAsync(ICurrentUser user, Guid orderId, string text = "Thank you!") =>
        new ReplyToReviewHandler(Db, user, Language, Clock).HandleAsync(new ReplyToReview(orderId, text), CancellationToken.None);

    private Task<PublicPartnerDto> ProfileAsync(PartnerProfile partner) =>
        new GetPublicPartnerHandler(Db, Language, _data.Requests.Files).HandleAsync(new GetPublicPartner(partner.Slug!), CancellationToken.None);

    private async Task<IReadOnlyList<PublicReviewDto>> PublicReviewsAsync(PartnerProfile partner) =>
        (await new GetPartnerReviewsHandler(Db).HandleAsync(new GetPartnerReviews(partner.Slug!), CancellationToken.None)).Items;

    [Fact]
    public async Task The_customer_reviews_a_completed_order_once_and_the_partner_replies_once()
    {
        var (open, _) = await GivenOrderAsync(completed: false);
        (await Should.ThrowAsync<DomainException>(() => ReviewAsync(_data.Customer, open.Id))).Code.ShouldBe("review.order_not_completed");

        var (order, aram) = await GivenOrderAsync();
        var partner = OfferTestData.As(aram);
        (await Should.ThrowAsync<DomainException>(() => ReviewAsync(partner, order.Id))).Code.ShouldBe("review.wrong_party");

        var reviewed = await ReviewAsync(_data.Customer, order.Id);
        reviewed.Review.ShouldNotBeNull().Rating.ShouldBe(5);
        reviewed.Review.Text.ShouldBe("Quick and tidy work");
        reviewed.Actions.ShouldNotContain(OrderActions.Review);
        Db.Notifications.Single(n => n.Type == NotificationType.ReviewReceived).UserId.ShouldBe(aram.UserId);
        (await Should.ThrowAsync<DomainException>(() => ReviewAsync(_data.Customer, order.Id))).Code.ShouldBe("review.already_submitted");

        (await _data.OrderAsync(order.Id, partner)).Actions.ShouldContain(OrderActions.ReplyReview);
        var replied = await ReplyAsync(partner, order.Id);
        replied.Review!.Reply.ShouldBe("Thank you!");
        replied.Actions.ShouldNotContain(OrderActions.ReplyReview);
        Db.Notifications.Single(n => n.Type == NotificationType.ReviewReplied).UserId.ShouldBe(_data.Requests.Partners.User.Id);
        (await Should.ThrowAsync<DomainException>(() => ReplyAsync(partner, order.Id))).Code.ShouldBe("review.already_replied");
    }

    [Fact]
    public async Task Reviews_show_on_the_public_profile_until_staff_hide_them()
    {
        var customer = _data.Requests.Partners.User;
        customer.UpdateProfile("Anna Petrosyan", null);
        await Db.SaveChangesAsync();
        var (order, aram) = await GivenOrderAsync();
        (await ProfileAsync(aram)).ReviewCount.ShouldBe(0);

        await ReviewAsync(_data.Customer, order.Id, rating: 4);

        var profile = await ProfileAsync(aram);
        profile.Rating.ShouldBe(4.0);
        profile.ReviewCount.ShouldBe(1);
        var shown = (await PublicReviewsAsync(aram)).ShouldHaveSingleItem();
        shown.CustomerName.ShouldBe("Anna");
        shown.Rating.ShouldBe(4);

        var reviewId = shown.Id;
        var hidden = await new HideReviewHandler(Db, Clock).HandleAsync(new HideReview(reviewId, "Contains a phone number"), CancellationToken.None);
        hidden.IsHidden.ShouldBeTrue();
        hidden.HiddenReason.ShouldBe("Contains a phone number");
        (await PublicReviewsAsync(aram)).ShouldBeEmpty();
        (await ProfileAsync(aram)).Rating.ShouldBeNull();
        (await new GetAdminReviewsHandler(Db).HandleAsync(new GetAdminReviews(Hidden: true), CancellationToken.None)).Items.ShouldHaveSingleItem().PartnerName.ShouldBe("Aram");
        (await Should.ThrowAsync<DomainException>(() =>
            new HideReviewHandler(Db, Clock).HandleAsync(new HideReview(reviewId, "Again"), CancellationToken.None))).Code.ShouldBe("review.already_hidden");

        (await new RestoreReviewHandler(Db).HandleAsync(new RestoreReview(reviewId), CancellationToken.None)).IsHidden.ShouldBeFalse();
        (await PublicReviewsAsync(aram)).ShouldHaveSingleItem();
    }
}
