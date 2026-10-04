using System.Net;
using System.Net.Http.Headers;
using HomeServices.Infrastructure.Files;
using Microsoft.Extensions.Options;

namespace HomeServices.Infrastructure.IntegrationTests.Files;

[Collection(SeaweedFsCollection.Name)]
public sealed class S3FileStorageTests(SeaweedFsFixture fixture) : IDisposable
{
    private static readonly byte[] Content = [.. Enumerable.Range(0, 5000).Select(i => (byte)(i % 251))];

    private readonly S3FileStorage _storage = fixture.CreateStorage();
    private readonly HttpClient _browser = new();

    private static string NewKey(string name = "photo.jpg") => $"tests/{Guid.NewGuid():N}/{name}";

    private static DateTimeOffset InFiveMinutes => DateTimeOffset.UtcNow.AddMinutes(5);

    public void Dispose()
    {
        _storage.Dispose();
        _browser.Dispose();
    }

    private async Task<HttpResponseMessage> BrowserPutAsync(Uri url, byte[] content, string contentType)
    {
        using var body = new ByteArrayContent(content);
        body.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return await _browser.PutAsync(url, body);
    }

    [Fact]
    public async Task A_browser_uploads_with_the_signed_link_and_the_API_reads_the_file()
    {
        var key = NewKey();
        var url = await _storage.CreateUploadUrlAsync(key, "image/jpeg", InFiveMinutes, CancellationToken.None);

        (await BrowserPutAsync(url, Content, "image/jpeg")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var info = await _storage.GetInfoAsync(key, CancellationToken.None);
        info.ShouldNotBeNull();
        info.Size.ShouldBe(Content.Length);
        info.ContentType.ShouldBe("image/jpeg");
        (await _storage.ReadStartAsync(key, 12, CancellationToken.None)).ShouldBe(Content[..12]);
        await using var whole = await _storage.OpenReadAsync(key, CancellationToken.None);
        using var copy = new MemoryStream();
        await whole.CopyToAsync(copy);
        copy.ToArray().ShouldBe(Content);
    }

    [Fact]
    public async Task A_tampered_upload_link_is_refused()
    {
        var key = NewKey();
        var url = await _storage.CreateUploadUrlAsync(key, "image/jpeg", InFiveMinutes, CancellationToken.None);
        var tampered = new Uri(url.ToString().Replace(key, NewKey(), StringComparison.Ordinal));

        (await BrowserPutAsync(tampered, Content, "image/jpeg")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_browser_downloads_with_the_signed_link()
    {
        var key = NewKey("contract.pdf");
        await _storage.PutAsync(key, new MemoryStream(Content), "application/pdf", CancellationToken.None);

        var url = await _storage.CreateDownloadUrlAsync(key, InFiveMinutes, CancellationToken.None);
        var response = await _browser.GetAsync(url);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync()).ShouldBe(Content);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/pdf");
    }

    [Fact]
    public async Task Unsigned_downloads_are_refused()
    {
        var key = NewKey();
        await _storage.PutAsync(key, new MemoryStream(Content), "image/jpeg", CancellationToken.None);

        var response = await _browser.GetAsync($"{fixture.ServiceUrl}/{SeaweedFsFixture.Bucket}/{key}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_missing_object_has_no_info()
    {
        (await _storage.GetInfoAsync(NewKey(), CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task Reading_the_start_of_a_short_object_returns_all_of_it()
    {
        var key = NewKey();
        await _storage.PutAsync(key, new MemoryStream([1, 2, 3]), "image/jpeg", CancellationToken.None);

        (await _storage.ReadStartAsync(key, 12, CancellationToken.None)).ShouldBe(new byte[] { 1, 2, 3 });
    }

    [Fact]
    public async Task Deleted_objects_are_gone_and_deleting_twice_is_fine()
    {
        var key = NewKey();
        await _storage.PutAsync(key, new MemoryStream(Content), "image/jpeg", CancellationToken.None);

        await _storage.DeleteAsync(key, CancellationToken.None);
        await _storage.DeleteAsync(key, CancellationToken.None);

        (await _storage.GetInfoAsync(key, CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task Preparing_an_existing_bucket_again_is_fine()
    {
        await _storage.EnsureReadyAsync(CancellationToken.None);
        await _storage.EnsureReadyAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Preparing_with_sites_lets_those_browsers_upload()
    {
        using var storage = fixture.CreateStorage(corsOrigins: ["https://staging.example.am/"]);
        await storage.EnsureReadyAsync(CancellationToken.None);

        var url = await storage.CreateUploadUrlAsync(NewKey(), "image/jpeg", InFiveMinutes, CancellationToken.None);
        using var preflight = new HttpRequestMessage(HttpMethod.Options, url);
        preflight.Headers.Add("Origin", "https://staging.example.am");
        preflight.Headers.Add("Access-Control-Request-Method", "PUT");
        preflight.Headers.Add("Access-Control-Request-Headers", "content-type");
        using var response = await _browser.SendAsync(preflight);

        response.IsSuccessStatusCode.ShouldBeTrue();
        response.Headers.GetValues("Access-Control-Allow-Origin").ShouldContain(value => value == "https://staging.example.am" || value == "*");
    }
}

/// <summary>Signed links are computed without contacting the storage.</summary>
public class S3SignedLinkTests
{
    private static S3FileStorage Storage(string? publicServiceUrl, bool createBucket = false) => new(Options.Create(new StorageSettings
    {
        // Nothing listens here: these tests must not send requests.
        ServiceUrl = "http://127.0.0.1:9",
        PublicServiceUrl = publicServiceUrl,
        Bucket = "home-services",
        AccessKey = "key",
        SecretKey = "secret",
        CreateBucket = createBucket,
    }));

    [Fact]
    public async Task Links_use_the_public_address_when_one_is_set()
    {
        using var storage = Storage("https://files.example.am");

        var upload = await storage.CreateUploadUrlAsync("files/a/original.jpg", "image/jpeg", DateTimeOffset.UtcNow.AddMinutes(15), CancellationToken.None);
        var download = await storage.CreateDownloadUrlAsync("files/a/original.jpg", DateTimeOffset.UtcNow.AddMinutes(15), CancellationToken.None);

        upload.GetLeftPart(UriPartial.Path).ShouldBe("https://files.example.am/home-services/files/a/original.jpg");
        upload.Query.ShouldContain("X-Amz-Signature=");
        upload.Query.ShouldContain("content-type");
        download.GetLeftPart(UriPartial.Path).ShouldBe("https://files.example.am/home-services/files/a/original.jpg");
    }

    [Fact]
    public async Task Links_use_the_service_address_and_plain_http_when_no_public_address_is_set()
    {
        using var storage = Storage(publicServiceUrl: null);

        var url = await storage.CreateDownloadUrlAsync("files/a/thumbnail.jpg", DateTimeOffset.UtcNow.AddMinutes(15), CancellationToken.None);

        url.GetLeftPart(UriPartial.Path).ShouldBe("http://127.0.0.1:9/home-services/files/a/thumbnail.jpg");
    }

    [Fact]
    public void Browser_CORS_rules_name_the_sites_and_the_methods_they_need()
    {
        var rule = S3FileStorage.BrowserCors(["https://staging.example.am/", " https://admin.example.am ", ""]).Rules.ShouldHaveSingleItem();

        rule.AllowedOrigins.ShouldBe(new[] { "https://staging.example.am", "https://admin.example.am" });
        rule.AllowedMethods.ShouldBe(new[] { "GET", "PUT", "HEAD" });
        rule.AllowedHeaders.ShouldBe(new[] { "*" });
        rule.ExposeHeaders.ShouldBe(new[] { "ETag" });
        rule.MaxAgeSeconds.ShouldBe(3600);
    }

    [Fact]
    public async Task Preparing_does_nothing_when_bucket_creation_is_off()
    {
        using var storage = Storage(publicServiceUrl: null, createBucket: false);

        await storage.EnsureReadyAsync(CancellationToken.None);
    }
}
