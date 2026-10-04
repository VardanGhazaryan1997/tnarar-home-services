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

/// <summary>All categories (inactive too) as a tree, with every translation, for the Back Office.</summary>
public sealed record GetAdminCategories : IQuery<IReadOnlyList<AdminCategoryNodeDto>>;

/// <summary>The fields a staff member edits on a category.</summary>
public interface ICategoryFields
{
    string Slug { get; }

    IReadOnlyDictionary<string, string> Name { get; }

    string? Icon { get; }

    Guid? ParentId { get; }

    int SortOrder { get; }
}

public sealed record CreateCategory(string Slug, IReadOnlyDictionary<string, string> Name, string? Icon, Guid? ParentId, int SortOrder)
    : ICommand<AdminCategoryDto>, ICategoryFields;

public sealed record UpdateCategory(Guid Id, string Slug, IReadOnlyDictionary<string, string> Name, string? Icon, Guid? ParentId, int SortOrder)
    : ICommand<AdminCategoryDto>, ICategoryFields;

/// <summary>Shows or hides a category on the Portal. Hiding a category also hides its subcategories.</summary>
public sealed record SetCategoryActive(Guid Id, bool IsActive) : ICommand<AdminCategoryDto>;

/// <summary>Deletes (soft) a category without subcategories. Its slug can then be reused.</summary>
public sealed record DeleteCategory(Guid Id) : ICommand<bool>;

public abstract class CategoryFieldsValidator<T> : AbstractValidator<T>
    where T : ICategoryFields
{
    protected CategoryFieldsValidator(ILanguageCatalog languages)
    {
        RuleFor(x => x.Slug).ValidSlug();
        RuleFor(x => x.Name).LocalizedName(languages);
        RuleFor(x => x.Icon).MaximumLength(Category.IconMaxLength).WithErrorCode("icon.too_long");
        RuleFor(x => x.SortOrder).ValidSortOrder();
    }
}

public sealed class CreateCategoryValidator(ILanguageCatalog languages) : CategoryFieldsValidator<CreateCategory>(languages);

public sealed class UpdateCategoryValidator(ILanguageCatalog languages) : CategoryFieldsValidator<UpdateCategory>(languages);

public sealed class GetAdminCategoriesHandler(IAppDbContext db) : IQueryHandler<GetAdminCategories, IReadOnlyList<AdminCategoryNodeDto>>
{
    public async Task<IReadOnlyList<AdminCategoryNodeDto>> HandleAsync(GetAdminCategories query, CancellationToken cancellationToken)
    {
        var categories = await db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        var byParent = categories.ToLookup(c => c.ParentId);

        IReadOnlyList<AdminCategoryNodeDto> Build(Guid? parentId) =>
            byParent[parentId]
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Slug, StringComparer.Ordinal)
                .Select(c => new AdminCategoryNodeDto(
                    c.Id, c.Slug, AdminCategoryDto.Translations(c.Name), c.Icon, c.ParentId, c.SortOrder, c.IsActive, Build(c.Id)))
                .ToList();

        return Build(null);
    }
}

public sealed class CreateCategoryHandler(IAppDbContext db) : ICommandHandler<CreateCategory, AdminCategoryDto>
{
    public async Task<AdminCategoryDto> HandleAsync(CreateCategory command, CancellationToken cancellationToken)
    {
        await CategoryChecks.EnsureSlugFreeAsync(db, command.Slug, exceptId: null, cancellationToken);
        await CategoryChecks.EnsureValidParentAsync(db, command.ParentId, cancellationToken);

        var category = Category.Create(command.Slug, LocalizedText.From(command.Name), command.SortOrder, command.ParentId, command.Icon);
        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return AdminCategoryDto.From(category);
    }
}

public sealed class UpdateCategoryHandler(IAppDbContext db) : ICommandHandler<UpdateCategory, AdminCategoryDto>
{
    public async Task<AdminCategoryDto> HandleAsync(UpdateCategory command, CancellationToken cancellationToken)
    {
        var category = await CategoryChecks.LoadAsync(db, command.Id, cancellationToken);

        if (command.ParentId != category.ParentId)
        {
            category.MoveTo(command.ParentId); // rejects making it its own parent
            await CategoryChecks.EnsureValidParentAsync(db, command.ParentId, cancellationToken);
            if (command.ParentId is not null && await db.Categories.AnyAsync(c => c.ParentId == category.Id, cancellationToken))
            {
                throw new DomainException("category.has_subcategories", "A category with subcategories can't become a subcategory.");
            }
        }

        await CategoryChecks.EnsureSlugFreeAsync(db, command.Slug, category.Id, cancellationToken);

        category.ChangeSlug(command.Slug);
        category.Rename(LocalizedText.From(command.Name));
        category.ChangeIcon(command.Icon);
        category.Reorder(command.SortOrder);
        await db.SaveChangesAsync(cancellationToken);
        return AdminCategoryDto.From(category);
    }
}

public sealed class SetCategoryActiveHandler(IAppDbContext db) : ICommandHandler<SetCategoryActive, AdminCategoryDto>
{
    public async Task<AdminCategoryDto> HandleAsync(SetCategoryActive command, CancellationToken cancellationToken)
    {
        var category = await CategoryChecks.LoadAsync(db, command.Id, cancellationToken);
        if (command.IsActive)
        {
            category.Activate();
        }
        else
        {
            category.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);
        return AdminCategoryDto.From(category);
    }
}

public sealed class DeleteCategoryHandler(IAppDbContext db) : ICommandHandler<DeleteCategory, bool>
{
    public async Task<bool> HandleAsync(DeleteCategory command, CancellationToken cancellationToken)
    {
        var category = await CategoryChecks.LoadAsync(db, command.Id, cancellationToken);
        if (await db.Categories.AnyAsync(c => c.ParentId == category.Id, cancellationToken))
        {
            throw new DomainException("category.has_subcategories", "Move or delete its subcategories first.");
        }

        // Partner profiles (task T23) will also block deleting a category that is in use.
        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

internal static class CategoryChecks
{
    public static async Task<Category> LoadAsync(IAppDbContext db, Guid id, CancellationToken cancellationToken) =>
        await db.Categories.SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
        ?? throw new NotFoundException("The category does not exist.", "category.not_found");

    public static async Task EnsureSlugFreeAsync(IAppDbContext db, string slug, Guid? exceptId, CancellationToken cancellationToken)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        if (await db.Categories.AnyAsync(c => c.Slug == normalized && c.Id != exceptId, cancellationToken))
        {
            throw new ConflictException($"Another category already uses '{normalized}'.", "category.slug_taken");
        }
    }

    /// <summary>Categories have two levels: a parent must exist and be a top-level category.</summary>
    public static async Task EnsureValidParentAsync(IAppDbContext db, Guid? parentId, CancellationToken cancellationToken)
    {
        if (parentId is null)
        {
            return;
        }

        var parent = await db.Categories.AsNoTracking().SingleOrDefaultAsync(c => c.Id == parentId, cancellationToken)
            ?? throw new DomainException("category.parent_not_found", "The parent category does not exist.");

        if (parent.ParentId is not null)
        {
            throw new DomainException("category.too_deep", "A subcategory can't have subcategories of its own.");
        }
    }
}
