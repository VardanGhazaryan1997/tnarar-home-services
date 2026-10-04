using HomeServices.Application.Files;
using HomeServices.Application.Identity;
using HomeServices.Application.Notifications;
using HomeServices.Application.Offers;
using HomeServices.Application.Orders;
using HomeServices.Application.Partners;
using HomeServices.Application.Payments;
using HomeServices.Application.Reviews;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Notifications;
using HomeServices.Domain.Offers;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Payments;
using HomeServices.Domain.Requests;
using HomeServices.Infrastructure.IntegrationTests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Infrastructure.IntegrationTests.Payments;

[Collection(PostgresCollection.Name)]
public class PaymentReviewNotificationStorageTests(PostgresFixture db)
{
    private sealed class RecordingSms : ISmsSender
    {
        public List<(PhoneNumber To, string Message)> Sent { get; } = [];

        public Task SendAsync(PhoneNumber to, string message, CancellationToken cancellationToken)
        {
            Sent.Add((to, message));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingPusher : INotificationPusher
    {
        public List<Guid> Pushed { get; } = [];

        public Task PushAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken)
        {
            Pushed.Add(notification.Id);
            return Task.CompletedTask;
        }
    }

    private static LocalizedText Text(string hy) => LocalizedText.Empty.With("hy", hy);

    private static string UniqueSlug(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..30];

    private static User NewUser() => User.Register(PhoneNumber.Parse($"+37493{Random.Shared.Next(100_000, 999_999)}"));

    /// <summary>A work order between a new customer and a new approved partner with a unique name.</summary>
    private async Task<(Guid OrderId, FakeCurrentUser Customer, FakeCurrentUser Partner, string PartnerName, string Slug)> GivenOrderAsync()
    {
        var category = Category.Create(UniqueSlug("cat"), Text("Սանտեխնիկա"), 1);
        var city = City.Create(UniqueSlug("city"), Text("Երևան"), 1);
        var customer = NewUser();
        var owner = NewUser();
        var work = StoredFile.Begin(FileOwnerType.User, owner.Id.ToString(), "work.jpg", "image/jpeg", 100, db.Clock.Now);
        work.MarkReady(100, db.Clock.Now);
        var name = $"Builder {Guid.NewGuid():N}"[..20];
        var partner = PartnerProfile.Create(owner.Id, PartnerType.Company, name);
        partner.UpdateDetails(PartnerType.Company, name, new string('a', 60), null, null);
        partner.SetServices([category.Id]);
        partner.SetAreas([(city.Id, null)]);
        partner.AddMedia(PartnerMediaKind.WorkExample, work.Id, null);
        partner.Submit(db.Clock.Now);
        partner.Approve(db.Clock.Now);
        var request = ServiceRequest.Create(customer.Id, RequestKind.Open, category.Id, city.Id, null, "The bathroom needs new tiles.", null, null, null, null);
        request.SendTo([partner.Id], RecipientSource.Matched, db.Clock.Now);
        await using (var context = db.CreateContext())
        {
            context.AddRange(category, city, customer, owner, work, partner, request);
            await context.SaveChangesAsync();
        }

        var customerUser = new FakeCurrentUser { UserId = customer.Id.ToString() };
        var partnerUser = new FakeCurrentUser { UserId = owner.Id.ToString() };
        OfferDto offer;
        await using (var context = db.CreateContext())
        {
            offer = await new SendOfferHandler(context, partnerUser, db.Clock).HandleAsync(
                new SendOffer(
                    request.Id,
                    OfferKind.Work,
                    "New tiles on the bathroom walls.",
                    [new OfferLine("Remove old tiles", true)],
                    200_000,
                    false,
                    null,
                    new DateOnly(2026, 10, 10),
                    5,
                    null,
                    [new OfferStageTerms("Deposit", PaymentPurpose.Deposit, 50_000), new OfferStageTerms(null, PaymentPurpose.Final, 150_000)],
                    null),
                CancellationToken.None);
        }

        await using (var context = db.CreateContext())
        {
            var order = await new AcceptOfferHandler(context, customerUser, new FakeCurrentLanguage(), db.Clock).HandleAsync(new AcceptOffer(offer.Id), CancellationToken.None);
            return (order.Id, customerUser, partnerUser, name, partner.Slug!);
        }
    }

