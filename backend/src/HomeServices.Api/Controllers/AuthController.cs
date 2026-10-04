using HomeServices.Application.Errors;
using HomeServices.Application.Identity;
using HomeServices.Application.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers;

/// <summary>
/// Portal sign-in by phone + SMS code. The access token is returned in the body (keep it in memory);
/// the refresh token is set as an httpOnly cookie that only the auth endpoints receive.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    public const string RefreshCookieName = "hs_refresh";
    public const string RefreshCookiePath = "/api/v1/auth";

    public sealed record SendCodeRequest(string Phone);

    public sealed record VerifyCodeRequest(string Phone, string Code);

    public sealed record SessionResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, MyProfileDto User, bool IsNewUser);

    /// <summary>Texts a 6-digit sign-in code to the phone number.</summary>
    [HttpPost("otp/send")]
    public Task<SignInCodeSent> SendCode(
        SendCodeRequest request,
        [FromServices] ICommandHandler<SendSignInCode, SignInCodeSent> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new SendSignInCode(request.Phone), cancellationToken);

    /// <summary>Checks the code, creates the account on first sign-in, and starts a session.</summary>
    [HttpPost("otp/verify")]
    public async Task<SessionResponse> VerifyCodeAsync(
        VerifyCodeRequest request,
        [FromServices] ICommandHandler<VerifySignInCode, AuthSession> handler,
        CancellationToken cancellationToken) =>
        StartSession(await handler.HandleAsync(new VerifySignInCode(request.Phone, request.Code), cancellationToken));

    /// <summary>Uses the refresh cookie to issue a new access token (and a new refresh cookie).</summary>
    [HttpPost("refresh")]
    public async Task<SessionResponse> RefreshAsync(
        [FromServices] ICommandHandler<RefreshSession, AuthSession> handler,
        CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshCookieName]
            ?? throw new UnauthorizedException("Please sign in.", "session.missing");

        return StartSession(await handler.HandleAsync(new RefreshSession(refreshToken), cancellationToken));
    }

    /// <summary>Ends the current session and removes the refresh cookie.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync(
        [FromServices] ICommandHandler<EndSession, bool> handler,
        CancellationToken cancellationToken)
    {
        if (Request.Cookies[RefreshCookieName] is { } refreshToken)
        {
            await handler.HandleAsync(new EndSession(refreshToken), cancellationToken);
        }

        Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = RefreshCookiePath });
        return NoContent();
    }

    private SessionResponse StartSession(AuthSession session)
    {
        Response.Cookies.Append(RefreshCookieName, session.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = session.RefreshTokenExpiresAt,
        });

        return new SessionResponse(session.AccessToken, session.AccessTokenExpiresAt, session.User, session.IsNewUser);
    }
}
