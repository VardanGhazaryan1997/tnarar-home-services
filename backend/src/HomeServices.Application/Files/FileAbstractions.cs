namespace HomeServices.Application.Files;

/// <summary>
/// Object storage (S3-compatible: SeaweedFS in development, any S3 service in production).
/// Browsers upload and download directly with signed links; the API never streams large files.
/// </summary>
public interface IFileStorage
{
    /// <summary>A signed link the browser can PUT the file to, with this exact Content-Type, until <paramref name="expiresAt"/>.</summary>
    Task<Uri> CreateUploadUrlAsync(string key, string contentType, DateTimeOffset expiresAt, CancellationToken cancellationToken);

    /// <summary>A signed link to download the object until <paramref name="expiresAt"/>.</summary>
    Task<Uri> CreateDownloadUrlAsync(string key, DateTimeOffset expiresAt, CancellationToken cancellationToken);

    /// <summary>Size and type of a stored object, or null when there is no such object.</summary>
    Task<StoredObjectInfo?> GetInfoAsync(string key, CancellationToken cancellationToken);

    /// <summary>The first <paramref name="count"/> bytes of an object (fewer if it's shorter).</summary>
    Task<byte[]> ReadStartAsync(string key, int count, CancellationToken cancellationToken);

    /// <summary>The whole object. The caller disposes the stream.</summary>
    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken);

    Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken);

    /// <summary>Deletes an object; does nothing when it doesn't exist.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken);

    /// <summary>Prepares the storage for use (e.g. creates the bucket in development).</summary>
    Task EnsureReadyAsync(CancellationToken cancellationToken);
}

public sealed record StoredObjectInfo(long Size, string? ContentType);

/// <summary>Reads images and makes thumbnails.</summary>
public interface IImageProcessor
{
    /// <summary>
    /// The image's size (after applying its EXIF orientation) and a JPEG thumbnail no larger than
    /// <paramref name="maxSize"/> on its longest side, or null when the data isn't a readable image.
    /// </summary>
    ProcessedImage? Process(Stream image, int maxSize);
}

public sealed record ProcessedImage(int Width, int Height, byte[] ThumbnailJpeg);

/// <summary>Upload rules (configuration section "Files").</summary>
public sealed class FileSettings
{
    public const string SectionName = "Files";

    /// <summary>How long a signed upload link works.</summary>
    public int UploadUrlLifetimeMinutes { get; set; } = 15;

    /// <summary>How long a signed download link works.</summary>
    public int DownloadUrlLifetimeMinutes { get; set; } = 60;

    /// <summary>How many uploads one person may start per hour.</summary>
    public int MaxUploadsPerHour { get; set; } = 60;
}
