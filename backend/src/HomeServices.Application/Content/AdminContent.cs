using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Languages;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Common;
using HomeServices.Domain.Content;
using HomeServices.Domain.Localization;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Content;

/// <summary>A page with every translation, for the Back Office.</summary>
public sealed record AdminPageDto(
    Guid Id,
    string Slug,
    IReadOnlyDictionary<string, string> Title,
    IReadOnlyDictionary<string, string> Body,
    bool ShowInFooter,
    int SortOrder,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? UpdatedAt)
{
    public static AdminPageDto From(StaticPage page) =>
        new(page.Id, page.Slug, page.Title.Values, page.Body.Values, page.ShowInFooter, page.SortOrder, page.IsPublished, page.PublishedAt, page.UpdatedAt ?? page.CreatedAt);
}

public sealed record AdminFaqDto(
    Guid Id,
    IReadOnlyDictionary<string, string> Question,
    IReadOnlyDictionary<string, string> Answer,
    string Audience,
    int SortOrder,
    bool IsPublished)
{
    public static AdminFaqDto From(FaqItem item) =>
        new(item.Id, item.Question.Values, item.Answer.Values, item.Audience.ToString(), item.SortOrder, item.IsPublished);
}

public interface IPageFields
{
    string Slug { get; }

    IReadOnlyDictionary<string, string> Title { get; }

    IReadOnlyDictionary<string, string> Body { get; }

    int SortOrder { get; }
}

public interface IFaqFields
{
    IReadOnlyDictionary<string, string> Question { get; }

    IReadOnlyDictionary<string, string> Answer { get; }

    FaqAudience Audience { get; }

    int SortOrder { get; }
}

/// <summary>All pages, drafts included, by position.</summary>
public sealed record GetAdminPages : IQuery<IReadOnlyList<AdminPageDto>>;

public sealed record GetAdminPage(Guid Id) : IQuery<AdminPageDto>;

/// <summary>Creates a draft page.</summary>
public sealed record CreatePage(string Slug, IReadOnlyDictionary<string, string> Title, IReadOnlyDictionary<string, string> Body, bool ShowInFooter, int SortOrder)
    : ICommand<AdminPageDto>, IPageFields;

public sealed record UpdatePage(Guid Id, string Slug, IReadOnlyDictionary<string, string> Title, IReadOnlyDictionary<string, string> Body, bool ShowInFooter, int SortOrder)
    : ICommand<AdminPageDto>, IPageFields;

public sealed record SetPagePublished(Guid Id, bool IsPublished) : ICommand<AdminPageDto>;

/// <summary>Deletes a page (kept in history); its slug can be used again.</summary>
public sealed record DeletePage(Guid Id) : ICommand<bool>;

/// <summary>All questions, unpublished included, by audience and position.</summary>
public sealed record GetAdminFaqs : IQuery<IReadOnlyList<AdminFaqDto>>;

/// <summary>Creates an unpublished question.</summary>
public sealed record CreateFaq(IReadOnlyDictionary<string, string> Question, IReadOnlyDictionary<string, string> Answer, FaqAudience Audience, int SortOrder)
    : ICommand<AdminFaqDto>, IFaqFields;

public sealed record UpdateFaq(Guid Id, IReadOnlyDictionary<string, string> Question, IReadOnlyDictionary<string, string> Answer, FaqAudience Audience, int SortOrder)
    : ICommand<AdminFaqDto>, IFaqFields;

public sealed record SetFaqPublished(Guid Id, bool IsPublished) : ICommand<AdminFaqDto>;

public sealed record DeleteFaq(Guid Id) : ICommand<bool>;

public abstract class PageFieldsValidator<T> : AbstractValidator<T>
    where T : IPageFields
{
    protected PageFieldsValidator(ILanguageCatalog languages)
    {
        RuleFor(x => x.Slug).Must(Slug.IsValid).WithErrorCode("slug.invalid");
        RuleFor(x => x.Title).LocalizedText(languages, "title", StaticPage.TitleMaxLength);
        RuleFor(x => x.Body).LocalizedText(languages, "body", StaticPage.BodyMaxLength, defaultRequired: false);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0).WithErrorCode("sort_order.invalid");
    }
}

