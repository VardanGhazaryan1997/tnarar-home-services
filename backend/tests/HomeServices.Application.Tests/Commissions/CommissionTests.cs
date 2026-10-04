using HomeServices.Application.Abstractions;
using HomeServices.Application.Commissions;
using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Orders;
using HomeServices.Application.Tests.Offers;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Commissions;
using HomeServices.Domain.Notifications;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Commissions;

public class CommissionTests
{
    // OfferTestData.Now is Monday 2026-10-05 09:00 UTC (13:00 in Yerevan).
    private static readonly DateTimeOffset NextMonday = OfferTestData.Now.AddDays(7);

    private readonly OfferTestData _data = new();

    public CommissionTests()
    {
        Db.CommissionRates.Add(CommissionRate.Default(10m));
        Db.SaveChanges();
    }

    private InMemoryAppDbContext Db => _data.Requests.Db;

    private FakeClock Clock => _data.Requests.Clock;

    private ICurrentLanguage Language => _data.Requests.Language;

    private CommissionSettings Settings { get; } = new();

    private IOptions<CommissionSettings> Config => Options.Create(Settings);

    private async Task<OrderDto> AcceptedOrderAsync(PartnerProfile partner, int price = 100_000)
    {
        var request = _data.Requests.GivenRequest(partners: [partner]);
        var offer = await _data.SendAsync(partner, _data.Work(request.Id, price));
        return await _data.AcceptAsync(offer.Id);
    }

    private async Task<OrderDto> CompletedOrderAsync(PartnerProfile partner, int price = 100_000) => await CompleteAsync(await AcceptedOrderAsync(partner, price), partner);

    private async Task<OrderDto> CompleteAsync(OrderDto order, PartnerProfile partner)
    {
        await new RequestOrderCompletionHandler(Db, OfferTestData.As(partner), Language, Options.Create(new OrderSettings()), Clock)
            .HandleAsync(new RequestOrderCompletion(order.Id), CancellationToken.None);
        return await new ConfirmOrderCompletionHandler(Db, _data.Customer, Language, Clock).HandleAsync(new ConfirmOrderCompletion(order.Id), CancellationToken.None);
    }

    private CommissionObligation Obligation(Guid orderId) => Db.CommissionObligations.AsNoTracking().Single(o => o.OrderId == orderId);

    private Task<CommissionCycleResult> CycleAsync() =>
        new RunCommissionCycleHandler(Db, Config, Clock).HandleAsync(new RunCommissionCycle(), CancellationToken.None);

    private Task<CommissionRatesDto> SetRateAsync(Guid categoryId, decimal? percent) =>
        new SetCategoryCommissionRateHandler(Db, Language).HandleAsync(new SetCategoryCommissionRate(categoryId, percent), CancellationToken.None);

    private Task<AdminStatementDetailDto> SettleAsync(Guid statementId, int amount) =>
        new RecordSettlementHandler(Db, Config, Clock).HandleAsync(
            new RecordSettlement(statementId, amount, SettlementMethod.BankTransfer, DateOnly.FromDateTime(Clock.Now.UtcDateTime), "TX-42"), CancellationToken.None);

    private Task<CommissionSummaryDto> SummaryAsync(ICurrentUser user) =>
        new GetMyCommissionSummaryHandler(Db, user, Config, Clock).HandleAsync(new GetMyCommissionSummary(), CancellationToken.None);

    private List<Guid> Notified(NotificationType type) => Db.Notifications.AsNoTracking().Where(n => n.Type == type).Select(n => n.UserId).ToList();

    private ServiceRequest StoredRequest(Guid id) => Db.ServiceRequests.AsNoTracking().Include(r => r.Recipients).Single(r => r.Id == id);

