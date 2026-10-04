using HomeServices.Application.Files;

namespace HomeServices.Application.Tests.Support;

/// <summary>Object storage in a dictionary. Signed links are readable fake URLs.</summary>
public sealed class FakeFileStorage : IFileStorage
{
    public Dictionary<string, (byte[] Content, string ContentType)> Objects { get; } = [];

    public List<string> Deleted { get; } = [];

    public void Upload(string key, byte[] content, string contentType = "application/octet-stream") => Objects[key] = (content, contentType);

    public Task<Uri> CreateUploadUrlAsync(string key, string contentType, DateTimeOffset expiresAt, CancellationToken cancellationToken) =>
        Task.FromResult(new Uri($"https://storage.test/{key}?put&type={Uri.EscapeDataString(contentType)}&expires={expiresAt.ToUnixTimeSeconds()}"));

    public Task<Uri> CreateDownloadUrlAsync(string key, DateTimeOffset expiresAt, CancellationToken cancellationToken) =>
        Task.FromResult(new Uri($"https://storage.test/{key}?get&expires={expiresAt.ToUnixTimeSeconds()}"));

    public Task<StoredObjectInfo?> GetInfoAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(Objects.TryGetValue(key, out var stored) ? new StoredObjectInfo(stored.Content.Length, stored.ContentType) : null);

    public Task<byte[]> ReadStartAsync(string key, int count, CancellationToken cancellationToken) =>
        Task.FromResult(Objects[key].Content.Take(count).ToArray());

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new MemoryStream(Objects[key].Content));

    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        Objects[key] = (buffer.ToArray(), contentType);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        Objects.Remove(key);
        Deleted.Add(key);
        return Task.CompletedTask;
    }

    public Task EnsureReadyAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>Treats any data as a 4000×3000 image (or as unreadable, when <see cref="Readable"/> is false).</summary>
public sealed class FakeImageProcessor : IImageProcessor
{
    public static readonly byte[] Thumbnail = [0xFF, 0xD8, 0xFF, 0x01];

    public bool Readable { get; set; } = true;

    public int? LastMaxSize { get; private set; }

    public ProcessedImage? Process(Stream image, int maxSize)
    {
        LastMaxSize = maxSize;
        return Readable ? new ProcessedImage(4000, 3000, Thumbnail) : null;
    }
}

/// <summary>First bytes of real files of each type.</summary>
public static class SampleFiles
{
    public static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, (byte)'J', (byte)'F', (byte)'I', (byte)'F', 0, 1, 1, 1];

    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D];

    public static readonly byte[] Webp = "RIFF\u0024\0\0\0WEBPVP8 "u8.ToArray();

    public static readonly byte[] Mp4 = "\0\0\0\u0018ftypmp42\0\0\0\0"u8.ToArray();

    public static readonly byte[] QuickTime = "\0\0\0\u0014ftypqt  \0\0\0\0"u8.ToArray();

    public static readonly byte[] OldQuickTime = "\0\0\0\u0008wide\0\0\0\0mdat"u8.ToArray();

    public static readonly byte[] Pdf = "%PDF-1.7\n%abc"u8.ToArray();

    public static readonly byte[] Executable = "MZ\u0090\0\u0003\0\0\0\u0004\0\0\0"u8.ToArray();
}
