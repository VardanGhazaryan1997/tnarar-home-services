using HomeServices.Domain.Common;

namespace HomeServices.Domain.Staff;

/// <summary>A named bundle of permissions assigned to staff (Operator, Finance, …). Edited by Super Admins only.</summary>
public sealed class Role : AuditableEntity, IAudited
{
    public const int NameMaxLength = 64;
    public const int DescriptionMaxLength = 256;

    private List<string> _permissions = [];

    private Role()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    /// <summary>Built-in roles can be re-permissioned but not renamed or deleted.</summary>
    public bool IsSystem { get; private set; }

    public IReadOnlyList<string> Permissions => _permissions.AsReadOnly();

    public static Role Create(string name, string description, IEnumerable<string> permissions, bool isSystem = false)
    {
        var role = new Role { IsSystem = isSystem };
        role.Rename(name, description, allowSystem: true);
        role.SetPermissions(permissions);
        return role;
    }

    public void Rename(string name, string description) => Rename(name, description, allowSystem: false);

    /// <summary>Changes only the description (allowed for built-in roles too).</summary>
    public void Describe(string description) => Rename(Name, description, allowSystem: true);

    public void SetPermissions(IEnumerable<string> permissions)
    {
        var distinct = permissions.Distinct(StringComparer.Ordinal).ToList();
        var unknown = distinct.FirstOrDefault(p => !Staff.Permissions.IsKnown(p));
        if (unknown is not null)
        {
            throw new DomainException("role.permission_unknown", $"'{unknown}' is not a permission.");
        }

        var reserved = distinct.FirstOrDefault(p => !Staff.Permissions.IsGrantable(p));
        if (reserved is not null)
        {
            throw new DomainException("role.permission_super_admin_only", $"'{reserved}' is reserved for Super Admins.");
        }

        _permissions = distinct.Order(StringComparer.Ordinal).ToList();
    }

    private void Rename(string name, string description, bool allowSystem)
    {
        if (IsSystem && !allowSystem)
        {
            throw new DomainException("role.system_cannot_be_renamed", "Built-in roles can't be renamed.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length == 0 || trimmed.Length > NameMaxLength)
        {
            throw new DomainException("role.name_invalid", $"A role name of up to {NameMaxLength} characters is required.");
        }

        if (description.Length > DescriptionMaxLength)
        {
            throw new DomainException("role.description_too_long", $"The description can be at most {DescriptionMaxLength} characters.");
        }

        Name = trimmed;
        Description = description.Trim();
    }
}
