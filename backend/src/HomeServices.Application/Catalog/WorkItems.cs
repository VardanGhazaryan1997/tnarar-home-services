using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Languages;
using HomeServices.Application.Messaging;
using HomeServices.Domain;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Catalog;

/// <summary>
/// Active work items with names in the request language, in catalog order. <paramref name="Category"/> is a category slug:
/// a subcategory gives its own items, a main category the items of all its subcategories; null gives every item.
/// </summary>
public sealed record GetWorkItems(string? Category = null) : IQuery<IReadOnlyList<WorkItemDto>>;

/// <summary>A work item for the Portal. Prices are the usual labour price range per unit in AMD (null until priced).</summary>
public sealed record WorkItemDto(
    Guid Id,
    Guid CategoryId,
    string Slug,
    string Name,
    WorkUnit Unit,
    WorkSurface Surface,
    int? PriceMin = null,
    int? PriceTypical = null,
    int? PriceMax = null);

/// <summary>
/// Every work item (hidden ones too) with all translations, for the Back Office. Filters: a category (a main category
/// includes its subcategories), text in the slug or any name, and active or hidden.
/// </summary>
public sealed record GetAdminWorkItems(Guid? CategoryId = null, string? Search = null, bool? IsActive = null)
    : IQuery<IReadOnlyList<AdminWorkItemDto>>;

public sealed record AdminWorkItemDto(
    Guid Id,
    Guid CategoryId,
    string Slug,
    IReadOnlyDictionary<string, string> Name,
    WorkUnit Unit,
    WorkSurface Surface,
    int SortOrder,
    bool IsActive,
    int? PriceMin = null,
    int? PriceTypical = null,
    int? PriceMax = null,
    bool IsPriceLocked = false)
{
    public static AdminWorkItemDto From(WorkItem item) =>
        new(
            item.Id,
            item.CategoryId,
            item.Slug,
            AdminCategoryDto.Translations(item.Name),
            item.Unit,
            item.Surface,
            item.SortOrder,
            item.IsActive,
            item.PriceMin,
            item.PriceTypical,
            item.PriceMax,
            item.IsPriceLocked);
}

/// <summary>The fields a staff member edits on a work item.</summary>
public interface IWorkItemFields
{
    Guid CategoryId { get; }

    string Slug { get; }

    IReadOnlyDictionary<string, string> Name { get; }

    WorkUnit Unit { get; }

    WorkSurface Surface { get; }

    int SortOrder { get; }

    /// <summary>The labour price range per unit in AMD: all three, or none for an item without prices yet.</summary>
    int? PriceMin { get; }

    int? PriceTypical { get; }

    int? PriceMax { get; }

    /// <summary>True to keep the staff price range fixed (partner prices won't adjust it).</summary>
    bool IsPriceLocked { get; }
}

public sealed record CreateWorkItem(
    Guid CategoryId,
    string Slug,
    IReadOnlyDictionary<string, string> Name,
    WorkUnit Unit,
    WorkSurface Surface,
    int SortOrder,
    int? PriceMin = null,
    int? PriceTypical = null,
    int? PriceMax = null,
    bool IsPriceLocked = false) : ICommand<AdminWorkItemDto>, IWorkItemFields;

public sealed record UpdateWorkItem(
    Guid Id,
    Guid CategoryId,
    string Slug,
    IReadOnlyDictionary<string, string> Name,
    WorkUnit Unit,
    WorkSurface Surface,
    int SortOrder,
    int? PriceMin = null,
    int? PriceTypical = null,
    int? PriceMax = null,
    bool IsPriceLocked = false) : ICommand<AdminWorkItemDto>, IWorkItemFields;

/// <summary>Shows or hides a work item on the Portal.</summary>
public sealed record SetWorkItemActive(Guid Id, bool IsActive) : ICommand<AdminWorkItemDto>;

/// <summary>Deletes (soft) a work item. Its slug can then be reused.</summary>
public sealed record DeleteWorkItem(Guid Id) : ICommand<bool>;

public abstract class WorkItemFieldsValidator<T> : AbstractValidator<T>
    where T : IWorkItemFields
{
    protected WorkItemFieldsValidator(ILanguageCatalog languages)
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithErrorCode("category.required");
        RuleFor(x => x.Slug).ValidSlug();
        RuleFor(x => x.Name).LocalizedName(languages);
        RuleFor(x => x.Unit).IsInEnum().WithErrorCode("unit.invalid");
        RuleFor(x => x.Surface).IsInEnum().WithErrorCode("surface.invalid");
        RuleFor(x => x.SortOrder).ValidSortOrder();
        RuleFor(x => x)
            .Must(x => x.PriceMin is null == x.PriceTypical is null && x.PriceTypical is null == x.PriceMax is null)
            .OverridePropertyName("price")
            .WithErrorCode("price.incomplete");
        RuleFor(x => x)
            .Must(x => WorkItemPrices.From(x) is not { IsValid: false })
            .OverridePropertyName("price")
            .WithErrorCode("price.invalid");
    }
}

