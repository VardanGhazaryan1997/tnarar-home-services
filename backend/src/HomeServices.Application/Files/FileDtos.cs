using HomeServices.Domain.Files;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Files;

/// <summary>
/// Everything the browser needs to upload a file: PUT the bytes to <see cref="UploadUrl"/> with
/// <see cref="Headers"/>, then call "complete" with <see cref="FileId"/>.
/// </summary>
public sealed record UploadTicket(
    Guid FileId,
    string UploadUrl,
    string Method,
    IReadOnlyDictionary<string, string> Headers,
    DateTimeOffset ExpiresAt);

/// <summary>A file. <see cref="Url"/> and <see cref="ThumbnailUrl"/> are signed links that expire; they're set once the file is ready.</summary>
public sealed record FileDto(
    Guid Id,
    string Kind,
    string Status,
    string FileName,
    string ContentType,
    long Size,
    int? Width,
    int? Height,
    string? Url,
    string? ThumbnailUrl,
    DateTimeOffset? UrlExpiresAt,
    DateTimeOffset CreatedAt);

/// <summary>Builds <see cref="FileDto"/>s with fresh download links.</summary>
public sealed class FileDtoFactory(IFileStorage storage, IOptions<FileSettings> settings, TimeProvider clock)
{
    public async Task<FileDto> CreateAsync(StoredFile file, CancellationToken cancellationToken)
    {
        string? url = null;
        string? thumbnailUrl = null;
        DateTimeOffset? expiresAt = null;

        if (file.Status == FileStatus.Ready)
        {
            expiresAt = clock.GetUtcNow().AddMinutes(settings.Value.DownloadUrlLifetimeMinutes);
            url = (await storage.CreateDownloadUrlAsync(file.Key, expiresAt.Value, cancellationToken)).ToString();
            if (file.ThumbnailKey is not null)
            {
                thumbnailUrl = (await storage.CreateDownloadUrlAsync(file.ThumbnailKey, expiresAt.Value, cancellationToken)).ToString();
            }
        }

        return new FileDto(
            file.Id,
            file.Kind.ToString(),
            file.Status.ToString(),
            file.OriginalFileName,
            file.ContentType,
            file.Size,
            file.Width,
            file.Height,
            url,
            thumbnailUrl,
            expiresAt,
            file.CreatedAt);
    }
}
