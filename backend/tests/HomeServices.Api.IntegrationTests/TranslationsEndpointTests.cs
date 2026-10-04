using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Common;
using HomeServices.Application.Staff;
using HomeServices.Application.Translations;
using HomeServices.Domain.Staff;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class TranslationsEndpointTests(ApiFactory factory)
{
    private HttpClient StaffClient(params string[] permissions)
    {
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", "Translator", "hash");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.Services.GetRequiredService<IStaffTokenService>().CreateAccessToken(staff, permissions).Token);
        return client;
    }

    private static string UniqueNamespace() => $"t{Guid.NewGuid():N}"[..20];

    private static StringContent JsonBody(string json) => new(json, Encoding.UTF8, "application/json");

    [Fact]
    public async Task Imported_texts_are_served_to_the_apps_with_fallback_and_an_etag()
    {
        var ns = UniqueNamespace();
        var admin = StaffClient(Permissions.TranslationsManage);

        var imported = await admin.PostAsync($"/api/v1/admin/translations/{ns}/import/hy", JsonBody("""{ "home": { "title": "Բարի գալուստ", "cta": "Սկսել" } }"""));
        imported.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await imported.Content.ReadFromJsonAsync<TranslationImportResultDto>())!.Added.ShouldBe(2);
        (await admin.PutAsJsonAsync($"/api/v1/admin/translations/{ns}/keys/home.title", new { values = new Dictionary<string, string> { ["ru"] = "Добро пожаловать" } }))
            .StatusCode.ShouldBe(HttpStatusCode.OK);

        var app = factory.CreateClient();
        var response = await app.GetAsync($"/api/v1/i18n/ru/{ns}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");
        (await response.Content.ReadAsStringAsync()).ShouldBe("""{"home":{"cta":"Սկսել","title":"Добро пожаловать"}}""");
        var etag = response.Headers.ETag!.Tag;

        using var again = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/i18n/ru/{ns}");
        again.Headers.IfNoneMatch.ParseAdd(etag);
        (await app.SendAsync(again)).StatusCode.ShouldBe(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task Unknown_languages_are_404()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/i18n/xx/portal");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().ShouldBe("language.not_found");
    }

    [Fact]
    public async Task Translators_see_progress_missing_keys_and_export_files()
    {
        var ns = UniqueNamespace();
        var admin = StaffClient(Permissions.TranslationsManage);
        await admin.PostAsync($"/api/v1/admin/translations/{ns}/import/hy", JsonBody("""{ "a": "Ա", "b": "Բ" }"""));
        await admin.PostAsync($"/api/v1/admin/translations/{ns}/import/ru", JsonBody("""{ "a": "А" }"""));

        var namespaces = await admin.GetFromJsonAsync<List<TranslationNamespaceDto>>("/api/v1/admin/translations");
        namespaces!.Single(n => n.Namespace == ns).Languages.Single(l => l.Language == "ru").Missing.ShouldBe(1);

        var missing = await admin.GetFromJsonAsync<List<MissingTranslationsDto>>("/api/v1/admin/translations/missing");
        missing!.Single(m => m.Namespace == ns && m.Language == "ru").MissingKeys.ShouldBe(new[] { "b" });

        var rows = await admin.GetFromJsonAsync<PagedResult<TranslationRowDto>>($"/api/v1/admin/translations/{ns}?missingIn=ru");
        rows!.Items.ShouldHaveSingleItem().Key.ShouldBe("b");

        var export = await admin.GetAsync($"/api/v1/admin/translations/{ns}/export/ru?withFallback=true");
        export.Content.Headers.ContentDisposition!.FileName.ShouldBe($"{ns}.ru.json");
        (await export.Content.ReadAsStringAsync()).ShouldBe("""{"a":"А","b":"Բ"}""");

        (await admin.DeleteAsync($"/api/v1/admin/translations/{ns}/keys/b")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Languages_are_added_inactive_and_activated()
    {
        var admin = StaffClient(Permissions.TranslationsManage);
        var code = "z" + (char)('a' + Random.Shared.Next(26)) + (char)('a' + Random.Shared.Next(26));

        var created = await admin.PostAsJsonAsync("/api/v1/admin/languages", new { code, name = "Test", nativeName = "Test", sortOrder = 50 });
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await created.Content.ReadFromJsonAsync<AdminLanguageDto>())!.IsActive.ShouldBeFalse();

        var activated = await admin.PostAsync($"/api/v1/admin/languages/{code}/activate", null);
        (await activated.Content.ReadFromJsonAsync<AdminLanguageDto>())!.IsActive.ShouldBeTrue();
        (await factory.CreateClient().GetAsync($"/api/v1/i18n/{code}/portal")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var deactivated = await admin.PostAsync($"/api/v1/admin/languages/{code}/deactivate", null);
        (await deactivated.Content.ReadFromJsonAsync<AdminLanguageDto>())!.IsActive.ShouldBeFalse();
        (await admin.PostAsync("/api/v1/admin/languages/hy/deactivate", null)).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Managing_texts_needs_the_permission()
    {
        var outsider = StaffClient(Permissions.CatalogManage);

        (await outsider.GetAsync("/api/v1/admin/translations")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await outsider.GetAsync("/api/v1/admin/languages")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
