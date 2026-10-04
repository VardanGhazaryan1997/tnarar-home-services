using HomeServices.Domain.Content;
using HomeServices.Domain.Localization;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Infrastructure.IntegrationTests.Content;

[Collection(PostgresCollection.Name)]
public class ContentStorageTests(PostgresFixture db)
{
    private static LocalizedText Text(string hy) => LocalizedText.Empty.With("hy", hy);

    [Fact]
    public async Task A_deleted_pages_address_can_be_used_again()
    {
        var slug = $"page-{Guid.NewGuid():N}"[..20];
        var page = StaticPage.Create(slug, Text("Էջ"), Text("Տեքստ"), true, 1);
        await using (var context = db.CreateContext())
        {
            context.StaticPages.Add(page);
            await context.SaveChangesAsync();
            context.StaticPages.Remove(page);
            await context.SaveChangesAsync();
        }

        await using (var context = db.CreateContext())
        {
            context.StaticPages.Add(StaticPage.Create(slug, Text("Նոր"), Text("Տեքստ"), true, 1));
            await context.SaveChangesAsync();

            (await context.StaticPages.CountAsync(p => p.Slug == slug)).ShouldBe(1);
            (await context.StaticPages.IgnoreQueryFilters().CountAsync(p => p.Slug == slug)).ShouldBe(2);
        }
    }

    [Fact]
    public async Task Two_live_pages_cannot_share_an_address()
    {
        var slug = $"page-{Guid.NewGuid():N}"[..20];
        await using var context = db.CreateContext();
        context.StaticPages.AddRange(StaticPage.Create(slug, Text("Ա"), Text("x"), false, 1), StaticPage.Create(slug, Text("Բ"), Text("y"), false, 2));

        await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Questions_are_saved_with_their_texts_and_audience()
    {
        var faq = FaqItem.Create(Text("Ինչպե՞ս"), Text("Այսպես"), FaqAudience.Customers, 2);
        await using (var context = db.CreateContext())
        {
            context.FaqItems.Add(faq);
            await context.SaveChangesAsync();
        }

        await using (var context = db.CreateContext())
        {
            var saved = await context.FaqItems.SingleAsync(f => f.Id == faq.Id);
            saved.Audience.ShouldBe(FaqAudience.Customers);
            saved.Answer.Get("ru", "hy").ShouldBe("Այսպես");
        }
    }
}
