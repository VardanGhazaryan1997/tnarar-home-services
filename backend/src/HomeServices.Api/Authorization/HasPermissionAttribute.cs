using Microsoft.AspNetCore.Authorization;

namespace HomeServices.Api.Authorization;

/// <summary>
/// Restricts a Back Office endpoint to staff who have <paramref name="permission"/> (Super Admins always pass).
/// Usage: <c>[HasPermission(Permissions.PartnersApprove)]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute(string permission) : AuthorizeAttribute(PermissionPolicyProvider.PolicyPrefix + permission)
{
    public string Permission { get; } = permission;
}
