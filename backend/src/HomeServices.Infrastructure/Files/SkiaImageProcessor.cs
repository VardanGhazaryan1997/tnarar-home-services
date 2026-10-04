using HomeServices.Application.Files;
using SkiaSharp;

namespace HomeServices.Infrastructure.Files;

/// <summary>Image thumbnails with SkiaSharp. Phone photos are turned upright using their EXIF orientation.</summary>
public sealed class SkiaImageProcessor : IImageProcessor
{
    public const int JpegQuality = 80;

    public ProcessedImage? Process(Stream image, int maxSize)
    {
        using var codec = SKCodec.Create(image);
        if (codec is null)
        {
            return null;
        }

        using var bitmap = SKBitmap.Decode(codec);
        if (bitmap is null)
        {
            return null;
        }

        var origin = codec.EncodedOrigin;
        var sideways = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var width = sideways ? bitmap.Height : bitmap.Width;
        var height = sideways ? bitmap.Width : bitmap.Height;

        var scale = Math.Min(1f, maxSize / (float)Math.Max(width, height));
        var thumbnailWidth = Math.Max(1, (int)Math.Round(width * scale));
        var thumbnailHeight = Math.Max(1, (int)Math.Round(height * scale));

        using var surface = SKSurface.Create(new SKImageInfo(thumbnailWidth, thumbnailHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;

        // JPEG has no transparency: transparent PNG areas become white, not black.
        canvas.Clear(SKColors.White);
        canvas.SetMatrix(SKMatrix.CreateScale(scale, scale).PreConcat(Orientation(origin, width, height)));
        using (var source = SKImage.FromBitmap(bitmap))
        {
            canvas.DrawImage(source, 0, 0, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        }

        using var snapshot = surface.Snapshot();
        using var jpeg = snapshot.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
        return new ProcessedImage(width, height, jpeg.ToArray());
    }

    // Maps the stored pixels onto an upright picture of width × height.
    private static SKMatrix Orientation(SKEncodedOrigin origin, float width, float height) => origin switch
    {
        SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, width, 0, 1, 0, 0, 0, 1),
        SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, width, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightTop => new SKMatrix(0, -1, width, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, width, -1, 0, height, 0, 0, 1),
        SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, height, 0, 0, 1),
        _ => SKMatrix.Identity,
    };
}
