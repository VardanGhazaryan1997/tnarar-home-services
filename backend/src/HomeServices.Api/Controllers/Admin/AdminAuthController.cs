using HomeServices.Application.Errors;
using HomeServices.Application.Messaging;
using HomeServices.Application.Staff;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers.Admin;

/// <summary>
/// Back Office sign-in: email + password, then an authenticator code (mandatory 2FA).
/// The access token is returned in the body; the refresh token is an httpOnly cookie scoped to these endpoints.
/// </summary>
[ApiController]
[Route("api/v1/admin/auth")]
public sealed class AdminAuthController : ControllerBase
{
    public const string RefreshCookieName = "hs_staff_refresh";
    public const string RefreshCookiePath = "/api/v1/admin/auth";

    public sealed record SignInRequest(string Email, string Password);

    public sealed record TwoFactorRequest(string ChallengeToken, string Code);

    public sealed record AcceptInviteRequest(string Token, string Password);

    public sealed record StaffSessionResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, StaffProfileDto Staff);

    /// <summary>Checks email and password. Returns a challenge token for the 2FA step (and setup details on first sign-in).</summary>
    [HttpPost("login")]
    public Task<StaffSignInResult> SignInWithPassword(
        SignInRequest request,
        [FromServices] ICommandHandler<StaffSignIn, StaffSignInResult> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new StaffSignIn(request.Email, request.Password), cancellationToken);

    /// <summary>
    /// An invited staff member chooses a password with the token from their invitation link (no sign-in).
    /// They then sign in normally and set up 2FA. 404 "staff.invite_invalid", 422 "staff.invite_expired".
    /// </summary>
    [HttpPost("accept-invite")]
    public async Task<IActionResult> AcceptInviteAsync(
        AcceptInviteRequest request,
        [FromServices] ICommandHandler<AcceptStaffInvite, bool> handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new AcceptStaffInvite(request.Token, request.Password), cancellationToken);
        return NoContent();
    }

    /// <summary>First sign-in: confirms the authenticator app with a code, enables 2FA and starts a session.</summary>
    [HttpPost("2fa/setup")]
    public async Task<StaffSessionResponse> CompleteSetupAsync(
        TwoFactorRequest request,
        [FromServices] ICommandHandler<CompleteStaffTwoFactorSetup, StaffSession> handler,
        CancellationToken cancellationToken) =>
        StartSession(await handler.HandleAsync(new CompleteStaffTwoFactorSetup(request.ChallengeToken, request.Code), cancellationToken));

    /// <summary>Later sign-ins: checks the authenticator code and starts a session.</summary>
    [HttpPost("2fa/verify")]
    public async Task<StaffSessionResponse> VerifyAsync(
        TwoFactorRequest request,
        [FromServices] ICommandHandler<VerifyStaffTwoFactor, StaffSession> handler,
        CancellationToken cancellationToken) =>
        StartSession(await handler.HandleAsync(new VerifyStaffTwoFactor(request.ChallengeToken, request.Code), cancellationToken));

    [HttpPost("refresh")]
    public async Task<StaffSessionResponse> RefreshAsync(
        [FromServices] ICommandHandler<RefreshStaffSession, StaffSession> handler,
        CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshCookieName]
            ?? throw new UnauthorizedException("Please sign in.", "session.missing");

        return StartSession(await handler.HandleAsync(new RefreshStaffSession(refreshToken), cancellationToken));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync(
        [FromServices] ICommandHandler<EndStaffSession, bool> handler,
        CancellationToken cancellationToken)
    {
        if (Request.Cookies[RefreshCookieName] is { } refreshToken)
        {
            await handler.HandleAsync(new EndStaffSession(refreshToken), cancellationToken);
        }

        Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = RefreshCookiePath });
        return NoContent();
    }

    private StaffSessionResponse StartSession(StaffSession session)
    {
        Response.Cookies.Append(RefreshCookieName, session.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = session.RefreshTokenExpiresAt,
        });

        return new StaffSessionResponse(session.AccessToken, session.AccessTokenExpiresAt, session.Staff);
    }
}
