using HomeServices.Application.Common;
using HomeServices.Application.Realtime;
using HomeServices.Application.Requests;
using HomeServices.Application.Tests.Offers;
using HomeServices.Domain;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Tests.Realtime;

/// <summary>Status changes reach the open Portal pages of everyone involved (T1), and closed requests say how they closed (D4).</summary>
public class LiveUpdatesTests
{
    private readonly OfferTestData _data = new();

    private Guid CustomerId => _data.Requests.Partners.User.Id;

    private Task<MyRequestDto> CancelAsync(Guid requestId) =>
        new CancelMyRequestHandler(_data.Requests.Db, _data.Customer, _data.Requests.Language, _data.Requests.Files, _data.Requests.Live, _data.Requests.Clock)
            .HandleAsync(new CancelMyRequest(requestId, null), CancellationToken.None);

    private Task<PagedResult<MyRequestListItemDto>> MineAsync() =>
        new GetMyRequestsHandler(_data.Requests.Db, _data.Customer, _data.Requests.Language).HandleAsync(new GetMyRequests(), CancellationToken.None);

    private Task<MyRequestDto> MineAsync(Guid id) =>
        new GetMyRequestHandler(_data.Requests.Db, _data.Customer, _data.Requests.Language, _data.Requests.Files).HandleAsync(new GetMyRequest(id), CancellationToken.None);

    [Fact]
    public async Task Cancelling_a_request_tells_its_customer_and_every_partner_who_received_it()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var gor = _data.Requests.GivenPartner("Gor");
        var request = _data.Requests.GivenRequest(partners: [aram, gor]);

        await CancelAsync(request.Id);

