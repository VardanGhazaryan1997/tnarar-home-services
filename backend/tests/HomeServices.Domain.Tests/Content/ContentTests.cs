using HomeServices.Domain.Content;
using HomeServices.Domain.Localization;

namespace HomeServices.Domain.Tests.Content;

public class ContentTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    private static LocalizedText Text(string hy) => LocalizedText.Empty.With("hy", hy);

    [Fact]
    public void A_page_starts_as_a_draft()
    {
        var page = StaticPage.Create(" Terms ", Text("Պայմաններ"), LocalizedText.Empty, showInFooter: true, sortOrder: 2);

        page.Slug.ShouldBe("terms");
        page.Title.Get("hy", "hy").ShouldBe("Պայմաններ");
        page.ShowInFooter.ShouldBeTrue();
        page.SortOrder.ShouldBe(2);
        page.IsPublished.ShouldBeFalse();
        page.PublishedAt.ShouldBeNull();
    }

    [Fact]
    public void A_page_needs_a_title_and_a_valid_slug()
    {
        Should.Throw<DomainException>(() => StaticPage.Create("terms", LocalizedText.Empty, LocalizedText.Empty, false, 1)).Code.ShouldBe("page.title_required");
        Should.Throw<DomainException>(() => StaticPage.Create("terms of use", Text("x"), LocalizedText.Empty, false, 1)).Code.ShouldBe("page.slug_invalid");
    }

    [Fact]
    public void Only_a_written_page_is_published_and_it_keeps_its_first_publication_date()
    {
        var page = StaticPage.Create("terms", Text("Պայմաններ"), LocalizedText.Empty, false, 1);
        Should.Throw<DomainException>(() => page.Publish(Now)).Code.ShouldBe("page.body_required");

        page.Update("terms", Text("Պայմաններ"), Text("Տեքստ"), false, 1);
        page.Publish(Now);
        page.Unpublish();
        page.Publish(Now.AddDays(1));

        page.IsPublished.ShouldBeTrue();
        page.PublishedAt.ShouldBe(Now);
        Should.Throw<DomainException>(() => page.Update("terms", Text("Պայմաններ"), LocalizedText.Empty, false, 1)).Code.ShouldBe("page.body_required");
    }

    [Fact]
    public void A_question_needs_both_texts_and_a_known_audience()
    {
        var faq = FaqItem.Create(Text("Ինչպե՞ս"), Text("Այսպես"), FaqAudience.Partners, 3);

        faq.Audience.ShouldBe(FaqAudience.Partners);
        faq.SortOrder.ShouldBe(3);
        faq.IsPublished.ShouldBeFalse();
        faq.Publish();
        faq.IsPublished.ShouldBeTrue();
        faq.Unpublish();
        faq.IsPublished.ShouldBeFalse();

        Should.Throw<DomainException>(() => FaqItem.Create(Text("?"), LocalizedText.Empty, FaqAudience.General, 1)).Code.ShouldBe("faq.text_required");
        Should.Throw<DomainException>(() => faq.Update(Text("?"), Text("!"), (FaqAudience)9, 1)).Code.ShouldBe("faq.audience_invalid");
    }
}
