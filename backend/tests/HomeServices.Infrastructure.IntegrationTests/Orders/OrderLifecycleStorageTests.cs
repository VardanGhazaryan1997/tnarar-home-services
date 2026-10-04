using HomeServices.Application.Offers;
using HomeServices.Application.Orders;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Offers;
using HomeServices.Domain.Orders;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Requests;
using HomeServices.Infrastructure.IntegrationTests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Infrastructure.IntegrationTests.Orders;

[Collection(PostgresCollection.Name)]
public class OrderLifecycleStorageTests(PostgresFixture db)
{
    private static LocalizedText Text(string hy) => LocalizedText.Empty.With("hy", hy);

    private static string UniqueSlug(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..30];

    private static User NewUser() => User.Register(PhoneNumber.Parse($"+37493{Random.Shared.Next(100_000, 999_999)}"));

    /// <summary>A work order between a new customer and a new approved partner with a unique name.</summary>
    private async Task<(Guid OrderId, FakeCurrentUser Customer, FakeCurrentUser Partner, string PartnerName)> GivenOrderAsync()
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
            return (order.Id, customerUser, partnerUser, name);
        }
    }

    [Fact]
    public async Task Changes_and_status_history_are_saved_and_read_back()
    {
        var (orderId, customer, partner, _) = await GivenOrderAsync();
        var language = new FakeCurrentLanguage();

        await using (var context = db.CreateContext())
        {
            await new StartOrderHandler(context, partner, language, db.Clock).HandleAsync(new StartOrder(orderId), CancellationToken.None);
        }

        OrderDto proposed;
        await using (var context = db.CreateContext())
        {
            proposed = await new ProposeOrderChangeHandler(context, partner, language, db.Clock).HandleAsync(
                new ProposeOrderChange(orderId, ChangeRequestKind.ExtraWork, "Floor tiles too", "The floor is cracked.", 80_000, null, null, null),
                CancellationToken.None);
        }

        await using (var context = db.CreateContext())
        {
            var accepted = await new AcceptOrderChangeHandler(context, customer, language, db.Clock)
                .HandleAsync(new AcceptOrderChange(orderId, proposed.ChangeRequests[0].Id), CancellationToken.None);
            accepted.Price.ShouldBe(280_000);
        }

        await using (var context = db.CreateContext())
        {
            var stored = await context.Orders.AsNoTracking()
                .Include(o => o.Stages).Include(o => o.StatusChanges).Include(o => o.ChangeRequests)
                .SingleAsync(o => o.Id == orderId);
            stored.Status.ShouldBe(OrderStatus.InProgress);
            stored.Price.ShouldBe(280_000);
            stored.StartedAt.ShouldBe(db.Clock.Now);
            stored.Stages.OrderBy(s => s.SortOrder).Select(s => (s.Purpose, s.Amount, s.SortOrder))
                .ShouldBe(new[] { (PaymentPurpose.Deposit, 50_000, 1), (PaymentPurpose.Stage, 80_000, 2), (PaymentPurpose.Final, 150_000, 3) });
            var change = stored.ChangeRequests.ShouldHaveSingleItem();
            change.Status.ShouldBe(ChangeRequestStatus.Accepted);
            change.ProposedBy.ShouldBe(OrderParty.Partner);
            change.Description.ShouldBe("The floor is cracked.");
            stored.StatusChanges.OrderBy(c => c.Sequence).Select(c => c.ChangedBy).ShouldBe(new OrderParty?[] { OrderParty.Customer, OrderParty.Partner });
        }

        await using (var context = db.CreateContext())
        {
            var cancelled = await new CancelOrderHandler(context, customer, language, db.Clock)
                .HandleAsync(new CancelOrder(orderId, "We are moving"), CancellationToken.None);
            cancelled.NeedsAttentionSince.ShouldBe(db.Clock.Now);
            cancelled.History[^1].ShouldBe(new OrderStatusChangeDto("Cancelled", db.Clock.Now, "Customer", "We are moving"));
        }
    }

    [Fact]
    public async Task Orders_the_customer_left_unanswered_complete_and_staff_find_orders()
    {
        var (orderId, _, partner, partnerName) = await GivenOrderAsync();
        var language = new FakeCurrentLanguage();
        var settings = Options.Create(new OrderSettings { AutoCompleteDays = 7 });

        await using (var context = db.CreateContext())
        {
            var done = await new RequestOrderCompletionHandler(context, partner, language, settings, db.Clock)
                .HandleAsync(new RequestOrderCompletion(orderId), CancellationToken.None);
            done.AutoCompleteAt.ShouldBe(db.Clock.Now.AddDays(7));
        }

        await using (var context = db.CreateContext())
        {
            var found = await new GetAdminOrdersHandler(context, language)
                .HandleAsync(new GetAdminOrders(Status: OrderStatus.CompletionRequested, Search: partnerName.ToUpperInvariant()), CancellationToken.None);
            var row = found.Items.ShouldHaveSingleItem();
            row.Id.ShouldBe(orderId);
            row.PendingChange.ShouldBeFalse();
            row.Place.CategoryName.ShouldBe("Սանտեխնիկա");
        }

        await using (var context = db.CreateContext())
        {
            (await new CompleteUnansweredOrdersHandler(context, new FixedTimeProvider(db.Clock.Now.AddDays(6))).HandleAsync(new CompleteUnansweredOrders(), CancellationToken.None))
                .ShouldBe(0);
        }

        await using (var context = db.CreateContext())
        {
            (await new CompleteUnansweredOrdersHandler(context, new FixedTimeProvider(db.Clock.Now.AddDays(8))).HandleAsync(new CompleteUnansweredOrders(), CancellationToken.None))
                .ShouldBeGreaterThanOrEqualTo(1);
        }

        await using (var context = db.CreateContext())
        {
            var stored = await context.Orders.AsNoTracking().SingleAsync(o => o.Id == orderId);
            stored.Status.ShouldBe(OrderStatus.Completed);
            stored.AutoCompleteAt.ShouldBeNull();
        }
    }
}