public sealed class CreateWorkItemValidator(ILanguageCatalog languages) : WorkItemFieldsValidator<CreateWorkItem>(languages);

public sealed class UpdateWorkItemValidator(ILanguageCatalog languages) : WorkItemFieldsValidator<UpdateWorkItem>(languages);

public sealed class GetWorkItemsHandler(IAppDbContext db, ICurrentLanguage language) : IQueryHandler<GetWorkItems, IReadOnlyList<WorkItemDto>>
{
    public async Task<IReadOnlyList<WorkItemDto>> HandleAsync(GetWorkItems query, CancellationToken cancellationToken)
    {
        // Only items whose subcategory and main category are both shown on the Portal.
        // (Order leaves out subcategories of hidden main categories.)
        var categories = await db.Categories.AsNoTracking().Where(c => c.IsActive).ToListAsync(cancellationToken);
        var visible = WorkItemCatalog.Order(categories);

        var wanted = query.Category is null
            ? visible
            : WorkItemCatalog.Within(visible, categories, categories.SingleOrDefault(c => c.Slug == query.Category.Trim().ToLowerInvariant())?.Id);
        if (wanted.Count == 0)
        {
            return [];
        }

        var ids = wanted.Keys.ToList();
        var items = await db.WorkItems.AsNoTracking()
            .Where(w => w.IsActive && ids.Contains(w.CategoryId))
            .ToListAsync(cancellationToken);

        return WorkItemCatalog.Sort(items, wanted)
            .Select(w => new WorkItemDto(
                w.Id, w.CategoryId, w.Slug, w.Name.Get(language.Code, language.DefaultCode), w.Unit, w.Surface, w.PriceMin, w.PriceTypical, w.PriceMax))
            .ToList();
    }
}

public sealed class GetAdminWorkItemsHandler(IAppDbContext db) : IQueryHandler<GetAdminWorkItems, IReadOnlyList<AdminWorkItemDto>>
{
    public async Task<IReadOnlyList<AdminWorkItemDto>> HandleAsync(GetAdminWorkItems query, CancellationToken cancellationToken)
    {
        var categories = await db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        var order = WorkItemCatalog.Order(categories);
        var wanted = query.CategoryId is null ? order : WorkItemCatalog.Within(order, categories, query.CategoryId);

        var items = db.WorkItems.AsNoTracking();
        if (query.IsActive is { } active)
        {
            items = items.Where(w => w.IsActive == active);
        }

        // Names are jsonb per language, and there are a few hundred items: search in memory.
        var search = query.Search?.Trim();
        return WorkItemCatalog.Sort(await items.ToListAsync(cancellationToken), wanted)
            .Where(w => string.IsNullOrEmpty(search) || Matches(w, search))
            .Select(AdminWorkItemDto.From)
            .ToList();
    }

    private static bool Matches(WorkItem item, string search) =>
        item.Slug.Contains(search, StringComparison.OrdinalIgnoreCase)
        || item.Name.Values.Values.Any(name => name.Contains(search, StringComparison.CurrentCultureIgnoreCase));
}

public sealed class CreateWorkItemHandler(IAppDbContext db) : ICommandHandler<CreateWorkItem, AdminWorkItemDto>
{
    public async Task<AdminWorkItemDto> HandleAsync(CreateWorkItem command, CancellationToken cancellationToken)
    {
        await WorkItemChecks.EnsureSubcategoryAsync(db, command.CategoryId, cancellationToken);
        await WorkItemChecks.EnsureSlugFreeAsync(db, command.Slug, exceptId: null, cancellationToken);

        var item = WorkItem.Create(command.CategoryId, command.Slug, LocalizedText.From(command.Name), command.Unit, command.Surface, command.SortOrder);
        item.SetPrice(WorkItemPrices.From(command));
        item.LockPrice(command.IsPriceLocked);
        db.WorkItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return AdminWorkItemDto.From(item);
    }
}

public sealed class UpdateWorkItemHandler(IAppDbContext db) : ICommandHandler<UpdateWorkItem, AdminWorkItemDto>
{
    public async Task<AdminWorkItemDto> HandleAsync(UpdateWorkItem command, CancellationToken cancellationToken)
    {
        var item = await WorkItemChecks.LoadAsync(db, command.Id, cancellationToken);
        if (command.CategoryId != item.CategoryId)
        {
            await WorkItemChecks.EnsureSubcategoryAsync(db, command.CategoryId, cancellationToken);
        }

        await WorkItemChecks.EnsureSlugFreeAsync(db, command.Slug, item.Id, cancellationToken);

        item.Update(command.CategoryId, command.Slug, LocalizedText.From(command.Name), command.Unit, command.Surface, command.SortOrder);
        item.SetPrice(WorkItemPrices.From(command));
        item.LockPrice(command.IsPriceLocked);
        await db.SaveChangesAsync(cancellationToken);
        return AdminWorkItemDto.From(item);
    }
}

