using HomeServices.Infrastructure.Files;
using SkiaSharp;

namespace HomeServices.Infrastructure.IntegrationTests.Files;

public class SkiaImageProcessorTests
{
    private static readonly SkiaImageProcessor Processor = new();

    private static readonly SKColor Red = new(255, 0, 0);
    private static readonly SKColor Green = new(0, 255, 0);
    private static readonly SKColor Blue = new(0, 0, 255);
    private static readonly SKColor Yellow = new(255, 255, 0);

    private static byte[] Encode(SKBitmap bitmap, SKEncodedImageFormat format)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 95);
        return data.ToArray();
    }

    private static byte[] Solid(int width, int height, SKEncodedImageFormat format, SKColor? color = null)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(color ?? SKColors.SteelBlue);
        }

        return Encode(bitmap, format);
    }

    // 400×200 JPEG with four coloured quarters: red | green over blue | yellow.
    private static byte[] Quarters()
    {
        using var bitmap = new SKBitmap(400, 200);
        using (var canvas = new SKCanvas(bitmap))
        {
            void Fill(SKColor color, float x, float y)
            {
                using var paint = new SKPaint { Color = color };
                canvas.DrawRect(x, y, 200, 100, paint);
            }

            Fill(Red, 0, 0);
            Fill(Green, 200, 0);
            Fill(Blue, 0, 100);
            Fill(Yellow, 200, 100);
        }

        return Encode(bitmap, SKEncodedImageFormat.Jpeg);
    }

    // Adds an EXIF block with only an Orientation tag, as phone cameras write.
    private static byte[] WithExifOrientation(byte[] jpeg, ushort orientation)
    {
        byte[] tiff =
        [
            (byte)'M', (byte)'M', 0, 42, 0, 0, 0, 8, // big-endian header, first directory at offset 8
            0, 1, // one entry
            0x01, 0x12, 0, 3, 0, 0, 0, 1, (byte)(orientation >> 8), (byte)orientation, 0, 0, // Orientation, SHORT, 1 value
            0, 0, 0, 0, // no next directory
        ];
        byte[] exif = [.. "Exif\0\0"u8, .. tiff];
        var length = exif.Length + 2;
        return [.. jpeg[..2], 0xFF, 0xE1, (byte)(length >> 8), (byte)length, .. exif, .. jpeg[2..]];
    }

    private static SKBitmap DecodeThumbnail(byte[] jpeg)
    {
        jpeg[..3].ShouldBe(new byte[] { 0xFF, 0xD8, 0xFF });
        return SKBitmap.Decode(jpeg);
    }

    private static void ShouldBeNear(SKColor actual, SKColor expected)
    {
        Math.Abs(actual.Red - expected.Red).ShouldBeLessThan(60, $"{actual} vs {expected}");
        Math.Abs(actual.Green - expected.Green).ShouldBeLessThan(60, $"{actual} vs {expected}");
        Math.Abs(actual.Blue - expected.Blue).ShouldBeLessThan(60, $"{actual} vs {expected}");
    }

    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg, 2000, 1000, 480, 240)]
    [InlineData(SKEncodedImageFormat.Png, 1000, 3000, 160, 480)]
    [InlineData(SKEncodedImageFormat.Webp, 960, 960, 480, 480)]
    public void Large_images_get_a_JPEG_thumbnail_that_fits_the_limit(SKEncodedImageFormat format, int width, int height, int thumbWidth, int thumbHeight)
    {
        var result = Processor.Process(new MemoryStream(Solid(width, height, format)), 480);

        result.ShouldNotBeNull();
        result.Width.ShouldBe(width);
        result.Height.ShouldBe(height);
        using var thumbnail = DecodeThumbnail(result.ThumbnailJpeg);
        thumbnail.Width.ShouldBe(thumbWidth);
        thumbnail.Height.ShouldBe(thumbHeight);
    }

    [Fact]
    public void Small_images_are_not_enlarged()
    {
        var result = Processor.Process(new MemoryStream(Solid(100, 50, SKEncodedImageFormat.Png)), 480);

        using var thumbnail = DecodeThumbnail(result!.ThumbnailJpeg);
        thumbnail.Width.ShouldBe(100);
        thumbnail.Height.ShouldBe(50);
    }

    [Fact]
    public void Transparent_areas_become_white()
    {
        var result = Processor.Process(new MemoryStream(Solid(50, 50, SKEncodedImageFormat.Png, SKColors.Transparent)), 480);

        using var thumbnail = DecodeThumbnail(result!.ThumbnailJpeg);
        ShouldBeNear(thumbnail.GetPixel(25, 25), SKColors.White);
    }

    // Orientation values 1–8 and which quarter of the stored picture ends up top-left when shown upright.
    [Theory]
    [InlineData(1, 400, 200, "red")] // as stored
    [InlineData(2, 400, 200, "green")] // mirrored
    [InlineData(3, 400, 200, "yellow")] // upside down
    [InlineData(4, 400, 200, "blue")] // mirrored upside down
    [InlineData(5, 200, 400, "red")] // transposed
    [InlineData(6, 200, 400, "blue")] // phone held upright: turn 90° clockwise
    [InlineData(7, 200, 400, "yellow")] // transverse
    [InlineData(8, 200, 400, "green")] // turn 90° anticlockwise
    public void Photos_are_turned_upright_using_their_EXIF_orientation(int orientation, int width, int height, string topLeft)
    {
        var result = Processor.Process(new MemoryStream(WithExifOrientation(Quarters(), (ushort)orientation)), 480);

        result.ShouldNotBeNull();
        result.Width.ShouldBe(width);
        result.Height.ShouldBe(height);
        using var thumbnail = DecodeThumbnail(result.ThumbnailJpeg);
        thumbnail.Width.ShouldBe(width);
        thumbnail.Height.ShouldBe(height);
        var expected = topLeft switch { "red" => Red, "green" => Green, "blue" => Blue, _ => Yellow };
        ShouldBeNear(thumbnail.GetPixel(20, 20), expected);
    }

    [Fact]
    public void Data_that_is_not_an_image_gives_nothing()
    {
        Processor.Process(new MemoryStream("%PDF-1.7 not an image"u8.ToArray()), 480).ShouldBeNull();
        Processor.Process(new MemoryStream(), 480).ShouldBeNull();
    }

    [Fact]
    public void A_truncated_image_gives_nothing()
    {
        var jpeg = Solid(200, 200, SKEncodedImageFormat.Jpeg);

        Processor.Process(new MemoryStream(jpeg[..20]), 480).ShouldBeNull();
    }
}
