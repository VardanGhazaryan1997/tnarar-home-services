using HomeServices.Domain.Commissions;

namespace HomeServices.Domain.Tests.Commissions;

public class CommissionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Monday = new(2026, 10, 5);
    private static readonly Guid Partner = Guid.NewGuid();

    private static CommissionObligation Owed(int price, decimal percent = 10m, Guid? partner = null) =>
        CommissionObligation.For(Guid.NewGuid(), partner ?? Partner, Guid.NewGuid(), price, percent, Now.AddDays(-5));

    private static CommissionStatement Statement(params int[] prices) =>
        CommissionStatement.Issue(Partner, Monday, prices.Select(p => Owed(p)).ToList(), Monday.AddDays(14), Now);

    [Theory]
    [InlineData(100_000, 10, 10_000)]
    [InlineData(12_345, 10, 1_235)]
    [InlineData(12_344, 10, 1_234)]
    [InlineData(99_999, 7.5, 7_500)]
    [InlineData(50, 1, 1)]
    [InlineData(0, 10, 0)]
    public void The_amount_is_the_price_times_the_rate_rounded_half_up(int price, double percent, int expected) =>
        CommissionRate.AmountFor(price, (decimal)percent).ShouldBe(expected);

    [Fact]
    public void Rates_are_zero_to_fifty_percent_with_two_decimals()
    {
        var rate = CommissionRate.ForCategory(Guid.NewGuid(), 12.5m);
        rate.Percent.ShouldBe(12.5m);
        rate.Change(0m);
        rate.Percent.ShouldBe(0m);
        rate.Change(50m);
        rate.Percent.ShouldBe(50m);

        foreach (var bad in new[] { -1m, 50.01m, 10.005m })
        {
            Should.Throw<DomainException>(() => rate.Change(bad)).Code.ShouldBe("commission.rate_invalid");
        }

        var standard = CommissionRate.Default(10m);
        standard.Id.ShouldBe(CommissionRate.DefaultId);
        standard.CategoryId.ShouldBeNull();
    }

    [Fact]
    public void An_obligation_freezes_the_price_and_rate_and_is_billed_once()
    {
        var obligation = Owed(80_000, 12.5m);
        obligation.Amount.ShouldBe(10_000);
        obligation.RatePercent.ShouldBe(12.5m);
        obligation.StatementId.ShouldBeNull();

        var statementId = Guid.NewGuid();
        obligation.BillOn(statementId);
        obligation.StatementId.ShouldBe(statementId);
        Should.Throw<DomainException>(() => obligation.BillOn(Guid.NewGuid())).Code.ShouldBe("commission.already_billed");
        Should.Throw<DomainException>(() => Owed(-1)).Code.ShouldBe("commission.price_invalid");
    }

    [Fact]
    public void A_statement_covers_one_partners_week_and_bills_its_obligations()
    {
        var owed = new[] { Owed(100_000), Owed(50_000) };

        var statement = CommissionStatement.Issue(Partner, Monday, owed, new DateOnly(2026, 10, 19), Now);

        statement.PeriodStart.ShouldBe(Monday);
        statement.PeriodEnd.ShouldBe(new DateOnly(2026, 10, 11));
        statement.Total.ShouldBe(15_000);
        statement.Outstanding.ShouldBe(15_000);
        statement.Status.ShouldBe(StatementStatus.Open);
        statement.IssuedAt.ShouldBe(Now);
        owed.ShouldAllBe(o => o.StatementId == statement.Id);

        statement.IsOverdue(new DateOnly(2026, 10, 19)).ShouldBeFalse();
        statement.IsOverdue(new DateOnly(2026, 10, 20)).ShouldBeTrue();

        Should.Throw<DomainException>(() => CommissionStatement.Issue(Partner, Monday.AddDays(1), [Owed(1_000)], Monday, Now)).Code.ShouldBe("commission.week_invalid");
        Should.Throw<DomainException>(() => CommissionStatement.Issue(Partner, Monday, [], Monday, Now)).Code.ShouldBe("commission.statement_empty");
        Should.Throw<DomainException>(() => CommissionStatement.Issue(Partner, Monday, [Owed(1_000, partner: Guid.NewGuid())], Monday, Now))
            .Code.ShouldBe("commission.statement_empty");
    }

    [Fact]
    public void A_statement_with_nothing_to_pay_is_paid_at_once()
    {
        var statement = CommissionStatement.Issue(Partner, Monday, [Owed(100_000, 0m)], Monday.AddDays(14), Now);

        statement.Status.ShouldBe(StatementStatus.Paid);
        statement.PaidAt.ShouldBe(Now);
        statement.IsOverdue(Monday.AddDays(60)).ShouldBeFalse();
    }

    [Fact]
    public void Settlements_add_up_until_the_statement_is_paid()
    {
        var statement = Statement(100_000);

        var first = statement.Settle(4_000, SettlementMethod.BankTransfer, new DateOnly(2026, 10, 12), "  TX-1  ", Now);
        first.Amount.ShouldBe(4_000);
        first.Reference.ShouldBe("TX-1");
        first.StatementId.ShouldBe(statement.Id);
        first.PartnerProfileId.ShouldBe(Partner);
        first.RecordedAt.ShouldBe(Now);
        statement.PaidAmount.ShouldBe(4_000);
        statement.Status.ShouldBe(StatementStatus.Open);

        Should.Throw<DomainException>(() => statement.Settle(6_001, SettlementMethod.Cash, new DateOnly(2026, 10, 12), null, Now)).Code.ShouldBe("commission.settlement_amount_invalid");
        Should.Throw<DomainException>(() => statement.Settle(0, SettlementMethod.Cash, new DateOnly(2026, 10, 12), null, Now)).Code.ShouldBe("commission.settlement_amount_invalid");

        var last = statement.Settle(6_000, SettlementMethod.Cash, new DateOnly(2026, 10, 12), "   ", Now.AddHours(1));
        last.Reference.ShouldBeNull();
        statement.Status.ShouldBe(StatementStatus.Paid);
        statement.PaidAt.ShouldBe(Now.AddHours(1));
        statement.Outstanding.ShouldBe(0);
        Should.Throw<DomainException>(() => statement.Settle(1, SettlementMethod.Cash, new DateOnly(2026, 10, 12), null, Now)).Code.ShouldBe("commission.already_paid");
    }

    [Fact]
    public void Settlements_need_a_known_method_a_past_date_and_a_short_reference()
    {
        var statement = Statement(100_000);

        Should.Throw<DomainException>(() => statement.Settle(1, (SettlementMethod)99, new DateOnly(2026, 10, 12), null, Now)).Code.ShouldBe("commission.method_invalid");
        Should.Throw<DomainException>(() => statement.Settle(1, SettlementMethod.Cash, new DateOnly(2026, 10, 14), null, Now)).Code.ShouldBe("commission.date_future");
        Should.Throw<DomainException>(() => statement.Settle(1, SettlementMethod.Cash, new DateOnly(2026, 10, 12), new string('x', 201), Now))
            .Code.ShouldBe("commission.reference_too_long");
        statement.PaidAmount.ShouldBe(0);
    }

    [Fact]
    public void The_overdue_reminder_is_marked_once()
    {
        var statement = Statement(100_000);

        statement.MarkOverdueReminded(Now).ShouldBeTrue();
        statement.MarkOverdueReminded(Now.AddDays(1)).ShouldBeFalse();
        statement.OverdueReminderAt.ShouldBe(Now);
    }
}
