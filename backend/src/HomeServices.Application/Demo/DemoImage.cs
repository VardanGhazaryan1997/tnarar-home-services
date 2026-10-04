using System.IO.Compression;

namespace HomeServices.Application.Demo;

/// <summary>
/// Draws simple placeholder pictures for demo profiles as PNG files: a soft two-colour gradient with a few
/// "tiles", different for every seed. No image library needed.
/// </summary>
public static class DemoImage
{
    private static readonly uint[] CrcTable = BuildCrcTable();

    /// <summary>A PNG of <paramref name="width"/> × <paramref name="height"/> pixels; the same seed gives the same picture.</summary>
    public static byte[] Png(int seed, int width = 640, int height = 480)
    {
        var random = new Random(seed);
        var from = (R: random.Next(60, 200), G: random.Next(60, 200), B: random.Next(60, 200));
        var to = (R: random.Next(120, 255), G: random.Next(120, 255), B: random.Next(120, 255));
        var tile = random.Next(48, 120);

        // Raw scanlines: a filter byte (0 = none) then RGB pixels.
        var raw = new byte[height * ((width * 3) + 1)];
        var index = 0;
        for (var y = 0; y < height; y++)
        {
            raw[index++] = 0;
            for (var x = 0; x < width; x++)
            {
                var t = (x + y) / (double)(width + height);
                var shade = ((x / tile) + (y / tile)) % 2 == 0 ? 1.0 : 0.88;
                raw[index++] = (byte)(((from.R * (1 - t)) + (to.R * t)) * shade);
                raw[index++] = (byte)(((from.G * (1 - t)) + (to.G * t)) * shade);
                raw[index++] = (byte)(((from.B * (1 - t)) + (to.B * t)) * shade);
            }
        }

        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        var header = new byte[13];
        WriteBigEndian(header, 0, (uint)width);
        WriteBigEndian(header, 4, (uint)height);
        header[8] = 8; // bit depth
        header[9] = 2; // colour type: RGB
        WriteChunk(png, "IHDR", header);

        using (var compressed = new MemoryStream())
        {
            using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            {
                zlib.Write(raw);
            }

            WriteChunk(png, "IDAT", compressed.ToArray());
        }

        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        var length = new byte[4];
        WriteBigEndian(length, 0, (uint)data.Length);
        stream.Write(length);

        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(data);

        var crc = Crc(Crc(0xFFFFFFFFu, typeBytes), data) ^ 0xFFFFFFFFu;
        var crcBytes = new byte[4];
        WriteBigEndian(crcBytes, 0, crc);
        stream.Write(crcBytes);
    }

    private static uint Crc(uint crc, byte[] data)
    {
        foreach (var b in data)
        {
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        return crc;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }

    private static void WriteBigEndian(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }
}