public sealed class SetWorkItemActiveHandler(IAppDbContext db) : ICommandHandler<SetWorkItemActive, AdminWorkItemDto>
{
    public async Task<AdminWorkItemDto> HandleAsync(SetWorkItemActive command, CancellationToken cancellationToken)
    {
        var item = await WorkItemChecks.LoadAsync(db, command.Id, cancellationToken);
        if (command.IsActive)
        {
            item.Activate();
        }
        else
        {
            item.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);
        return AdminWorkItemDto.From(item);
    }
}

public sealed class DeleteWorkItemHandler(IAppDbContext db) : ICommandHandler<DeleteWorkItem, bool>
{
    public async Task<bool> HandleAsync(DeleteWorkItem command, CancellationToken cancellationToken)
    {
        // Partner price lists (A4) will block deleting an item that is priced; hiding it is the alternative.
        var item = await WorkItemChecks.LoadAsync(db, command.Id, cancellationToken);
        db.WorkItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

internal static class WorkItemPrices
{
    /// <summary>The range from the edited fields; null unless all three prices are given.</summary>
    public static PriceRange? From(IWorkItemFields fields) =>
        fields is { PriceMin: { } min, PriceTypical: { } typical, PriceMax: { } max } ? new PriceRange(min, typical, max) : null;
}

internal static class WorkItemChecks
{
    public static async Task<WorkItem> LoadAsync(IAppDbContext db, Guid id, CancellationToken cancellationToken) =>
        await db.WorkItems.SingleOrDefaultAsync(w => w.Id == id, cancellationToken)
        ?? throw new NotFoundException("The work item does not exist.", "work_item.not_found");

    public static async Task EnsureSlugFreeAsync(IAppDbContext db, string slug, Guid? exceptId, CancellationToken cancellationToken)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        if (await db.WorkItems.AnyAsync(w => w.Slug == normalized && w.Id != exceptId, cancellationToken))
        {
            throw new ConflictException($"Another work item already uses '{normalized}'.", "work_item.slug_taken");
        }
    }

    /// <summary>Work items hang under subcategories, never directly under a main category.</summary>
    public static async Task EnsureSubcategoryAsync(IAppDbContext db, Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await db.Categories.AsNoTracking().SingleOrDefaultAsync(c => c.Id == categoryId, cancellationToken)
            ?? throw new DomainException("work_item.category_not_found", "The category does not exist.");

        if (category.ParentId is null)
        {
            throw new DomainException("work_item.category_not_subcategory", "Choose a subcategory, not a main category.");
        }
    }
}

/// <summary>Catalog order for work items: main category, then subcategory, then the item's own order.</summary>
internal static class WorkItemCatalog
{
    /// <summary>Each category's position in the tree (main categories' sort order, then their subcategories').</summary>
    public static Dictionary<Guid, int> Order(IReadOnlyList<Category> categories)
    {
        var byParent = categories.ToLookup(c => c.ParentId);
        var order = new Dictionary<Guid, int>();
        foreach (var main in byParent[null].OrderBy(c => c.SortOrder).ThenBy(c => c.Slug, StringComparer.Ordinal))
        {
            order[main.Id] = order.Count;
            foreach (var sub in byParent[main.Id].OrderBy(c => c.SortOrder).ThenBy(c => c.Slug, StringComparer.Ordinal))
            {
                order[sub.Id] = order.Count;
            }
        }

        return order;
    }

    /// <summary>
    /// The category and (for a main category) its subcategories, keeping their positions from <paramref name="order"/>;
    /// empty for an unknown category.
    /// </summary>
    public static Dictionary<Guid, int> Within(Dictionary<Guid, int> order, IReadOnlyList<Category> categories, Guid? categoryId)
    {
        if (categoryId is not { } id || !order.ContainsKey(id))
        {
            return new Dictionary<Guid, int>();
        }

        var children = categories.Where(c => c.ParentId == id).Select(c => c.Id).ToHashSet();
        return order.Where(entry => entry.Key == id || children.Contains(entry.Key)).ToDictionary(entry => entry.Key, entry => entry.Value);
    }

    /// <summary>Items in known categories, in catalog order.</summary>
    public static IEnumerable<WorkItem> Sort(IEnumerable<WorkItem> items, Dictionary<Guid, int> order) =>
        items.Where(w => order.ContainsKey(w.CategoryId))
            .OrderBy(w => order[w.CategoryId])
            .ThenBy(w => w.SortOrder)
            .ThenBy(w => w.Slug, StringComparer.Ordinal);
}