    [Fact]
    public async Task Payments_are_saved_answered_and_listed_for_staff()
    {
        var (orderId, customer, partner, partnerName, _) = await GivenOrderAsync();
        var language = new FakeCurrentLanguage();

        OrderDto recorded;
        await using (var context = db.CreateContext())
        {
            recorded = await new RecordPaymentHandler(context, customer, language, db.Clock).HandleAsync(
                new RecordPayment(orderId, 50_000, PaymentMethod.BankTransfer, new DateOnly(2026, 10, 5), null, "Deposit"), CancellationToken.None);
        }

        await using (var context = db.CreateContext())
        {
            var disputed = await new DisputePaymentHandler(context, partner, language, db.Clock)
                .HandleAsync(new DisputePayment(recorded.Payments[0].Id, "Not received yet"), CancellationToken.None);
            disputed.Payments.ShouldHaveSingleItem().Status.ShouldBe("Disputed");
        }

        await using (var context = db.CreateContext())
        {
            var queue = await new GetAdminPaymentsHandler(context).HandleAsync(new GetAdminPayments(PaymentStatus.Disputed, orderId), CancellationToken.None);
            var row = queue.Items.ShouldHaveSingleItem();
            row.PartnerName.ShouldBe(partnerName);
            row.Method.ShouldBe("BankTransfer");
            row.OrderPrice.ShouldBe(200_000);

            var resolved = await new ResolvePaymentHandler(context, db.Clock).HandleAsync(new ResolvePayment(row.Id, true, "Bank statement seen"), CancellationToken.None);
            resolved.Status.ShouldBe("Confirmed");
        }

        await using (var context = db.CreateContext())
        {
            (await new GetOrderHandler(context, customer, language).HandleAsync(new GetOrder(orderId), CancellationToken.None)).PaidAmount.ShouldBe(50_000);
        }
    }

    [Fact]
    public async Task Reviews_feed_the_public_rating_and_notifications_are_delivered_once()
    {
        var (orderId, customer, partner, _, slug) = await GivenOrderAsync();
        var language = new FakeCurrentLanguage();
        var settings = Options.Create(new OrderSettings());

        await using (var context = db.CreateContext())
        {
            await new RequestOrderCompletionHandler(context, partner, language, settings, db.Clock).HandleAsync(new RequestOrderCompletion(orderId), CancellationToken.None);
        }

        await using (var context = db.CreateContext())
        {
            await new ConfirmOrderCompletionHandler(context, customer, language, db.Clock).HandleAsync(new ConfirmOrderCompletion(orderId), CancellationToken.None);
        }

        await using (var context = db.CreateContext())
        {
            await new SubmitReviewHandler(context, customer, language, db.Clock).HandleAsync(new SubmitReview(orderId, 4, "Neat work"), CancellationToken.None);
        }

        await using (var context = db.CreateContext())
        {
            var files = new FileDtoFactory(new FakeFileStorage(), Options.Create(new FileSettings()), db.Clock);
            var profile = await new GetPublicPartnerHandler(context, language, files).HandleAsync(new GetPublicPartner(slug), CancellationToken.None);
            profile.Rating.ShouldBe(4.0);
            profile.ReviewCount.ShouldBe(1);
            (await new GetPartnerReviewsHandler(context).HandleAsync(new GetPartnerReviews(slug), CancellationToken.None)).Items.ShouldHaveSingleItem().Text.ShouldBe("Neat work");
        }

        var partnerId = Guid.Parse(partner.UserId!);
        await using (var context = db.CreateContext())
        {
            var mine = await context.Notifications.AsNoTracking().Where(n => n.UserId == partnerId).OrderBy(n => n.CreatedAt).ToListAsync();
            mine.Select(n => n.Type).ShouldContain(NotificationType.OfferAccepted);
            mine.Select(n => n.Type).ShouldContain(NotificationType.ReviewReceived);
            var list = await new GetMyNotificationsHandler(context, partner).HandleAsync(new GetMyNotifications(), CancellationToken.None);
            list.Items.First(n => n.Type == "ReviewReceived").Params["rating"].ShouldBe("4");
        }

        var sms = new RecordingSms();
        var pusher = new RecordingPusher();
        await using (var context = db.CreateContext())
        {
            var deliver = new DeliverNotificationsHandler(context, sms, pusher, Options.Create(new NotificationSettings { BatchSize = 1000 }), db.Clock);
            await deliver.HandleAsync(new DeliverNotifications(), CancellationToken.None);
            (await deliver.HandleAsync(new DeliverNotifications(), CancellationToken.None)).ShouldBe(0);
        }

        await using (var context = db.CreateContext())
        {
            (await context.Notifications.AnyAsync(n => n.UserId == partnerId && n.DeliveredAt == null)).ShouldBeFalse();
            var accepted = await context.Notifications.SingleAsync(n => n.UserId == partnerId && n.Type == NotificationType.OfferAccepted);
            pusher.Pushed.ShouldContain(accepted.Id);
            sms.Sent.ShouldContain(m => m.Message.Contains($"/hy/orders/{orderId}", StringComparison.Ordinal));
        }
    }
}
