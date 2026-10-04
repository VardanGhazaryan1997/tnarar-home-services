using HomeServices.Application.Errors;
using HomeServices.Application.Offers;
using HomeServices.Domain;
using HomeServices.Domain.Offers;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Requests;

namespace HomeServices.Application.Tests.Offers;

public class SendOfferTests
{
    private readonly OfferTestData _data = new();

    [Fact]
    public async Task A_partner_sends_a_work_offer_on_a_request_they_received()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        request.FlagForOperator(AttentionReason.NoResponse, OfferTestData.Now);
        await _data.Requests.Db.SaveChangesAsync();

        var dto = await _data.SendAsync(aram, _data.Work(request.Id));

        dto.Kind.ShouldBe("Work");
        dto.Status.ShouldBe("Sent");
        dto.Partner.PartnerId.ShouldBe(aram.Id);
        dto.Partner.DisplayName.ShouldBe("Aram");
        dto.Summary.ShouldBe("Replace the kitchen tap and the pipes under the sink.");
        dto.Lines.ShouldBe(new[] { new OfferLineDto("Remove the old tap", true), new OfferLineDto("Tiling", false) });
        dto.Price.ShouldBe(100_000);
        dto.MaterialsIncluded.ShouldBeTrue();
        dto.MaterialsNote.ShouldBe("Tap included");
        dto.StartDate.ShouldBe(new DateOnly(2026, 10, 8));
        dto.DurationDays.ShouldBe(2);
        dto.Stages.ShouldBe(new[] { new PaymentStageDto("Deposit", "Deposit", 50_000), new PaymentStageDto(null, "Final", 50_000) });
        dto.ExpiresAt.ShouldBe(OfferTestData.Now.AddDays(SendOffer.DefaultValidDays));
        dto.OrderId.ShouldBeNull();

