using HomeServices.Domain.Catalog;
using HomeServices.Domain.Stores;

namespace HomeServices.Domain.Tests.Stores;

public class StoreProductTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 11);

    private static StoreProduct Putty(int price = 5000, int? sale = null, DateOnly? saleEnds = null, decimal packSize = 25m) =>
        StoreProduct.Create(Guid.NewGuid(), " Putty Finish ", " Knauf ", " SKU-1 ", packSize, "25 kg bag", price, sale, saleEnds, true, null, Now);

    [Fact]
    public void A_product_is_created_active_and_not_linked()
    {
        var product = Putty();

        (product.Name, product.Brand, product.Code, product.PackLabel, product.IsActive, product.InStock).ShouldBe(("Putty Finish", "Knauf", "SKU-1", "25 kg bag", true, true));
        (product.MaterialId, product.Grade).ShouldBe((null, null));
        product.PriceUpdatedAt.ShouldBe(Now);
    }

    [Fact]
    public void Changing_only_the_price_keeps_everything_else_and_counts_as_checked_when_it_changed()
    {
        var product = Putty();
        var later = Now.AddDays(3);

        product.ChangePrice(5000, null, null, false, later);
        (product.InStock, product.PriceUpdatedAt).ShouldBe((false, Now));

        product.ChangePrice(4500, 4000, Today.AddDays(7), true, later);
        (product.Price, product.SalePrice, product.SaleEndsOn, product.InStock, product.PriceUpdatedAt)
            .ShouldBe((4500, (int?)4000, (DateOnly?)Today.AddDays(7), true, later));
        (product.Name, product.PackSize).ShouldBe(("Putty Finish", 25m));
    }

    [Theory]
    [InlineData(0, null, false, "store_product.invalid")]
    [InlineData(Material.PriceLimit + 1, null, false, "store_product.invalid")]
    [InlineData(5000, 5000, true, "store_product.sale_invalid")]
    [InlineData(5000, 4000, false, "store_product.sale_invalid")]
    public void Quick_price_changes_follow_the_same_rules(int price, int? sale, bool withEnd, string code)
    {
        var product = Putty();

        Should.Throw<DomainException>(() => product.ChangePrice(price, sale, withEnd ? Today : null, false, Now.AddDays(1))).Code.ShouldBe(code);
        (product.Price, product.SalePrice, product.InStock, product.PriceUpdatedAt).ShouldBe((5000, (int?)null, true, Now));
    }

    [Fact]
    public void Blank_optional_fields_become_empty()
    {
        var product = StoreProduct.Create(Guid.NewGuid(), "Putty", " ", "", 1m, "", 100, null, null, true, null, Now);

        (product.Brand, product.Code, product.PackLabel).ShouldBe((null, null, null));
    }

    [Theory]
    [InlineData("", 1, 100)]
    [InlineData("Putty", 0, 100)]
    [InlineData("Putty", -5, 100)]
    [InlineData("Putty", 1, 0)]
    [InlineData("Putty", 1, Material.PriceLimit + 1)]
    public void A_product_needs_a_name_pack_size_and_price(string name, int packSize, int price) =>
        Should.Throw<DomainException>(() => StoreProduct.Create(Guid.NewGuid(), name, null, null, packSize, null, price, null, null, true, null, Now))
            .Code.ShouldBe("store_product.invalid");

    [Fact]
    public void Text_fields_have_length_limits()
    {
        Should.Throw<DomainException>(() => StoreProduct.Create(Guid.NewGuid(), new string('x', StoreProduct.MaxNameLength + 1), null, null, 1m, null, 100, null, null, true, null, Now)).Code.ShouldBe("store_product.invalid");
        Should.Throw<DomainException>(() => StoreProduct.Create(Guid.NewGuid(), "Putty", new string('x', StoreProduct.MaxBrandLength + 1), null, 1m, null, 100, null, null, true, null, Now)).Code.ShouldBe("store_product.invalid");
        Should.Throw<DomainException>(() => StoreProduct.Create(Guid.NewGuid(), "Putty", null, new string('x', StoreProduct.MaxCodeLength + 1), 1m, null, 100, null, null, true, null, Now)).Code.ShouldBe("store_product.invalid");
        Should.Throw<DomainException>(() => StoreProduct.Create(Guid.NewGuid(), "Putty", null, null, 1m, new string('x', StoreProduct.MaxPackLabelLength + 1), 100, null, null, true, null, Now)).Code.ShouldBe("store_product.invalid");
    }

    [Fact]
    public void A_sale_needs_a_lower_price_and_an_end_date()
    {
        Should.Throw<DomainException>(() => Putty(5000, 5000, Today)).Code.ShouldBe("store_product.sale_invalid");
        Should.Throw<DomainException>(() => Putty(5000, 6000, Today)).Code.ShouldBe("store_product.sale_invalid");
        Should.Throw<DomainException>(() => Putty(5000, 0, Today)).Code.ShouldBe("store_product.sale_invalid");
        Should.Throw<DomainException>(() => Putty(5000, 4000, null)).Code.ShouldBe("store_product.sale_invalid");
        Should.Throw<DomainException>(() => Putty(5000, null, Today)).Code.ShouldBe("store_product.sale_invalid");
        Putty(5000, 4000, Today).SalePrice.ShouldBe(4000);
    }

    [Fact]
    public void The_sale_price_counts_up_to_and_including_its_last_day()
    {
        var product = Putty(5000, 4000, Today);

        product.CurrentPrice(Today.AddDays(-5)).ShouldBe(4000);
        product.CurrentPrice(Today).ShouldBe(4000);
        product.CurrentPrice(Today.AddDays(1)).ShouldBe(5000);
        Putty(5000).CurrentPrice(Today).ShouldBe(5000);
    }

    [Fact]
    public void The_unit_price_is_the_pack_price_over_the_pack_size()
    {
        var product = Putty(5000, 4000, Today, 25m);

        product.UnitPrice(Today).ShouldBe(160m);
        product.UnitPrice(Today.AddDays(1)).ShouldBe(200m);
        Putty(1000, packSize: 3m).UnitPrice(Today).ShouldBe(1000m / 3m);
    }

    [Fact]
    public void A_product_links_to_a_material_with_a_grade()
    {
        var product = Putty();
        var material = Guid.NewGuid();

        product.LinkTo(material, MaterialGrade.Premium);
        (product.MaterialId, product.Grade).ShouldBe((material, MaterialGrade.Premium));

        product.Unlink();
        (product.MaterialId, product.Grade).ShouldBe((null, null));
    }

    [Fact]
    public void Linking_needs_a_known_grade()
    {
        var product = Putty();

        Should.Throw<DomainException>(() => product.LinkTo(Guid.NewGuid(), null)).Code.ShouldBe("store_product.grade_required");
        Should.Throw<DomainException>(() => product.LinkTo(Guid.NewGuid(), (MaterialGrade)99)).Code.ShouldBe("store_product.invalid");
        Should.Throw<DomainException>(() => product.LinkTo(Guid.Empty, MaterialGrade.Budget)).Code.ShouldBe("store_product.invalid");
        product.MaterialId.ShouldBeNull();
    }

    [Fact]
    public void The_price_counts_as_checked_when_it_changes_or_is_confirmed()
    {
        var product = Putty();
        var later = Now.AddDays(10);

        product.Update("Putty", "Knauf", "SKU-1", 25m, "25 kg bag", 5000, null, null, false, null, later); // only stock changed
        product.PriceUpdatedAt.ShouldBe(Now);

        product.Update("Putty", "Knauf", "SKU-1", 25m, "25 kg bag", 5200, null, null, false, null, later);
        product.PriceUpdatedAt.ShouldBe(later);

        product.Update("Putty", "Knauf", "SKU-1", 25m, "25 kg bag", 5200, 4800, Today, false, null, later.AddDays(1));
        product.PriceUpdatedAt.ShouldBe(later.AddDays(1));

        product.ConfirmPrice(later.AddDays(20));
        product.PriceUpdatedAt.ShouldBe(later.AddDays(20));
    }

    [Fact]
    public void A_price_goes_stale_after_the_fresh_period()
    {
        var product = Putty();
        var freshFor = TimeSpan.FromDays(90);

        product.IsStale(Now.AddDays(90), freshFor).ShouldBeFalse();
        product.IsStale(Now.AddDays(91), freshFor).ShouldBeTrue();
    }

    [Fact]
    public void A_product_can_be_hidden()
    {
        var product = Putty();

        product.Deactivate();
        product.IsActive.ShouldBeFalse();
        product.Activate();
        product.IsActive.ShouldBeTrue();
    }
}
