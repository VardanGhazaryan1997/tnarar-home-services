using HomeServices.Application.Catalog;
using HomeServices.Application.Errors;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;

namespace HomeServices.Application.Tests.Catalog;

public class WorkItemsTests
{
    private static readonly Dictionary<string, string> PlasteringName = new() { ["hy"] = "Պատերի սվաղում", ["en"] = "Wall plastering" };

    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();

    private static LocalizedText Text(string hy, string? en = null)
    {
        var text = LocalizedText.Empty.With("hy", hy);
        return en is null ? text : text.With("en", en);
    }

    private async Task<Category> GivenCategory(string slug, int sortOrder = 1, Category? parent = null, bool active = true)
    {
        var category = Category.Create(slug, Text(slug), sortOrder, parent?.Id);
        if (!active)
        {
            category.Deactivate();
        }

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    private async Task<WorkItem> GivenItem(Category category, string slug, int sortOrder = 1, bool active = true, string? en = null)
    {
        var item = WorkItem.Create(category.Id, slug, Text(slug, en), WorkUnit.SquareMeter, WorkSurface.Wall, sortOrder);
        if (!active)
        {
            item.Deactivate();
        }

        _db.WorkItems.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    private Task<IReadOnlyList<WorkItemDto>> Public(string? category = null, string language = "hy") =>
        new GetWorkItemsHandler(_db, new FakeCurrentLanguage(language)).HandleAsync(new GetWorkItems(category), CancellationToken.None);

    private Task<IReadOnlyList<AdminWorkItemDto>> Admin(Guid? categoryId = null, string? search = null, bool? isActive = null) =>
        new GetAdminWorkItemsHandler(_db).HandleAsync(new GetAdminWorkItems(categoryId, search, isActive), CancellationToken.None);

    private Task<AdminWorkItemDto> Create(Guid categoryId, string slug = "wall-plastering", int sortOrder = 1) =>
        new CreateWorkItemHandler(_db).HandleAsync(
            new CreateWorkItem(categoryId, slug, PlasteringName, WorkUnit.SquareMeter, WorkSurface.Wall, sortOrder),
            CancellationToken.None);

    private Task<AdminWorkItemDto> Update(Guid id, Guid categoryId, string slug = "wall-plastering") =>
        new UpdateWorkItemHandler(_db).HandleAsync(
            new UpdateWorkItem(id, categoryId, slug, PlasteringName, WorkUnit.RunningMeter, WorkSurface.Floor, 3),
            CancellationToken.None);

    [Fact]
    public async Task The_public_list_is_in_catalog_order_with_names_in_the_request_language()
    {
        var renovation = await GivenCategory("renovation", 2);
        var plumbing = await GivenCategory("plumbing", 1);
        var painting = await GivenCategory("painting", 2, renovation);
        var plastering = await GivenCategory("plastering", 1, renovation);
        var leaks = await GivenCategory("leaks", 1, plumbing);
        await GivenItem(painting, "wall-painting", en: "Wall painting");
        await GivenItem(plastering, "ceiling-plastering", 2);
        await GivenItem(plastering, "wall-plastering", 1, en: "Wall plastering");
        await GivenItem(leaks, "leak-repair");

        var items = await Public(language: "en");

        items.Select(i => i.Slug).ShouldBe(new[] { "leak-repair", "wall-plastering", "ceiling-plastering", "wall-painting" });
        items[1].Name.ShouldBe("Wall plastering");
        items[1].CategoryId.ShouldBe(plastering.Id);
        items[1].Unit.ShouldBe(WorkUnit.SquareMeter);
        items[1].Surface.ShouldBe(WorkSurface.Wall);
        items[0].Name.ShouldBe("leak-repair"); // no English name: falls back to the default language
    }

    [Fact]
    public async Task The_public_list_leaves_out_hidden_items_and_items_in_hidden_categories()
    {
        var renovation = await GivenCategory("renovation");
        var hiddenMain = await GivenCategory("roofing", active: false);
        var plastering = await GivenCategory("plastering", 1, renovation);
        var hiddenSub = await GivenCategory("painting", 2, renovation, active: false);
        var underHiddenMain = await GivenCategory("roof-repair", 1, hiddenMain);
        await GivenItem(plastering, "wall-plastering");
        await GivenItem(plastering, "hidden-item", active: false);
        await GivenItem(hiddenSub, "wall-painting");
        await GivenItem(underHiddenMain, "roof-patching");

        (await Public()).Select(i => i.Slug).ShouldBe(new[] { "wall-plastering" });
    }

    [Fact]
    public async Task The_public_list_can_be_narrowed_to_a_subcategory_or_a_main_category()
    {
        var renovation = await GivenCategory("renovation", 1);
        var plumbing = await GivenCategory("plumbing", 2);
        var plastering = await GivenCategory("plastering", 1, renovation);
        var painting = await GivenCategory("painting", 2, renovation);
        var leaks = await GivenCategory("leaks", 1, plumbing);
        await GivenItem(plastering, "wall-plastering");
        await GivenItem(painting, "wall-painting");
        await GivenItem(leaks, "leak-repair");

        (await Public(" Plastering ")).Select(i => i.Slug).ShouldBe(new[] { "wall-plastering" });
        (await Public("renovation")).Select(i => i.Slug).ShouldBe(new[] { "wall-plastering", "wall-painting" });
        (await Public("no-such-category")).ShouldBeEmpty();
    }

    [Fact]
    public async Task The_admin_list_includes_hidden_items_with_every_translation_and_filters()
    {
        var renovation = await GivenCategory("renovation", 1);
        var plumbing = await GivenCategory("plumbing", 2);
        var plastering = await GivenCategory("plastering", 1, renovation);
        var leaks = await GivenCategory("leaks", 1, plumbing);
        var plaster = await GivenItem(plastering, "wall-plastering", en: "Wall plastering");
        var hidden = await GivenItem(plastering, "ceiling-plastering", 2, active: false);
        await GivenItem(leaks, "leak-repair");

        var all = await Admin();
        all.Select(i => i.Slug).ShouldBe(new[] { "wall-plastering", "ceiling-plastering", "leak-repair" });
        all[0].Name.ShouldBe(new Dictionary<string, string> { ["hy"] = "wall-plastering", ["en"] = "Wall plastering" });
        all[1].IsActive.ShouldBeFalse();

        (await Admin(renovation.Id)).Select(i => i.Id).ShouldBe(new[] { plaster.Id, hidden.Id });
        (await Admin(leaks.Id)).Select(i => i.Slug).ShouldBe(new[] { "leak-repair" });
        (await Admin(isActive: false)).Select(i => i.Id).ShouldBe(new[] { hidden.Id });
        (await Admin(search: "WALL PLAST")).Select(i => i.Id).ShouldBe(new[] { plaster.Id });
        (await Admin(search: "leak")).Select(i => i.Slug).ShouldBe(new[] { "leak-repair" });
        (await Admin(Guid.NewGuid())).ShouldBeEmpty();
    }

    [Fact]
    public async Task Creates_a_work_item_under_a_subcategory()
    {
        var renovation = await GivenCategory("renovation");
        var plastering = await GivenCategory("plastering", 1, renovation);

        var created = await Create(plastering.Id, " Wall-Plastering ", sortOrder: 4);

        created.Slug.ShouldBe("wall-plastering");
        created.CategoryId.ShouldBe(plastering.Id);
        created.Name.ShouldBe(PlasteringName);
        created.Unit.ShouldBe(WorkUnit.SquareMeter);
        created.Surface.ShouldBe(WorkSurface.Wall);
        created.SortOrder.ShouldBe(4);
        created.IsActive.ShouldBeTrue();
        _db.WorkItems.Single(w => w.Id == created.Id).Name.Get("en", "hy").ShouldBe("Wall plastering");
    }

    [Fact]
    public async Task Work_items_go_under_an_existing_subcategory()
    {
        var renovation = await GivenCategory("renovation");

        (await Should.ThrowAsync<DomainException>(() => Create(renovation.Id))).Code.ShouldBe("work_item.category_not_subcategory");
        (await Should.ThrowAsync<DomainException>(() => Create(Guid.NewGuid()))).Code.ShouldBe("work_item.category_not_found");
    }

    [Fact]
    public async Task Slugs_must_be_unique()
    {
        var renovation = await GivenCategory("renovation");
        var plastering = await GivenCategory("plastering", 1, renovation);
        var existing = await GivenItem(plastering, "wall-plastering");
        var other = await GivenItem(plastering, "ceiling-plastering");

        (await Should.ThrowAsync<ConflictException>(() => Create(plastering.Id, "WALL-PLASTERING"))).Code.ShouldBe("work_item.slug_taken");
        (await Should.ThrowAsync<ConflictException>(() => Update(other.Id, plastering.Id, "wall-plastering"))).Code.ShouldBe("work_item.slug_taken");
        (await Update(existing.Id, plastering.Id, "wall-plastering")).Slug.ShouldBe("wall-plastering"); // keeping its own slug is fine
    }

    [Fact]
    public async Task Updates_every_field_and_can_move_to_another_subcategory()
    {
        var renovation = await GivenCategory("renovation");
        var plastering = await GivenCategory("plastering", 1, renovation);
        var flooring = await GivenCategory("flooring", 2, renovation);
        var item = await GivenItem(plastering, "wall-plastering");

        var updated = await Update(item.Id, flooring.Id, "skirting");

        updated.CategoryId.ShouldBe(flooring.Id);
        updated.Slug.ShouldBe("skirting");
        updated.Unit.ShouldBe(WorkUnit.RunningMeter);
        updated.Surface.ShouldBe(WorkSurface.Floor);
        updated.SortOrder.ShouldBe(3);
        (await Should.ThrowAsync<DomainException>(() => Update(item.Id, renovation.Id))).Code.ShouldBe("work_item.category_not_subcategory");
    }

    [Fact]
    public async Task Hides_shows_and_deletes_a_work_item()
    {
        var renovation = await GivenCategory("renovation");
        var plastering = await GivenCategory("plastering", 1, renovation);
        var item = await GivenItem(plastering, "wall-plastering");

        (await new SetWorkItemActiveHandler(_db).HandleAsync(new SetWorkItemActive(item.Id, false), CancellationToken.None)).IsActive.ShouldBeFalse();
        (await new SetWorkItemActiveHandler(_db).HandleAsync(new SetWorkItemActive(item.Id, true), CancellationToken.None)).IsActive.ShouldBeTrue();

        (await new DeleteWorkItemHandler(_db).HandleAsync(new DeleteWorkItem(item.Id), CancellationToken.None)).ShouldBeTrue();
        _db.WorkItems.ShouldBeEmpty();
    }

    [Fact]
    public async Task Unknown_work_items_are_not_found()
    {
        var missing = Guid.NewGuid();

        (await Should.ThrowAsync<NotFoundException>(() =>
            new SetWorkItemActiveHandler(_db).HandleAsync(new SetWorkItemActive(missing, true), CancellationToken.None)))
            .Code.ShouldBe("work_item.not_found");
        (await Should.ThrowAsync<NotFoundException>(() =>
            new DeleteWorkItemHandler(_db).HandleAsync(new DeleteWorkItem(missing), CancellationToken.None)))
            .Code.ShouldBe("work_item.not_found");
        await Should.ThrowAsync<NotFoundException>(() => Update(missing, Guid.NewGuid()));
    }

    [Fact]
    public async Task A_category_with_work_items_cannot_be_deleted()
    {
        var renovation = await GivenCategory("renovation");
        var plastering = await GivenCategory("plastering", 1, renovation);
        await GivenItem(plastering, "wall-plastering");

        (await Should.ThrowAsync<DomainException>(() =>
            new DeleteCategoryHandler(_db).HandleAsync(new DeleteCategory(plastering.Id), CancellationToken.None)))
            .Code.ShouldBe("category.has_work_items");
    }

    [Fact]
    public async Task The_validator_checks_every_field()
    {
        var validator = new CreateWorkItemValidator(new FakeLanguageCatalog("hy"));

        (await validator.ValidateAsync(new CreateWorkItem(Guid.NewGuid(), "wall-plastering", PlasteringName, WorkUnit.Point, WorkSurface.None, 0)))
            .IsValid.ShouldBeTrue();

        var errors = (await validator.ValidateAsync(new CreateWorkItem(
                Guid.Empty, "bad slug", new Dictionary<string, string> { ["en"] = "Plastering" }, (WorkUnit)99, (WorkSurface)99, -1)))
            .Errors.Select(e => e.ErrorCode);

        errors.ShouldBe(
            new[] { "category.required", "slug.invalid", "name.default_language_required", "unit.invalid", "surface.invalid", "sort_order.invalid" },
            ignoreOrder: true);
    }
}
