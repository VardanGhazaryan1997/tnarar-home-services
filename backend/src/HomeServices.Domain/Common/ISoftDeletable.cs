namespace HomeServices.Domain.Common;

/// <summary>
/// Deleting this entity hides it instead of removing the row. Normal queries skip
/// deleted rows; history (orders, payments, audit) can still reach them.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }

    DateTimeOffset? DeletedAt { get; }

    string? DeletedBy { get; }

    void MarkDeleted(DateTimeOffset at, string? by);
}
