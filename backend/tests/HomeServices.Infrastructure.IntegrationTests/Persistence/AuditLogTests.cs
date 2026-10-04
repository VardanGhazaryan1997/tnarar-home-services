using HomeServices.Domain.Auditing;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Staff;
using HomeServices.Infrastructure.IntegrationTests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public class AuditLogTests(PostgresFixture db)
{
    private void ActAs(string? userId, bool isStaff = false)
    {
        db.CurrentUser.UserId = userId;
        db.CurrentUser.IsStaff = isStaff;
    }

    private async Task<Widget> SaveNewWidget(string name = "Plumbing")
    {
        var widget = new Widget { DisplayName = name };
        await using var context = db.CreateContext();
        context.Widgets.Add(widget);
        await context.SaveChangesAsync();
        return widget;
    }

    private async Task ChangeWidget(Guid id, Action<Widget> change)
    {
        await using var context = db.CreateContext();
        change(await context.Widgets.SingleAsync(w => w.Id == id));
        await context.SaveChangesAsync();
    }

    private async Task<List<AuditLogEntry>> EntriesFor(object id, AuditAction? action = null)
    {
        await using var context = db.CreateContext();
        var entityId = id.ToString()!;
        var entries = await context.AuditLog.Where(e => e.EntityId == entityId).ToListAsync();
        return entries.Where(e => action is null || e.Action == action).ToList();
    }

    [Fact]
    public async Task Creating_an_audited_entity_logs_who_did_it_and_the_values()
    {
        ActAs("staff-1", isStaff: true);
        var widget = new Widget { DisplayName = "Plumbing", Title = LocalizedText.From(new Dictionary<string, string> { ["en"] = "Water supply" }), Views = 7 };
        await using (var context = db.CreateContext())
        {
            context.Widgets.Add(widget);
            await context.SaveChangesAsync();
        }

        var entry = (await EntriesFor(widget.Id)).ShouldHaveSingleItem();
        entry.Action.ShouldBe(AuditAction.Created);
        entry.ActorType.ShouldBe(AuditActorType.Staff);
        entry.ActorId.ShouldBe("staff-1");
        entry.EntityType.ShouldBe(nameof(Widget));
        entry.OccurredAt.ShouldBe(db.Clock.Now);
        entry.Changes.ShouldContain(new AuditPropertyChange("DisplayName", null, "Plumbing"));
        entry.Changes.Single(c => c.Property == "Title").NewValue.ShouldNotBeNull().ShouldContain("Water supply");
        entry.Changes.Select(c => c.Property).ShouldNotContain("Id");
        entry.Changes.Select(c => c.Property).ShouldNotContain("CreatedAt");
        entry.Changes.Select(c => c.Property).ShouldNotContain("Views");
    }

    [Fact]
    public async Task Updating_logs_only_the_changed_values_with_old_and_new()
    {
        ActAs(null);
        var widget = await SaveNewWidget("Heating");

        ActAs("staff-2", isStaff: true);
        await ChangeWidget(widget.Id, w => w.Rename("Heating and cooling"));

        var entry = (await EntriesFor(widget.Id, AuditAction.Updated)).ShouldHaveSingleItem();
        entry.ActorId.ShouldBe("staff-2");
        entry.Changes.ShouldBe(new[] { new AuditPropertyChange("DisplayName", "Heating", "Heating and cooling") });
    }

    [Fact]
    public async Task Changing_only_ignored_properties_logs_nothing()
    {
        ActAs(null);
        var widget = await SaveNewWidget();

        await ChangeWidget(widget.Id, w => w.Views++);

        (await EntriesFor(widget.Id, AuditAction.Updated)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Secret_values_are_redacted_but_the_change_is_logged()
    {
        ActAs(null);
        var widget = await SaveNewWidget();

        await ChangeWidget(widget.Id, w => w.Secret = "s3cret");
        await ChangeWidget(widget.Id, w => w.Secret = "an0ther");

        var changes = (await EntriesFor(widget.Id, AuditAction.Updated)).SelectMany(e => e.Changes).ToList();
        changes.ShouldBe(
            new[] { new AuditPropertyChange("Secret", null, "***"), new AuditPropertyChange("Secret", "***", "***") },
            ignoreOrder: true);
    }

    [Fact]
    public async Task Soft_deleting_logs_a_deletion()
    {
        ActAs(null);
        var widget = await SaveNewWidget();

        ActAs("staff-3", isStaff: true);
        await using (var context = db.CreateContext())
        {
            context.Widgets.Remove(await context.Widgets.SingleAsync(w => w.Id == widget.Id));
            await context.SaveChangesAsync();
        }

        var entry = (await EntriesFor(widget.Id, AuditAction.Deleted)).ShouldHaveSingleItem();
        entry.ActorId.ShouldBe("staff-3");
        entry.Changes.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("user-1", false, AuditActorType.User)]
    [InlineData("staff-1", true, AuditActorType.Staff)]
    [InlineData(null, false, AuditActorType.System)]
    public async Task The_actor_type_comes_from_the_current_user(string? userId, bool isStaff, AuditActorType expected)
    {
        ActAs(userId, isStaff);

        var widget = await SaveNewWidget();

        var entry = (await EntriesFor(widget.Id)).ShouldHaveSingleItem();
        entry.ActorType.ShouldBe(expected);
        entry.ActorId.ShouldBe(userId);
    }

    [Fact]
    public async Task Entities_that_are_not_audited_are_not_logged()
    {
        ActAs(null);
        var gadget = new Gadget { DisplayName = "Not audited" };
        await using (var context = db.CreateContext())
        {
            context.Gadgets.Add(gadget);
            await context.SaveChangesAsync();
        }

        (await EntriesFor(gadget.Id)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Synchronous_saves_are_logged_too()
    {
        ActAs(null);
        var widget = new Widget { DisplayName = "Electrical" };
        using (var context = db.CreateContext())
        {
            context.Widgets.Add(widget);
            context.SaveChanges();
        }

        (await EntriesFor(widget.Id)).ShouldHaveSingleItem().Action.ShouldBe(AuditAction.Created);
    }

    [Fact]
    public async Task A_failed_save_leaves_no_entries_behind_for_the_retry()
    {
        ActAs(null);
        var name = $"Taken {Guid.NewGuid():N}";
        await using (var first = db.CreateContext())
        {
            first.Roles.Add(Role.Create(name, "", []));
            await first.SaveChangesAsync();
        }

        var duplicate = Role.Create(name, "", []);
        await using (var context = db.CreateContext())
        {
            context.Roles.Add(duplicate);
            await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());

            duplicate.Rename($"Free {Guid.NewGuid():N}", "");
            await context.SaveChangesAsync();
        }

        (await EntriesFor(duplicate.Id)).ShouldHaveSingleItem().Action.ShouldBe(AuditAction.Created);
    }

    [Fact]
    public async Task Real_entities_log_mapped_fields_lists_composite_keys_and_redacted_secrets()
    {
        ActAs(null);
        var role = Role.Create($"Night shift {Guid.NewGuid():N}", "", [Permissions.OrdersView, Permissions.AuditView]);
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", "Ani", "hash");
        staff.AssignRole(role.Id);
        await using (var context = db.CreateContext())
        {
            context.Roles.Add(role);
            context.StaffUsers.Add(staff);
            await context.SaveChangesAsync();
        }

        (await EntriesFor(role.Id)).ShouldHaveSingleItem().Changes
            .ShouldContain(new AuditPropertyChange("Permissions", null, "[\"audit.view\",\"orders.view\"]"));
        (await EntriesFor(staff.Id)).ShouldHaveSingleItem().Changes
            .ShouldContain(new AuditPropertyChange("PasswordHash", null, AuditLogEntry.RedactedValue));
        var assignment = (await EntriesFor($"{staff.Id}/{role.Id}")).ShouldHaveSingleItem();
        assignment.EntityType.ShouldBe(nameof(StaffUserRole));
        assignment.Action.ShouldBe(AuditAction.Created);
    }
}
