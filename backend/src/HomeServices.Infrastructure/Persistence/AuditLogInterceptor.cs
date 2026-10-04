using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using HomeServices.Application.Abstractions;
using HomeServices.Domain.Auditing;
using HomeServices.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HomeServices.Infrastructure.Persistence;

/// <summary>
/// Writes an <see cref="AuditLogEntry"/> for every created, changed or deleted <see cref="IAudited"/>
/// entity, in the same transaction as the change. Register it after <see cref="AuditingInterceptor"/>
/// so soft deletes are already turned into updates.
/// </summary>
public sealed class AuditLogInterceptor(TimeProvider clock, ICurrentUser currentUser) : SaveChangesInterceptor
{
    // Bookkeeping columns: already shown on the entry itself (who, when, action).
    private static readonly HashSet<string> BookkeepingProperties =
    [
        nameof(AuditableEntity.CreatedAt), nameof(AuditableEntity.CreatedBy),
        nameof(AuditableEntity.UpdatedAt), nameof(AuditableEntity.UpdatedBy),
        nameof(ISoftDeletable.IsDeleted), nameof(ISoftDeletable.DeletedAt), nameof(ISoftDeletable.DeletedBy),
    ];

    private static readonly ConcurrentDictionary<IReadOnlyProperty, AuditMode> Modes = new();

    private enum AuditMode
    {
        Audited,
        Redacted,
        Ignored,
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData) => DiscardPending(eventData.Context);

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        DiscardPending(eventData.Context);
        return Task.CompletedTask;
    }

    private void Capture(DbContext? context)
    {
        if (context?.Model.FindEntityType(typeof(AuditLogEntry)) is null)
        {
            return;
        }

        var changed = context.ChangeTracker.Entries()
            .Where(e => e.Entity is IAudited && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();
        if (changed.Count == 0)
        {
            return;
        }

        var now = clock.GetUtcNow();
        var actorId = currentUser.UserId;
        var actorType = actorId is null ? AuditActorType.System
            : currentUser.IsStaff ? AuditActorType.Staff
            : AuditActorType.User;
        var traceId = Activity.Current?.Id;

        foreach (var entry in changed)
        {
            var action = ActionFor(entry);
            var changes = ChangesFor(entry);
            if (action == AuditAction.Updated && changes.Count == 0)
            {
                continue;
            }

            context.Set<AuditLogEntry>().Add(AuditLogEntry.Record(
                now, actorType, actorId, action, entry.Metadata.ClrType.Name, KeyOf(entry), changes, Truncate(traceId)));
        }
    }

    /// <summary>A failed save must not leave its audit entries behind for the next attempt to save twice.</summary>
    private static void DiscardPending(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<AuditLogEntry>().Where(e => e.State == EntityState.Added).ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    private static AuditAction ActionFor(EntityEntry entry) => entry.State switch
    {
        EntityState.Added => AuditAction.Created,
        EntityState.Deleted => AuditAction.Deleted,
        _ when entry.Entity is ISoftDeletable &&
               entry.Property(nameof(ISoftDeletable.IsDeleted)) is { IsModified: true, CurrentValue: true, OriginalValue: false } =>
            AuditAction.Deleted,
        _ => AuditAction.Updated,
    };

    private static List<AuditPropertyChange> ChangesFor(EntityEntry entry)
    {
        var changes = new List<AuditPropertyChange>();
        var singleKey = entry.Metadata.FindPrimaryKey() is { Properties.Count: 1 };

        foreach (var property in entry.Properties)
        {
            var metadata = property.Metadata;
            var mode = ModeOf(metadata);
            if (mode == AuditMode.Ignored || BookkeepingProperties.Contains(metadata.Name) || (singleKey && metadata.IsPrimaryKey()))
            {
                continue;
            }

            object? oldValue;
            object? newValue;
            switch (entry.State)
            {
                case EntityState.Added:
                    (oldValue, newValue) = (null, property.CurrentValue);
                    break;
                case EntityState.Deleted:
                    (oldValue, newValue) = (property.OriginalValue, null);
                    break;
                default:
                    if (!property.IsModified || AreEqual(metadata, property.OriginalValue, property.CurrentValue))
                    {
                        continue;
                    }

                    (oldValue, newValue) = (property.OriginalValue, property.CurrentValue);
                    break;
            }

            if (oldValue is null && newValue is null)
            {
                continue;
            }

            var name = DisplayName(metadata.Name);
            changes.Add(mode == AuditMode.Redacted
                ? new AuditPropertyChange(name, Redact(oldValue), Redact(newValue))
                : new AuditPropertyChange(name, Format(metadata, oldValue), Format(metadata, newValue)));
        }

        return changes;
    }

    private static bool AreEqual(IProperty property, object? a, object? b) =>
        property.GetValueComparer() is { } comparer ? comparer.Equals(a, b) : Equals(a, b);

    private static AuditMode ModeOf(IReadOnlyProperty property) => Modes.GetOrAdd(property, static p =>
    {
        var member = (MemberInfo?)p.PropertyInfo ?? p.FieldInfo;
        return member?.GetCustomAttribute<NotAuditedAttribute>() is not null ? AuditMode.Ignored
            : member?.GetCustomAttribute<AuditRedactedAttribute>() is not null ? AuditMode.Redacted
            : AuditMode.Audited;
    });

    private static string KeyOf(EntityEntry entry) =>
        string.Join('/', entry.Metadata.FindPrimaryKey()!.Properties.Select(p => Format(p, entry.Property(p.Name).CurrentValue)));

    // "_permissions" (a mapped field) reads as "Permissions".
    private static string DisplayName(string name)
    {
        var trimmed = name.TrimStart('_');
        return trimmed.Length == 0 ? name : char.ToUpperInvariant(trimmed[0]) + trimmed[1..];
    }

    private static string? Redact(object? value) => value is null ? null : AuditLogEntry.RedactedValue;

    // Value objects (LocalizedText, PhoneNumber) are shown as stored, through their converter.
    private static string? Format(IReadOnlyProperty property, object? value) =>
        value is null or string or Enum or bool or IFormattable or IEnumerable
            ? FormatValue(value)
            : FormatValue((property.GetValueConverter() ?? property.FindTypeMapping()?.Converter) is { } converter
                ? converter.ConvertToProvider(value)
                : value);

    private static string? FormatValue(object? value) => value switch
    {
        null => null,
        string text => text,
        Enum enumValue => enumValue.ToString(),
        bool flag => flag ? "true" : "false",
        DateTimeOffset moment => moment.ToString("O", CultureInfo.InvariantCulture),
        DateTime moment => moment.ToString("O", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        IEnumerable items => JsonSerializer.Serialize(items.Cast<object?>().Select(FormatValue)),
        _ => value.ToString(),
    };

    private static string? Truncate(string? value) =>
        value is { Length: > AuditLogEntry.TraceIdMaxLength } ? value[..AuditLogEntry.TraceIdMaxLength] : value;
}
