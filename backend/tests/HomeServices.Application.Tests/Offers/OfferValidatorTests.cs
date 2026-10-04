using HomeServices.Application.Offers;
using HomeServices.Application.Orders;
using HomeServices.Domain.Offers;
using HomeServices.Domain.Orders;

namespace HomeServices.Application.Tests.Offers;

public class OfferValidatorTests
{
    private readonly OfferTestData _data = new();

    private async Task<IEnumerable<string>> ErrorsAsync(SendOffer command) =>
        (await new SendOfferValidator(_data.Requests.Clock).ValidateAsync(command)).Errors.Select(e => $"{e.PropertyName}:{e.ErrorCode}");

    private SendOffer Work => _data.Work(Guid.NewGuid());

    private SendOffer Visit => _data.Visit(Guid.NewGuid());

    [Fact]
    public async Task Complete_offers_are_valid()
    {
        (await ErrorsAsync(Work)).ShouldBeEmpty();
        (await ErrorsAsync(Visit)).ShouldBeEmpty();
        (await ErrorsAsync(Work with { Lines = null, Stages = null, StartDate = null, DurationDays = null, MaterialsNote = null, ValidDays = 30 })).ShouldBeEmpty();
    }

    [Fact]
    public async Task Common_fields_are_checked()
    {
        (await ErrorsAsync(Work with { Kind = (OfferKind)9 })).ShouldContain("Kind:kind.invalid");
        (await ErrorsAsync(Work with { Summary = " " })).ShouldBe(new[] { "Summary:summary.required" });
        (await ErrorsAsync(Work with { Summary = "Too short" })).ShouldBe(new[] { "Summary:summary.length" });
        (await ErrorsAsync(Work with { MaterialsNote = new string('m', Offer.MaterialsNoteMaxLength + 1) })).ShouldBe(new[] { "MaterialsNote:materials_note.too_long" });
        (await ErrorsAsync(Work with { ValidDays = 0 })).ShouldBe(new[] { "ValidDays:valid_days.invalid" });
        (await ErrorsAsync(Work with { ValidDays = SendOffer.MaxValidDays + 1 })).ShouldBe(new[] { "ValidDays:valid_days.invalid" });
    }

    [Fact]
    public async Task Work_offers_need_a_price_and_sensible_terms()
    {
        (await ErrorsAsync(Work with { Price = 0, Stages = null })).ShouldBe(new[] { "Price:price.invalid" });
        (await ErrorsAsync(Work with { Lines = [new OfferLine(" ", true)] })).ShouldBe(new[] { "Lines:lines.invalid" });
        (await ErrorsAsync(Work with { Lines = Enumerable.Range(0, Offer.MaxLines + 1).Select(i => new OfferLine($"Line {i}", true)).ToList() }))
            .ShouldBe(new[] { "Lines:lines.too_many" });
        (await ErrorsAsync(Work with { StartDate = new DateOnly(2026, 10, 3) })).ShouldBe(new[] { "StartDate:start_date.past" });
        (await ErrorsAsync(Work with { StartDate = new DateOnly(2026, 10, 4) })).ShouldBeEmpty();
        (await ErrorsAsync(Work with { DurationDays = 0 })).ShouldBe(new[] { "DurationDays:duration_days.invalid" });
        (await ErrorsAsync(Work with { VisitAt = OfferTestData.Now.AddDays(1) })).ShouldBe(new[] { "VisitAt:visit_at.not_allowed" });
    }

    [Fact]
    public async Task Payment_stages_add_up_to_the_price()
    {
        (await ErrorsAsync(Work with { Stages = [new(null, PaymentPurpose.Deposit, 10)] })).ShouldBe(new[] { "Stages:stages.sum_mismatch" });
        (await ErrorsAsync(Work with { Stages = [new(null, PaymentPurpose.Final, 50_000), new(null, PaymentPurpose.Stage, 50_000)] }))
            .ShouldBe(new[] { "Stages:stages.final_last" });
        (await ErrorsAsync(Work with { Stages = [new(null, PaymentPurpose.Deposit, 0), new(null, PaymentPurpose.Final, 100_000)] }))
            .ShouldBe(new[] { "Stages:stages.invalid" });
        (await ErrorsAsync(Work with { Stages = [new(null, (PaymentPurpose)9, 100_000)] })).ShouldBe(new[] { "Stages:stages.invalid" });
        (await ErrorsAsync(Work with { Price = 11, Stages = Enumerable.Range(0, Offer.MaxStages + 1).Select(_ => new OfferStageTerms(null, PaymentPurpose.Stage, 1)).ToList() }))
            .ShouldBe(new[] { "Stages:stages.too_many" });
    }

    [Fact]
    public async Task Visit_offers_have_a_future_time_and_nothing_else()
    {
        (await ErrorsAsync(Visit with { VisitAt = null })).ShouldBe(new[] { "VisitAt:visit_at.required" });
        (await ErrorsAsync(Visit with { VisitAt = OfferTestData.Now })).ShouldBe(new[] { "VisitAt:visit_at.past" });
        (await ErrorsAsync(Visit with { Price = -1 })).ShouldBe(new[] { "Price:price.invalid" });
        (await ErrorsAsync(Visit with
            {
                Lines = [new OfferLine("Measure", true)],
                Stages = [new OfferStageTerms(null, PaymentPurpose.Final, 1)],
                StartDate = new DateOnly(2026, 10, 9),
                DurationDays = 1,
            }))
            .ShouldBe(
                new[] { "Lines:lines.not_allowed", "Stages:stages.not_allowed", "StartDate:start_date.not_allowed", "DurationDays:duration_days.not_allowed" },
                ignoreOrder: true);
    }

    [Fact]
    public async Task Lists_and_reasons_are_checked()
    {
        (await new GetMyOffersValidator().ValidateAsync(new GetMyOffers((OfferStatus)9, 0, 101))).Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "status.invalid", "page.invalid", "page_size.invalid" }, ignoreOrder: true);
        (await new GetMyOrdersValidator().ValidateAsync(new GetMyOrders((OrderRole)9, (OrderStatus)9, 0, 0))).Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "as.invalid", "status.invalid", "page.invalid", "page_size.invalid" }, ignoreOrder: true);
        (await new RejectOfferValidator().ValidateAsync(new RejectOffer(Guid.NewGuid(), new string('r', Offer.ReasonMaxLength + 1)))).Errors.Single().ErrorCode
            .ShouldBe("reason.too_long");
        (await new RejectOfferValidator().ValidateAsync(new RejectOffer(Guid.NewGuid(), null))).IsValid.ShouldBeTrue();
    }
}
