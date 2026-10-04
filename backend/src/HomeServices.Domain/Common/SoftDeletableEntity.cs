namespace HomeServices.Domain.Common;

/// <summary>An auditable entity that is soft-deleted (see <see cref="ISoftDeletable"/>).</summary>
public abstract class SoftDeletableEntity : AuditableEntity, ISoftDeletable
{
    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public string? DeletedBy { get; private set; }

    public void MarkDeleted(DateTimeOffset at, string? by)
    {
        IsDeleted = true;
        DeletedAt = at;
        DeletedBy = by;
    }
}
