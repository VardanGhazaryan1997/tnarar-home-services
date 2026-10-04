using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HomeServices.Api.Controllers.Admin;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Staff;
using HomeServices.Domain.Staff;
using HomeServices.Infrastructure.Persistence;
using HomeServices.Infrastructure.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class StaffAuthFlowTests(ApiFactory factory)
{
    private const string Password = "Correct-Horse-Battery-1";

    private TotpService Totp => factory.Services.GetRequiredService<TotpService>();

    private async Task<string> GivenStaff()
    {
        var email = $"staff-{Guid.NewGuid():N}@example.com";
        await using var scope = factory.Services.CreateAsyncScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.StaffUsers.Add(StaffUser.Create(email, "Operator", hasher.Hash(Password)));
        await db.SaveChangesAsync();
        return email;
    }

    private static async Task<StaffSignInResult> SignIn(HttpClient client, string email, string password = Password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/admin/auth/login", new { email, password });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<StaffSignInResult>())!;
    }

    private async Task<AdminAuthController.StaffSessionResponse> SetUpTwoFactorAndSignIn(HttpClient client, string email)
    {
        var signIn = await SignIn(client, email);
        var response = await client.PostAsJsonAsync("/api/v1/admin/auth/2fa/setup", new
        {
            challengeToken = signIn.ChallengeToken,
            code = Totp.GenerateCode(signIn.SetupSecret!, DateTimeOffset.UtcNow),
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<AdminAuthController.StaffSessionResponse>())!;
    }

    [Fact]
    public async Task First_sign_in_requires_setting_up_an_authenticator_app()
    {
        var signIn = await SignIn(factory.CreateClient(), await GivenStaff());

        signIn.Status.ShouldBe(StaffSignInStatus.TwoFactorSetupRequired);
        signIn.ChallengeToken.ShouldNotBeNullOrWhiteSpace();
        signIn.SetupSecret.ShouldNotBeNullOrWhiteSpace();
        signIn.SetupUri!.ShouldStartWith("otpauth://totp/");
    }

    [Fact]
    public async Task Confirming_the_authenticator_starts_a_staff_session_with_a_refresh_cookie()
    {
        var client = factory.CreateClient();
        var email = await GivenStaff();
        var signIn = await SignIn(client, email);

        var response = await client.PostAsJsonAsync("/api/v1/admin/auth/2fa/setup", new
        {
            challengeToken = signIn.ChallengeToken,
            code = Totp.GenerateCode(signIn.SetupSecret!, DateTimeOffset.UtcNow),
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var session = (await response.Content.ReadFromJsonAsync<AdminAuthController.StaffSessionResponse>())!;
        session.Staff.Email.ShouldBe(email);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(AdminAuthController.RefreshCookieName, StringComparison.Ordinal));
        cookie.ShouldContain("httponly", Case.Insensitive);
        cookie.ShouldContain($"path={AdminAuthController.RefreshCookiePath}", Case.Insensitive);
    }

    [Fact]
    public async Task Later_sign_ins_ask_for_the_authenticator_code()
    {
        var client = factory.CreateClient();
        var email = await GivenStaff();
        var first = await SignIn(client, email);
        var secret = first.SetupSecret!;
        await client.PostAsJsonAsync("/api/v1/admin/auth/2fa/setup", new { challengeToken = first.ChallengeToken, code = Totp.GenerateCode(secret, DateTimeOffset.UtcNow) });

        var second = await SignIn(client, email);
        second.Status.ShouldBe(StaffSignInStatus.TwoFactorRequired);
        second.SetupSecret.ShouldBeNull();

        // The next 30-second code (still inside the allowed window); the current one was just used.
        var response = await client.PostAsJsonAsync("/api/v1/admin/auth/2fa/verify", new
        {
            challengeToken = second.ChallengeToken,
            code = Totp.GenerateCode(secret, DateTimeOffset.UtcNow.AddSeconds(30)),
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Staff_tokens_open_the_Back_Office_but_not_the_Portal()
    {
        var client = factory.CreateClient();
        var session = await SetUpTwoFactorAndSignIn(client, await GivenStaff());

        (await client.GetAsync("/api/v1/admin/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        var me = await client.GetAsync("/api/v1/admin/me");
        me.StatusCode.ShouldBe(HttpStatusCode.OK);
        var profile = (await me.Content.ReadFromJsonAsync<StaffProfileDto>())!;
        profile.FullName.ShouldBe("Operator");
        profile.Permissions.ShouldBeEmpty(); // no roles assigned yet

        (await client.GetAsync("/api/v1/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_wrong_password_returns_401_with_a_generic_code()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/admin/auth/login", new { email = await GivenStaff(), password = "wrong" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await ProblemCode(response)).ShouldBe("staff.invalid_credentials");
    }

    [Fact]
    public async Task A_wrong_authenticator_code_returns_422()
    {
        var client = factory.CreateClient();
        var signIn = await SignIn(client, await GivenStaff());
        var wrong = Totp.GenerateCode(signIn.SetupSecret!, DateTimeOffset.UtcNow.AddMinutes(10));

        var response = await client.PostAsJsonAsync("/api/v1/admin/auth/2fa/setup", new { challengeToken = signIn.ChallengeToken, code = wrong });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ProblemCode(response)).ShouldBe("staff.totp_invalid");
    }

    [Fact]
    public async Task The_staff_refresh_cookie_rotates_and_logout_ends_it()
    {
        var client = factory.CreateClient();
        await SetUpTwoFactorAndSignIn(client, await GivenStaff());

        (await client.PostAsync("/api/v1/admin/auth/refresh", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.PostAsync("/api/v1/admin/auth/logout", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var afterLogout = await client.PostAsync("/api/v1/admin/auth/refresh", null);
        afterLogout.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task The_configured_Super_Admin_is_created_on_startup()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var admin = await db.StaffUsers.SingleAsync(s => s.Email == "admin@homeservices.local");

        admin.IsSuperAdmin.ShouldBeTrue();
    }

    private static async Task<string?> ProblemCode(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("code").GetString();
}
