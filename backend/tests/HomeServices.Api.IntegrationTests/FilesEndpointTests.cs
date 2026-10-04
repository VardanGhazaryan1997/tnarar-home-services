using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Files;
using HomeServices.Application.Identity;
using HomeServices.Application.Staff;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Staff;
using HomeServices.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class FilesEndpointTests(ApiFactory factory)
{
    private static byte[] Photo(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.SteelBlue);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }

    private static string KeyFrom(string url) => new Uri(url).AbsolutePath.TrimStart('/');

    private async Task<HttpClient> PortalClient()
    {
        var user = User.Register(PhoneNumber.Parse($"+37477{Random.Shared.Next(100_000, 999_999)}"));
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        return Client(factory.Services.GetRequiredService<ITokenService>().CreateAccessToken(user).Token);
    }

    private HttpClient StaffClient()
    {
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", "Moderator", "hash");
        return Client(factory.Services.GetRequiredService<IStaffTokenService>().CreateAccessToken(staff, []).Token);
    }

    private HttpClient Client(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<UploadTicket> RequestUploadAsync(HttpClient client, string fileName, string contentType, long size)
    {
        var response = await client.PostAsJsonAsync("/api/v1/files/uploads", new { fileName, contentType, size });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<UploadTicket>())!;
    }

    private static async Task<string?> ProblemCode(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();

    [Fact]
    public async Task A_user_uploads_a_photo_and_gets_a_thumbnail()
    {
        var client = await PortalClient();
        var photo = Photo(1600, 1200);

        var ticket = await RequestUploadAsync(client, "kitchen.jpg", "image/jpeg", photo.Length);
        ticket.Method.ShouldBe("PUT");
        ticket.Headers["Content-Type"].ShouldBe("image/jpeg");
        factory.Storage.Upload(KeyFrom(ticket.UploadUrl), photo, "image/jpeg");

        var response = await client.PostAsync($"/api/v1/files/{ticket.FileId}/complete", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var file = (await response.Content.ReadFromJsonAsync<FileDto>())!;
        file.Status.ShouldBe("Ready");
        file.Kind.ShouldBe("Image");
        file.FileName.ShouldBe("kitchen.jpg");
        file.Width.ShouldBe(1600);
        file.Height.ShouldBe(1200);
        file.Url.ShouldNotBeNull();
        using var thumbnail = SKBitmap.Decode(factory.Storage.Read(KeyFrom(file.ThumbnailUrl!)));
        thumbnail.Width.ShouldBe(480);
        thumbnail.Height.ShouldBe(360);

        var again = await client.GetFromJsonAsync<FileDto>($"/api/v1/files/{ticket.FileId}");
        again!.Status.ShouldBe("Ready");
    }

    [Fact]
    public async Task Staff_can_upload_and_can_see_users_files_but_other_users_cannot()
    {
        var owner = await PortalClient();
        var ticket = await RequestUploadAsync(owner, "contract.pdf", "application/pdf", 100);
        var staff = StaffClient();

        (await staff.GetAsync($"/api/v1/files/{ticket.FileId}")).StatusCode.ShouldBe(HttpStatusCode.OK);
        var stranger = await (await PortalClient()).GetAsync($"/api/v1/files/{ticket.FileId}");
        stranger.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ProblemCode(stranger)).ShouldBe("file.not_found");

        (await RequestUploadAsync(staff, "invoice.pdf", "application/pdf", 100)).FileId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Signed_out_requests_get_401()
    {
        var client = factory.CreateClient();

        (await client.PostAsJsonAsync("/api/v1/files/uploads", new { fileName = "a.jpg", contentType = "image/jpeg", size = 1 }))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.GetAsync($"/api/v1/files/{Guid.NewGuid()}")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refused_types_and_sizes_return_400_with_field_error_codes()
    {
        var client = await PortalClient();

        var gif = await client.PostAsJsonAsync("/api/v1/files/uploads", new { fileName = "a.gif", contentType = "image/gif", size = 10 });
        var huge = await client.PostAsJsonAsync("/api/v1/files/uploads", new { fileName = "a.jpg", contentType = "image/jpeg", size = 50L * 1024 * 1024 });

        gif.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await gif.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").GetProperty("contentType")[0].GetString()
            .ShouldBe("content_type.not_allowed");
        huge.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await huge.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").GetProperty("size")[0].GetString()
            .ShouldBe("size.too_large");
    }

    [Fact]
    public async Task Completing_before_uploading_returns_422_and_can_be_retried()
    {
        var client = await PortalClient();
        var ticket = await RequestUploadAsync(client, "a.pdf", "application/pdf", 100);

        var early = await client.PostAsync($"/api/v1/files/{ticket.FileId}/complete", null);
        early.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ProblemCode(early)).ShouldBe("file.not_uploaded");

        factory.Storage.Upload(KeyFrom(ticket.UploadUrl), "%PDF-1.7 test"u8.ToArray(), "application/pdf");
        (await client.PostAsync($"/api/v1/files/{ticket.FileId}/complete", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_file_that_is_not_what_it_claims_is_rejected_and_deleted()
    {
        var client = await PortalClient();
        var ticket = await RequestUploadAsync(client, "photo.jpg", "image/jpeg", 100);
        var key = KeyFrom(ticket.UploadUrl);
        factory.Storage.Upload(key, "MZ this is a program"u8.ToArray(), "image/jpeg");

        var response = await client.PostAsync($"/api/v1/files/{ticket.FileId}/complete", null);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ProblemCode(response)).ShouldBe("file.invalid_content");
        factory.Storage.Contains(key).ShouldBeFalse();
        (await client.GetFromJsonAsync<FileDto>($"/api/v1/files/{ticket.FileId}"))!.Status.ShouldBe("Rejected");
    }
}
