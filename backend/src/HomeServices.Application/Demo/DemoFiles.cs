using HomeServices.Application.Files;
using HomeServices.Domain.Files;

namespace HomeServices.Application.Demo;

/// <summary>Stores the placeholder pictures of demo data: draws the PNG, puts it in file storage and returns the ready file record.</summary>
internal static class DemoFiles
{
    public static async Task<StoredFile> StoreImageAsync(
        IFileStorage storage,
        FileOwnerType ownerType,
        string ownerId,
        string name,
        int seed,
        int width,
        int height,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var bytes = DemoImage.Png(seed, width, height);
        var file = StoredFile.Begin(ownerType, ownerId, name, "image/png", bytes.Length, now);
        using (var content = new MemoryStream(bytes))
        {
            await storage.PutAsync(file.Key, content, file.ContentType, cancellationToken);
        }

        file.MarkReady(bytes.Length, now, thumbnailKey: null, width, height);
        return file;
    }

    /// <summary>A number that is the same on every run (unlike <c>string.GetHashCode</c>), to seed a picture by name.</summary>
    public static int StableSeed(string text)
    {
        var hash = 17;
        foreach (var c in text)
        {
            hash = unchecked((hash * 31) + c);
        }

        return hash & 0x7FFFFFFF;
    }
}
