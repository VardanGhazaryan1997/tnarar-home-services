using System.Collections.Concurrent;
using HomeServices.Application.Files;

namespace HomeServices.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Stands in for S3 in API tests. Tests "upload" by calling <see cref="Upload"/>, as a browser
/// would PUT to the signed link. The real S3 storage is covered by the Infrastructure tests.
/// </summary>
public sealed class InMemoryFileStorage : IFileStorage
{
    private readonly ConcurrentDictionary<string, (byte[] Content, string ContentType)> _objects = new();

    public bool Contains(string key) => _objects.ContainsKey(key);

    public void Upload(string key, byte[] content, string contentType) => _objects[key] = (content, contentType);

    public byte[] Read(string key) => _objects[key].Content;

    public Task<Uri> CreateUploadUrlAsync(string key, string contentType, DateTimeOffset expiresAt, CancellationToken cancellationToken) =>
        Task.FromResult(new Uri($"https://storage.test/{key}?upload"));

    public Task<Uri> CreateDownloadUrlAsync(string key, DateTimeOffset expiresAt, CancellationToken cancellationToken) =>
        Task.FromResult(new Uri($"https://storage.test/{key}?download"));

    public Task<StoredObjectInfo?> GetInfoAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(_objects.TryGetValue(key, out var stored) ? new StoredObjectInfo(stored.Content.Length, stored.ContentType) : null);

    public Task<byte[]> ReadStartAsync(string key, int count, CancellationToken cancellationToken) =>
        Task.FromResult(_objects[key].Content.Take(count).ToArray());

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new MemoryStream(_objects[key].Content));

    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        _objects[key] = (buffer.ToArray(), contentType);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        _objects.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <summary>Makes <see cref="EnsureReadyAsync"/> fail, as when the storage isn't running.</summary>
    public Exception? EnsureReadyFailure { get; set; }

    public int EnsureReadyCalls { get; private set; }

    public Task EnsureReadyAsync(CancellationToken cancellationToken)
    {
        EnsureReadyCalls++;
        return EnsureReadyFailure is null ? Task.CompletedTask : Task.FromException(EnsureReadyFailure);
    }
}