    [Fact]
    public async Task Rates_apply_to_a_category_and_its_subcategories_unless_they_have_their_own()
    {
        var catalog = _data.Requests.Partners;
        var rates = await new GetCommissionRatesHandler(Db, Language).HandleAsync(new GetCommissionRates(), CancellationToken.None);
        rates.DefaultPercent.ShouldBe(10m);
        rates.Categories.Select(c => c.CategoryId).ShouldContain(catalog.Boilers.Id);
        var heatingAt = rates.Categories.ToList().FindIndex(c => c.CategoryId == catalog.Heating.Id);
        rates.Categories[heatingAt + 1].CategoryId.ShouldBe(catalog.Boilers.Id);
        rates.Categories.Single(c => c.CategoryId == catalog.Plumbing.Id).ShouldBe(new CategoryRateDto(catalog.Plumbing.Id, "Սանտեխնիկա", null, null, 10m));

        rates = await SetRateAsync(catalog.Heating.Id, 8m);
        rates.Categories.Single(c => c.CategoryId == catalog.Boilers.Id).ShouldBe(new CategoryRateDto(catalog.Boilers.Id, "Կաթսաներ", catalog.Heating.Id, null, 8m));

        rates = await SetRateAsync(catalog.Boilers.Id, 5m);
        rates.Categories.Single(c => c.CategoryId == catalog.Boilers.Id).EffectivePercent.ShouldBe(5m);
        rates = await SetRateAsync(catalog.Boilers.Id, 6.25m);
        rates.Categories.Single(c => c.CategoryId == catalog.Boilers.Id).Percent.ShouldBe(6.25m);

        rates = await SetRateAsync(catalog.Boilers.Id, null);
        rates.Categories.Single(c => c.CategoryId == catalog.Boilers.Id).ShouldBe(new CategoryRateDto(catalog.Boilers.Id, "Կաթսաներ", catalog.Heating.Id, null, 8m));
        (await SetRateAsync(catalog.Boilers.Id, null)).Categories.Single(c => c.CategoryId == catalog.Boilers.Id).Percent.ShouldBeNull();

        rates = await new SetDefaultCommissionRateHandler(Db, Language).HandleAsync(new SetDefaultCommissionRate(12m), CancellationToken.None);
        rates.DefaultPercent.ShouldBe(12m);
        rates.Categories.Single(c => c.CategoryId == catalog.Plumbing.Id).EffectivePercent.ShouldBe(12m);

        (await Should.ThrowAsync<NotFoundException>(() => SetRateAsync(Guid.NewGuid(), 5m))).Code.ShouldBe("category.not_found");
    }

    [Fact]
    public async Task The_default_rate_is_created_when_it_is_missing()
    {
        Db.CommissionRates.RemoveRange(Db.CommissionRates);
        await Db.SaveChangesAsync();

        var rates = await new SetDefaultCommissionRateHandler(Db, Language).HandleAsync(new SetDefaultCommissionRate(9m), CancellationToken.None);

        rates.DefaultPercent.ShouldBe(9m);
        Db.CommissionRates.AsNoTracking().Single().Id.ShouldBe(CommissionRate.DefaultId);
    }

    [Fact]
    public async Task Completing_an_order_charges_the_final_price_at_the_rate_frozen_then()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        await SetRateAsync(_data.Requests.Partners.Plumbing.Id, 12.5m);

        var order = await CompletedOrderAsync(aram, 80_000);

        var owed = Obligation(order.Id);
        owed.PartnerProfileId.ShouldBe(aram.Id);
        owed.CategoryId.ShouldBe(_data.Requests.Partners.Plumbing.Id);
        owed.OrderPrice.ShouldBe(80_000);
        owed.RatePercent.ShouldBe(12.5m);
        owed.Amount.ShouldBe(10_000);
        owed.CompletedAt.ShouldBe(Clock.Now);
        owed.StatementId.ShouldBeNull();

