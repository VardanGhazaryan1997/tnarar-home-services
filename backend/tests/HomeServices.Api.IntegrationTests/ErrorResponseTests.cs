using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HomeServices.Api.IntegrationTests.Infrastructure;

namespace HomeServices.Api.IntegrationTests;

/// <summary>
/// Every error leaves the API as RFC 7807 ProblemDetails with a stable `code`
/// the frontend can translate, plus a `traceId` for support.
/// </summary>
[Collection(ApiCollection.Name)]
public class ErrorResponseTests(ApiFactory factory)
{
    [Fact]
    public async Task Validation_errors_return_400_with_error_codes_per_field()
    {
        var (response, body) = await GetProblem("validation");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().ShouldBe("validation_failed");
        var errors = body.GetProperty("errors");
        errors.GetProperty("name").EnumerateArray().Select(e => e.GetString()!).ToArray().ShouldBe(new[] { "name.required", "name.too_short" });
        errors.GetProperty("phoneNumber").EnumerateArray().Select(e => e.GetString()!).ToArray().ShouldBe(new[] { "phone.invalid" });
    }

    [Fact]
    public async Task Enums_are_read_and_written_by_name()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/test/errors/body", new { name = "x", colour = "Blue", count = 1 });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("colour").GetString().ShouldBe("Blue");
    }

    [Fact]
    public async Task Unreadable_values_return_400_with_a_code_per_field()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/test/errors/body", new { name = "x", colour = "Green", count = 1 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetString().ShouldBe("validation_failed");
        body.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        var errors = body.GetProperty("errors");
        errors.EnumerateObject().Select(e => e.Name).ShouldBe(new[] { "colour" });
        errors.GetProperty("colour")[0].GetString().ShouldBe("value.invalid");
    }

    [Fact]
    public async Task Missing_required_values_return_400_with_value_required()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/test/errors/body", new { colour = "Red", count = 1 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        errors.GetProperty("name")[0].GetString().ShouldBe("value.required");
    }

    [Fact]
    public async Task An_empty_body_is_reported_as_the_body()
    {
        using var content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json");

        var response = await factory.CreateClient().PostAsync("/api/v1/test/errors/body", content);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        errors.EnumerateObject().ShouldHaveSingleItem().Value[0].GetString().ShouldBe("value.required");
    }

    [Theory]
    [InlineData("not-found", HttpStatusCode.NotFound, "partner.not_found")]
    [InlineData("conflict", HttpStatusCode.Conflict, "phone.taken")]
    [InlineData("forbidden", HttpStatusCode.Forbidden, "forbidden")]
    [InlineData("domain", HttpStatusCode.UnprocessableEntity, "order.not_in_progress")]
    [InlineData("unauthorized", HttpStatusCode.Unauthorized, "session.expired")]
    [InlineData("too-many", HttpStatusCode.TooManyRequests, "otp.resend_too_soon")]
    public async Task Known_errors_map_to_their_status_and_code(string kind, HttpStatusCode status, string code)
    {
        var (response, body) = await GetProblem(kind);

        response.StatusCode.ShouldBe(status);
        body.GetProperty("status").GetInt32().ShouldBe((int)status);
        body.GetProperty("code").GetString().ShouldBe(code);
    }

    [Fact]
    public async Task Unexpected_errors_return_500_without_leaking_details()
    {
        var (response, body) = await GetProblem("unhandled");

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        body.GetProperty("code").GetString().ShouldBe("internal_error");
        body.GetRawText().ShouldNotContain(ErrorsTestController.SecretMessage);
    }

    [Theory]
    [InlineData("validation")]
    [InlineData("unhandled")]
    public async Task Error_responses_are_problem_json_with_a_trace_id(string kind)
    {
        var (response, body) = await GetProblem(kind);

        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        body.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    private async Task<(HttpResponseMessage Response, JsonElement Body)> GetProblem(string kind)
    {
        var response = await factory.CreateClient().GetAsync($"/api/v1/test/errors/{kind}");
        var json = await response.Content.ReadAsStringAsync();
        return (response, JsonDocument.Parse(json).RootElement.Clone());
    }
}
