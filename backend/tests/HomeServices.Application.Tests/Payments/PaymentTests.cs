using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Orders;
using HomeServices.Application.Payments;
using HomeServices.Application.Tests.Offers;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Notifications;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Tests.Payments;

public class PaymentTests
{
    private readonly OfferTestData _data = new();

    private IAppDbContext Db => _data.Requests.Db;

    private FakeClock Clock => _data.Requests.Clock;

    private ICurrentLanguage Language => _data.Requests.Language;

    private async Task<(OrderDto Order, PartnerProfile Partner)> GivenOrderAsync()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var offer = await _data.SendAsync(aram, _data.Work(request.Id));
        return (await _data.AcceptAsync(offer.Id), aram);
    }

    private Task<OrderDto> RecordAsync(ICurrentUser user, Guid orderId, int amount, Guid? stageId = null, PaymentMethod method = PaymentMethod.Cash) =>
        new RecordPaymentHandler(Db, user, Language, Clock).HandleAsync(
            new RecordPayment(orderId, amount, method, new DateOnly(2026, 10, 5), stageId, "Paid at the door"), CancellationToken.None);

    private Task<OrderDto> ConfirmAsync(ICurrentUser user, Guid id) =>
        new ConfirmPaymentHandler(Db, user, Language, Clock).HandleAsync(new ConfirmPayment(id), CancellationToken.None);

    private Task<OrderDto> DisputeAsync(ICurrentUser user, Guid id, string reason) =>
        new DisputePaymentHandler(Db, user, Language, Clock).HandleAsync(new DisputePayment(id, reason), CancellationToken.None);

    private Task<OrderDto> WithdrawAsync(ICurrentUser user, Guid id) =>
        new WithdrawPaymentHandler(Db, user, Language, Clock).HandleAsync(new WithdrawPayment(id), CancellationToken.None);

    private List<(Guid UserId, NotificationType Type)> Notifications(NotificationType type) =>
        Db.Notifications.AsNoTracking().Where(n => n.Type == type).Select(n => new { n.UserId, n.Type }).AsEnumerable().Select(n => (n.UserId, n.Type)).ToList();

    [Fact]
    public async Task The_customer_records_a_payment_and_the_partner_confirms_it()
    {
        var (order, aram) = await GivenOrderAsync();
        var partner = OfferTestData.As(aram);
        var deposit = order.Stages[0];

        var recorded = await RecordAsync(_data.Customer, order.Id, 50_000, deposit.Id, PaymentMethod.BankTransfer);

        var payment = recorded.Payments.ShouldHaveSingleItem();
        payment.ShouldBe(new PaymentDto(
            payment.Id, deposit.Id, "Deposit", 50_000, "BankTransfer", new DateOnly(2026, 10, 5), "Paid at the door", "Customer", true, "Pending",
            Clock.Now, null, null, null, null));
        recorded.PaidAmount.ShouldBe(0);
        Notifications(NotificationType.PaymentRecorded).ShouldBe(new[] { (aram.UserId, NotificationType.PaymentRecorded) });

        var asPartner = await _data.OrderAsync(order.Id, partner);
        asPartner.Payments.ShouldHaveSingleItem().Mine.ShouldBeFalse();

        Clock.Now = OfferTestData.Now.AddHours(2);
        var confirmed = await ConfirmAsync(partner, payment.Id);
        confirmed.Payments[0].Status.ShouldBe("Confirmed");
        confirmed.Payments[0].AnsweredAt.ShouldBe(Clock.Now);
        confirmed.PaidAmount.ShouldBe(50_000);
        Notifications(NotificationType.PaymentAnswered).ShouldBe(new[] { (_data.Requests.Partners.User.Id, NotificationType.PaymentAnswered) });
    }

    [Fact]
    public async Task Records_follow_the_rules_and_cannot_add_up_to_more_than_the_price()
    {
        var (order, aram) = await GivenOrderAsync();
        var partner = OfferTestData.As(aram);
        var first = (await RecordAsync(partner, order.Id, 60_000)).Payments[0];

        (await Should.ThrowAsync<DomainException>(() => ConfirmAsync(partner, first.Id))).Code.ShouldBe("payment.wrong_party");
        (await Should.ThrowAsync<DomainException>(() => WithdrawAsync(_data.Customer, first.Id))).Code.ShouldBe("payment.wrong_party");
        (await Should.ThrowAsync<DomainException>(() => RecordAsync(_data.Customer, order.Id, 50_000))).Code.ShouldBe("payment.exceeds_price");
        (await Should.ThrowAsync<DomainException>(() => RecordAsync(_data.Customer, order.Id, 1, Guid.NewGuid()))).Code.ShouldBe("payment.stage_invalid");
        var stranger = new FakeCurrentUser(_data.Requests.Partners.OtherUser.Id);
        (await Should.ThrowAsync<NotFoundException>(() => ConfirmAsync(stranger, first.Id))).Code.ShouldBe("payment.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => RecordAsync(stranger, order.Id, 1))).Code.ShouldBe("order.not_found");

        var withdrawn = await WithdrawAsync(partner, first.Id);
        withdrawn.Payments[0].Status.ShouldBe("Withdrawn");
        (await Should.ThrowAsync<DomainException>(() => ConfirmAsync(_data.Customer, first.Id))).Code.ShouldBe("payment.not_pending");

        var full = await RecordAsync(_data.Customer, order.Id, 100_000);
        full.Payments.Count.ShouldBe(2);
        full.Actions.ShouldNotContain(OrderActions.RecordPayment);
    }

    [Fact]
    public async Task A_disputed_payment_goes_to_the_team_who_decide_it()
    {
        var (order, aram) = await GivenOrderAsync();
        var partner = OfferTestData.As(aram);
        var payment = (await RecordAsync(_data.Customer, order.Id, 30_000)).Payments[0];

        var disputed = await DisputeAsync(partner, payment.Id, "Nothing arrived");
        disputed.Payments[0].Status.ShouldBe("Disputed");
        disputed.Payments[0].DisputeReason.ShouldBe("Nothing arrived");

        var queue = await new GetAdminPaymentsHandler(Db).HandleAsync(new GetAdminPayments(PaymentStatus.Disputed), CancellationToken.None);
        var row = queue.Items.ShouldHaveSingleItem();
        row.OrderId.ShouldBe(order.Id);
        row.PartnerName.ShouldBe("Aram");
        row.CustomerPhone.ShouldBe("+37477123456");

        var resolved = await new ResolvePaymentHandler(Db, Clock).HandleAsync(new ResolvePayment(payment.Id, false, "No proof"), CancellationToken.None);
        resolved.Status.ShouldBe("Rejected");
        resolved.ResolutionNote.ShouldBe("No proof");
        Notifications(NotificationType.PaymentResolved).Count.ShouldBe(2);
        (await _data.OrderAsync(order.Id, _data.Customer)).PaidAmount.ShouldBe(0);

        (await Should.ThrowAsync<DomainException>(() =>
            new ResolvePaymentHandler(Db, Clock).HandleAsync(new ResolvePayment(payment.Id, true, null), CancellationToken.None))).Code.ShouldBe("payment.not_disputed");
    }
}
