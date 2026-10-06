using HomeServices.Domain.Catalog;
using HomeServices.Domain.Partners;

namespace HomeServices.Domain.Tests.Partners;

public class PartnerPriceTests
{
    [Fact]
    public void A_price_can_be_a_single_value_or_a_range()
    {
        var profileId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var single = PartnerPrice.Create(profileId, itemId, 5000, null, includesMaterials: true);
        (single.PartnerProfileId, single.WorkItemId, single.PriceFrom, single.PriceTo, single.IncludesMaterials)
            .ShouldBe((profileId, itemId, 5000, (int?)null, true));

        single.Change(4000, 6000, includesMaterials: false);
        (single.PriceFrom, single.PriceTo, single.IncludesMaterials).ShouldBe((4000, (int?)6000, false));

        single.Change(7000, 7000, false);
        single.PriceTo.ShouldBeNull();
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(-5, null)]
    [InlineData(5000, 4000)]
    [InlineData(PriceRange.Limit + 1, null)]
    [InlineData(1000, PriceRange.Limit + 1)]
    public void Prices_must_be_positive_in_order_and_within_the_limit(int from, int? to)
    {
        PartnerPrice.IsValid(from, to).ShouldBeFalse();
        Should.Throw<DomainException>(() => PartnerPrice.Create(Guid.NewGuid(), Guid.NewGuid(), from, to, false))
            .Code.ShouldBe("partner_price.invalid");
    }
}