        var stored = _data.StoredRequest(request.Id);
        stored.FindRecipient(aram.Id)!.Status.ShouldBe(RecipientStatus.Responded);
        stored.NeedsAttention.ShouldBeFalse();
    }

    [Fact]
    public async Task A_visit_offer_and_a_chosen_validity()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);

        var dto = await _data.SendAsync(aram, _data.Visit(request.Id) with { ValidDays = 2 });

        dto.Kind.ShouldBe("Visit");
        dto.Price.ShouldBe(0);
        dto.VisitAt.ShouldBe(OfferTestData.Now.AddDays(1));
        dto.Stages.ShouldBeEmpty();
        dto.ExpiresAt.ShouldBe(OfferTestData.Now.AddDays(2));
    }

    [Fact]
    public async Task Only_partners_who_received_the_request_can_offer()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var gor = _data.Requests.GivenPartner("Gor");
        var request = _data.Requests.GivenRequest(partners: [gor]);

        (await Should.ThrowAsync<NotFoundException>(() => _data.SendAsync(aram, _data.Work(request.Id)))).Code.ShouldBe("request.not_found");
        (await Should.ThrowAsync<NotFoundException>(() =>
                new SendOfferHandler(_data.Requests.Db, _data.Customer, _data.Requests.Clock).HandleAsync(_data.Work(request.Id), CancellationToken.None)))
            .Code.ShouldBe("partner.not_found");
    }

    [Fact]
    public async Task A_partner_who_declined_or_cannot_work_cannot_offer()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var gor = _data.Requests.GivenPartner("Gor");
        var request = _data.Requests.GivenRequest(partners: [aram, gor]);
        request.Decline(aram.Id, null, OfferTestData.Now);
        await _data.Requests.Db.SaveChangesAsync();
        gor.Suspend("Complaints.");
        await _data.Requests.Db.SaveChangesAsync();

        (await Should.ThrowAsync<DomainException>(() => _data.SendAsync(aram, _data.Work(request.Id)))).Code.ShouldBe("request.already_declined");
        (await Should.ThrowAsync<DomainException>(() => _data.SendAsync(gor, _data.Work(request.Id)))).Code.ShouldBe("offer.partner_unavailable");
    }

    [Fact]
    public async Task Closed_requests_take_no_offers()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        request.Cancel(null, OfferTestData.Now);
        await _data.Requests.Db.SaveChangesAsync();

        (await Should.ThrowAsync<DomainException>(() => _data.SendAsync(aram, _data.Work(request.Id)))).Code.ShouldBe("request.not_open");
    }

    [Fact]
    public async Task One_waiting_offer_of_each_kind_per_partner()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var first = await _data.SendAsync(aram, _data.Work(request.Id));

        (await Should.ThrowAsync<DomainException>(() => _data.SendAsync(aram, _data.Work(request.Id, 90_000)))).Code.ShouldBe("offer.already_sent");
        await _data.SendAsync(aram, _data.Visit(request.Id));

        await _data.WithdrawAsync(aram, first.Id);
        (await _data.SendAsync(aram, _data.Work(request.Id, 90_000))).Price.ShouldBe(90_000);
    }

    [Fact]
    public async Task An_expired_offer_can_be_replaced()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var first = await _data.SendAsync(aram, _data.Work(request.Id));
        _data.Requests.Clock.Now = OfferTestData.Now.AddDays(8);

        await _data.SendAsync(aram, _data.Work(request.Id, 90_000) with { StartDate = null });

        _data.StoredOffer(first.Id).Status.ShouldBe(OfferStatus.Expired);
    }

    [Fact]
    public async Task A_second_visit_after_an_accepted_one_is_refused()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var visit = await _data.SendAsync(aram, _data.Visit(request.Id));
        await _data.AcceptAsync(visit.Id);

        (await Should.ThrowAsync<DomainException>(() => _data.SendAsync(aram, _data.Visit(request.Id)))).Code.ShouldBe("offer.visit_already_agreed");
    }

    [Fact]
    public async Task The_partner_withdraws_only_their_own_waiting_offer()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var gor = _data.Requests.GivenPartner("Gor");
        var request = _data.Requests.GivenRequest(partners: [aram, gor]);
        var offer = await _data.SendAsync(aram, _data.Work(request.Id));

        (await Should.ThrowAsync<NotFoundException>(() => _data.WithdrawAsync(gor, offer.Id))).Code.ShouldBe("offer.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => _data.WithdrawAsync(gor, Guid.NewGuid()))).Code.ShouldBe("offer.not_found");
        var withdrawn = await _data.WithdrawAsync(aram, offer.Id);

        withdrawn.Status.ShouldBe("Withdrawn");
        withdrawn.DecidedAt.ShouldBe(OfferTestData.Now);
        (await Should.ThrowAsync<DomainException>(() => _data.WithdrawAsync(aram, offer.Id))).Code.ShouldBe("offer.not_open");
    }

    [Fact]
    public async Task A_partner_lists_their_offers_by_status()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var first = _data.Requests.GivenRequest(description: "First request for the bathroom.", partners: [aram]);
        var second = _data.Requests.GivenRequest(partners: [aram]);
        var third = _data.Requests.GivenRequest(partners: [aram]);
        var old = await _data.SendAsync(aram, _data.Work(first.Id));
        _data.Requests.Clock.Now = OfferTestData.Now.AddDays(8);
        var waiting = await _data.SendAsync(aram, _data.Visit(second.Id) with { VisitAt = OfferTestData.Now.AddDays(9) });
        var accepted = await _data.SendAsync(aram, _data.Work(third.Id) with { StartDate = null });
        await _data.AcceptAsync(accepted.Id);

        var all = await _data.MyOffersAsync(aram);
        all.TotalCount.ShouldBe(3);
        all.Items[2].Id.ShouldBe(old.Id);
        all.Items[2].Status.ShouldBe("Expired");
        all.Items[2].RequestExcerpt.ShouldBe("First request for the bathroom.");
        all.Items[2].Place.CityName.ShouldBe("Երևան");

        (await _data.MyOffersAsync(aram, new GetMyOffers(OfferStatus.Sent))).Items.ShouldHaveSingleItem().Id.ShouldBe(waiting.Id);
        (await _data.MyOffersAsync(aram, new GetMyOffers(OfferStatus.Expired))).Items.ShouldHaveSingleItem().Id.ShouldBe(old.Id);
        var acceptedItem = (await _data.MyOffersAsync(aram, new GetMyOffers(OfferStatus.Accepted))).Items.ShouldHaveSingleItem();
        acceptedItem.OrderId.ShouldNotBeNull();
        (await _data.MyOffersAsync(aram, new GetMyOffers(Page: 2, PageSize: 2))).Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Suspended_partners_without_offers_still_list_nothing()
    {
        var suspended = _data.Requests.GivenPartner("Suspended", status: PartnerStatus.Suspended);

        (await _data.MyOffersAsync(suspended)).Items.ShouldBeEmpty();
    }
}