public sealed class CreatePageValidator(ILanguageCatalog languages) : PageFieldsValidator<CreatePage>(languages);

public sealed class UpdatePageValidator(ILanguageCatalog languages) : PageFieldsValidator<UpdatePage>(languages);

public abstract class FaqFieldsValidator<T> : AbstractValidator<T>
    where T : IFaqFields
{
    protected FaqFieldsValidator(ILanguageCatalog languages)
    {
        RuleFor(x => x.Question).LocalizedText(languages, "question", FaqItem.QuestionMaxLength);
        RuleFor(x => x.Answer).LocalizedText(languages, "answer", FaqItem.AnswerMaxLength);
        RuleFor(x => x.Audience).IsInEnum().WithErrorCode("audience.invalid");
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0).WithErrorCode("sort_order.invalid");
    }
}

public sealed class CreateFaqValidator(ILanguageCatalog languages) : FaqFieldsValidator<CreateFaq>(languages);

public sealed class UpdateFaqValidator(ILanguageCatalog languages) : FaqFieldsValidator<UpdateFaq>(languages);

public sealed class GetAdminPagesHandler(IAppDbContext db) : IQueryHandler<GetAdminPages, IReadOnlyList<AdminPageDto>>
{
    public async Task<IReadOnlyList<AdminPageDto>> HandleAsync(GetAdminPages query, CancellationToken cancellationToken) =>
        (await db.StaticPages.AsNoTracking().ToListAsync(cancellationToken))
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Slug, StringComparer.Ordinal)
            .Select(AdminPageDto.From)
            .ToList();
}

public sealed class GetAdminPageHandler(IAppDbContext db) : IQueryHandler<GetAdminPage, AdminPageDto>
{
    public async Task<AdminPageDto> HandleAsync(GetAdminPage query, CancellationToken cancellationToken) =>
        AdminPageDto.From(await ContentRules.LoadPageAsync(db, query.Id, cancellationToken));
}

public sealed class CreatePageHandler(IAppDbContext db) : ICommandHandler<CreatePage, AdminPageDto>
{
    public async Task<AdminPageDto> HandleAsync(CreatePage command, CancellationToken cancellationToken)
    {
        await ContentRules.EnsureSlugFreeAsync(db, command.Slug, exceptId: null, cancellationToken);
        var page = StaticPage.Create(command.Slug, LocalizedText.From(command.Title), LocalizedText.From(command.Body), command.ShowInFooter, command.SortOrder);
        db.StaticPages.Add(page);
        await db.SaveChangesAsync(cancellationToken);
        return AdminPageDto.From(page);
    }
}

public sealed class UpdatePageHandler(IAppDbContext db) : ICommandHandler<UpdatePage, AdminPageDto>
{
    public async Task<AdminPageDto> HandleAsync(UpdatePage command, CancellationToken cancellationToken)
    {
        var page = await ContentRules.LoadPageAsync(db, command.Id, cancellationToken);
        await ContentRules.EnsureSlugFreeAsync(db, command.Slug, page.Id, cancellationToken);
        page.Update(command.Slug, LocalizedText.From(command.Title), LocalizedText.From(command.Body), command.ShowInFooter, command.SortOrder);
        await db.SaveChangesAsync(cancellationToken);
        return AdminPageDto.From(page);
    }
}

public sealed class SetPagePublishedHandler(IAppDbContext db, TimeProvider clock) : ICommandHandler<SetPagePublished, AdminPageDto>
{
    public async Task<AdminPageDto> HandleAsync(SetPagePublished command, CancellationToken cancellationToken)
    {
        var page = await ContentRules.LoadPageAsync(db, command.Id, cancellationToken);
        if (command.IsPublished)
        {
            page.Publish(clock.GetUtcNow());
        }
        else
        {
            page.Unpublish();
        }

        await db.SaveChangesAsync(cancellationToken);
        return AdminPageDto.From(page);
    }
}

