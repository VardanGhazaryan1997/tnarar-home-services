using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using HomeServices.Api.Controllers;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Identity;
using HomeServices.Domain.Identity;
using HomeServices.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public partial class AuthFlowTests(ApiFactory factory)
{
    private static string UniquePhone() => $"+3749{Random.Shared.Next(1_000_000, 9_999_999)}";

    private HttpClient NewClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    private async Task<string> SendCodeAndReadSms(HttpClient client, string phone)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/otp/send", new { phone });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var sms = factory.Services.GetRequiredService<FakeSmsSender>();
        return SixDigits().Match(sms.LastMessageTo(PhoneNumber.Parse(phone))!).Value;
    }

    private async Task<AuthController.SessionResponse> SignIn(HttpClient client, string phone)
    {
        var code = await SendCodeAndReadSms(client, phone);
        var response = await client.PostAsJsonAsync("/api/v1/auth/otp/verify", new { phone, code });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<AuthController.SessionResponse>())!;
    }

    [Fact]
    public async Task Sending_a_code_reports_its_lifetime_and_resend_delay()
    {
        var response = await NewClient().PostAsJsonAsync("/api/v1/auth/otp/send", new { phone = UniquePhone() });

        var body = (await response.Content.ReadFromJsonAsync<SignInCodeSent>())!;
        body.ExpiresInSeconds.ShouldBe(300);
        body.ResendAfterSeconds.ShouldBe(1); // test configuration; 60 by default
    }

    [Fact]
    public async Task First_sign_in_creates_an_account_and_returns_an_access_token_and_a_refresh_cookie()
    {
        var client = NewClient();
        var phone = UniquePhone();
        var code = await SendCodeAndReadSms(client, phone);

        var response = await client.PostAsJsonAsync("/api/v1/auth/otp/verify", new { phone, code });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var session = (await response.Content.ReadFromJsonAsync<AuthController.SessionResponse>())!;
        session.IsNewUser.ShouldBeTrue();
        session.User.PhoneNumber.ShouldBe(phone);
        session.AccessToken.ShouldNotBeNullOrWhiteSpace();

        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(AuthController.RefreshCookieName, StringComparison.Ordinal));
        cookie.ShouldContain("httponly", Case.Insensitive);
        cookie.ShouldContain("samesite=strict", Case.Insensitive);
        cookie.ShouldContain($"path={AuthController.RefreshCookiePath}", Case.Insensitive);

        (await response.Content.ReadAsStringAsync()).ShouldNotContain("refreshToken");
    }

    [Fact]
    public async Task The_access_token_unlocks_the_users_own_profile()
    {
        var client = NewClient();
        var session = await SignIn(client, UniquePhone());

        (await client.GetAsync("/api/v1/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        var update = await client.PutAsJsonAsync("/api/v1/me", new { fullName = "Ani Petrosyan", email = "ani@example.com" });
        update.StatusCode.ShouldBe(HttpStatusCode.OK);

        var me = (await client.GetFromJsonAsync<MyProfileDto>("/api/v1/me"))!;
        me.FullName.ShouldBe("Ani Petrosyan");
        me.IsProfileComplete.ShouldBeTrue();
    }

    [Fact]
    public async Task Signing_in_again_with_the_same_phone_returns_the_same_account()
    {
        var phone = UniquePhone();
        var first = await SignIn(NewClient(), phone);
        await Task.Delay(TimeSpan.FromSeconds(1.2)); // past the resend cooldown

        var second = await SignIn(NewClient(), phone);

        second.IsNewUser.ShouldBeFalse();
        second.User.Id.ShouldBe(first.User.Id);
    }

    [Fact]
    public async Task The_refresh_cookie_issues_a_new_access_token_and_rotates()
    {
        var client = NewClient();
        await SignIn(client, UniquePhone());

        var refreshed = await client.PostAsync("/api/v1/auth/refresh", null);

        refreshed.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await refreshed.Content.ReadFromJsonAsync<AuthController.SessionResponse>())!.AccessToken.ShouldNotBeNullOrWhiteSpace();
        refreshed.Headers.GetValues("Set-Cookie").ShouldContain(c => c.StartsWith(AuthController.RefreshCookieName, StringComparison.Ordinal));
    }

    [Fact]
    public async Task After_logout_the_refresh_cookie_no_longer_works()
    {
        var client = NewClient();
        await SignIn(client, UniquePhone());

        (await client.PostAsync("/api/v1/auth/logout", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var refreshed = await client.PostAsync("/api/v1/auth/refresh", null);
        refreshed.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await ProblemCode(refreshed)).ShouldBe("session.missing");
    }

    [Fact]
    public async Task A_wrong_code_returns_422_otp_invalid()
    {
        var client = NewClient();
        var phone = UniquePhone();
        var code = await SendCodeAndReadSms(client, phone);
        var wrong = code == "000000" ? "111111" : "000000";

        var response = await client.PostAsJsonAsync("/api/v1/auth/otp/verify", new { phone, code = wrong });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ProblemCode(response)).ShouldBe("otp.invalid");
    }

    [Fact]
    public async Task Requesting_another_code_too_soon_returns_429()
    {
        var client = NewClient();
        var phone = UniquePhone();
        await SendCodeAndReadSms(client, phone);

        var response = await client.PostAsJsonAsync("/api/v1/auth/otp/send", new { phone });

        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        (await ProblemCode(response)).ShouldBe("otp.resend_too_soon");
    }

    [Fact]
    public async Task An_invalid_phone_number_returns_400()
    {
        var response = await NewClient().PostAsJsonAsync("/api/v1/auth/otp/send", new { phone = "12" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ProblemCode(response)).ShouldBe("validation_failed");
    }

    private static async Task<string?> ProblemCode(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("code").GetString();

    [GeneratedRegex("[0-9]{6}")]
    private static partial Regex SixDigits();
}
