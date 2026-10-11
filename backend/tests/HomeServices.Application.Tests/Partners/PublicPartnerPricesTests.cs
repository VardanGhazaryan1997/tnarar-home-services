using HomeServices.Application.Catalog;
using HomeServices.Application.Errors;
using HomeServices.Application.Partners;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Partners;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Partners;

public class PublicPartnerPricesTests
{
    private readonly PartnerTestData _data = new();
    private readonly WorkItem _leaks;
    private readonly WorkItem _taps;
    private readonly WorkItem _pipes;
    private readonly WorkItem _unpriced;
    private readonly WorkItem _windows;

    public PublicPartnerPricesTests()
    {
        var plumbingWork = Category.Create("plumbing-work", PartnerTestData.Text("Սանտեխնիկական աշխատանքներ"), 1, _data.Plumbing.Id);
        var cleaning = Category.Create("cleaning", PartnerTestData.Text("Մաքրում"), 9);
        var windows = Category.Create("windows", PartnerTestData.Text("Պատուհաններ"), 1, cleaning.Id);
        _data.Db.Categories.AddRange(plumbingWork, cleaning, windows);
        _leaks = Item(plumbingWork, "leak-repair", 1, new PriceRange(10_000, 15_000, 20_000));
        _taps = Item(plumbingWork, "tap-installation", 2, new PriceRange(5_000, 7_000, 9_000));
        _pipes = Item(plumbingWork, "pipe-replacement", 3, new PriceRange(3_000, 4_000, 5_000));
        _unpriced = Item(plumbingWork, "drain-cleaning", 4, new PriceRange(1_000, 2_000, 3_000));
        _windows = Item(windows, "window-cleaning", 1, new PriceRange(1_000, 2_000, 3_000));
        _data.Db.SaveChanges();
    }

    private WorkItem Item(Category category, string slug, int order, PriceRange price)
    {
        var item = WorkItem.Create(category.Id, slug, PartnerTestData.Text(slug), WorkUnit.Piece, WorkSurface.None, order);
        item.SetPrice(price);
        _data.Db.WorkItems.Add(item);
        return item;
    }

    private Task<IReadOnlyList<PublicPriceDto>> PricesAsync(string slug) =>
        new GetPublicPartnerPricesHandler(_data.Db, new FakeCurrentLanguage("hy"), Options.Create(new PricingSettings()), _data.Clock).HandleAsync(new GetPublicPartnerPrices(slug), CancellationToken.None);

    [Fact]
    public async Task Visitors_see_a_partners_prices_next_to_the_market()
    {
        var partner = _data.GivenProfile("Aram", PartnerStatus.Approved);
        _data.Db.PartnerPrices.AddRange(
            PartnerPrice.Create(partner.Id, _leaks.Id, 8_000, null, false, PartnerTestData.Now),
            PartnerPrice.Create(partner.Id, _taps.Id, 6_000, 8_000, false, PartnerTestData.Now),
            PartnerPrice.Create(partner.Id, _pipes.Id, 9_000, null, true, PartnerTestData.Now),
            PartnerPrice.Create(partner.Id, _windows.Id, 1_000, null, false, PartnerTestData.Now), // not a service they offer
            PartnerPrice.Create(partner.Id, _unpriced.Id, 2_000, null, false, PartnerTestData.Now.AddDays(-91))); // out of date
        await _data.Db.SaveChangesAsync();

        var prices = await PricesAsync(partner.Slug!.ToUpperInvariant());

        prices.Select(p => (p.Name, p.PriceFrom, p.PriceTo, p.Comparison)).ShouldBe(new[]
        {
            ("leak-repair", 8_000, (int?)null, (string?)PriceComparison.Below),
            ("tap-installation", 6_000, (int?)8_000, (string?)PriceComparison.Typical),
            ("pipe-replacement", 9_000, (int?)null, (string?)null),
        });
        prices[0].MarketMin.ShouldBe(10_000);
        prices[0].MainCategoryId.ShouldBe(_data.Plumbing.Id);
    }

    [Fact]
    public async Task Partners_who_arent_public_have_no_prices_page()
    {
        var hidden = _data.GivenProfile("Hidden", PartnerStatus.UnderReview);

        (await Should.ThrowAsync<NotFoundException>(() => PricesAsync(hidden.Slug!))).Code.ShouldBe("partner.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => PricesAsync("nobody"))).Code.ShouldBe("partner.not_found");
    }

    [Theory]
    [InlineData(9_999, null, false, "Below")]
    [InlineData(10_000, null, false, "Typical")]
    [InlineData(18_000, 24_000, false, "Above")]
    [InlineData(1_000, null, true, null)]
    public void Prices_are_compared_with_the_middle_half_of_the_market(int from, int? to, bool materials, string? expected) =>
        PriceComparison.Of(from, to, materials, 10_000, 20_000).ShouldBe(expected);
}
