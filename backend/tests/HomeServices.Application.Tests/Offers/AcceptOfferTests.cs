using HomeServices.Application.Errors;
using HomeServices.Application.Orders;
using HomeServices.Application.Requests;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Offers;
using HomeServices.Domain.Orders;
using HomeServices.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Tests.Offers;

public class AcceptOfferTests
{
    private readonly OfferTestData _data = new();

    [Fact]
    public async Task The_customer_compares_offers_waiting_and_cheapest_first()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var gor = _data.Requests.GivenPartner("Gor");
        var ani = _data.Requests.GivenPartner("Ani");
        var request = _data.Requests.GivenRequest(partners: [aram, gor, ani]);
        var expensive = await _data.SendAsync(aram, _data.Work(request.Id, 150_000));
        var cheap = await _data.SendAsync(gor, _data.Work(request.Id, 80_000));
        var rejected = await _data.SendAsync(ani, _data.Work(request.Id, 50_000));
        await _data.RejectAsync(rejected.Id, "Not now");
        var withdrawn = await _data.SendAsync(aram, _data.Visit(request.Id));
        await _data.WithdrawAsync(aram, withdrawn.Id);

        var offers = await _data.OffersAsync(request.Id);

        offers.Select(o => o.Id).ShouldBe(new[] { cheap.Id, expensive.Id, rejected.Id });
        offers[2].Status.ShouldBe("Rejected");
        offers[2].RejectReason.ShouldBe("Not now");
        offers[0].Partner.DisplayName.ShouldBe("Gor");
    }

    [Fact]
    public async Task A_partner_sees_only_their_own_offers_and_strangers_nothing()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var gor = _data.Requests.GivenPartner("Gor");
        var outsider = _data.Requests.GivenPartner("Outsider");
        var request = _data.Requests.GivenRequest(partners: [aram, gor]);
        var mine = await _data.SendAsync(aram, _data.Work(request.Id));
        await _data.SendAsync(gor, _data.Work(request.Id, 90_000));
        await _data.WithdrawAsync(aram, mine.Id);

        (await _data.OffersAsync(request.Id, OfferTestData.As(aram))).ShouldHaveSingleItem().Status.ShouldBe("Withdrawn");
        (await Should.ThrowAsync<NotFoundException>(() => _data.OffersAsync(request.Id, OfferTestData.As(outsider)))).Code.ShouldBe("request.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => _data.OffersAsync(Guid.NewGuid()))).Code.ShouldBe("request.not_found");
    }

    [Fact]
    public async Task One_offer_is_visible_to_its_customer_and_its_partner_only()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var gor = _data.Requests.GivenPartner("Gor");
        var request = _data.Requests.GivenRequest(partners: [aram, gor]);
        var offer = await _data.SendAsync(aram, _data.Work(request.Id));

        (await _data.GetAsync(offer.Id, _data.Customer)).Id.ShouldBe(offer.Id);
        (await _data.GetAsync(offer.Id, OfferTestData.As(aram))).Id.ShouldBe(offer.Id);
        (await Should.ThrowAsync<NotFoundException>(() => _data.GetAsync(offer.Id, OfferTestData.As(gor)))).Code.ShouldBe("offer.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => _data.GetAsync(offer.Id, new FakeCurrentUser(_data.Requests.Partners.OtherUser.Id))))
            .Code.ShouldBe("offer.not_found");
    }

    [Fact]
    public async Task Accepting_a_work_offer_creates_the_order_and_closes_the_request()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var gor = _data.Requests.GivenPartner("Gor");
        var request = _data.Requests.GivenRequest(partners: [aram, gor]);
        var chosen = await _data.SendAsync(aram, _data.Work(request.Id));
        var other = await _data.SendAsync(gor, _data.Work(request.Id, 90_000));
        _data.Requests.Clock.Now = OfferTestData.Now.AddHours(3);

        var order = await _data.AcceptAsync(chosen.Id);

        order.Kind.ShouldBe("Work");
        order.Status.ShouldBe("Confirmed");
        order.MyRole.ShouldBe("Customer");
        order.RequestId.ShouldBe(request.Id);
        order.OfferId.ShouldBe(chosen.Id);
        order.ParentOrderId.ShouldBeNull();
        order.Price.ShouldBe(100_000);
        order.Place.CategoryName.ShouldBe("Սանտեխնիկա");
        order.Terms.Summary.ShouldBe("Replace the kitchen tap and the pipes under the sink.");
        order.Terms.Lines.Count.ShouldBe(2);
        order.Terms.MaterialsNote.ShouldBe("Tap included");
        order.Terms.StartDate.ShouldBe(new DateOnly(2026, 10, 8));
        order.Terms.Stages.Count.ShouldBe(2);
        order.Stages.Select(s => (s.Purpose, s.Amount)).ShouldBe(new[] { ("Deposit", 50_000), ("Final", 50_000) });
        order.History.ShouldHaveSingleItem().ShouldBe(new OrderStatusChangeDto("Confirmed", OfferTestData.Now.AddHours(3), "Customer", null));
        order.Partner.DisplayName.ShouldBe("Aram");
        order.Partner.Phone.ShouldStartWith("+37499");
        order.Customer.Phone.ShouldBe("+37477123456");

        var accepted = _data.StoredOffer(chosen.Id);
        accepted.Status.ShouldBe(OfferStatus.Accepted);
        accepted.OrderId.ShouldBe(order.Id);
        _data.StoredOffer(other.Id).Status.ShouldBe(OfferStatus.Closed);
        var stored = _data.StoredRequest(request.Id);
        stored.Status.ShouldBe(RequestStatus.Closed);
        stored.ClosedAt.ShouldBe(OfferTestData.Now.AddHours(3));
    }

    [Fact]
    public async Task Accepting_again_returns_the_same_order()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var offer = await _data.SendAsync(aram, _data.Work(request.Id));

        var first = await _data.AcceptAsync(offer.Id);
        var again = await _data.AcceptAsync(offer.Id);

        again.Id.ShouldBe(first.Id);
        (await _data.Requests.Db.Orders.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task A_visit_keeps_the_request_open_and_the_work_order_follows_it()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var gor = _data.Requests.GivenPartner("Gor");
        var request = _data.Requests.GivenRequest(partners: [aram, gor]);
        var visit = await _data.SendAsync(aram, _data.Visit(request.Id, fee: 5_000));
        var gorsOffer = await _data.SendAsync(gor, _data.Work(request.Id, 90_000));

        var visitOrder = await _data.AcceptAsync(visit.Id);

        visitOrder.Kind.ShouldBe("Visit");
        visitOrder.Terms.VisitAt.ShouldBe(OfferTestData.Now.AddDays(1));
        visitOrder.Stages.ShouldHaveSingleItem().Amount.ShouldBe(5_000);
        _data.StoredRequest(request.Id).Status.ShouldBe(RequestStatus.Open);
        _data.StoredOffer(gorsOffer.Id).Status.ShouldBe(OfferStatus.Sent);

        var work = await _data.SendAsync(aram, _data.Work(request.Id));
        var workOrder = await _data.AcceptAsync(work.Id);

        workOrder.ParentOrderId.ShouldBe(visitOrder.Id);
        _data.StoredOffer(gorsOffer.Id).Status.ShouldBe(OfferStatus.Closed);
    }

    [Fact]
    public async Task Only_the_requests_customer_decides_on_its_offers()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var offer = await _data.SendAsync(aram, _data.Work(request.Id));
        var stranger = new FakeCurrentUser(_data.Requests.Partners.OtherUser.Id);

        (await Should.ThrowAsync<NotFoundException>(() => _data.AcceptAsync(offer.Id, stranger))).Code.ShouldBe("offer.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => _data.AcceptAsync(offer.Id, OfferTestData.As(aram)))).Code.ShouldBe("offer.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => _data.RejectAsync(offer.Id, user: stranger))).Code.ShouldBe("offer.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => _data.AcceptAsync(Guid.NewGuid()))).Code.ShouldBe("offer.not_found");
    }

    [Fact]
    public async Task Expired_withdrawn_and_closed_offers_cannot_be_accepted()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var gor = _data.Requests.GivenPartner("Gor");
        var ani = _data.Requests.GivenPartner("Ani");
        var request = _data.Requests.GivenRequest(partners: [aram, gor, ani]);
        var withdrawn = await _data.SendAsync(aram, _data.Work(request.Id));
        await _data.WithdrawAsync(aram, withdrawn.Id);
        var other = await _data.SendAsync(gor, _data.Work(request.Id, 90_000));
        var chosen = await _data.SendAsync(ani, _data.Work(request.Id, 80_000));

        (await Should.ThrowAsync<DomainException>(() => _data.AcceptAsync(withdrawn.Id))).Code.ShouldBe("offer.not_open");

        _data.Requests.Clock.Now = OfferTestData.Now.AddDays(8);
        (await Should.ThrowAsync<DomainException>(() => _data.AcceptAsync(chosen.Id))).Code.ShouldBe("offer.expired");

        _data.Requests.Clock.Now = OfferTestData.Now;
        await _data.AcceptAsync(chosen.Id);
        (await Should.ThrowAsync<DomainException>(() => _data.AcceptAsync(other.Id))).Code.ShouldBe("request.not_open");
    }

    [Fact]
    public async Task An_offer_from_a_partner_who_was_suspended_meanwhile_cannot_be_accepted()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var offer = await _data.SendAsync(aram, _data.Work(request.Id));
        aram.Suspend("Complaints.");
        await _data.Requests.Db.SaveChangesAsync();

        (await Should.ThrowAsync<DomainException>(() => _data.AcceptAsync(offer.Id))).Code.ShouldBe("offer.partner_unavailable");
    }

    [Fact]
    public async Task Cancelling_the_request_closes_waiting_offers()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var offer = await _data.SendAsync(aram, _data.Work(request.Id));

        await new CancelMyRequestHandler(_data.Requests.Db, _data.Customer, _data.Requests.Language, _data.Requests.Files, _data.Requests.Clock)
            .HandleAsync(new CancelMyRequest(request.Id, null), CancellationToken.None);

        _data.StoredOffer(offer.Id).Status.ShouldBe(OfferStatus.Closed);
    }

    [Fact]
    public async Task Staff_cancelling_the_request_closes_waiting_offers_too()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var offer = await _data.SendAsync(aram, _data.Visit(request.Id));

        await new CancelRequestByStaffHandler(_data.Requests.Db, _data.Requests.Language, _data.Requests.Files, _data.Requests.Clock)
            .HandleAsync(new CancelRequestByStaff(request.Id, "Duplicate"), CancellationToken.None);

        _data.StoredOffer(offer.Id).Status.ShouldBe(OfferStatus.Closed);
        (await _data.Requests.Db.Orders.AnyAsync(o => o.Kind == OrderKind.Visit)).ShouldBeFalse();
    }

    [Fact]
    public async Task Rejecting_tells_the_partner_why()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var offer = await _data.SendAsync(aram, _data.Work(request.Id));

        var dto = await _data.RejectAsync(offer.Id, " Too expensive ");

        dto.Status.ShouldBe("Rejected");
        dto.RejectReason.ShouldBe("Too expensive");
        (await _data.GetAsync(offer.Id, OfferTestData.As(aram))).RejectReason.ShouldBe("Too expensive");
        _data.StoredRequest(request.Id).Status.ShouldBe(RequestStatus.Open);
    }
}
