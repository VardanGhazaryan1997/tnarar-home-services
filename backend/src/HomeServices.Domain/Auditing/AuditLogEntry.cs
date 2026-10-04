using HomeServices.Domain.Common;

namespace HomeServices.Domain.Auditing;

public enum AuditAction
{
    Created = 1,
    Updated = 2,
    Deleted = 3,
}

public enum AuditActorType
{
    /// <summary>Startup tasks, background jobs and anything without a signed-in user.</summary>
    System = 1,

    /// <summary>A Portal user (customer or partner).</summary>
    User = 2,

    /// <summary>A Back Office staff member.</summary>
    Staff = 3,
}

/// <summary>One changed property. Values are display strings; secret values are replaced by <see cref="AuditLogEntry.RedactedValue"/>.</summary>
public sealed record AuditPropertyChange(string Property, string? OldValue, string? NewValue);

/// <summary>
/// An immutable record of one change to an audited entity: who made it, when, and the old and new values.
/// Written automatically when changes are saved; never edited afterwards.
/// </summary>
public sealed class AuditLogEntry : Entity
{
    public const int EntityTypeMaxLength = 100;
    public const int EntityIdMaxLength = 200;
    public const int ActorIdMaxLength = 100;
    public const int TraceIdMaxLength = 100;
    public const string RedactedValue = "***";

    private AuditLogEntry()
    {
    }

    public DateTimeOffset OccurredAt { get; private set; }

    public AuditActorType ActorType { get; private set; }

    /// <summary>The user or staff id; null for system changes.</summary>
    public string? ActorId { get; private set; }

    public AuditAction Action { get; private set; }

    /// <summary>The entity's class name, e.g. "Category".</summary>
    public string EntityType { get; private set; } = string.Empty;

    /// <summary>The entity's key; composite keys are joined with "/".</summary>
    public string EntityId { get; private set; } = string.Empty;

    public IReadOnlyList<AuditPropertyChange> Changes { get; private set; } = [];

    /// <summary>The request's trace id, to find the matching logs.</summary>
    public string? TraceId { get; private set; }

    public static AuditLogEntry Record(
        DateTimeOffset occurredAt,
        AuditActorType actorType,
        string? actorId,
        AuditAction action,
        string entityType,
        string entityId,
        IEnumerable<AuditPropertyChange> changes,
        string? traceId = null)
    {
        if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(entityId))
        {
            throw new DomainException("audit.entity_required", "An audit entry needs the entity type and id.");
        }

        if ((actorType == AuditActorType.System) != string.IsNullOrWhiteSpace(actorId))
        {
            throw new DomainException("audit.actor_invalid", "User and staff changes need an actor id; system changes have none.");
        }

        return new AuditLogEntry
        {
            OccurredAt = occurredAt,
            ActorType = actorType,
            ActorId = actorType == AuditActorType.System ? null : actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Changes = changes.ToList(),
            TraceId = traceId,
        };
    }
}
