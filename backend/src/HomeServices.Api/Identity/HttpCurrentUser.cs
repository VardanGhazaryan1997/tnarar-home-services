using HomeServices.Application.Abstractions;
using HomeServices.Infrastructure.Staff;

namespace HomeServices.Api.Identity;

/// <summary>The signed-in user from the access token's "sub" claim (null for anonymous requests).</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? UserId => accessor.HttpContext?.User.FindFirst("sub")?.Value;

    /// <summary>Staff access tokens carry "staff": "true"; Portal tokens don't.</summary>
    public bool IsStaff => accessor.HttpContext?.User.HasClaim(StaffTokenService.StaffClaim, "true") ?? false;
}
