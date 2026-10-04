using HomeServices.Application.Abstractions;
using HomeServices.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Languages;

/// <summary>Languages shown in the language switcher, in display order.</summary>
public sealed record GetActiveLanguages : IQuery<IReadOnlyList<LanguageDto>>;

public sealed class GetActiveLanguagesHandler(IAppDbContext db) : IQueryHandler<GetActiveLanguages, IReadOnlyList<LanguageDto>>
{
    public async Task<IReadOnlyList<LanguageDto>> HandleAsync(GetActiveLanguages query, CancellationToken cancellationToken) =>
        await db.Languages
            .AsNoTracking()
            .Where(l => l.IsActive)
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.Code)
            .Select(l => new LanguageDto(l.Code, l.Name, l.NativeName, l.IsDefault))
            .ToListAsync(cancellationToken);
}
