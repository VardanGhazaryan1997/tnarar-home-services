namespace HomeServices.Domain.Common;

/// <summary>
/// An entity that records who created and last changed it. The values are set
/// automatically when changes are saved; application code doesn't set them.
/// </summary>
public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAt { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public string? UpdatedBy { get; private set; }

    public void MarkCreated(DateTimeOffset at, string? by)
    {
        CreatedAt = at;
        CreatedBy = by;
    }

    public void MarkUpdated(DateTimeOffset at, string? by)
    {
        UpdatedAt = at;
        UpdatedBy = by;
    }
}
