using HomeServices.Domain.Offers;

namespace HomeServices.Domain.Tests.Offers;

public class OfferTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Request = Guid.NewGuid();
    private static readonly Guid Partner = Guid.NewGuid();

    private static OfferTerms Work(int price = 100_000, IReadOnlyList<OfferStageTerms>? stages = null, IReadOnlyList<OfferLine>? lines = null) => new(
        "  Replace the kitchen tap and the pipes under the sink.  ",
        lines ?? [new OfferLine(" Remove the old tap ", true), new OfferLine("Tiling", false)],
        price,
        true,
        " Tap and fittings included ",
        new DateOnly(2026, 10, 8),
        2,
        null,
        stages ?? []);

    private static OfferTerms Visit(int fee = 0, DateTimeOffset? at = null) =>
        new("I'll come and measure the bathroom.", [], fee, false, null, null, null, at ?? Now.AddDays(1), []);

    private static Offer NewOffer(OfferTerms? terms = null, OfferKind kind = OfferKind.Work) =>
        Offer.Create(Request, Partner, kind, terms ?? Work(), Now.AddDays(7), Now);

    [Fact]
    public void A_work_offer_keeps_clean_terms_and_waits()
    {
        var offer = NewOffer();

        offer.Status.ShouldBe(OfferStatus.Sent);
        offer.Kind.ShouldBe(OfferKind.Work);
        offer.RequestId.ShouldBe(Request);
        offer.PartnerProfileId.ShouldBe(Partner);
        offer.Summary.ShouldBe("Replace the kitchen tap and the pipes under the sink.");
        offer.MaterialsNote.ShouldBe("Tap and fittings included");
        offer.StartDate.ShouldBe(new DateOnly(2026, 10, 8));
        offer.DurationDays.ShouldBe(2);
        offer.VisitAt.ShouldBeNull();
        offer.ExpiresAt.ShouldBe(Now.AddDays(7));
        offer.Items.Select(i => (i.Title, i.Included, i.SortOrder)).ShouldBe(new[] { ("Remove the old tap", true, 1), ("Tiling", false, 2) });
        offer.IsOpen(Now).ShouldBeTrue();
    }

    [Fact]
    public void Without_stages_the_whole_price_is_one_final_payment()
    {
        var stage = NewOffer().Stages.ShouldHaveSingleItem();

        stage.Purpose.ShouldBe(PaymentPurpose.Final);
        stage.Amount.ShouldBe(100_000);
        stage.Title.ShouldBeNull();
    }

    [Fact]
    public void Payment_stages_must_add_up_to_the_price_with_the_final_one_last()
    {
        var offer = NewOffer(Work(stages: [new(" Deposit ", PaymentPurpose.Deposit, 30_000), new(null, PaymentPurpose.Final, 70_000)]));
        offer.Stages.Select(s => (s.Title, s.Purpose, s.Amount, s.SortOrder))
            .ShouldBe(new[] { ("Deposit", PaymentPurpose.Deposit, 30_000, 1), ((string?)null, PaymentPurpose.Final, 70_000, 2) });

        Code(() => NewOffer(Work(stages: [new(null, PaymentPurpose.Deposit, 30_000)]))).ShouldBe("offer.stages_sum_mismatch");
        Code(() => NewOffer(Work(stages: [new(null, PaymentPurpose.Final, 30_000), new(null, PaymentPurpose.Stage, 70_000)]))).ShouldBe("offer.final_stage_last");
        Code(() => NewOffer(Work(stages: [new(null, PaymentPurpose.Deposit, 0), new(null, PaymentPurpose.Final, 100_000)]))).ShouldBe("offer.stage_invalid");
        Code(() => NewOffer(Work(stages: [new(null, (PaymentPurpose)9, 100_000)]))).ShouldBe("offer.stage_invalid");
        Code(() => NewOffer(Work(stages: [new(new string('t', Offer.StageTitleMaxLength + 1), PaymentPurpose.Final, 100_000)]))).ShouldBe("offer.stage_invalid");
        Code(() => NewOffer(Work(price: 11, stages: Enumerable.Range(0, Offer.MaxStages + 1).Select(_ => new OfferStageTerms(null, PaymentPurpose.Stage, 1)).ToList())))
            .ShouldBe("offer.too_many_stages");
    }

    [Fact]
    public void Work_terms_are_checked()
    {
        Code(() => NewOffer(Work() with { Summary = "Too short" })).ShouldBe("offer.summary_length");
        Code(() => NewOffer(Work(price: 0))).ShouldBe("offer.price_invalid");
        Code(() => NewOffer(Work(price: Offer.MaxPrice + 1))).ShouldBe("offer.price_invalid");
        Code(() => NewOffer(Work() with { MaterialsNote = new string('m', Offer.MaterialsNoteMaxLength + 1) })).ShouldBe("offer.materials_note_too_long");
        Code(() => NewOffer(Work() with { DurationDays = 0 })).ShouldBe("offer.duration_invalid");
        Code(() => NewOffer(Work() with { VisitAt = Now.AddDays(1) })).ShouldBe("offer.visit_time_invalid");
        Code(() => NewOffer(Work(lines: [new OfferLine("  ", true)]))).ShouldBe("offer.line_invalid");
        Code(() => NewOffer(Work(lines: Enumerable.Range(0, Offer.MaxLines + 1).Select(i => new OfferLine($"Line {i}", true)).ToList())))
            .ShouldBe("offer.too_many_lines");
    }

    [Fact]
    public void An_offer_needs_parties_a_kind_and_time_to_answer()
    {
        Code(() => Offer.Create(Guid.Empty, Partner, OfferKind.Work, Work(), Now.AddDays(7), Now)).ShouldBe("offer.parties_required");
        Code(() => Offer.Create(Request, Guid.Empty, OfferKind.Work, Work(), Now.AddDays(7), Now)).ShouldBe("offer.parties_required");
        Code(() => Offer.Create(Request, Partner, (OfferKind)9, Work(), Now.AddDays(7), Now)).ShouldBe("offer.kind_invalid");
        Code(() => Offer.Create(Request, Partner, OfferKind.Work, Work(), Now, Now)).ShouldBe("offer.expiry_invalid");
    }

    [Fact]
    public void A_visit_offer_has_a_time_and_a_fee_which_may_be_zero()
    {
        var free = NewOffer(Visit(), OfferKind.Visit);
        free.Price.ShouldBe(0);
        free.VisitAt.ShouldBe(Now.AddDays(1));
        free.Stages.ShouldBeEmpty();
        free.Items.ShouldBeEmpty();

        var paid = NewOffer(Visit(fee: 5_000), OfferKind.Visit);
        paid.Stages.ShouldHaveSingleItem().Amount.ShouldBe(5_000);

        Code(() => NewOffer(Visit(at: Now), OfferKind.Visit)).ShouldBe("offer.visit_time_invalid");
        Code(() => NewOffer(Visit() with { VisitAt = null }, OfferKind.Visit)).ShouldBe("offer.visit_time_invalid");
        Code(() => NewOffer(Visit() with { Lines = [new OfferLine("Measure", true)] }, OfferKind.Visit)).ShouldBe("offer.visit_terms_invalid");
        Code(() => NewOffer(Visit() with { StartDate = new DateOnly(2026, 10, 9) }, OfferKind.Visit)).ShouldBe("offer.visit_terms_invalid");
        Code(() => NewOffer(Visit(fee: -1), OfferKind.Visit)).ShouldBe("offer.price_invalid");
    }

    [Fact]
    public void Accepting_records_the_order()
    {
        var offer = NewOffer();
        var orderId = Guid.NewGuid();

        offer.Accept(orderId, Now.AddHours(1));

        offer.Status.ShouldBe(OfferStatus.Accepted);
        offer.OrderId.ShouldBe(orderId);
        offer.DecidedAt.ShouldBe(Now.AddHours(1));
        Code(() => offer.Accept(Guid.NewGuid(), Now)).ShouldBe("offer.not_open");
        Code(() => offer.Reject(null, Now)).ShouldBe("offer.not_open");
        Code(() => offer.Withdraw(Now)).ShouldBe("offer.not_open");
    }

    [Fact]
    public void An_expired_offer_cannot_be_accepted_and_shows_as_expired()
    {
        var offer = NewOffer();
        var later = Now.AddDays(8);

        offer.IsOpen(later).ShouldBeFalse();
        offer.StatusAt(later).ShouldBe(OfferStatus.Expired);
        offer.StatusAt(Now).ShouldBe(OfferStatus.Sent);
        Code(() => offer.Accept(Guid.NewGuid(), later)).ShouldBe("offer.expired");
        Code(() => offer.Reject(null, later)).ShouldBe("offer.expired");

        offer.Expire(Now).ShouldBeFalse();
        offer.Expire(later).ShouldBeTrue();
        offer.Status.ShouldBe(OfferStatus.Expired);
        offer.Expire(later).ShouldBeFalse();
    }

    [Fact]
    public void The_customer_rejects_with_an_optional_reason()
    {
        var offer = NewOffer();

        Code(() => offer.Reject(new string('r', Offer.ReasonMaxLength + 1), Now)).ShouldBe("offer.reason_too_long");
        offer.Reject("  Too expensive ", Now.AddHours(2));

        offer.Status.ShouldBe(OfferStatus.Rejected);
        offer.RejectReason.ShouldBe("Too expensive");
        offer.DecidedAt.ShouldBe(Now.AddHours(2));

        var silent = NewOffer();
        silent.Reject("  ", Now);
        silent.RejectReason.ShouldBeNull();
    }

    [Fact]
    public void The_partner_withdraws_a_waiting_offer()
    {
        var offer = NewOffer();
        offer.Withdraw(Now.AddHours(1));
        offer.Status.ShouldBe(OfferStatus.Withdrawn);
        offer.DecidedAt.ShouldBe(Now.AddHours(1));

        var old = NewOffer();
        old.Withdraw(Now.AddDays(8));
        old.Status.ShouldBe(OfferStatus.Expired);
    }

    [Fact]
    public void Closing_the_request_closes_only_waiting_offers()
    {
        var waiting = NewOffer();
        var rejected = NewOffer();
        rejected.Reject(null, Now);
        var old = NewOffer();

        waiting.Close(Now.AddHours(1));
        rejected.Close(Now.AddHours(1));
        old.Close(Now.AddDays(8));

        waiting.Status.ShouldBe(OfferStatus.Closed);
        waiting.DecidedAt.ShouldBe(Now.AddHours(1));
        rejected.Status.ShouldBe(OfferStatus.Rejected);
        old.Status.ShouldBe(OfferStatus.Expired);
    }

    private static string Code(Action action) => Should.Throw<DomainException>(action).Code;
}
