namespace HomeServices.Domain.Common;

/// <summary>
/// Changes to this entity are written to the audit log (who changed what, when, old and new values).
/// Use <see cref="NotAuditedAttribute"/> and <see cref="AuditRedactedAttribute"/> on properties that
/// are noisy or secret.
/// </summary>
public interface IAudited;

/// <summary>The audit log ignores this property: changing only it writes no audit entry.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class NotAuditedAttribute : Attribute;

/// <summary>The audit log records that this property changed, but never its values.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class AuditRedactedAttribute : Attribute;
