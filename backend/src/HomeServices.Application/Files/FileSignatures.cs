using System.Text;

namespace HomeServices.Application.Files;

/// <summary>
/// Checks that a file's first bytes match its declared type, so a renamed executable can't
/// pass as a photo. Browsers send the type they guess from the file name; this is the real check.
/// </summary>
public static class FileSignatures
{
    /// <summary>How many bytes <see cref="Matches"/> needs.</summary>
    public const int HeaderLength = 12;

    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly string[] QuickTimeAtoms = ["ftyp", "moov", "mdat", "wide", "free", "skip"];

    public static bool Matches(string contentType, ReadOnlySpan<byte> header) => contentType switch
    {
        "image/jpeg" => header.StartsWith(Jpeg.AsSpan()),
        "image/png" => header.StartsWith(Png.AsSpan()),
        "image/webp" => header.Length >= 12 && Ascii(header[..4]) == "RIFF" && Ascii(header[8..12]) == "WEBP",
        "video/mp4" => header.Length >= 8 && Ascii(header[4..8]) == "ftyp",
        "video/quicktime" => header.Length >= 8 && QuickTimeAtoms.Contains(Ascii(header[4..8])),
        "application/pdf" => header.StartsWith("%PDF-"u8),
        _ => false,
    };

    private static string Ascii(ReadOnlySpan<byte> bytes) => Encoding.ASCII.GetString(bytes);
}
