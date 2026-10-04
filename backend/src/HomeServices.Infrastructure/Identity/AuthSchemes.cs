using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace HomeServices.Infrastructure.Identity;

/// <summary>Two token audiences: Portal users and Back Office staff. A token for one is rejected by the other.</summary>
public static class AuthSchemes
{
    public const string Portal = JwtBearerDefaults.AuthenticationScheme;
    public const string Staff = "Staff";
}

public static class AuthPolicies
{
    /// <summary>Signed-in Back Office staff (any role). Endpoints add permissions with [HasPermission].</summary>
    public const string Staff = "Staff";

    /// <summary>Anyone signed in: a Portal user or a Back Office staff member (e.g. for uploading files).</summary>
    public const string SignedIn = "SignedIn";
}
