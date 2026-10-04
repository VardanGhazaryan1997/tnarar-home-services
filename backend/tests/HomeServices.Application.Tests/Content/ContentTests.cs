using HomeServices.Application.Content;
using HomeServices.Application.Errors;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Content;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Tests.Content;

public class ContentTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly FakeClock _clock = new(Now);
    private readonly FakeLanguageCatalog _languages = new();

    private static Dictionary<string, string> Texts(string hy, string? ru = null) =>
        ru is null ? new() { ["hy"] = hy } : new() { ["hy"] = hy, ["ru"] = ru };

    private Task<AdminPageDto> CreatePageAsync(string slug, bool footer = true, int sortOrder = 1, string body = "Տեքստ") =>
        new CreatePageHandler(_db).HandleAsync(new CreatePage(slug, Texts($"Էջ {slug}", $"Страница {slug}"), Texts(body), footer, sortOrder), CancellationToken.None);

    private Task<AdminPageDto> PublishAsync(Guid id, bool published = true) =>
        new SetPagePublishedHandler(_db, _clock).HandleAsync(new SetPagePublished(id, published), CancellationToken.None);

    private async Task<AdminFaqDto> CreateFaqAsync(string question, FaqAudience audience, int sortOrder, bool publish = true)
    {
        var faq = await new CreateFaqHandler(_db).HandleAsync(
            new CreateFaq(Texts(question, $"{question} (ru)"), Texts("Պատասխան"), audience, sortOrder), CancellationToken.None);
        return publish ? await new SetFaqPublishedHandler(_db).HandleAsync(new SetFaqPublished(faq.Id, true), CancellationToken.None) : faq;
    }

    [Fact]
    public async Task Pages_are_created_as_drafts_and_listed_for_staff()
    {
        var terms = await CreatePageAsync("terms", sortOrder: 2);
        await CreatePageAsync("about", footer: false, sortOrder: 1);

        terms.IsPublished.ShouldBeFalse();
        terms.Title.ShouldBe(new Dictionary<string, string> { ["hy"] = "Էջ terms", ["ru"] = "Страница terms" }, ignoreOrder: true);
        (await new GetAdminPagesHandler(_db).HandleAsync(new GetAdminPages(), CancellationToken.None)).Select(p => p.Slug)
            .ShouldBe(new[] { "about", "terms" });
        (await new GetAdminPageHandler(_db).HandleAsync(new GetAdminPage(terms.Id), CancellationToken.None)).Slug.ShouldBe("terms");
    }

    [Fact]
    public async Task Page_addresses_are_unique()
    {
        var terms = await CreatePageAsync("terms");
        var privacy = await CreatePageAsync("privacy");

        (await Should.ThrowAsync<ConflictException>(() => CreatePageAsync("TERMS"))).Code.ShouldBe("page.slug_taken");
        (await Should.ThrowAsync<ConflictException>(() => new UpdatePageHandler(_db).HandleAsync(
            new UpdatePage(privacy.Id, "terms", Texts("x"), Texts("y"), true, 1), CancellationToken.None))).Code.ShouldBe("page.slug_taken");
        (await new UpdatePageHandler(_db).HandleAsync(new UpdatePage(terms.Id, "terms", Texts("Նոր"), Texts("Նոր տեքստ"), false, 5), CancellationToken.None))
            .SortOrder.ShouldBe(5);
    }

    [Fact]
    public async Task Only_published_pages_are_public_in_the_request_language()
    {
        var terms = await CreatePageAsync("terms", sortOrder: 2);
        var about = await CreatePageAsync("about", footer: false, sortOrder: 1);
        await CreatePageAsync("draft");
        await PublishAsync(terms.Id);
        await PublishAsync(about.Id);
        var russian = new FakeCurrentLanguage("ru");

        var links = await new GetPublicPagesHandler(_db, russian).HandleAsync(new GetPublicPages(), CancellationToken.None);
        links.ShouldBe(new[] { new PublicPageLinkDto("about", "Страница about", false), new PublicPageLinkDto("terms", "Страница terms", true) });

        var page = await new GetPublicPageHandler(_db, russian).HandleAsync(new GetPublicPage(" TERMS "), CancellationToken.None);
        page.Title.ShouldBe("Страница terms");
        page.Body.ShouldBe("Տեքստ");
        (await Should.ThrowAsync<NotFoundException>(() => new GetPublicPageHandler(_db, russian).HandleAsync(new GetPublicPage("draft"), CancellationToken.None)))
            .Code.ShouldBe("page.not_found");
    }

    [Fact]
    public async Task Pages_are_published_unpublished_and_deleted()
    {
        var empty = await CreatePageAsync("empty", body: "");
        (await Should.ThrowAsync<DomainException>(() => PublishAsync(empty.Id))).Code.ShouldBe("page.body_required");

        var terms = await CreatePageAsync("terms");
        (await PublishAsync(terms.Id)).PublishedAt.ShouldBe(Now);
        (await PublishAsync(terms.Id, false)).IsPublished.ShouldBeFalse();

        (await new DeletePageHandler(_db).HandleAsync(new DeletePage(terms.Id), CancellationToken.None)).ShouldBeTrue();
        (await _db.StaticPages.AnyAsync(p => p.Id == terms.Id)).ShouldBeFalse();
        (await Should.ThrowAsync<NotFoundException>(() => PublishAsync(terms.Id))).Code.ShouldBe("page.not_found");
    }

    [Fact]
    public async Task Questions_are_grouped_by_audience_and_only_published_ones_are_public()
    {
        await CreateFaqAsync("Partner question", FaqAudience.Partners, 1);
        await CreateFaqAsync("Second general", FaqAudience.General, 2);
        var first = await CreateFaqAsync("First general", FaqAudience.General, 1);
        await CreateFaqAsync("Hidden", FaqAudience.General, 0, publish: false);
        var russian = new FakeCurrentLanguage("ru");

        var all = await new GetPublicFaqsHandler(_db, russian).HandleAsync(new GetPublicFaqs(), CancellationToken.None);
        all.Select(f => f.Question).ShouldBe(new[] { "First general (ru)", "Second general (ru)", "Partner question (ru)" });
        all[0].ShouldBe(new PublicFaqDto(first.Id, "First general (ru)", "Պատասխան", "General"));

        var partners = await new GetPublicFaqsHandler(_db, russian).HandleAsync(new GetPublicFaqs(FaqAudience.Partners), CancellationToken.None);
        partners.ShouldHaveSingleItem().Audience.ShouldBe("Partners");

        (await new GetAdminFaqsHandler(_db).HandleAsync(new GetAdminFaqs(), CancellationToken.None)).Count.ShouldBe(4);
    }

    [Fact]
    public async Task Questions_are_edited_hidden_and_deleted()
    {
        var faq = await CreateFaqAsync("Question", FaqAudience.General, 1);

        var updated = await new UpdateFaqHandler(_db).HandleAsync(new UpdateFaq(faq.Id, Texts("Նոր հարց"), Texts("Նոր պատասխան"), FaqAudience.Customers, 4), CancellationToken.None);
        updated.Audience.ShouldBe("Customers");
        updated.Question.ShouldBe(new Dictionary<string, string> { ["hy"] = "Նոր հարց" });

        (await new SetFaqPublishedHandler(_db).HandleAsync(new SetFaqPublished(faq.Id, false), CancellationToken.None)).IsPublished.ShouldBeFalse();
        (await new DeleteFaqHandler(_db).HandleAsync(new DeleteFaq(faq.Id), CancellationToken.None)).ShouldBeTrue();
        (await Should.ThrowAsync<NotFoundException>(() => new DeleteFaqHandler(_db).HandleAsync(new DeleteFaq(faq.Id), CancellationToken.None)))
            .Code.ShouldBe("faq.not_found");
    }

    [Fact]
    public async Task Page_and_question_fields_are_validated()
    {
        (await new CreatePageValidator(_languages).ValidateAsync(new CreatePage("terms", Texts("Պայմաններ"), new Dictionary<string, string>(), true, 0)))
            .IsValid.ShouldBeTrue();
        (await new CreatePageValidator(_languages).ValidateAsync(new CreatePage(
                "Terms of use",
                new Dictionary<string, string> { ["ru"] = new string('t', 201) },
                new Dictionary<string, string> { ["french"] = "x" },
                true,
                -1)))
            .Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "slug.invalid", "title.too_long", "title.default_language_required", "body.language_invalid", "sort_order.invalid" }, ignoreOrder: true);
        (await new UpdatePageValidator(_languages).ValidateAsync(new UpdatePage(Guid.NewGuid(), "terms", null!, null!, false, 0)))
            .Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "title.required", "body.required" }, ignoreOrder: true);

        (await new CreateFaqValidator(_languages).ValidateAsync(new CreateFaq(Texts("?"), Texts("!"), FaqAudience.General, 0))).IsValid.ShouldBeTrue();
        (await new UpdateFaqValidator(_languages).ValidateAsync(new UpdateFaq(Guid.NewGuid(), Texts(new string('q', 301)), new Dictionary<string, string> { ["ru"] = "!" }, (FaqAudience)9, -1)))
            .Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "question.too_long", "answer.default_language_required", "audience.invalid", "sort_order.invalid" }, ignoreOrder: true);
    }
}
