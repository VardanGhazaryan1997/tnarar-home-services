using HomeServices.Domain.Common;

namespace HomeServices.Domain.Files;

public enum FileKind
{
    Image = 1,
    Video = 2,
    Document = 3,
}

public enum FileStatus
{
    /// <summary>An upload link was issued; the file may not be in storage yet.</summary>
    Pending = 1,

    /// <summary>Uploaded and checked; images have a thumbnail.</summary>
    Ready = 2,

    /// <summary>Uploaded but refused (too large, not what it claimed to be). The stored object is deleted.</summary>
    Rejected = 3,
}

public enum FileOwnerType
{
    User = 1,
    Staff = 2,
}

/// <summary>
/// A file in object storage (photos, videos, documents). The browser uploads it directly with a
/// signed link; the API then checks it and, for images, makes a thumbnail.
/// </summary>
public sealed class StoredFile : AuditableEntity
{
    public const int FileNameMaxLength = 255;
    public const int ContentTypeMaxLength = 100;
    public const int KeyMaxLength = 300;
    public const int OwnerIdMaxLength = 64;
    public const int RejectionCodeMaxLength = 64;
    public const int ThumbnailMaxSize = 480;

    private static readonly Dictionary<string, (FileKind Kind, string Extension)> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = (FileKind.Image, ".jpg"),
        ["image/png"] = (FileKind.Image, ".png"),
        ["image/webp"] = (FileKind.Image, ".webp"),
        ["video/mp4"] = (FileKind.Video, ".mp4"),
        ["video/quicktime"] = (FileKind.Video, ".mov"),
        ["application/pdf"] = (FileKind.Document, ".pdf"),
    };

    private StoredFile()
    {
    }

    public FileOwnerType OwnerType { get; private set; }

    public string OwnerId { get; private set; } = string.Empty;

    public FileKind Kind { get; private set; }

    public FileStatus Status { get; private set; }

    /// <summary>The name the file had on the uploader's device (shown to people, never used as a path).</summary>
    public string OriginalFileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    /// <summary>Declared size when the upload starts; the actual size once it's ready.</summary>
    public long Size { get; private set; }

    /// <summary>Object key in the storage bucket, e.g. "files/2026/10/{id}/original.jpg".</summary>
    public string Key { get; private set; } = string.Empty;

    public string? ThumbnailKey { get; private set; }

    public int? Width { get; private set; }

    public int? Height { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Why the file was rejected (an error code), if it was.</summary>
    public string? RejectionCode { get; private set; }

    /// <summary>The largest allowed size for a kind of file, in bytes.</summary>
    public static long MaxSizeFor(FileKind kind) => kind switch
    {
        FileKind.Image => 10L * 1024 * 1024,
        FileKind.Video => 200L * 1024 * 1024,
        _ => 10L * 1024 * 1024,
    };

    /// <summary>The kind of file for an allowed content type.</summary>
    public static FileKind KindOf(string contentType) =>
        AllowedTypes.TryGetValue(contentType.Trim(), out var allowed)
            ? allowed.Kind
            : throw new DomainException("file.type_not_allowed", $"Files of type '{contentType}' can't be uploaded.");

    public static bool IsAllowedType(string? contentType) => contentType is not null && AllowedTypes.ContainsKey(contentType.Trim());

    /// <summary>Starts an upload: checks the type and declared size and picks the storage key.</summary>
    public static StoredFile Begin(FileOwnerType ownerType, string ownerId, string fileName, string contentType, long size, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(ownerId) || ownerId.Length > OwnerIdMaxLength)
        {
            throw new DomainException("file.owner_required", "A file needs an owner.");
        }

        var type = contentType.Trim().ToLowerInvariant();
        var kind = KindOf(type);
        if (size <= 0 || size > MaxSizeFor(kind))
        {
            throw new DomainException("file.too_large", $"The file must be between 1 byte and {MaxSizeFor(kind)} bytes.");
        }

        var file = new StoredFile
        {
            OwnerType = ownerType,
            OwnerId = ownerId,
            Kind = kind,
            Status = FileStatus.Pending,
            OriginalFileName = CleanFileName(fileName),
            ContentType = type,
            Size = size,
        };
        file.Key = $"files/{now.UtcDateTime:yyyy}/{now.UtcDateTime:MM}/{file.Id:N}/original{AllowedTypes[type].Extension}";
        return file;
    }

    public bool IsOwnedBy(FileOwnerType ownerType, string? ownerId) => OwnerType == ownerType && OwnerId == ownerId;

    /// <summary>The file is in storage and checked. <paramref name="thumbnailKey"/> and the dimensions are for images.</summary>
    public void MarkReady(long actualSize, DateTimeOffset now, string? thumbnailKey = null, int? width = null, int? height = null)
    {
        EnsurePending();
        Size = actualSize;
        ThumbnailKey = thumbnailKey;
        Width = width;
        Height = height;
        Status = FileStatus.Ready;
        CompletedAt = now;
    }

    public void Reject(string code, DateTimeOffset now)
    {
        EnsurePending();
        Status = FileStatus.Rejected;
        RejectionCode = code;
        CompletedAt = now;
    }

    /// <summary>Where the thumbnail of this file goes.</summary>
    public string ThumbnailKeyFor() => Key[..Key.LastIndexOf('/')] + "/thumbnail.jpg";

    private void EnsurePending()
    {
        if (Status != FileStatus.Pending)
        {
            throw new DomainException("file.already_completed", "This upload was already completed.");
        }
    }

    // Keep only the name part, without control characters, within the length limit.
    private static string CleanFileName(string fileName)
    {
        var name = Path.GetFileName((fileName ?? string.Empty).Replace('\\', '/'));
        var cleaned = new string(name.Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (cleaned.Length == 0)
        {
            cleaned = "file";
        }

        return cleaned.Length > FileNameMaxLength ? cleaned[^FileNameMaxLength..] : cleaned;
    }
}
