using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Content;
using HomeServices.Application.Staff;
using HomeServices.Domain.Staff;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ContentEndpointTests(ApiFactory factory)
{
    private HttpClient StaffClient(params string[] permissions)
    {
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", "Editor", "hash");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.Services.GetRequiredService<IStaffTokenService>().CreateAccessToken(staff, permissions).Token);
        return client;
    }

    [Fact]
    public async Task An_editor_writes_and_publishes_a_page_visitors_can_read()
    {
        var editor = StaffClient(Permissions.ContentManage);
        var slug = $"p-{Guid.NewGuid():N}"[..20];

        var created = await editor.PostAsJsonAsync("/api/v1/admin/pages", new
        {
            slug,
            title = new { hy = "Պայմաններ", ru = "Условия" },
            body = new { hy = "# Պայմաններ" },
            showInFooter = true,
            sortOrder = 1,
        });
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var page = (await created.Content.ReadFromJsonAsync<AdminPageDto>())!;

        var visitor = factory.CreateClient();
        (await visitor.GetAsync($"/api/v1/pages/{slug}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        (await editor.PostAsync($"/api/v1/admin/pages/{page.Id}/publish", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        visitor.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru");
        var published = await visitor.GetFromJsonAsync<PublicPageDto>($"/api/v1/pages/{slug}");
        published!.Title.ShouldBe("Условия");
        published.Body.ShouldBe("# Պայմաններ");
        (await visitor.GetFromJsonAsync<List<PublicPageLinkDto>>("/api/v1/pages"))!.ShouldContain(l => l.Slug == slug && l.ShowInFooter);

        (await editor.DeleteAsync($"/api/v1/admin/pages/{page.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await visitor.GetAsync($"/api/v1/pages/{slug}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Published_questions_are_shown_by_audience()
    {
        var editor = StaffClient(Permissions.ContentManage);
        var question = $"Question {Guid.NewGuid():N}";
        var created = await editor.PostAsJsonAsync("/api/v1/admin/faqs", new { question = new { hy = question }, answer = new { hy = "Answer" }, audience = "Partners", sortOrder = 1 });
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var faq = (await created.Content.ReadFromJsonAsync<AdminFaqDto>())!;
        (await editor.PostAsync($"/api/v1/admin/faqs/{faq.Id}/publish", null)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var partners = await factory.CreateClient().GetFromJsonAsync<List<PublicFaqDto>>("/api/v1/faqs?audience=Partners");
        var customers = await factory.CreateClient().GetFromJsonAsync<List<PublicFaqDto>>("/api/v1/faqs?audience=Customers");

        partners!.ShouldContain(f => f.Question == question);
        customers!.ShouldNotContain(f => f.Question == question);
    }

    [Fact]
    public async Task Pages_need_a_default_language_title_and_the_permission()
    {
        var response = await StaffClient(Permissions.ContentManage).PostAsJsonAsync("/api/v1/admin/pages", new
        {
            slug = "no-title",
            title = new { ru = "Только русский" },
            body = new { },
            showInFooter = false,
            sortOrder = 0,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").GetProperty("title")[0].GetString()
            .ShouldBe("title.default_language_required");
        (await StaffClient(Permissions.CatalogManage).GetAsync("/api/v1/admin/pages")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
