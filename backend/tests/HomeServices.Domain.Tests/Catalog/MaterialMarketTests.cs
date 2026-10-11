using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;

namespace HomeServices.Domain.Tests.Catalog;

public class MaterialMarketTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 0, 0, 0, TimeSpan.Zero);

    private static Material Putty() =>
        Material.Create("putty", LocalizedText.Empty.With("en", "Putty"), MaterialUnit.Kilogram, 100, 200, 300, 1, Now);

    [Fact]
    public void Without_store_prices_the_staff_prices_count()
    {
        var putty = Putty();

        putty.PriceFor(MaterialGrade.Standard).ShouldBe(200);
        putty.PriceSourceFor(MaterialGrade.Standard).ShouldBe(MaterialPriceSource.Staff);
        (putty.MarketStandard, putty.MarketStoreCount, putty.MarketUpdatedAt, putty.IsPriceLocked).ShouldBe((null, 0, null, false));
    }

    [Fact]
    public void Store_prices_replace_staff_prices_per_grade()
    {
        var putty = Putty();

        putty.UpdateMarket(null, 250, 400, 3, Now);

        putty.PriceFor(MaterialGrade.Budget).ShouldBe(100); // no store price for this grade
        putty.PriceSourceFor(MaterialGrade.Budget).ShouldBe(MaterialPriceSource.Staff);
        putty.PriceFor(MaterialGrade.Standard).ShouldBe(250);
        putty.PriceSourceFor(MaterialGrade.Standard).ShouldBe(MaterialPriceSource.Stores);
        putty.PriceFor(MaterialGrade.Premium).ShouldBe(400);
        putty.StaffPriceFor(MaterialGrade.Standard).ShouldBe(200);
        putty.MarketPriceFor(MaterialGrade.Standard).ShouldBe(250);
        (putty.MarketStoreCount, putty.MarketUpdatedAt).ShouldBe((3, Now));
    }

    [Fact]
    public void A_locked_material_keeps_its_staff_prices_but_still_shows_store_prices()
    {
        var putty = Putty();
        putty.UpdateMarket(150, 250, 400, 3, Now);

        putty.LockPrice(true);

        putty.PriceFor(MaterialGrade.Standard).ShouldBe(200);
        putty.PriceSourceFor(MaterialGrade.Standard).ShouldBe(MaterialPriceSource.Staff);
        putty.MarketPriceFor(MaterialGrade.Standard).ShouldBe(250);

        putty.LockPrice(false);
        putty.PriceFor(MaterialGrade.Standard).ShouldBe(250);
    }

    [Fact]
    public void Store_prices_can_be_cleared()
    {
        var putty = Putty();
        putty.UpdateMarket(150, 250, 400, 3, Now);

        putty.UpdateMarket(null, null, null, 1, Now.AddDays(1));

        putty.PriceFor(MaterialGrade.Standard).ShouldBe(200);
        (putty.MarketStoreCount, putty.MarketUpdatedAt).ShouldBe((1, Now.AddDays(1)));
    }

    [Theory]
    [InlineData(0, null, null, 1)]
    [InlineData(null, -1, null, 1)]
    [InlineData(null, null, Material.PriceLimit + 1, 1)]
    [InlineData(null, null, null, -1)]
    public void Store_prices_are_within_the_price_limit(int? budget, int? standard, int? premium, int storeCount) =>
        Should.Throw<DomainException>(() => Putty().UpdateMarket(budget, standard, premium, storeCount, Now)).Code.ShouldBe("material.price_invalid");

    [Fact]
    public void Changing_store_prices_doesnt_count_as_staff_checking_prices()
    {
        var putty = Putty();

        putty.UpdateMarket(150, 250, 400, 3, Now.AddDays(100));

        putty.PricesCheckedAt.ShouldBe(Now);
    }
}
