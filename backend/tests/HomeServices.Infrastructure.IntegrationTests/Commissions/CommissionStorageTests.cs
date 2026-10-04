using HomeServices.Application.Commissions;
using HomeServices.Application.Offers;
using HomeServices.Application.Orders;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Commissions;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Offers;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Requests;
using HomeServices.Infrastructure.IntegrationTests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Infrastructure.IntegrationTests.Commissions;

[Collection(PostgresCollection.Name)]
public class CommissionStorageTests(PostgresFixture db)
{
    private static LocalizedText Text(string hy) => LocalizedText.Empty.With("hy", hy);

    private static string UniqueSlug(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..30];

    private static User NewUser() => User.Register(PhoneNumber.Parse($"+37491{Random.Shared.Next(100_000, 999_999)}"));

    [Fact]
    public async Task The_default_rate_is_seeded()
    {
        await using var context = db.CreateContext();
        var standard = await context.CommissionRates.AsNoTracking().SingleAsync(r => r.CategoryId == null);
        standard.Id.ShouldBe(CommissionRate.DefaultId);
        standard.Percent.ShouldBe(10m);
    }

    [Fact]
    public async Task A_completed_order_is_charged_billed_on_a_statement_and_settled()
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

        var language = new FakeCurrentLanguage();
        var customerUser = new FakeCurrentUser { UserId = customer.Id.ToString() };
        var partnerUser = new FakeCurrentUser { UserId = owner.Id.ToString() };
        await using (var context = db.CreateContext())
        {
            var rates = await new SetCategoryCommissionRateHandler(context, language).HandleAsync(new SetCategoryCommissionRate(category.Id, 12.5m), CancellationToken.None);
            rates.Categories.Single(c => c.CategoryId == category.Id).Percent.ShouldBe(12.5m);
        }

        Guid orderId;
        await using (var context = db.CreateContext())
        {
            var offer = await new SendOfferHandler(context, partnerUser, db.Clock).HandleAsync(
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
            orderId = (await new AcceptOfferHandler(context, customerUser, language, db.Clock).HandleAsync(new AcceptOffer(offer.Id), CancellationToken.None)).Id;
        }

        await using (var context = db.CreateContext())
        {
            await new RequestOrderCompletionHandler(context, partnerUser, language, Options.Create(new OrderSettings()), db.Clock)
                .HandleAsync(new RequestOrderCompletion(orderId), CancellationToken.None);
        }

        await using (var context = db.CreateContext())
        {
            await new ConfirmOrderCompletionHandler(context, customerUser, language, db.Clock).HandleAsync(new ConfirmOrderCompletion(orderId), CancellationToken.None);
        }

        await using (var context = db.CreateContext())
        {
            var owed = await context.CommissionObligations.AsNoTracking().SingleAsync(o => o.OrderId == orderId);
            owed.RatePercent.ShouldBe(12.5m);
            owed.Amount.ShouldBe(25_000);
            owed.CategoryId.ShouldBe(category.Id);
        }

        // A week later the job bills the finished week.
        var settings = Options.Create(new CommissionSettings());
        var nextWeek = new FixedTimeProvider(db.Clock.Now.AddDays(8));
        await using (var context = db.CreateContext())
        {
            (await new RunCommissionCycleHandler(context, settings, nextWeek).HandleAsync(new RunCommissionCycle(), CancellationToken.None)).StatementsIssued.ShouldBeGreaterThanOrEqualTo(1);
        }

        Guid statementId;
        await using (var context = db.CreateContext())
        {
            var statement = await context.CommissionStatements.AsNoTracking().SingleAsync(s => s.PartnerProfileId == partner.Id);
            statement.Total.ShouldBe(25_000);
            statement.Status.ShouldBe(StatementStatus.Open);
            statement.PeriodStart.ShouldBe(new DateOnly(2026, 10, 5));
            statementId = statement.Id;
            (await context.CommissionObligations.AsNoTracking().SingleAsync(o => o.OrderId == orderId)).StatementId.ShouldBe(statementId);
        }

        await using (var context = db.CreateContext())
        {
            var detail = await new RecordSettlementHandler(context, settings, nextWeek).HandleAsync(
                new RecordSettlement(statementId, 25_000, SettlementMethod.BankTransfer, new DateOnly(2026, 10, 13), "TX-7"), CancellationToken.None);
            detail.Summary.Statement.Status.ShouldBe("Paid");
            detail.Summary.PartnerName.ShouldBe(name);
            detail.Lines.ShouldHaveSingleItem().Summary.ShouldBe("New tiles on the bathroom walls.");
        }

        await using (var context = db.CreateContext())
        {
            var settlement = await context.Settlements.AsNoTracking().SingleAsync(s => s.StatementId == statementId);
            settlement.Method.ShouldBe(SettlementMethod.BankTransfer);
            settlement.Reference.ShouldBe("TX-7");
            (await context.CommissionStatements.AsNoTracking().SingleAsync(s => s.Id == statementId)).PaidAmount.ShouldBe(25_000);
            (await context.Database.SqlQueryRaw<string>("select status as \"Value\" from commission_statements where id = {0}", statementId).SingleAsync()).ShouldBe("Paid");
        }
    }
}