public sealed class DeletePageHandler(IAppDbContext db) : ICommandHandler<DeletePage, bool>
{
    public async Task<bool> HandleAsync(DeletePage command, CancellationToken cancellationToken)
    {
        db.StaticPages.Remove(await ContentRules.LoadPageAsync(db, command.Id, cancellationToken));
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class GetAdminFaqsHandler(IAppDbContext db) : IQueryHandler<GetAdminFaqs, IReadOnlyList<AdminFaqDto>>
{
    public async Task<IReadOnlyList<AdminFaqDto>> HandleAsync(GetAdminFaqs query, CancellationToken cancellationToken) =>
        (await db.FaqItems.AsNoTracking().ToListAsync(cancellationToken))
            .OrderBy(f => f.Audience)
            .ThenBy(f => f.SortOrder)
            .ThenBy(f => f.Id)
            .Select(AdminFaqDto.From)
            .ToList();
}

public sealed class CreateFaqHandler(IAppDbContext db) : ICommandHandler<CreateFaq, AdminFaqDto>
{
    public async Task<AdminFaqDto> HandleAsync(CreateFaq command, CancellationToken cancellationToken)
    {
        var item = FaqItem.Create(LocalizedText.From(command.Question), LocalizedText.From(command.Answer), command.Audience, command.SortOrder);
        db.FaqItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return AdminFaqDto.From(item);
    }
}

public sealed class UpdateFaqHandler(IAppDbContext db) : ICommandHandler<UpdateFaq, AdminFaqDto>
{
    public async Task<AdminFaqDto> HandleAsync(UpdateFaq command, CancellationToken cancellationToken)
    {
        var item = await ContentRules.LoadFaqAsync(db, command.Id, cancellationToken);
        item.Update(LocalizedText.From(command.Question), LocalizedText.From(command.Answer), command.Audience, command.SortOrder);
        await db.SaveChangesAsync(cancellationToken);
        return AdminFaqDto.From(item);
    }
}

public sealed class SetFaqPublishedHandler(IAppDbContext db) : ICommandHandler<SetFaqPublished, AdminFaqDto>
{
    public async Task<AdminFaqDto> HandleAsync(SetFaqPublished command, CancellationToken cancellationToken)
    {
        var item = await ContentRules.LoadFaqAsync(db, command.Id, cancellationToken);
        if (command.IsPublished)
        {
            item.Publish();
        }
        else
        {
            item.Unpublish();
        }

        await db.SaveChangesAsync(cancellationToken);
        return AdminFaqDto.From(item);
    }
}

public sealed class DeleteFaqHandler(IAppDbContext db) : ICommandHandler<DeleteFaq, bool>
{
    public async Task<bool> HandleAsync(DeleteFaq command, CancellationToken cancellationToken)
    {
        db.FaqItems.Remove(await ContentRules.LoadFaqAsync(db, command.Id, cancellationToken));
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

internal static class ContentRules
{
    public static async Task<StaticPage> LoadPageAsync(IAppDbContext db, Guid id, CancellationToken cancellationToken) =>
        await db.StaticPages.SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
        ?? throw new NotFoundException("Page not found.", "page.not_found");

    public static async Task<FaqItem> LoadFaqAsync(IAppDbContext db, Guid id, CancellationToken cancellationToken) =>
        await db.FaqItems.SingleOrDefaultAsync(f => f.Id == id, cancellationToken)
        ?? throw new NotFoundException("Question not found.", "faq.not_found");

    public static async Task EnsureSlugFreeAsync(IAppDbContext db, string slug, Guid? exceptId, CancellationToken cancellationToken)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        if (await db.StaticPages.AnyAsync(p => p.Slug == normalized && p.Id != exceptId, cancellationToken))
        {
            throw new ConflictException("Another page uses this address.", "page.slug_taken");
        }
    }
}
