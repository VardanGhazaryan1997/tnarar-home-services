using HomeServices.Application.Catalog;
using HomeServices.Application.Partners;
using HomeServices.Application.Tests.Partners;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Partners;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Catalog;

public class MarketPricesTests
{
    private readonly PartnerTestData _data = new();
    private readonly WorkItem _item;

    public MarketPricesTests()
    {
        var leaks = Category.Create("leaks", PartnerTestData.Text("Արտահոսքեր"), 1, _data.Plumbing.Id);
        _data.Db.Categories.Add(leaks);
        _item = WorkItem.Create(leaks.Id, "leak-repair", PartnerTestData.Text("leak-repair"), WorkUnit.Fixed, WorkSurface.None, 1);
        _item.SetPrice(new PriceRange(8000, 15000, 25000));
        _data.Db.WorkItems.Add(_item);
        _data.Db.SaveChanges();
    }

    private void GivenPrices(PartnerStatus status = PartnerStatus.Approved, bool materials = false, params (int From, int? To)[] prices)
    {
        foreach (var (from, to) in prices)
        {
            var partner = _data.GivenProfile($"Partner {Guid.NewGuid():N}"[..20], status);
            _data.Db.PartnerPrices.Add(PartnerPrice.Create(partner.Id, _item.Id, from, to, materials));
        }

        _data.Db.SaveChanges();
    }

    private Task<MarketPricesResult> RecalculateAsync(int minPartners = 5) =>
        new RecalculateMarketPricesHandler(_data.Db, Options.Create(new PricingSettings { MinPartners = minPartners }), _data.Clock)
            .HandleAsync(new RecalculateMarketPrices(), CancellationToken.None);

    [Fact]
    public async Task Five_partners_prices_replace_the_staff_range()
    {
        GivenPrices(prices: [(10000, null), (12000, null), (14000, 16000), (20000, null), (30000, null)]);

        var result = await RecalculateAsync();

        result.ShouldBe(new MarketPricesResult(1, 1));
        _item.MarketSource.ShouldBe(MarketPriceSource.Partners);
        _item.Market.ShouldBe(new PriceRange(12000, 15000, 20000));
        _item.MarketPartnerCount.ShouldBe(5);
        _item.MarketUpdatedAt.ShouldBe(PartnerTestData.Now);
        _item.Price.ShouldBe(new PriceRange(8000, 15000, 25000)); // the staff range is kept
    }

    [Fact]
    public async Task Too_few_partners_keep_the_staff_range()
    {
        GivenPrices(prices: [(10000, null), (12000, null), (14000, null), (20000, null)]);

        (await RecalculateAsync()).FromPartners.ShouldBe(0);

        _item.MarketSource.ShouldBe(MarketPriceSource.Staff);
        _item.Market.ShouldBe(new PriceRange(8000, 15000, 25000));
        _item.MarketPartnerCount.ShouldBe(4);
        (await RecalculateAsync(minPartners: 4)).FromPartners.ShouldBe(1);
    }

    [Fact]
    public async Task Only_approved_partners_labour_prices_count()
    {
        GivenPrices(prices: [(10000, null), (12000, null), (14000, null)]);
        GivenPrices(PartnerStatus.UnderReview, prices: [(1000, null), (1000, null)]);
        GivenPrices(materials: true, prices: [(90000, null), (90000, null)]);

        await RecalculateAsync(minPartners: 3);

        _item.MarketPartnerCount.ShouldBe(3);
        _item.Market.ShouldBe(new PriceRange(11000, 12000, 13000));
    }

    [Fact]
    public async Task A_locked_staff_range_is_kept()
    {
        _item.LockPrice(true);
        GivenPrices(prices: [(10000, null), (12000, null), (14000, null), (20000, null), (30000, null)]);

        await RecalculateAsync();

        _item.MarketSource.ShouldBe(MarketPriceSource.Staff);
        _item.Market.ShouldBe(new PriceRange(8000, 15000, 25000));
        _item.MarketPartnerCount.ShouldBe(5);
    }

    [Fact]
    public async Task Saving_partner_prices_updates_the_market_range_at_once()
    {
        GivenPrices(prices: [(10000, null), (12000, null), (14000, null), (20000, null), (30000, null)]);
        _item.MarketSource.ShouldBe(MarketPriceSource.Staff); // not recalculated yet
        await new SaveMyPartnerProfileHandler(_data.Db, _data.Me, _data.Dtos).HandleAsync(_data.ValidSave(), CancellationToken.None);

        // My profile is still a draft, so my own price doesn't count, but saving recalculates the items I offer.
        var list = await new SaveMyPricesHandler(_data.Db, _data.Me, new FakeCurrentLanguage("hy"), Options.Create(new PricingSettings()), _data.Clock)
            .HandleAsync(new SaveMyPrices([new MyPriceInput(_item.Id, 90000, null, false)]), CancellationToken.None);

        _item.MarketSource.ShouldBe(MarketPriceSource.Partners);
        list.Items.Single().MarketTypical.ShouldBe(14000);
    }

    [Theory]
    [InlineData(new[] { 100 }, 100, 100, 100)]
    [InlineData(new[] { 1, 2, 3, 4, 5 }, 2, 3, 4)]
    [InlineData(new[] { 10, 20, 30, 40 }, 17.5, 25, 32.5)]
    public void Percentiles_interpolate_between_ranks(int[] prices, double p25, double median, double p75)
    {
        var sorted = prices.Order().ToArray();

        MarketPrices.Percentile(sorted, 0.25).ShouldBe(p25);
        MarketPrices.Percentile(sorted, 0.5).ShouldBe(median);
        MarketPrices.Percentile(sorted, 0.75).ShouldBe(p75);
    }

    [Theory]
    [InlineData(147, 150)]
    [InlineData(999.4, 1000)]
    [InlineData(12_345, 12_300)]
    [InlineData(12_350, 12_400)]
    [InlineData(123_456, 123_000)]
    public void Prices_are_rounded_to_tidy_amounts(double price, int expected) => MarketPrices.Tidy(price).ShouldBe(expected);

    [Fact]
    public void No_prices_give_no_range() => MarketPrices.RangeOf([]).ShouldBeNull();
}
