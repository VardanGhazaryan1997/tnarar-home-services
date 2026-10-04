using HomeServices.Application.Files;
using HomeServices.Application.Requests;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Requests;
using HomeServices.Infrastructure.IntegrationTests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Infrastructure.IntegrationTests.Requests;

[Collection(PostgresCollection.Name)]
public class RequestStorageTests(PostgresFixture db)
{
    private static LocalizedText Text(string hy) => LocalizedText.Empty.With("hy", hy);

    private static string UniqueSlug(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..30];

    private static User NewUser() => User.Register(PhoneNumber.Parse($"+37493{Random.Shared.Next(100_000, 999_999)}"));

    private FileDtoFactory Files => new(new FakeFileStorage(), Options.Create(new FileSettings()), db.Clock);

    /// <summary>A fresh category and city, a customer, and an approved partner offering the category in the whole city.</summary>
    private async Task<(Category Category, City City, User Customer, PartnerProfile Partner, StoredFile Photo)> GivenMarketAsync()
    {
        var category = Category.Create(UniqueSlug("cat"), Text("Սանտեխնիկա"), 1);
        var city = City.Create(UniqueSlug("city"), Text("Երևան"), 1);
        city.AddDistrict("center", Text("Կենտրոն"), 1);
        var customer = NewUser();
        customer.UpdateProfile("Ani Petrosyan", null);
        var partnerOwner = NewUser();
        var photo = StoredFile.Begin(FileOwnerType.User, customer.Id.ToString(), "leak.jpg", "image/jpeg", 100, db.Clock.Now);
        photo.MarkReady(100, db.Clock.Now);
        var work = StoredFile.Begin(FileOwnerType.User, partnerOwner.Id.ToString(), "work.jpg", "image/jpeg", 100, db.Clock.Now);
        work.MarkReady(100, db.Clock.Now);

        var partner = PartnerProfile.Create(partnerOwner.Id, PartnerType.Specialist, "Aram Plumbing");
        partner.UpdateDetails(PartnerType.Specialist, "Aram Plumbing", new string('a', 60), null, null);
        partner.SetServices([category.Id]);
        partner.SetAreas([(city.Id, null)]);
        partner.AddMedia(PartnerMediaKind.WorkExample, work.Id, null);
        partner.Submit(db.Clock.Now);
        partner.Approve(db.Clock.Now);

        await using var context = db.CreateContext();
        context.AddRange(category, city, customer, partnerOwner, photo, work, partner);
        await context.SaveChangesAsync();
        return (category, city, customer, partner, photo);
    }

    [Fact]
    public async Task A_request_is_saved_with_its_recipients_and_media()
    {
        var (category, city, customer, partner, photo) = await GivenMarketAsync();
        var district = city.Districts.Single();
        var request = ServiceRequest.Create(customer.Id, RequestKind.Direct, category.Id, city.Id, district.Id, "The kitchen tap is leaking badly.", new DateOnly(2026, 10, 9), "evenings", 5_000, 20_000);
        request.AddMedia(photo.Id);
        request.SendTo([partner.Id], RecipientSource.Direct, db.Clock.Now);
        await using (var context = db.CreateContext())
        {
            context.ServiceRequests.Add(request);
            await context.SaveChangesAsync();
        }

        await using (var context = db.CreateContext())
        {
            var loaded = await context.ServiceRequests.Include(r => r.Recipients).Include(r => r.Media).SingleAsync(r => r.Id == request.Id);
            loaded.Decline(partner.Id, "Too far", db.Clock.Now.AddHours(1));
            await context.SaveChangesAsync();
        }

        await using (var context = db.CreateContext())
        {
            var saved = await context.ServiceRequests.Include(r => r.Recipients).Include(r => r.Media).SingleAsync(r => r.Id == request.Id);
            saved.Kind.ShouldBe(RequestKind.Direct);
            saved.DistrictId.ShouldBe(district.Id);
            saved.PreferredDate.ShouldBe(new DateOnly(2026, 10, 9));
            saved.BudgetMax.ShouldBe(20_000);
            saved.CreatedAt.ShouldBe(db.Clock.Now);
            saved.AttentionReason.ShouldBe(AttentionReason.DirectPartnerDeclined);
            saved.Media.ShouldHaveSingleItem().FileId.ShouldBe(photo.Id);
            var recipient = saved.Recipients.ShouldHaveSingleItem();
            recipient.Status.ShouldBe(RecipientStatus.Declined);
            recipient.DeclineReason.ShouldBe("Too far");
        }
    }