        await SetRateAsync(_data.Requests.Partners.Plumbing.Id, 20m);
        Obligation(order.Id).Amount.ShouldBe(10_000);
    }

    [Fact]
    public async Task Cancelled_orders_and_zero_rates_owe_nothing_and_unanswered_completions_are_charged()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var cancelled = await _data.AcceptAsync((await _data.SendAsync(aram, _data.Work(request.Id))).Id);
        await new CancelOrderHandler(Db, _data.Customer, Language, Clock).HandleAsync(new CancelOrder(cancelled.Id, "Changed my mind"), CancellationToken.None);

        await SetRateAsync(_data.Requests.Partners.Plumbing.Id, 0m);
        var free = await CompletedOrderAsync(aram);
        Db.CommissionObligations.AsNoTracking().ShouldBeEmpty();
        free.Status.ShouldBe("Completed");

        await SetRateAsync(_data.Requests.Partners.Plumbing.Id, null);
        var other = _data.Requests.GivenRequest(partners: [aram]);
        var waiting = await _data.AcceptAsync((await _data.SendAsync(aram, _data.Work(other.Id))).Id);
        await new RequestOrderCompletionHandler(Db, OfferTestData.As(aram), Language, Options.Create(new OrderSettings { AutoCompleteDays = 7 }), Clock)
            .HandleAsync(new RequestOrderCompletion(waiting.Id), CancellationToken.None);
        Clock.Now = OfferTestData.Now.AddDays(7);
        (await new CompleteUnansweredOrdersHandler(Db, Clock).HandleAsync(new CompleteUnansweredOrders(), CancellationToken.None)).ShouldBe(1);

        Db.CommissionObligations.AsNoTracking().ShouldHaveSingleItem().OrderId.ShouldBe(waiting.Id);
        Obligation(waiting.Id).Amount.ShouldBe(10_000);
    }

    [Fact]
    public async Task The_weekly_job_issues_statements_reminds_pauses_and_a_settlement_resumes()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var vahe = _data.Requests.GivenPartner("Vahe");
        var first = await CompletedOrderAsync(aram);
        await CompletedOrderAsync(aram, 50_000);
        await CompletedOrderAsync(vahe);

        var lateOrder = await AcceptedOrderAsync(aram);

        // Sunday 21:00 UTC is already Monday in Yerevan: that order belongs to the next week.
        Clock.Now = new DateTimeOffset(2026, 10, 11, 21, 0, 0, TimeSpan.Zero);
        await CompleteAsync(lateOrder, aram);

        Clock.Now = OfferTestData.Now.AddDays(1);
        (await CycleAsync()).ShouldBe(new CommissionCycleResult(0, 0, 0, 0));

        Clock.Now = NextMonday;
        (await CycleAsync()).ShouldBe(new CommissionCycleResult(2, 0, 0, 0));
        (await CycleAsync()).StatementsIssued.ShouldBe(0);

        var statement = Db.CommissionStatements.AsNoTracking().Single(s => s.PartnerProfileId == aram.Id);
        statement.PeriodStart.ShouldBe(new DateOnly(2026, 10, 5));
        statement.PeriodEnd.ShouldBe(new DateOnly(2026, 10, 11));
        statement.Total.ShouldBe(15_000);
        statement.DueOn.ShouldBe(new DateOnly(2026, 10, 19));
        Obligation(first.Id).StatementId.ShouldBe(statement.Id);
        Obligation(lateOrder.Id).StatementId.ShouldBeNull();
        Notified(NotificationType.CommissionStatementIssued).ShouldBe(new[] { aram.UserId, vahe.UserId }, ignoreOrder: true);
        Db.Notifications.AsNoTracking().First(n => n.Type == NotificationType.CommissionStatementIssued && n.UserId == aram.UserId).Link.ShouldBe("/commissions");

        await SettleAsync(Db.CommissionStatements.AsNoTracking().Single(s => s.PartnerProfileId == vahe.Id).Id, 10_000);

        Clock.Now = new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);
        (await CycleAsync()).ShouldBe(new CommissionCycleResult(1, 0, 0, 0));

        Clock.Now = new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);
        (await CycleAsync()).ShouldBe(new CommissionCycleResult(0, 1, 0, 0));
        (await CycleAsync()).RemindersSent.ShouldBe(0);
        Notified(NotificationType.CommissionOverdue).ShouldBe(new[] { aram.UserId });

        Clock.Now = new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
        (await CycleAsync()).PartnersPaused.ShouldBe(0);

        Clock.Now = new DateTimeOffset(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);
        (await CycleAsync()).PartnersPaused.ShouldBe(1);
        (await CycleAsync()).PartnersPaused.ShouldBe(0);
        Db.PartnerProfiles.AsNoTracking().Single(p => p.Id == aram.Id).DebtPausedSince.ShouldBe(Clock.Now);
        Notified(NotificationType.PartnerPausedForDebt).ShouldBe(new[] { aram.UserId });

        var summary = await SummaryAsync(OfferTestData.As(aram));
        summary.PausedSince.ShouldBe(Clock.Now);
        summary.Overdue.ShouldBe(25_000);

        // Paying the long-overdue statement lifts the pause; the newer one is overdue by less than 14 days.
        var detail = await SettleAsync(statement.Id, 15_000);
        detail.Summary.Statement.Status.ShouldBe("Paid");
        detail.Summary.PartnerPaused.ShouldBeFalse();
        detail.Settlements.ShouldHaveSingleItem().Reference.ShouldBe("TX-42");
        Notified(NotificationType.CommissionSettled).ShouldContain(aram.UserId);
        Db.PartnerProfiles.AsNoTracking().Single(p => p.Id == aram.Id).DebtPausedSince.ShouldBeNull();
        Notified(NotificationType.PartnerResumed).ShouldBe(new[] { aram.UserId });
        (await CycleAsync()).ShouldBe(new CommissionCycleResult(0, 0, 0, 0));
    }

    [Fact]
    public async Task The_job_resumes_a_paused_partner_who_owes_nothing_long_overdue()
    {
        var aram = _data.Requests.GivenPartner("Aram", p => p.PauseForDebt(OfferTestData.Now));

        (await CycleAsync()).ShouldBe(new CommissionCycleResult(0, 0, 0, 1));

        Db.PartnerProfiles.AsNoTracking().Single(p => p.Id == aram.Id).DebtPausedSince.ShouldBeNull();
    }

    [Fact]
    public async Task Paused_partners_get_no_new_requests()
    {
        var aram = _data.Requests.GivenPartner("Aram", p => p.PauseForDebt(OfferTestData.Now));
        var vahe = _data.Requests.GivenPartner("Vahe");

        var open = await _data.Requests.CreateAsync(_data.Requests.OpenRequest());
        StoredRequest(open.Id).Recipients.Select(r => r.PartnerProfileId).ShouldBe(new[] { vahe.Id });

        (await Should.ThrowAsync<DomainException>(() => _data.Requests.CreateAsync(_data.Requests.DirectRequest(aram.Id)))).Code.ShouldBe("request.partner_unavailable");
    }

    [Fact]
    public async Task Partners_see_their_commissions_and_statements_only()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var vahe = _data.Requests.GivenPartner("Vahe");
        var first = await CompletedOrderAsync(aram);
        await CompletedOrderAsync(vahe);
        var unbilled = await AcceptedOrderAsync(aram, 30_000);
        Clock.Now = NextMonday;
        await CycleAsync();
        Clock.Now = NextMonday.AddHours(1);
        await CompleteAsync(unbilled, aram);
        var me = OfferTestData.As(aram);

        var summary = await SummaryAsync(me);
        summary.ShouldBe(new CommissionSummaryDto(3_000, 10_000, 0, new DateOnly(2026, 10, 19), null, 14));

        var lines = await new GetMyCommissionsHandler(Db, me).HandleAsync(new GetMyCommissions(), CancellationToken.None);
        lines.Items.Select(l => l.OrderId).ShouldBe(new[] { unbilled.Id, first.Id });
        lines.Items[1].Summary.ShouldBe("Replace the kitchen tap and the pipes under the sink.");
        (await new GetMyCommissionsHandler(Db, me).HandleAsync(new GetMyCommissions(Unbilled: true), CancellationToken.None)).Items.ShouldHaveSingleItem().Amount.ShouldBe(3_000);

        var statements = await new GetMyStatementsHandler(Db, me, Config, Clock).HandleAsync(new GetMyStatements(), CancellationToken.None);
        var mine = statements.Items.ShouldHaveSingleItem();
        mine.Total.ShouldBe(10_000);
        mine.Overdue.ShouldBeFalse();

        var detail = await new GetMyStatementHandler(Db, me, Config, Clock).HandleAsync(new GetMyStatement(mine.Id), CancellationToken.None);
        detail.Lines.ShouldHaveSingleItem().OrderId.ShouldBe(first.Id);
        detail.Settlements.ShouldBeEmpty();

        var vahes = Db.CommissionStatements.AsNoTracking().Single(s => s.PartnerProfileId == vahe.Id).Id;
        (await Should.ThrowAsync<NotFoundException>(() => new GetMyStatementHandler(Db, me, Config, Clock).HandleAsync(new GetMyStatement(vahes), CancellationToken.None)))
            .Code.ShouldBe("commission.statement_not_found");
        var stranger = new FakeCurrentUser(_data.Requests.Partners.OtherUser.Id);
        (await Should.ThrowAsync<NotFoundException>(() => SummaryAsync(stranger))).Code.ShouldBe("partner.not_found");
    }

    [Fact]
    public async Task Staff_list_statements_by_state_and_partner_and_settlements_follow_the_rules()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var vahe = _data.Requests.GivenPartner("Vahe");
        await CompletedOrderAsync(aram);
        await CompletedOrderAsync(vahe, 50_000);
        Clock.Now = NextMonday;
        await CycleAsync();
        var aramStatement = Db.CommissionStatements.AsNoTracking().Single(s => s.PartnerProfileId == aram.Id).Id;
        var vaheStatement = Db.CommissionStatements.AsNoTracking().Single(s => s.PartnerProfileId == vahe.Id).Id;
        await SettleAsync(vaheStatement, 5_000);
        Clock.Now = new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

        Task<PagedResult<AdminStatementDto>> ListAsync(GetAdminStatements query) =>
            new GetAdminStatementsHandler(Db, Config, Clock).HandleAsync(query, CancellationToken.None);

        (await ListAsync(new GetAdminStatements())).TotalCount.ShouldBe(2);
        var overdue = (await ListAsync(new GetAdminStatements(StatementFilter.Overdue))).Items.ShouldHaveSingleItem();
        overdue.PartnerName.ShouldBe("Aram");
        overdue.Statement.Overdue.ShouldBeTrue();
        overdue.PartnerPaused.ShouldBeFalse();
        (await ListAsync(new GetAdminStatements(StatementFilter.Open))).Items.ShouldHaveSingleItem().PartnerId.ShouldBe(aram.Id);
        (await ListAsync(new GetAdminStatements(StatementFilter.Paid))).Items.ShouldHaveSingleItem().PartnerId.ShouldBe(vahe.Id);
        (await ListAsync(new GetAdminStatements(PartnerId: vahe.Id))).Items.ShouldHaveSingleItem().Statement.Id.ShouldBe(vaheStatement);

        var detail = await new GetAdminStatementHandler(Db, Config, Clock).HandleAsync(new GetAdminStatement(vaheStatement), CancellationToken.None);
        detail.Settlements.ShouldHaveSingleItem().ShouldBe(new SettlementDto(detail.Settlements[0].Id, 5_000, "BankTransfer", new DateOnly(2026, 10, 12), "TX-42", NextMonday));
        detail.Lines.ShouldHaveSingleItem().Amount.ShouldBe(5_000);

        (await Should.ThrowAsync<DomainException>(() => SettleAsync(aramStatement, 10_001))).Code.ShouldBe("commission.settlement_amount_invalid");
        (await Should.ThrowAsync<DomainException>(() => SettleAsync(vaheStatement, 1))).Code.ShouldBe("commission.already_paid");
        (await Should.ThrowAsync<NotFoundException>(() => SettleAsync(Guid.NewGuid(), 1))).Code.ShouldBe("commission.statement_not_found");
        (await Should.ThrowAsync<NotFoundException>(() =>
            new GetAdminStatementHandler(Db, Config, Clock).HandleAsync(new GetAdminStatement(Guid.NewGuid()), CancellationToken.None))).Code.ShouldBe("commission.statement_not_found");
    }

    [Fact]
    public void Validators_check_percents_amounts_and_pages()
    {
        new SetDefaultCommissionRateValidator().Validate(new SetDefaultCommissionRate(10.5m)).IsValid.ShouldBeTrue();
        new SetDefaultCommissionRateValidator().Validate(new SetDefaultCommissionRate(50.5m)).Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("percent.invalid");
        new SetDefaultCommissionRateValidator().Validate(new SetDefaultCommissionRate(1.234m)).Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("percent.invalid");
        new SetCategoryCommissionRateValidator().Validate(new SetCategoryCommissionRate(Guid.NewGuid(), null)).IsValid.ShouldBeTrue();
        var category = new SetCategoryCommissionRateValidator().Validate(new SetCategoryCommissionRate(Guid.NewGuid(), -1m)).Errors.ShouldHaveSingleItem();
        category.ErrorCode.ShouldBe("percent.invalid");
        category.PropertyName.ShouldBe("Percent");

        var settlement = new RecordSettlementValidator().Validate(new RecordSettlement(Guid.NewGuid(), 0, (SettlementMethod)9, new DateOnly(2026, 10, 5), new string('x', 201)));
        settlement.Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "amount.invalid", "method.invalid", "reference.too_long" }, ignoreOrder: true);
        new RecordSettlementValidator().Validate(new RecordSettlement(Guid.NewGuid(), 1, SettlementMethod.Cash, new DateOnly(2026, 10, 5), null)).IsValid.ShouldBeTrue();

        new GetAdminStatementsValidator().Validate(new GetAdminStatements((StatementFilter)9, null, 0, 101)).Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "filter.invalid", "page.invalid", "page_size.invalid" }, ignoreOrder: true);
        new GetMyCommissionsValidator().Validate(new GetMyCommissions(false, 0, 0)).Errors.Count.ShouldBe(2);
        new GetMyStatementsValidator().Validate(new GetMyStatements(1, 101)).Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("page_size.invalid");
    }

    [Fact]
    public void Weeks_start_on_monday_in_local_time()
    {
        CommissionSettings.WeekStart(new DateOnly(2026, 10, 11)).ShouldBe(new DateOnly(2026, 10, 5));
        CommissionSettings.WeekStart(new DateOnly(2026, 10, 12)).ShouldBe(new DateOnly(2026, 10, 12));
        Settings.LocalDate(new DateTimeOffset(2026, 10, 11, 20, 30, 0, TimeSpan.Zero)).ShouldBe(new DateOnly(2026, 10, 12));
        Settings.Offset.ShouldBe(TimeSpan.FromHours(4));
    }
}
