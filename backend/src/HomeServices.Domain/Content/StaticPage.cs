using HomeServices.Domain.Common;
using HomeServices.Domain.Localization;

namespace HomeServices.Domain.Content;

/// <summary>
/// An information page such as About, Terms or Privacy, written per language in Markdown. Pages start as
/// drafts; only published pages are visible on the Portal.
/// </summary>
public sealed class StaticPage : SoftDeletableEntity, IAudited
{
    public const int TitleMaxLength = 200;
    public const int BodyMaxLength = 50_000;

    private StaticPage()
    {
    }

    /// <summary>The page's address, e.g. "terms" for /hy/pages/terms.</summary>
    public string Slug { get; private set; } = string.Empty;

    public LocalizedText Title { get; private set; } = LocalizedText.Empty;

    /// <summary>Markdown per language. The Portal renders it without raw HTML.</summary>
    public LocalizedText Body { get; private set; } = LocalizedText.Empty;

    /// <summary>Linked from the Portal footer (Terms, Privacy, About…).</summary>
    public bool ShowInFooter { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsPublished { get; private set; }

    /// <summary>When the page was first published.</summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    public static StaticPage Create(string slug, LocalizedText title, LocalizedText body, bool showInFooter, int sortOrder)
    {
        var page = new StaticPage();
        page.Update(slug, title, body, showInFooter, sortOrder);
        return page;
    }

    /// <summary>Changes the page. The caller checks that no other page uses the slug. A published page needs a body.</summary>
    public void Update(string slug, LocalizedText title, LocalizedText body, bool showInFooter, int sortOrder)
    {
        if (title.Values.Count == 0)
        {
            throw new DomainException("page.title_required", "A page needs a title.");
        }

        if (IsPublished && body.Values.Count == 0)
        {
            throw new DomainException("page.body_required", "A published page needs some text.");
        }

        Slug = Common.Slug.Normalize(slug, "page.slug_invalid");
        Title = title;
        Body = body;
        ShowInFooter = showInFooter;
        SortOrder = sortOrder;
    }

    public void Publish(DateTimeOffset now)
    {
        if (Body.Values.Count == 0)
        {
            throw new DomainException("page.body_required", "Write the page before publishing it.");
        }

        IsPublished = true;
        PublishedAt ??= now;
    }

    public void Unpublish() => IsPublished = false;
}
