using HomeServices.Infrastructure.Identity;
using HomeServices.Infrastructure.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace HomeServices.Api.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

/// <summary>Passes when the staff token carries the permission, or the Super Admin flag.</summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var user = context.User;
        if (user.HasClaim(StaffTokenService.SuperAdminClaim, "true") || user.HasClaim(StaffTokenService.PermissionClaim, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

/// <summary>Builds "perm:&lt;code&gt;" policies on demand: staff scheme + signed-in staff + the permission.</summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public const string PolicyPrefix = "perm:";

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PolicyPrefix, StringComparison.Ordinal))
        {
            return await base.GetPolicyAsync(policyName);
        }

        return new AuthorizationPolicyBuilder(AuthSchemes.Staff)
            .RequireAuthenticatedUser()
            .RequireClaim(StaffTokenService.StaffClaim, "true")
            .AddRequirements(new PermissionRequirement(policyName[PolicyPrefix.Length..]))
            .Build();
    }
}
