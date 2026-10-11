using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;

namespace HomeServices.Domain.Tests.Catalog;

public class MaterialTests
{
    private static readonly LocalizedText Name = LocalizedText.Empty.With("hy", "Ներկ").With("en", "Paint");

    [Fact]
    public void A_material_has_a_price_per_grade()
    {
        var paint = Material.Create(" Interior-Paint ", Name, MaterialUnit.Liter, 1200, 2500, 5000, 3);

        (paint.Slug, paint.Unit, paint.IsActive).ShouldBe(("interior-paint", MaterialUnit.Liter, true));
        paint.PriceFor(MaterialGrade.Budget).ShouldBe(1200);
        paint.PriceFor(MaterialGrade.Standard).ShouldBe(2500);
        paint.PriceFor(MaterialGrade.Premium).ShouldBe(5000);
        paint.Deactivate();
        paint.IsActive.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0, 1, 2)]
    [InlineData(3, 2, 4)]
    [InlineData(1, 3, 2)]
    [InlineData(1, 1, Material.PriceLimit + 1)]
    public void Prices_go_up_with_the_grade(int budget, int standard, int premium) =>
        Should.Throw<DomainException>(() => Material.Create("paint", Name, MaterialUnit.Liter, budget, standard, premium, 1))
            .Code.ShouldBe("material.price_invalid");

    [Fact]
    public void A_material_needs_a_name_and_a_known_unit()
    {
        Should.Throw<DomainException>(() => Material.Create("paint", LocalizedText.Empty, MaterialUnit.Liter, 1, 1, 1, 1)).Code.ShouldBe("material.name_required");
        Should.Throw<DomainException>(() => Material.Create("paint", Name, (MaterialUnit)99, 1, 1, 1, 1)).Code.ShouldBe("material.unit_invalid");
    }

    [Fact]
    public void Work_uses_an_amount_per_unit_plus_waste()
    {
        var link = WorkItemMaterial.Create(Guid.NewGuid(), Guid.NewGuid(), 1.2m, 10, 1);

        link.AmountFor(10m).ShouldBe(13.2m);
        Should.Throw<DomainException>(() => WorkItemMaterial.Create(Guid.NewGuid(), Guid.NewGuid(), 0m, 10, 1)).Code.ShouldBe("work_item_material.invalid");
        Should.Throw<DomainException>(() => WorkItemMaterial.Create(Guid.NewGuid(), Guid.NewGuid(), 1m, -1, 1)).Code.ShouldBe("work_item_material.invalid");
    }
}
