using HomeServices.Domain.Localization;
using HomeServices.Infrastructure.IntegrationTests.Fakes;
using HomeServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public class LocalizedTextStorageTests(PostgresFixture db)
{
    [Fact]
    public async Task Translations_are_saved_and_loaded_unchanged()
    {
        var title = LocalizedText.From(new Dictionary<string, string> { ["hy"] = "Սանտեխնիկա", ["ru"] = "Сантехника", ["en"] = "Plumbing" });
        var widget = new Widget { DisplayName = "w", Title = title };
        await using (var context = db.CreateContext())
        {
            context.Widgets.Add(widget);
            await context.SaveChangesAsync();
        }

        await using var check = db.CreateContext();
        (await check.Widgets.SingleAsync(w => w.Id == widget.Id)).Title.ShouldBe(title);
    }

    [Fact]
    public async Task Replacing_a_translation_is_detected_and_saved()
    {
        var widget = new Widget { DisplayName = "w", Title = LocalizedText.Empty.With("hy", "Մաքրում") };
        await using (var context = db.CreateContext())
        {
            context.Widgets.Add(widget);
            await context.SaveChangesAsync();
        }

        await using (var context = db.CreateContext())
        {
            var loaded = await context.Widgets.SingleAsync(w => w.Id == widget.Id);
            loaded.Title = loaded.Title.With("en", "Cleaning");
            await context.SaveChangesAsync();
        }

        await using var check = db.CreateContext();
        (await check.Widgets.SingleAsync(w => w.Id == widget.Id)).Title.Get("en", "hy").ShouldBe("Cleaning");
    }

    [Fact]
    public async Task Translations_are_stored_as_jsonb()
    {
        await using var context = db.CreateContext();

        var type = await context.Database
            .SqlQueryRaw<string>("SELECT data_type AS \"Value\" FROM information_schema.columns WHERE table_name = 'widgets' AND column_name = 'title'")
            .SingleAsync();

        type.ShouldBe("jsonb");
    }

    [Fact]
    public void Empty_or_null_json_reads_as_empty_text()
    {
        LocalizedTextConverter.FromJson("{}").ShouldBe(LocalizedText.Empty);
        LocalizedTextConverter.FromJson("null").ShouldBe(LocalizedText.Empty);
    }
}
