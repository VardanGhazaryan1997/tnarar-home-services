using HomeServices.Infrastructure.IntegrationTests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public class AuditingTests(PostgresFixture db)
{
    [Fact]
    public async Task Saving_a_new_entity_records_who_created_it_and_when()
    {
        db.CurrentUser.UserId = "staff-1";
        var widget = new Widget { DisplayName = "Plumbing" };

        await using (var context = db.CreateContext())
        {
            context.Widgets.Add(widget);
            await context.SaveChangesAsync();
        }

        await using var check = db.CreateContext();
        var saved = await check.Widgets.SingleAsync(w => w.Id == widget.Id);
        saved.CreatedAt.ShouldBe(db.Clock.Now);
        saved.CreatedBy.ShouldBe("staff-1");
        saved.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Updating_records_who_changed_it_and_keeps_the_creation_details()
    {
        var created = db.Clock.Now;
        var widget = await SaveNewWidget("Heating", "staff-1");

        db.CurrentUser.UserId = "staff-2";
        db.Clock.Now = created.AddHours(3);
        await using (var context = db.CreateContext())
        {
            var loaded = await context.Widgets.SingleAsync(w => w.Id == widget.Id);
            loaded.Rename("Heating and cooling");
            await context.SaveChangesAsync();
        }

        await using var check = db.CreateContext();
        var saved = await check.Widgets.SingleAsync(w => w.Id == widget.Id);
        saved.DisplayName.ShouldBe("Heating and cooling");
        saved.UpdatedAt.ShouldBe(created.AddHours(3));
        saved.UpdatedBy.ShouldBe("staff-2");
        saved.CreatedAt.ShouldBe(created);
        saved.CreatedBy.ShouldBe("staff-1");
        db.Clock.Now = created;
    }

    [Fact]
    public async Task Deleting_a_soft_deletable_entity_hides_it_but_keeps_the_row()
    {
        var widget = await SaveNewWidget("Cladding", "staff-1");

        db.CurrentUser.UserId = "staff-3";
        await using (var context = db.CreateContext())
        {
            var loaded = await context.Widgets.SingleAsync(w => w.Id == widget.Id);
            context.Widgets.Remove(loaded);
            await context.SaveChangesAsync();
        }

        await using var check = db.CreateContext();
        (await check.Widgets.AnyAsync(w => w.Id == widget.Id)).ShouldBeFalse();
        var deleted = await check.Widgets.IgnoreQueryFilters().SingleAsync(w => w.Id == widget.Id);
        deleted.IsDeleted.ShouldBeTrue();
        deleted.DeletedAt.ShouldBe(db.Clock.Now);
        deleted.DeletedBy.ShouldBe("staff-3");
    }

    [Fact]
    public async Task Synchronous_saves_are_audited_too()
    {
        db.CurrentUser.UserId = "staff-4";
        var widget = new Widget { DisplayName = "Electrical" };

        using (var context = db.CreateContext())
        {
            context.Widgets.Add(widget);
            context.SaveChanges();
        }

        await using var check = db.CreateContext();
        (await check.Widgets.SingleAsync(w => w.Id == widget.Id)).CreatedBy.ShouldBe("staff-4");
    }

    [Fact]
    public async Task Domain_events_are_not_stored()
    {
        var widget = await SaveNewWidget("Cleaning", null);

        await using var context = db.CreateContext();
        var loaded = await context.Widgets.SingleAsync(w => w.Id == widget.Id);

        loaded.DomainEvents.ShouldBeEmpty();
    }

    private async Task<Widget> SaveNewWidget(string name, string? user)
    {
        db.CurrentUser.UserId = user;
        var widget = new Widget { DisplayName = name };
        await using var context = db.CreateContext();
        context.Widgets.Add(widget);
        await context.SaveChangesAsync();
        return widget;
    }
}
