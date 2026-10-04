using System.Text.Json;
using HomeServices.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.Property(e => e.ActorType).HasConversion<string>().HasMaxLength(16);
        builder.Property(e => e.ActorId).HasMaxLength(AuditLogEntry.ActorIdMaxLength);
        builder.Property(e => e.Action).HasConversion<string>().HasMaxLength(16);
        builder.Property(e => e.EntityType).HasMaxLength(AuditLogEntry.EntityTypeMaxLength);
        builder.Property(e => e.EntityId).HasMaxLength(AuditLogEntry.EntityIdMaxLength);
        builder.Property(e => e.TraceId).HasMaxLength(AuditLogEntry.TraceIdMaxLength);
        // Changes → jsonb is set up in AppDbContext.ConfigureConventions, so EF never mistakes
        // AuditPropertyChange for an entity type.

        builder.HasIndex(e => new { e.EntityType, e.EntityId, e.OccurredAt });
        builder.HasIndex(e => e.OccurredAt);
        builder.HasIndex(e => e.ActorId);
    }
}

/// <summary>Stores the changed properties as a JSON array: [{"property":"Name","oldValue":"…","newValue":"…"}].</summary>
public sealed class AuditChangesConverter() : ValueConverter<IReadOnlyList<AuditPropertyChange>, string>(
    changes => ToJson(changes),
    json => FromJson(json))
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string ToJson(IReadOnlyList<AuditPropertyChange> changes) => JsonSerializer.Serialize(changes, Json);

    public static IReadOnlyList<AuditPropertyChange> FromJson(string json) =>
        JsonSerializer.Deserialize<List<AuditPropertyChange>>(json, Json) ?? [];
}

public sealed class AuditChangesComparer() : ValueComparer<IReadOnlyList<AuditPropertyChange>>(
    (a, b) => a!.SequenceEqual(b!),
    changes => changes.Aggregate(0, (hash, change) => HashCode.Combine(hash, change.GetHashCode())),
    changes => changes.ToList());
