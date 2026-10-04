using HomeServices.Application.Catalog;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;

namespace HomeServices.Application.Tests.Catalog;

public class GetCategoriesTests
{
    private static LocalizedText Text(string hy, string? ru = null)
    {
        var text = LocalizedText.Empty.With("hy", hy);
        return ru is null ? text : text.With("ru", ru);
    }

    [Fact]
    public async Task Returns_active_categories_as_a_tree_in_display_order()
    {
        await using var db = InMemoryAppDbContext.Create();
        var repair = Category.Create("renovation", Text("Վերանորոգում"), 2);
        var plumbing = Category.Create("plumbing", Text("Սանտեխնիկա"), 1, icon: "pipe");
        var tiling = Category.Create("tiling", Text("Սալիկապատում"), 2, parentId: repair.Id);
        var painting = Category.Create("painting", Text("Ներկում"), 1, parentId: repair.Id);
        db.Categories.AddRange(repair, plumbing, tiling, painting);
        await db.SaveChangesAsync();

        var result = await Handle(db, "hy");

        result.Select(c => c.Slug).ShouldBe(new[] { "plumbing", "renovation" });
        result[0].Icon.ShouldBe("pipe");
        result[0].Children.ShouldBeEmpty();
        result[1].Children.Select(c => c.Slug).ShouldBe(new[] { "painting", "tiling" });
    }

    [Fact]
    public async Task Names_are_in_the_request_language_with_fallback_to_the_default()
    {
        await using var db = InMemoryAppDbContext.Create();
        db.Categories.AddRange(
            Category.Create("plumbing", Text("Սանտեխնիկա", ru: "Сантехника"), 1),
            Category.Create("cleaning", Text("Մաքրում"), 2));
        await db.SaveChangesAsync();

        var result = await Handle(db, "ru");

        result.Single(c => c.Slug == "plumbing").Name.ShouldBe("Сантехника");
        result.Single(c => c.Slug == "cleaning").Name.ShouldBe("Մաքրում");
    }

    [Fact]
    public async Task Inactive_categories_and_their_children_are_hidden()
    {
        await using var db = InMemoryAppDbContext.Create();
        var hidden = Category.Create("hidden", Text("x"), 1);
        hidden.Deactivate();
        var childOfHidden = Category.Create("child", Text("y"), 1, parentId: hidden.Id);
        var visible = Category.Create("visible", Text("z"), 2);
        db.Categories.AddRange(hidden, childOfHidden, visible);
        await db.SaveChangesAsync();

        var result = await Handle(db, "hy");

        result.Select(c => c.Slug).ShouldBe(new[] { "visible" });
    }

    private static Task<IReadOnlyList<CategoryDto>> Handle(InMemoryAppDbContext db, string language) =>
        new GetCategoriesHandler(db, new FakeCurrentLanguage(language)).HandleAsync(new GetCategories(), CancellationToken.None);
}
