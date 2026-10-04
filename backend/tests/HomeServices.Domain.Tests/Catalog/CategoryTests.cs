using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;

namespace HomeServices.Domain.Tests.Catalog;

public class CategoryTests
{
    private static readonly LocalizedText Plumbing = LocalizedText.Empty.With("hy", "Սանտեխնիկա").With("en", "Plumbing");

    [Fact]
    public void A_new_category_is_active_with_its_details()
    {
        var parentId = Guid.NewGuid();

        var category = Category.Create(" Plumbing ", Plumbing, sortOrder: 3, parentId, icon: "pipe");

        category.Slug.ShouldBe("plumbing");
        category.Name.ShouldBe(Plumbing);
        category.SortOrder.ShouldBe(3);
        category.ParentId.ShouldBe(parentId);
        category.Icon.ShouldBe("pipe");
        category.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Top_level_categories_have_no_parent_and_icon_is_optional()
    {
        var category = Category.Create("plumbing", Plumbing, 1);

        category.ParentId.ShouldBeNull();
        category.Icon.ShouldBeNull();
    }

    [Fact]
    public void Slug_must_be_valid_and_name_must_not_be_empty()
    {
        Should.Throw<DomainException>(() => Category.Create("bad slug", Plumbing, 1)).Code.ShouldBe("category.slug_invalid");
        Should.Throw<DomainException>(() => Category.Create("plumbing", LocalizedText.Empty, 1)).Code.ShouldBe("category.name_required");
    }

    [Fact]
    public void Can_be_renamed_but_not_to_an_empty_name()
    {
        var category = Category.Create("plumbing", Plumbing, 1);
        var renamed = Plumbing.With("ru", "Сантехника");

        category.Rename(renamed);

        category.Name.ShouldBe(renamed);
        Should.Throw<DomainException>(() => category.Rename(LocalizedText.Empty)).Code.ShouldBe("category.name_required");
    }

    [Fact]
    public void Can_be_deactivated_and_reactivated()
    {
        var category = Category.Create("plumbing", Plumbing, 1);

        category.Deactivate();
        category.IsActive.ShouldBeFalse();

        category.Activate();
        category.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Slug_can_be_changed_to_another_valid_slug()
    {
        var category = Category.Create("plumbing", Plumbing, 1);

        category.ChangeSlug(" Water-Supply ");

        category.Slug.ShouldBe("water-supply");
        Should.Throw<DomainException>(() => category.ChangeSlug("no spaces")).Code.ShouldBe("category.slug_invalid");
    }

    [Fact]
    public void Icon_is_trimmed_and_blank_clears_it()
    {
        var category = Category.Create("plumbing", Plumbing, 1, icon: " pipe ");
        category.Icon.ShouldBe("pipe");

        category.ChangeIcon("  ");

        category.Icon.ShouldBeNull();
    }

    [Fact]
    public void Icon_key_has_a_maximum_length()
    {
        Should.Throw<DomainException>(() => Category.Create("plumbing", Plumbing, 1, icon: new string('x', Category.IconMaxLength + 1)))
            .Code.ShouldBe("category.icon_too_long");
    }

    [Fact]
    public void Can_move_under_another_category_or_to_the_top_level()
    {
        var category = Category.Create("tiling", Plumbing, 1);
        var parentId = Guid.NewGuid();

        category.MoveTo(parentId);
        category.ParentId.ShouldBe(parentId);

        category.MoveTo(null);
        category.ParentId.ShouldBeNull();
    }

    [Fact]
    public void Cannot_be_its_own_parent()
    {
        var category = Category.Create("tiling", Plumbing, 1);

        Should.Throw<DomainException>(() => category.MoveTo(category.Id)).Code.ShouldBe("category.parent_invalid");
    }

    [Fact]
    public void Can_be_reordered()
    {
        var category = Category.Create("tiling", Plumbing, 1);

        category.Reorder(7);

        category.SortOrder.ShouldBe(7);
    }
}
