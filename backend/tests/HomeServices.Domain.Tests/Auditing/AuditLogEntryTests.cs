using System.Reflection;
using HomeServices.Domain.Auditing;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Common;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Staff;

namespace HomeServices.Domain.Tests.Auditing;

public class AuditLogEntryTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void An_entry_records_who_changed_what_and_when()
    {
        var entry = AuditLogEntry.Record(
            Now, AuditActorType.Staff, "staff-1", AuditAction.Updated, "Category", "cat-1",
            [new AuditPropertyChange("IsActive", "true", "false")], "trace-1");

        entry.OccurredAt.ShouldBe(Now);
        entry.ActorType.ShouldBe(AuditActorType.Staff);
        entry.ActorId.ShouldBe("staff-1");
        entry.Action.ShouldBe(AuditAction.Updated);
        entry.EntityType.ShouldBe("Category");
        entry.EntityId.ShouldBe("cat-1");
        entry.Changes.ShouldBe(new[] { new AuditPropertyChange("IsActive", "true", "false") });
        entry.TraceId.ShouldBe("trace-1");
    }

    [Fact]
    public void System_changes_have_no_actor()
    {
        var entry = AuditLogEntry.Record(Now, AuditActorType.System, null, AuditAction.Created, "Role", "r-1", []);

        entry.ActorId.ShouldBeNull();
        entry.TraceId.ShouldBeNull();
    }

    [Theory]
    [InlineData(AuditActorType.User, null)]
    [InlineData(AuditActorType.Staff, " ")]
    [InlineData(AuditActorType.System, "staff-1")]
    public void The_actor_id_must_match_the_actor_type(AuditActorType type, string? actorId)
    {
        Should.Throw<DomainException>(() => AuditLogEntry.Record(Now, type, actorId, AuditAction.Updated, "Role", "r-1", []))
            .Code.ShouldBe("audit.actor_invalid");
    }

    [Theory]
    [InlineData("", "r-1")]
    [InlineData("Role", " ")]
    public void The_entity_type_and_id_are_required(string entityType, string entityId)
    {
        Should.Throw<DomainException>(() => AuditLogEntry.Record(Now, AuditActorType.System, null, AuditAction.Created, entityType, entityId, []))
            .Code.ShouldBe("audit.entity_required");
    }

    [Theory]
    [InlineData(typeof(Language))]
    [InlineData(typeof(Category))]
    [InlineData(typeof(City))]
    [InlineData(typeof(District))]
    [InlineData(typeof(User))]
    [InlineData(typeof(StaffUser))]
    [InlineData(typeof(StaffUserRole))]
    [InlineData(typeof(Role))]
    public void Business_entities_are_audited(Type type)
    {
        typeof(IAudited).IsAssignableFrom(type).ShouldBeTrue();
    }

    [Theory]
    [InlineData(typeof(OtpCode))]
    [InlineData(typeof(RefreshToken))]
    [InlineData(typeof(StaffRefreshToken))]
    [InlineData(typeof(AuditLogEntry))]
    public void Tokens_codes_and_the_log_itself_are_not_audited(Type type)
    {
        typeof(IAudited).IsAssignableFrom(type).ShouldBeFalse();
    }

    [Theory]
    [InlineData(nameof(StaffUser.PasswordHash))]
    [InlineData(nameof(StaffUser.TotpSecret))]
    public void Staff_secrets_are_redacted(string property)
    {
        typeof(StaffUser).GetProperty(property)!.GetCustomAttribute<AuditRedactedAttribute>().ShouldNotBeNull();
    }

    [Theory]
    [InlineData(typeof(StaffUser), nameof(StaffUser.PendingTotpSecret))]
    [InlineData(typeof(StaffUser), nameof(StaffUser.LastTotpTimeStep))]
    [InlineData(typeof(StaffUser), nameof(StaffUser.FailedSignInCount))]
    [InlineData(typeof(StaffUser), nameof(StaffUser.LockedUntil))]
    [InlineData(typeof(StaffUser), nameof(StaffUser.LastSignInAt))]
    [InlineData(typeof(User), nameof(User.LastSignInAt))]
    public void Sign_in_bookkeeping_is_not_audited(Type type, string property)
    {
        type.GetProperty(property)!.GetCustomAttribute<NotAuditedAttribute>().ShouldNotBeNull();
    }
}