    [Fact]
    public async Task Creating_matching_listing_and_following_up_run_on_PostgreSQL()
    {
        var (category, city, customer, partner, photo) = await GivenMarketAsync();
        var district = city.Districts.Single();
        var customerUser = new FakeCurrentUser { UserId = customer.Id.ToString() };
        var partnerUser = new FakeCurrentUser { UserId = partner.UserId.ToString() };
        var settings = Options.Create(new RequestSettings());

        Guid requestId;
        await using (var context = db.CreateContext())
        {
            var command = new CreateRequest(RequestKind.Open, null, category.Id, city.Id, district.Id, "Bathroom pipes need replacing.", null, null, null, null, [photo.Id]);
            (await new CreateRequestValidator(context, customerUser, db.Clock).ValidateAsync(command)).IsValid.ShouldBeTrue();
            var created = await new CreateRequestHandler(context, customerUser, new FakeCurrentLanguage(), Files, settings, db.Clock)
                .HandleAsync(command, CancellationToken.None);
            created.SentTo.ShouldBe(1);
            created.Media.ShouldHaveSingleItem();
            created.Place.DistrictName.ShouldBe("Կենտրոն");
            requestId = created.Id;
        }

        await using (var context = db.CreateContext())
        {
            var inbox = await new GetInboxHandler(context, partnerUser, new FakeCurrentLanguage()).HandleAsync(new GetInbox(RecipientStatus.New), CancellationToken.None);
            inbox.Items.ShouldHaveSingleItem().MediaCount.ShouldBe(1);
            var opened = await new GetInboxRequestHandler(context, partnerUser, new FakeCurrentLanguage(), Files, db.Clock)
                .HandleAsync(new GetInboxRequest(requestId), CancellationToken.None);
            opened.CustomerFirstName.ShouldBe("Ani");

            var mine = await new GetMyRequestsHandler(context, customerUser, new FakeCurrentLanguage()).HandleAsync(new GetMyRequests(RequestStatus.Open), CancellationToken.None);
            mine.Items.ShouldHaveSingleItem().SentTo.ShouldBe(1);

            var admin = await new GetAdminRequestsHandler(context, new FakeCurrentLanguage())
                .HandleAsync(new GetAdminRequests(CategoryId: category.Id, Search: "PIPES"), CancellationToken.None);
            admin.Items.ShouldHaveSingleItem().CustomerName.ShouldBe("Ani Petrosyan");
            var byPhone = await new GetAdminRequestsHandler(context, new FakeCurrentLanguage())
                .HandleAsync(new GetAdminRequests(CityId: city.Id, Search: customer.Phone.Value), CancellationToken.None);
            byPhone.Items.ShouldHaveSingleItem().Id.ShouldBe(requestId);
        }

        db.Clock.Now = db.Clock.Now.AddHours(25);
        try
        {
            await using (var context = db.CreateContext())
            {
                (await new FlagUnansweredRequestsHandler(context, settings, db.Clock).HandleAsync(new FlagUnansweredRequests(), CancellationToken.None))
                    .ShouldBeGreaterThanOrEqualTo(1);
            }

            await using (var context = db.CreateContext())
            {
                var queue = await new GetAdminRequestsHandler(context, new FakeCurrentLanguage())
                    .HandleAsync(new GetAdminRequests(NeedsAttention: true, CategoryId: category.Id), CancellationToken.None);
                queue.Items.ShouldHaveSingleItem().AttentionReason.ShouldBe("NoResponse");
            }
        }
        finally
        {
            db.Clock.Now = db.Clock.Now.AddHours(-25);
        }
    }
}
