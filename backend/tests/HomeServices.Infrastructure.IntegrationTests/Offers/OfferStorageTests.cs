using HomeServices.Application.Offers;
using HomeServices.Application.Orders;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Offers;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Requests;
using HomeServices.Infrastructure.IntegrationTests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Infrastructure.IntegrationTests.Offers;

[Collection(PostgresCollection.Name)]
public class OfferStorageTests(PostgresFixture db)
{
    private static LocalizedText Text(string hy) => LocalizedText.Empty.With("hy", hy);

    private static string UniqueSlug(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..30];

    private static User NewUser() => User.Register(PhoneNumber.Parse($"+37491{Random.Shared.Next(100_000, 999_999)}"));

    /// <summary>A customer's open request received by two approved partners.</summary>
    private async Task<(ServiceRequest Request, User Customer, PartnerProfile Aram, PartnerProfile Gor)> GivenRequestAsync()
    {
        var category = Category.Create(UniqueSlug("cat"), Text("Սանտեխնիկա"), 1);
        var city = City.Create(UniqueSlug("city"), Text("Երևան"), 1);
        var customer = NewUser();
        customer.UpdateProfile("Ani Petrosyan", null);

        PartnerProfile Partner(User owner, StoredFile work, string name)
        {
            var partner = PartnerProfile.Create(owner.Id, PartnerType.Specialist, name);
            partner.UpdateDetails(PartnerType.Specialist, name, new string('a', 60), null, null);
            partner.SetServices([category.Id]);
            partner.SetAreas([(city.Id, null)]);
            partner.AddMedia(PartnerMediaKind.WorkExample, work.Id, null);
            partner.Submit(db.Clock.Now);
            partner.Approve(db.Clock.Now);
            return partner;
        }

        StoredFile Work(User owner)
        {
            var file = StoredFile.Begin(FileOwnerType.User, owner.Id.ToString(), "work.jpg", "image/jpeg", 100, db.Clock.Now);
            file.MarkReady(100, db.Clock.Now);
            return file;
        }

        var aramOwner = NewUser();
        var gorOwner = NewUser();
        var aramWork = Work(aramOwner);
        var gorWork = Work(gorOwner);
        var aram = Partner(aramOwner, aramWork, "Aram");
        var gor = Partner(gorOwner, gorWork, "Gor");
        var request = ServiceRequest.Create(customer.Id, RequestKind.Open, category.Id, city.Id, null, "The kitchen tap is leaking badly.", null, null, null, null);
        request.SendTo([aram.Id, gor.Id], RecipientSource.Matched, db.Clock.Now);

        await using var context = db.CreateContext();
        context.AddRange(category, city, customer, aramOwner, gorOwner, aramWork, gorWork, aram, gor, request);
        await context.SaveChangesAsync();
        return (request, customer, aram, gor);
    }

    private static SendOffer WorkOffer(Guid requestId, int price) => new(
        requestId,
        OfferKind.Work,
        "Replace the kitchen tap and the pipes.",
        [new OfferLine("Remove the old tap", true), new OfferLine("Tiling", false)],
        price,
        true,
        null,
        null,
        3,
        null,
        [new OfferStageTerms("Deposit", PaymentPurpose.Deposit, price / 2), new OfferStageTerms(null, PaymentPurpose.Final, price - (price / 2))],
        null);

    [Fact]
    public async Task Offers_and_the_accepted_order_are_saved_with_frozen_terms()
    {
        var (request, customer, aram, gor) = await GivenRequestAsync();
        var customerUser = new FakeCurrentUser { UserId = customer.Id.ToString() };

        OfferDto chosen;
        await using (var context = db.CreateContext())
        {
            chosen = await new SendOfferHandler(context, new FakeCurrentUser { UserId = aram.UserId.ToString() }, db.Clock)
                .HandleAsync(WorkOffer(request.Id, 100_000), CancellationToken.None);
        }

        await using (var context = db.CreateContext())
        {
            await new SendOfferHandler(context, new FakeCurrentUser { UserId = gor.UserId.ToString() }, db.Clock)
                .HandleAsync(WorkOffer(request.Id, 80_000), CancellationToken.None);
        }

        await using (var context = db.CreateContext())
        {
            var offers = await new GetRequestOffersHandler(context, customerUser, db.Clock).HandleAsync(new GetRequestOffers(request.Id), CancellationToken.None);
            offers.Select(o => o.Price).ShouldBe(new[] { 80_000, 100_000 });
            offers[1].Lines.Count.ShouldBe(2);
            offers[1].Stages.Count.ShouldBe(2);
            offers[1].SentAt.ShouldBe(db.Clock.Now);
        }

        OrderDto order;
        await using (var context = db.CreateContext())
        {
            order = await new AcceptOfferHandler(context, customerUser, new FakeCurrentLanguage(), db.Clock)
                .HandleAsync(new AcceptOffer(chosen.Id), CancellationToken.None);
        }

        await using (var context = db.CreateContext())
        {
            var stored = await context.Orders.Include(o => o.Stages).Include(o => o.StatusChanges).SingleAsync(o => o.Id == order.Id);
            stored.Stages.Count.ShouldBe(2);
            stored.StatusChanges.ShouldHaveSingleItem();
            stored.CreatedAt.ShouldBe(db.Clock.Now);
            (await context.ServiceRequests.SingleAsync(r => r.Id == request.Id)).Status.ShouldBe(RequestStatus.Closed);
            (await context.Offers.Where(o => o.RequestId == request.Id).Select(o => o.Status).ToListAsync())
                .ShouldBe(new[] { OfferStatus.Accepted, OfferStatus.Closed }, ignoreOrder: true);

            var asPartner = await new GetOrderHandler(context, new FakeCurrentUser { UserId = aram.UserId.ToString() }, new FakeCurrentLanguage())
                .HandleAsync(new GetOrder(order.Id), CancellationToken.None);
            asPartner.Terms.Summary.ShouldBe("Replace the kitchen tap and the pipes.");
            asPartner.Terms.Lines.Select(l => l.Title).ShouldBe(new[] { "Remove the old tap", "Tiling" });
            asPartner.Terms.DurationDays.ShouldBe(3);
            asPartner.Customer.FullName.ShouldBe("Ani Petrosyan");

            var mine = await new GetMyOrdersHandler(context, customerUser, new FakeCurrentLanguage()).HandleAsync(new GetMyOrders(), CancellationToken.None);
            mine.Items.ShouldHaveSingleItem().OtherParty.ShouldBe("Aram");
        }
    }

    [Fact]
    public async Task Two_decisions_on_the_same_offer_at_once_conflict()
    {
        var (request, _, aram, _) = await GivenRequestAsync();
        var offer = Offer.Create(request.Id, aram.Id, OfferKind.Work, new OfferTerms("Replace the kitchen tap.", [], 50_000, false, null, null, null, null, []), db.Clock.Now.AddDays(7), db.Clock.Now);
        await using (var context = db.CreateContext())
        {
            context.Offers.Add(offer);
            await context.SaveChangesAsync();
        }

        await using var first = db.CreateContext();
        await using var second = db.CreateContext();
        var mine = await first.Offers.SingleAsync(o => o.Id == offer.Id);
        var theirs = await second.Offers.SingleAsync(o => o.Id == offer.Id);
        mine.Reject(null, db.Clock.Now);
        theirs.Withdraw(db.Clock.Now);

        await first.SaveChangesAsync();
        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }
}
