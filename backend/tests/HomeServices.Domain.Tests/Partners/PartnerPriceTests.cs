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

        var single = PartnerPrice.Create(profileId, itemId, 5000, null, includesMaterials: true, DateTimeOffset.UnixEpoch);
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
        Should.Throw<DomainException>(() => PartnerPrice.Create(Guid.NewGuid(), Guid.NewGuid(), from, to, false, DateTimeOffset.UnixEpoch))
            .Code.ShouldBe("partner_price.invalid");
    }

    [Fact]
    public void A_price_is_fresh_until_it_goes_unconfirmed_too_long()
    {
        var setAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var price = PartnerPrice.Create(Guid.NewGuid(), Guid.NewGuid(), 5000, null, false, setAt);
        var freshFor = TimeSpan.FromDays(90);

        price.ConfirmedAt.ShouldBe(setAt);
        price.IsStale(setAt.AddDays(90), freshFor).ShouldBeFalse();
        price.IsStale(setAt.AddDays(91), freshFor).ShouldBeTrue();

        price.Confirm(setAt.AddDays(91));
        price.IsStale(setAt.AddDays(91), freshFor).ShouldBeFalse();
    }
}
