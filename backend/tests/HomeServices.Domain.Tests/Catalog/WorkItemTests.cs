using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;

namespace HomeServices.Domain.Tests.Catalog;

public class WorkItemTests
{
    private static readonly LocalizedText Plastering = LocalizedText.Empty.With("hy", "Պատերի սվաղում").With("en", "Wall plastering");

    [Fact]
    public void A_new_work_item_is_active_with_its_details()
    {
        var categoryId = Guid.NewGuid();

        var item = WorkItem.Create(categoryId, " Wall-Plastering ", Plastering, WorkUnit.SquareMeter, WorkSurface.Wall, sortOrder: 2);

        item.CategoryId.ShouldBe(categoryId);
        item.Slug.ShouldBe("wall-plastering");
        item.Name.ShouldBe(Plastering);
        item.Unit.ShouldBe(WorkUnit.SquareMeter);
        item.Surface.ShouldBe(WorkSurface.Wall);
        item.SortOrder.ShouldBe(2);
        item.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Update_changes_every_field()
    {
        var item = WorkItem.Create(Guid.NewGuid(), "wall-plastering", Plastering, WorkUnit.SquareMeter, WorkSurface.Wall, 1);
        var otherCategory = Guid.NewGuid();
        var name = LocalizedText.Empty.With("hy", "Վարդակի տեղադրում");

        item.Update(otherCategory, "socket-installation", name, WorkUnit.Point, WorkSurface.None, 7);

        item.CategoryId.ShouldBe(otherCategory);
        item.Slug.ShouldBe("socket-installation");
        item.Name.ShouldBe(name);
        item.Unit.ShouldBe(WorkUnit.Point);
        item.Surface.ShouldBe(WorkSurface.None);
        item.SortOrder.ShouldBe(7);
    }

    [Fact]
    public void Can_be_hidden_and_shown_again()
    {
        var item = WorkItem.Create(Guid.NewGuid(), "wall-plastering", Plastering, WorkUnit.SquareMeter, WorkSurface.Wall, 1);

        item.Deactivate();
        item.IsActive.ShouldBeFalse();

        item.Activate();
        item.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Slug_name_unit_and_surface_must_be_valid()
    {
        var category = Guid.NewGuid();

        Should.Throw<DomainException>(() => WorkItem.Create(category, "bad slug", Plastering, WorkUnit.SquareMeter, WorkSurface.Wall, 1))
            .Code.ShouldBe("work_item.slug_invalid");
        Should.Throw<DomainException>(() => WorkItem.Create(category, "plastering", LocalizedText.Empty, WorkUnit.SquareMeter, WorkSurface.Wall, 1))
            .Code.ShouldBe("work_item.name_required");
        Should.Throw<DomainException>(() => WorkItem.Create(category, "plastering", Plastering, (WorkUnit)99, WorkSurface.Wall, 1))
            .Code.ShouldBe("work_item.unit_invalid");
        Should.Throw<DomainException>(() => WorkItem.Create(category, "plastering", Plastering, WorkUnit.SquareMeter, (WorkSurface)99, 1))
            .Code.ShouldBe("work_item.surface_invalid");
    }

    [Fact]
    public void A_new_work_item_has_no_prices_until_staff_set_them()
    {
        var item = WorkItem.Create(Guid.NewGuid(), "wall-plastering", Plastering, WorkUnit.SquareMeter, WorkSurface.Wall, 1);
        item.Price.ShouldBeNull();

        item.SetPrice(new PriceRange(2500, 3500, 5000));

        item.Price.ShouldBe(new PriceRange(2500, 3500, 5000));
        (item.PriceMin, item.PriceTypical, item.PriceMax).ShouldBe((2500, 3500, 5000));

        item.SetPrice(null);
        item.Price.ShouldBeNull();
        item.PriceTypical.ShouldBeNull();
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(3000, 2000, 4000)]
    [InlineData(1000, 5000, 4000)]
    [InlineData(0, 0, PriceRange.Limit + 1)]
    public void Prices_must_be_in_order_and_within_the_limit(int min, int typical, int max)
    {
        var item = WorkItem.Create(Guid.NewGuid(), "wall-plastering", Plastering, WorkUnit.SquareMeter, WorkSurface.Wall, 1);

        new PriceRange(min, typical, max).IsValid.ShouldBeFalse();
        Should.Throw<DomainException>(() => item.SetPrice(new PriceRange(min, typical, max))).Code.ShouldBe("price.invalid");
        item.Price.ShouldBeNull();
    }

    [Fact]
    public void Equal_prices_are_a_valid_range()
    {
        new PriceRange(0, 0, 0).IsValid.ShouldBeTrue();
        new PriceRange(5000, 5000, 5000).IsValid.ShouldBeTrue();
    }
}
