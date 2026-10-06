using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;

namespace HomeServices.Domain.Tests.Catalog;

public class WorkItemMarketTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 9, 0, 0, TimeSpan.Zero);
    private static readonly PriceRange Staff = new(8000, 15000, 25000);
    private static readonly PriceRange Partners = new(10000, 14000, 20000);

    private static WorkItem Item()
    {
        var item = WorkItem.Create(Guid.NewGuid(), "leak-repair", LocalizedText.Empty.With("hy", "Արտահոսք"), WorkUnit.Fixed, WorkSurface.None, 1);
        item.SetPrice(Staff);
        return item;
    }

    [Fact]
    public void The_market_range_follows_the_staff_range_until_partners_take_over()
    {
        var item = Item();
        item.Market.ShouldBe(Staff);
        item.MarketSource.ShouldBe(MarketPriceSource.Staff);

        item.UpdateMarket(Partners, partnerCount: 5, minPartners: 5, Now);
        item.Market.ShouldBe(Partners);
        item.MarketSource.ShouldBe(MarketPriceSource.Partners);

        item.SetPrice(new PriceRange(1, 2, 3)); // staff edits don't override partners' prices
        item.Market.ShouldBe(Partners);

        item.UpdateMarket(Partners, partnerCount: 4, minPartners: 5, Now);
        item.Market.ShouldBe(new PriceRange(1, 2, 3));
        item.MarketSource.ShouldBe(MarketPriceSource.Staff);
        item.MarketPartnerCount.ShouldBe(4);
        item.MarketUpdatedAt.ShouldBe(Now);
    }

    [Fact]
    public void Locking_brings_back_the_staff_range()
    {
        var item = Item();
        item.UpdateMarket(Partners, 6, 5, Now);

        item.LockPrice(true);

        item.Market.ShouldBe(Staff);
        item.MarketSource.ShouldBe(MarketPriceSource.Staff);
        item.UpdateMarket(Partners, 6, 5, Now);
        item.Market.ShouldBe(Staff);
    }

    [Fact]
    public void Without_any_prices_there_is_no_market_range()
    {
        var item = WorkItem.Create(Guid.NewGuid(), "x", LocalizedText.Empty.With("hy", "x"), WorkUnit.Hour, WorkSurface.None, 1);

        item.UpdateMarket(null, 0, 5, Now);

        item.Market.ShouldBeNull();
    }
}
