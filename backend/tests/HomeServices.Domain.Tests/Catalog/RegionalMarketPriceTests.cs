using HomeServices.Domain.Catalog;

namespace HomeServices.Domain.Tests.Catalog;

public class RegionalMarketPriceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_regional_price_holds_the_range_and_its_partners()
    {
        var itemId = Guid.NewGuid();
        var regionId = Guid.NewGuid();

        var price = RegionalMarketPrice.Create(itemId, regionId, new PriceRange(1000, 2000, 3000), 5, Now);
        (price.WorkItemId, price.RegionId, price.PartnerCount, price.UpdatedAt).ShouldBe((itemId, regionId, 5, Now));
        price.Range.ShouldBe(new PriceRange(1000, 2000, 3000));

        price.Update(new PriceRange(1500, 2500, 3500), 6, Now.AddDays(1));
        (price.Range, price.PartnerCount, price.UpdatedAt).ShouldBe((new PriceRange(1500, 2500, 3500), 6, Now.AddDays(1)));
    }

    [Fact]
    public void A_regional_price_needs_a_work_item_a_region_and_a_partner()
    {
        var range = new PriceRange(1000, 2000, 3000);

        Should.Throw<DomainException>(() => RegionalMarketPrice.Create(Guid.Empty, Guid.NewGuid(), range, 5, Now)).Code.ShouldBe("regional_price.invalid");
        Should.Throw<DomainException>(() => RegionalMarketPrice.Create(Guid.NewGuid(), Guid.Empty, range, 5, Now)).Code.ShouldBe("regional_price.invalid");
        Should.Throw<DomainException>(() => RegionalMarketPrice.Create(Guid.NewGuid(), Guid.NewGuid(), range, 0, Now)).Code.ShouldBe("regional_price.invalid");
    }
}
