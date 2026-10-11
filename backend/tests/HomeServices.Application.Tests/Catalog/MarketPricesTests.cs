using HomeServices.Application.Catalog;
using HomeServices.Application.Estimates;
using HomeServices.Application.Partners;
using HomeServices.Application.Tests.Partners;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Estimates;
using HomeServices.Domain.Offers;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Requests;
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

    private List<PartnerProfile> GivenPrices(
        PartnerStatus status = PartnerStatus.Approved,
        bool materials = false,
        DateTimeOffset? confirmedAt = null,
        Guid? cityId = null,
        params (int From, int? To)[] prices)
    {
        var partners = new List<PartnerProfile>();
        foreach (var (from, to) in prices)
        {
            var partner = _data.GivenProfile(
                $"Partner {Guid.NewGuid():N}"[..20],
                status,
                configure: cityId is { } city ? p => p.SetAreas([(city, null)]) : null);
            _data.Db.PartnerPrices.Add(PartnerPrice.Create(partner.Id, _item.Id, from, to, materials, confirmedAt ?? PartnerTestData.Now));
            partners.Add(partner);
        }

        _data.Db.SaveChanges();
        return partners;
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

    // A customer accepted a work offer that priced this work at unitPrice per unit, at decidedAt.
    private PartnerProfile GivenAcceptedOffer(
        int unitPrice, DateTimeOffset decidedAt, bool materials = false, PartnerProfile? partner = null, Guid? cityId = null)
    {
        partner ??= _data.GivenProfile($"Deal {Guid.NewGuid():N}"[..20], PartnerStatus.Approved);
        var request = ServiceRequest.Create(_data.User.Id, RequestKind.Open, _data.Plumbing.Id, cityId ?? _data.Yerevan.Id, null, "Fix the leaking pipe, please.", null, null, null, null);
        var line = request.AddLine("Kitchen", _item.Id, WorkUnit.Fixed, 2m, null, null);
        var terms = new OfferTerms(
            "Fix the leak under the sink.",
            [new OfferLine("Kitchen · leak repair", true, line.Id, 2m, unitPrice)],
            unitPrice * 2,
            materials,
            null,
            null,
            1,
            null,
            []);
        var offer = Offer.Create(request.Id, partner.Id, OfferKind.Work, terms, decidedAt.AddDays(7), decidedAt);
        offer.Accept(Guid.NewGuid(), decidedAt);
        _data.Db.ServiceRequests.Add(request);
        _data.Db.Offers.Add(offer);
        _data.Db.SaveChanges();
        return partner;
    }

    [Fact]
    public async Task Accepted_offers_count_twice_as_much_as_listed_prices()
    {
        GivenPrices(prices: [(10000, null), (12000, null), (14000, null)]);
        GivenAcceptedOffer(40000, PartnerTestData.Now.AddDays(-10));
        GivenAcceptedOffer(1000, PartnerTestData.Now.AddDays(-400)); // too old
        GivenAcceptedOffer(1000, PartnerTestData.Now.AddDays(-5), materials: true); // not labour only

        (await RecalculateAsync(minPartners: 4)).FromPartners.ShouldBe(1);

        // 10,000 · 12,000 · 14,000 · 40,000 · 40,000 from four partners.
        _item.MarketPartnerCount.ShouldBe(4);
        _item.Market.ShouldBe(new PriceRange(12000, 14000, 40000));
    }

    [Fact]
    public async Task Prices_not_confirmed_for_90_days_dont_count()
    {
        GivenPrices(prices: [(10000, null), (12000, null), (14000, null), (20000, null)]);
        GivenPrices(confirmedAt: PartnerTestData.Now.AddDays(-91), prices: [(30000, null)]);
        GivenPrices(confirmedAt: PartnerTestData.Now.AddDays(-89), prices: [(16000, null)]);

        (await RecalculateAsync(minPartners: 5)).FromPartners.ShouldBe(1);

        _item.MarketPartnerCount.ShouldBe(5);
        _item.Market.ShouldBe(new PriceRange(12000, 14000, 16000));
    }

    [Fact]
    public async Task Prices_far_outside_the_rest_are_left_out()
    {
        GivenPrices(prices: [(100, null), (10000, null), (11000, null), (12000, null), (13000, null), (14000, null), (900000, null)]);

        await RecalculateAsync();

        _item.MarketPartnerCount.ShouldBe(5);
        _item.Market.ShouldBe(new PriceRange(11000, 12000, 13000));
    }

    [Fact]
    public async Task Many_deals_of_one_partner_count_as_one_price()
    {
        GivenPrices(prices: [(10000, null), (12000, null), (14000, null), (16000, null)]);
        var busy = GivenAcceptedOffer(2000, PartnerTestData.Now.AddDays(-3));
        GivenAcceptedOffer(3000, PartnerTestData.Now.AddDays(-2), partner: busy);
        GivenAcceptedOffer(4000, PartnerTestData.Now.AddDays(-1), partner: busy);

        await RecalculateAsync();

        // 3,000 (the median of their deals, twice) · 10,000 · 12,000 · 14,000 · 16,000.
        _item.MarketPartnerCount.ShouldBe(5);
        _item.Market.ShouldBe(new PriceRange(4800, 11000, 13500));
    }

    [Fact]
    public async Task A_region_gets_its_own_range_once_enough_partners_work_there()
    {
        var local = GivenPrices(cityId: _data.Masis.Id, prices: [(10000, null), (12000, null), (14000, null), (16000, null)]);
        GivenAcceptedOffer(18000, PartnerTestData.Now.AddDays(-5), cityId: _data.Masis.Id); // a deal in Ararat
        GivenPrices(prices: [(20000, null)]); // works in Yerevan only

        await RecalculateAsync();

        _item.MarketPartnerCount.ShouldBe(6);
        _item.Market.ShouldBe(new PriceRange(13000, 16000, 18000));
        var ararat = _data.Db.RegionalMarketPrices.Single();
        (ararat.WorkItemId, ararat.RegionId, ararat.PartnerCount).ShouldBe((_item.Id, _data.Ararat.Id, 5));
        ararat.Range.ShouldBe(new PriceRange(12500, 15000, 17500));

        var items = await new GetWorkItemsHandler(_data.Db, new FakeCurrentLanguage("hy"))
            .HandleAsync(new GetWorkItems(Region: "ARARAT"), CancellationToken.None);
        items.Single(i => i.Id == _item.Id).ShouldSatisfyAllConditions(
            i => i.PriceTypical.ShouldBe(15000),
            i => i.PartnerCount.ShouldBe(5),
            i => i.Regional.ShouldBeTrue());
        var national = await new GetWorkItemsHandler(_data.Db, new FakeCurrentLanguage("hy"))
            .HandleAsync(new GetWorkItems(Region: "nowhere"), CancellationToken.None);
        national.Single(i => i.Id == _item.Id).ShouldSatisfyAllConditions(i => i.PriceTypical.ShouldBe(16000), i => i.Regional.ShouldBeFalse());

        var priced = await new MeasureEstimateHandler(_data.Db, new FakeCurrentLanguage("hy")).HandleAsync(
            new MeasureEstimate([new RoomInput(RoomType.Kitchen, null, null, 10m, null, null, [new LineInput(_item.Id, 1m)])], CityId: _data.Masis.Id),
            CancellationToken.None);
        priced.TotalTypical.ShouldBe(15000);

        // Too few partners left there: the region falls back to the national range.
        _data.Db.PartnerPrices.Remove(_data.Db.PartnerPrices.Single(p => p.PartnerProfileId == local[0].Id));
        _data.Db.SaveChanges();
        await RecalculateAsync();
        _data.Db.RegionalMarketPrices.ShouldBeEmpty();
    }
}
