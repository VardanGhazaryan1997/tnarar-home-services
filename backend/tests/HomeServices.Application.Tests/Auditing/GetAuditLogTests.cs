using HomeServices.Application.Auditing;
using HomeServices.Application.Common;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Auditing;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Staff;

namespace HomeServices.Application.Tests.Auditing;

public class GetAuditLogTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();

    private Task<PagedResult<AuditLogEntryDto>> Handle(GetAuditLog query) =>
        new GetAuditLogHandler(_db).HandleAsync(query, CancellationToken.None);

    private async Task<AuditLogEntry> Given(
        int minutesAfterStart,
        AuditAction action = AuditAction.Updated,
        string entityType = "Category",
        string entityId = "cat-1",
        AuditActorType actorType = AuditActorType.System,
        string? actorId = null,
        params AuditPropertyChange[] changes)
    {
        var entry = AuditLogEntry.Record(Start.AddMinutes(minutesAfterStart), actorType, actorId, action, entityType, entityId, changes);
        _db.AuditLog.Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    [Fact]
    public async Task Entries_are_listed_newest_first()
    {
        var first = await Given(1);
        var third = await Given(3);
        var second = await Given(2);

        var result = await Handle(new GetAuditLog());

        result.Items.Select(e => e.Id).ShouldBe(new[] { third.Id, second.Id, first.Id });
        result.TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task Entries_include_what_changed()
    {
        await Given(1, AuditAction.Updated, "Category", "cat-1", AuditActorType.System, null, new AuditPropertyChange("IsActive", "true", "false"));

        var entry = (await Handle(new GetAuditLog())).Items.Single();

        entry.Action.ShouldBe("Updated");
        entry.ActorType.ShouldBe("System");
        entry.EntityType.ShouldBe("Category");
        entry.EntityId.ShouldBe("cat-1");
        entry.Changes.ShouldBe(new[] { new AuditChangeDto("IsActive", "true", "false") });
    }

    [Fact]
    public async Task Entries_can_be_filtered_by_entity()
    {
        var match = await Given(1, entityType: "Category", entityId: "cat-1");
        await Given(2, entityType: "Category", entityId: "cat-2");
        await Given(3, entityType: "City", entityId: "cat-1");

        var result = await Handle(new GetAuditLog(EntityType: "Category", EntityId: "cat-1"));

        result.Items.Select(e => e.Id).ShouldBe(new[] { match.Id });
    }

    [Fact]
    public async Task Entries_can_be_filtered_by_actor_and_action()
    {
        var match = await Given(1, AuditAction.Deleted, actorType: AuditActorType.Staff, actorId: "staff-1");
        await Given(2, AuditAction.Updated, actorType: AuditActorType.Staff, actorId: "staff-1");
        await Given(3, AuditAction.Deleted, actorType: AuditActorType.Staff, actorId: "staff-2");

        var result = await Handle(new GetAuditLog(ActorId: "staff-1", Action: AuditAction.Deleted));

        result.Items.Select(e => e.Id).ShouldBe(new[] { match.Id });
    }

    [Fact]
    public async Task Entries_can_be_filtered_by_time_range_inclusive()
    {
        await Given(1);
        var from = await Given(2);
        var to = await Given(3);
        await Given(4);

        var result = await Handle(new GetAuditLog(From: Start.AddMinutes(2), To: Start.AddMinutes(3)));

        result.Items.Select(e => e.Id).ShouldBe(new[] { to.Id, from.Id });
    }

    [Fact]
    public async Task Results_are_paged()
    {
        for (var minute = 1; minute <= 5; minute++)
        {
            await Given(minute);
        }

        var result = await Handle(new GetAuditLog(Page: 2, PageSize: 2));

        result.Items.Count.ShouldBe(2);
        result.Items.Select(e => e.OccurredAt).ShouldBe(new[] { Start.AddMinutes(3), Start.AddMinutes(2) });
        result.Page.ShouldBe(2);
        result.PageSize.ShouldBe(2);
        result.TotalCount.ShouldBe(5);
        result.TotalPages.ShouldBe(3);
    }

    [Fact]
    public async Task Staff_and_user_names_are_shown_for_their_changes()
    {
        var staff = StaffUser.Create("ani@example.com", "Ani Staff", "hash");
        var user = User.Register(PhoneNumber.Parse("+37491234567"));
        user.UpdateProfile("Aram Customer", null);
        _db.StaffUsers.Add(staff);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        await Given(1, actorType: AuditActorType.Staff, actorId: staff.Id.ToString());
        await Given(2, actorType: AuditActorType.User, actorId: user.Id.ToString());
        await Given(3, actorType: AuditActorType.Staff, actorId: Guid.NewGuid().ToString());
        await Given(4);

        var names = (await Handle(new GetAuditLog())).Items.Select(e => e.ActorName);

        names.ShouldBe(new[] { null, null, "Aram Customer", "Ani Staff" });
    }

    [Theory]
    [InlineData(0, 50, "page.invalid")]
    [InlineData(1, 0, "page_size.invalid")]
    [InlineData(1, 201, "page_size.invalid")]
    public void Page_and_page_size_are_validated(int page, int pageSize, string code)
    {
        var errors = new GetAuditLogValidator().Validate(new GetAuditLog(Page: page, PageSize: pageSize)).Errors;

        errors.Select(e => e.ErrorCode).ShouldBe(new[] { code });
    }

    [Fact]
    public void The_time_range_must_not_end_before_it_starts()
    {
        var errors = new GetAuditLogValidator().Validate(new GetAuditLog(From: Start, To: Start.AddMinutes(-1))).Errors;

        errors.Select(e => e.ErrorCode).ShouldBe(new[] { "range.invalid" });
    }

    [Fact]
    public void Unknown_actions_are_rejected()
    {
        var errors = new GetAuditLogValidator().Validate(new GetAuditLog(Action: (AuditAction)99)).Errors;

        errors.Select(e => e.ErrorCode).ShouldBe(new[] { "action.invalid" });
    }

    [Fact]
    public void The_defaults_are_valid()
    {
        new GetAuditLogValidator().Validate(new GetAuditLog()).IsValid.ShouldBeTrue();
    }
}
