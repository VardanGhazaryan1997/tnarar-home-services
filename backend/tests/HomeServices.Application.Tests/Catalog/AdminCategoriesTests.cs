using HomeServices.Application.Catalog;
using HomeServices.Application.Errors;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;

namespace HomeServices.Application.Tests.Catalog;

public class AdminCategoriesTests
{
    private static readonly Dictionary<string, string> PlumbingName = new() { ["hy"] = "Սանտեխնիկա", ["en"] = "Plumbing" };

    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();

    private static LocalizedText Text(string hy) => LocalizedText.Empty.With("hy", hy);

    private async Task<Category> Given(string slug, Guid? parentId = null, bool active = true)
    {
        var category = Category.Create(slug, Text(slug), 1, parentId);
        if (!active)
        {
            category.Deactivate();
        }

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    private Task<AdminCategoryDto> Create(string slug, Guid? parentId = null, string? icon = null, int sortOrder = 1) =>
        new CreateCategoryHandler(_db).HandleAsync(new CreateCategory(slug, PlumbingName, icon, parentId, sortOrder), CancellationToken.None);

    private Task<AdminCategoryDto> Update(Guid id, string slug, Guid? parentId = null, string? icon = null, int sortOrder = 1) =>
        new UpdateCategoryHandler(_db).HandleAsync(new UpdateCategory(id, slug, PlumbingName, icon, parentId, sortOrder), CancellationToken.None);

    [Fact]
    public async Task The_admin_tree_includes_inactive_categories_with_every_translation()
    {
        var renovation = await Given("renovation");
        await Given("tiling", renovation.Id, active: false);
        await Given("cleaning");

        var tree = await new GetAdminCategoriesHandler(_db).HandleAsync(new GetAdminCategories(), CancellationToken.None);

        tree.Select(c => c.Slug).ShouldBe(new[] { "cleaning", "renovation" });
        var tiling = tree.Single(c => c.Slug == "renovation").Children.ShouldHaveSingleItem();
        tiling.Slug.ShouldBe("tiling");
        tiling.IsActive.ShouldBeFalse();
        tiling.ParentId.ShouldBe(renovation.Id);
        tiling.Name.ShouldBe(new Dictionary<string, string> { ["hy"] = "tiling" });
    }

    [Fact]
    public async Task Creates_a_category_with_its_translations()
    {
        var created = await Create(" Plumbing ", icon: "pipe", sortOrder: 4);

        created.Slug.ShouldBe("plumbing");
        created.Name.ShouldBe(PlumbingName);
        created.Icon.ShouldBe("pipe");
        created.SortOrder.ShouldBe(4);
        created.IsActive.ShouldBeTrue();
        _db.Categories.Single(c => c.Id == created.Id).Name.Get("en", "hy").ShouldBe("Plumbing");
    }

    [Fact]
    public async Task Slugs_must_be_unique()
    {
        await Given("plumbing");

        (await Should.ThrowAsync<ConflictException>(() => Create("PLUMBING"))).Code.ShouldBe("category.slug_taken");
    }

    [Fact]
    public async Task Creates_a_subcategory_under_a_top_level_category()
    {
        var renovation = await Given("renovation");

        var created = await Create("tiling", renovation.Id);

        created.ParentId.ShouldBe(renovation.Id);
    }

    [Fact]
    public async Task The_parent_must_exist()
    {
        (await Should.ThrowAsync<DomainException>(() => Create("tiling", Guid.NewGuid()))).Code.ShouldBe("category.parent_not_found");
    }

    [Fact]
    public async Task Categories_have_only_two_levels()
    {
        var renovation = await Given("renovation");
        var tiling = await Given("tiling", renovation.Id);

        (await Should.ThrowAsync<DomainException>(() => Create("floor-tiling", tiling.Id))).Code.ShouldBe("category.too_deep");
    }

    [Fact]
    public async Task Updates_slug_name_icon_position_and_parent()
    {
        var renovation = await Given("renovation");
        var tiling = await Given("tiles");

        var updated = await Update(tiling.Id, "tiling", renovation.Id, icon: "tile", sortOrder: 3);

        updated.Slug.ShouldBe("tiling");
        updated.Name.ShouldBe(PlumbingName);
        updated.Icon.ShouldBe("tile");
        updated.SortOrder.ShouldBe(3);
        updated.ParentId.ShouldBe(renovation.Id);
    }

    [Fact]
    public async Task Keeping_its_own_slug_is_not_a_conflict()
    {
        var plumbing = await Given("plumbing");

        (await Update(plumbing.Id, "plumbing")).Slug.ShouldBe("plumbing");
    }

    [Fact]
    public async Task Cannot_take_another_categorys_slug()
    {
        await Given("plumbing");
        var heating = await Given("heating");

        (await Should.ThrowAsync<ConflictException>(() => Update(heating.Id, "plumbing"))).Code.ShouldBe("category.slug_taken");
    }

    [Fact]
    public async Task Cannot_become_its_own_parent()
    {
        var plumbing = await Given("plumbing");

        (await Should.ThrowAsync<DomainException>(() => Update(plumbing.Id, "plumbing", plumbing.Id))).Code.ShouldBe("category.parent_invalid");
    }

    [Fact]
    public async Task A_category_with_subcategories_cannot_become_a_subcategory()
    {
        var renovation = await Given("renovation");
        await Given("tiling", renovation.Id);
        var construction = await Given("construction");

        (await Should.ThrowAsync<DomainException>(() => Update(renovation.Id, "renovation", construction.Id)))
            .Code.ShouldBe("category.has_subcategories");
    }

    [Fact]
    public async Task Can_move_a_subcategory_back_to_the_top_level()
    {
        var renovation = await Given("renovation");
        var tiling = await Given("tiling", renovation.Id);

        (await Update(tiling.Id, "tiling", parentId: null)).ParentId.ShouldBeNull();
    }

    [Fact]
    public async Task Unknown_categories_are_not_found()
    {
        (await Should.ThrowAsync<NotFoundException>(() => Update(Guid.NewGuid(), "x"))).Code.ShouldBe("category.not_found");
    }

    [Fact]
    public async Task Can_be_hidden_and_shown_again()
    {
        var plumbing = await Given("plumbing");
        var handler = new SetCategoryActiveHandler(_db);

        (await handler.HandleAsync(new SetCategoryActive(plumbing.Id, false), CancellationToken.None)).IsActive.ShouldBeFalse();
        (await handler.HandleAsync(new SetCategoryActive(plumbing.Id, true), CancellationToken.None)).IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Deletes_a_category_without_subcategories()
    {
        var plumbing = await Given("plumbing");

        (await new DeleteCategoryHandler(_db).HandleAsync(new DeleteCategory(plumbing.Id), CancellationToken.None)).ShouldBeTrue();

        _db.Categories.Any(c => c.Id == plumbing.Id).ShouldBeFalse();
    }

    [Fact]
    public async Task A_category_with_subcategories_cannot_be_deleted()
    {
        var renovation = await Given("renovation");
        await Given("tiling", renovation.Id);

        (await Should.ThrowAsync<DomainException>(() =>
                new DeleteCategoryHandler(_db).HandleAsync(new DeleteCategory(renovation.Id), CancellationToken.None)))
            .Code.ShouldBe("category.has_subcategories");
    }
}