        var (users, update) = _data.Requests.Live.Requests.ShouldHaveSingleItem();
        users.ShouldBe(new[] { CustomerId, aram.UserId, gor.UserId }, ignoreOrder: true);
        update.ShouldBe(new RequestUpdateDto(request.Id, "Cancelled", null, OfferTestData.Now));
    }

    [Fact]
    public async Task Staff_cancelling_a_request_tells_everyone_too()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);

        await new CancelRequestByStaffHandler(_data.Requests.Db, _data.Requests.Language, _data.Requests.Files, _data.Requests.Live, _data.Requests.Clock)
            .HandleAsync(new CancelRequestByStaff(request.Id, "Duplicate"), CancellationToken.None);

        var (users, update) = _data.Requests.Live.Requests.ShouldHaveSingleItem();
        users.ShouldBe(new[] { CustomerId, aram.UserId }, ignoreOrder: true);
        update.Status.ShouldBe("Cancelled");
    }

    [Fact]
    public async Task Sending_and_withdrawing_an_offer_tell_the_customer_and_its_partner()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var gor = _data.Requests.GivenPartner("Gor");
        var request = _data.Requests.GivenRequest(partners: [aram, gor]);

        var offer = await _data.SendAsync(aram, _data.Work(request.Id));
        _data.Requests.Clock.Now = OfferTestData.Now.AddHours(1);
        await _data.WithdrawAsync(aram, offer.Id);

        _data.Requests.Live.Offers.Select(o => o.Update).ShouldBe(new[]
        {
            new OfferUpdateDto(offer.Id, request.Id, aram.Id, "Sent", null, OfferTestData.Now),
            new OfferUpdateDto(offer.Id, request.Id, aram.Id, "Withdrawn", null, OfferTestData.Now.AddHours(1)),
        });
        _data.Requests.Live.Offers.ShouldAllBe(o => o.Users.Count == 2 && o.Users.Contains(CustomerId) && o.Users.Contains(aram.UserId));
        _data.Requests.Live.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Accepting_a_work_offer_pushes_the_offer_and_the_closed_request_with_its_order()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var gor = _data.Requests.GivenPartner("Gor");
        var request = _data.Requests.GivenRequest(partners: [aram, gor]);
        var offer = await _data.SendAsync(aram, _data.Work(request.Id));
        _data.Requests.Live.Offers.Clear();

        var order = await _data.AcceptAsync(offer.Id);

        var (offerUsers, offerUpdate) = _data.Requests.Live.Offers.ShouldHaveSingleItem();
        offerUsers.ShouldBe(new[] { CustomerId, aram.UserId }, ignoreOrder: true);
        offerUpdate.ShouldBe(new OfferUpdateDto(offer.Id, request.Id, aram.Id, "Accepted", order.Id, OfferTestData.Now));
        var (requestUsers, requestUpdate) = _data.Requests.Live.Requests.ShouldHaveSingleItem();
        requestUsers.ShouldBe(new[] { CustomerId, aram.UserId, gor.UserId }, ignoreOrder: true);
        requestUpdate.ShouldBe(new RequestUpdateDto(request.Id, "Closed", order.Id, OfferTestData.Now));
    }

    [Fact]
    public async Task Accepting_a_visit_pushes_the_offer_but_the_request_stays_open()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var visit = await _data.SendAsync(aram, _data.Visit(request.Id));

        await _data.AcceptAsync(visit.Id);

        _data.Requests.Live.Offers.Last().Update.Status.ShouldBe("Accepted");
        _data.Requests.Live.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Accepting_again_pushes_nothing_new()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var offer = await _data.SendAsync(aram, _data.Work(request.Id));
        await _data.AcceptAsync(offer.Id);
        var pushed = (_data.Requests.Live.Offers.Count, _data.Requests.Live.Requests.Count);

        await _data.AcceptAsync(offer.Id);

        (_data.Requests.Live.Offers.Count, _data.Requests.Live.Requests.Count).ShouldBe(pushed);
    }

    [Fact]
    public async Task Rejecting_an_offer_pushes_it()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var offer = await _data.SendAsync(aram, _data.Work(request.Id));

        await _data.RejectAsync(offer.Id, "Too expensive");

        _data.Requests.Live.Offers.Last().Update.ShouldBe(new OfferUpdateDto(offer.Id, request.Id, aram.Id, "Rejected", null, OfferTestData.Now));
    }

    [Fact]
    public async Task Offers_on_a_cancelled_request_can_no_longer_be_accepted_or_rejected()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var offer = await _data.SendAsync(aram, _data.Work(request.Id));

        await CancelAsync(request.Id);

        (await Should.ThrowAsync<DomainException>(() => _data.AcceptAsync(offer.Id))).Code.ShouldBe("request.not_open");
        (await Should.ThrowAsync<DomainException>(() => _data.RejectAsync(offer.Id))).Code.ShouldBe("offer.not_open");
        (await _data.Requests.Db.Orders.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task An_expired_offer_can_be_neither_accepted_nor_rejected()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var offer = await _data.SendAsync(aram, _data.Work(request.Id));

        _data.Requests.Clock.Now = offer.ExpiresAt;

        (await Should.ThrowAsync<DomainException>(() => _data.AcceptAsync(offer.Id))).Code.ShouldBe("offer.expired");
        (await Should.ThrowAsync<DomainException>(() => _data.RejectAsync(offer.Id))).Code.ShouldBe("offer.expired");
        (await _data.OffersAsync(request.Id)).ShouldHaveSingleItem().Status.ShouldBe("Expired");
    }

    [Fact]
    public async Task Only_a_request_closed_by_an_accepted_work_offer_carries_its_order()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var accepted = _data.Requests.GivenRequest(partners: [aram]);
        var open = _data.Requests.GivenRequest(partners: [aram]);
        var cancelled = _data.Requests.GivenRequest(partners: [aram]);
        var offer = await _data.SendAsync(aram, _data.Work(accepted.Id));
        var visit = await _data.SendAsync(aram, _data.Visit(open.Id));
        var order = await _data.AcceptAsync(offer.Id);
        await _data.AcceptAsync(visit.Id);
        await CancelAsync(cancelled.Id);

        var list = (await MineAsync()).Items.ToDictionary(i => i.Id);

        list[accepted.Id].Status.ShouldBe("Closed");
        list[accepted.Id].OrderId.ShouldBe(order.Id);
        list[open.Id].OrderId.ShouldBeNull();
        list[cancelled.Id].OrderId.ShouldBeNull();
        (await MineAsync(accepted.Id)).OrderId.ShouldBe(order.Id);
        (await MineAsync(open.Id)).OrderId.ShouldBeNull();
    }
}
