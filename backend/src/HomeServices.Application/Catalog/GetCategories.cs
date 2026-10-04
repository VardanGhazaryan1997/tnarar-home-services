using HomeServices.Application.Abstractions;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Catalog;

/// <summary>Active service categories as a tree, with names in the request language.</summary>
public sealed record GetCategories : IQuery<IReadOnlyList<CategoryDto>>;

public sealed record CategoryDto(Guid Id, string Slug, string Name, string? Icon, IReadOnlyList<CategoryDto> Children);

public sealed class GetCategoriesHandler(IAppDbContext db, ICurrentLanguage language)
    : IQueryHandler<GetCategories, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> HandleAsync(GetCategories query, CancellationToken cancellationToken)
    {
        // Reference data is small: load it and build the tree in memory (names are JSON per language).
        var categories = await db.Categories.AsNoTracking().Where(c => c.IsActive).ToListAsync(cancellationToken);
        var byParent = categories.ToLookup(c => c.ParentId);

        IReadOnlyList<CategoryDto> Build(Guid? parentId) =>
            byParent[parentId]
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Slug, StringComparer.Ordinal)
                .Select(c => ToDto(c, Build(c.Id)))
                .ToList();

        return Build(null);
    }

    private CategoryDto ToDto(Category category, IReadOnlyList<CategoryDto> children) =>
        new(category.Id, category.Slug, category.Name.Get(language.Code, language.DefaultCode), category.Icon, children);
}
