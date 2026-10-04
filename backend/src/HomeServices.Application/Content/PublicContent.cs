using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Content;
using HomeServices.Domain.Localization;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Content;

/// <summary>Published pages (title and slug only), by position. <see cref="PublicPageLinkDto.ShowInFooter"/> marks footer links.</summary>
public sealed record GetPublicPages : IQuery<IReadOnlyList<PublicPageLinkDto>>;

/// <summary>A published page in the request language (falling back to the default language).</summary>
public sealed record GetPublicPage(string Slug) : IQuery<PublicPageDto>;

/// <summary>Published questions in the request language, by audience and position; <see cref="Audience"/> narrows them.</summary>
public sealed record GetPublicFaqs(FaqAudience? Audience = null) : IQuery<IReadOnlyList<PublicFaqDto>>;

public sealed record PublicPageLinkDto(string Slug, string Title, bool ShowInFooter);

/// <summary><see cref="Body"/> is Markdown.</summary>
public sealed record PublicPageDto(string Slug, string Title, string Body, DateTimeOffset UpdatedAt);

public sealed record PublicFaqDto(Guid Id, string Question, string Answer, string Audience);

public sealed class GetPublicPagesHandler(IAppDbContext db, ICurrentLanguage language) : IQueryHandler<GetPublicPages, IReadOnlyList<PublicPageLinkDto>>
{
    public async Task<IReadOnlyList<PublicPageLinkDto>> HandleAsync(GetPublicPages query, CancellationToken cancellationToken) =>
        (await db.StaticPages.AsNoTracking().Where(p => p.IsPublished).ToListAsync(cancellationToken))
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Slug, StringComparer.Ordinal)
            .Select(p => new PublicPageLinkDto(p.Slug, Localize(p.Title), p.ShowInFooter))
            .ToList();

    private string Localize(LocalizedText text) => text.Get(language.Code, language.DefaultCode);
}

public sealed class GetPublicPageHandler(IAppDbContext db, ICurrentLanguage language) : IQueryHandler<GetPublicPage, PublicPageDto>
{
    public async Task<PublicPageDto> HandleAsync(GetPublicPage query, CancellationToken cancellationToken)
    {
        var slug = query.Slug.Trim().ToLowerInvariant();
        var page = await db.StaticPages.AsNoTracking().SingleOrDefaultAsync(p => p.Slug == slug && p.IsPublished, cancellationToken)
            ?? throw new NotFoundException("Page not found.", "page.not_found");

        return new PublicPageDto(
            page.Slug,
            page.Title.Get(language.Code, language.DefaultCode),
            page.Body.Get(language.Code, language.DefaultCode),
            page.UpdatedAt ?? page.CreatedAt);
    }
}

public sealed class GetPublicFaqsHandler(IAppDbContext db, ICurrentLanguage language) : IQueryHandler<GetPublicFaqs, IReadOnlyList<PublicFaqDto>>
{
    public async Task<IReadOnlyList<PublicFaqDto>> HandleAsync(GetPublicFaqs query, CancellationToken cancellationToken)
    {
        var items = db.FaqItems.AsNoTracking().Where(f => f.IsPublished);
        if (query.Audience is { } audience)
        {
            items = items.Where(f => f.Audience == audience);
        }

        return (await items.ToListAsync(cancellationToken))
            .OrderBy(f => f.Audience)
            .ThenBy(f => f.SortOrder)
            .ThenBy(f => f.Id)
            .Select(f => new PublicFaqDto(f.Id, f.Question.Get(language.Code, language.DefaultCode), f.Answer.Get(language.Code, language.DefaultCode), f.Audience.ToString()))
            .ToList();
    }
}
