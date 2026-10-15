using System.Drawing.Imaging;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace Aprint.Features.Capture;

public static class WindowsTextRegionDetector
{
    private const int TileOverlap = 96;
    private static readonly SemaphoreSlim DetectionSemaphore = new(1, 1);

    public static async Task<IReadOnlyList<Rectangle>?> DetectRegionsAsync(
        Bitmap bitmap,
        CancellationToken cancellationToken = default)
    {
        await DetectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var engine = OcrEngine.TryCreateFromUserProfileLanguages();
            if (engine is null)
            {
                return null;
            }

            var maximumDimension = checked((int)OcrEngine.MaxImageDimension);
            var tileStride = maximumDimension - TileOverlap;
            var lineBounds = new List<Rectangle>();

            for (var top = 0; top < bitmap.Height; top += tileStride)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var height = Math.Min(maximumDimension, bitmap.Height - top);
                for (var left = 0; left < bitmap.Width; left += tileStride)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var width = Math.Min(maximumDimension, bitmap.Width - left);
                    var tileBounds = new Rectangle(left, top, width, height);
                    using var encodedImage = await Task.Run(
                        () => EncodeTile(bitmap, tileBounds),
                        cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    using var randomAccessStream = encodedImage.AsRandomAccessStream();
                    var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
                    cancellationToken.ThrowIfCancellationRequested();
                    using var softwareBitmap = await decoder.GetSoftwareBitmapAsync(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Premultiplied);
                    cancellationToken.ThrowIfCancellationRequested();
                    var result = await engine.RecognizeAsync(softwareBitmap);
                    cancellationToken.ThrowIfCancellationRequested();

                    foreach (var line in result.Lines)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var words = line.Words;
                        if (words.Count == 0)
                        {
                            continue;
                        }

                        var leftEdge = words.Min(word => word.BoundingRect.X);
                        var topEdge = words.Min(word => word.BoundingRect.Y);
                        var rightEdge = words.Max(word => word.BoundingRect.X + word.BoundingRect.Width);
                        var bottomEdge = words.Max(word => word.BoundingRect.Y + word.BoundingRect.Height);
                        var padding = Math.Max(2, (int)Math.Ceiling((bottomEdge - topEdge) * 0.08));
                        var region = Rectangle.FromLTRB(
                            Math.Max(0, (int)Math.Floor(leftEdge) - padding),
                            Math.Max(0, (int)Math.Floor(topEdge) - padding),
                            Math.Min(tileBounds.Width, (int)Math.Ceiling(rightEdge) + padding),
                            Math.Min(tileBounds.Height, (int)Math.Ceiling(bottomEdge) + padding));

                        if (region.Width > 0 && region.Height > 0)
                        {
                            lineBounds.Add(new Rectangle(region.X + left, region.Y + top, region.Width, region.Height));
                        }
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return MergeDuplicateLines(lineBounds);
        }
        finally
        {
            DetectionSemaphore.Release();
        }
    }

    private static MemoryStream EncodeTile(Bitmap bitmap, Rectangle tileBounds)
    {
        using var tile = bitmap.Clone(tileBounds, PixelFormat.Format32bppArgb);
        var encodedImage = new MemoryStream();
        try
        {
            tile.Save(encodedImage, ImageFormat.Png);
            encodedImage.Position = 0;
            return encodedImage;
        }
        catch
        {
            encodedImage.Dispose();
            throw;
        }
    }

    private static IReadOnlyList<Rectangle> MergeDuplicateLines(List<Rectangle> lines)
    {
        var merged = new List<Rectangle>();
        foreach (var line in lines)
        {
            var duplicateIndex = merged.FindIndex(existing => IsDuplicateLine(existing, line));
            if (duplicateIndex < 0)
            {
                merged.Add(line);
                continue;
            }

            merged[duplicateIndex] = Rectangle.Union(merged[duplicateIndex], line);
        }

        return merged;
    }

    private static bool IsDuplicateLine(Rectangle first, Rectangle second)
    {
        var overlap = Rectangle.Intersect(first, second);
        if (overlap.IsEmpty)
        {
            return false;
        }

        var minimumOverlapWidth = Math.Min(first.Width, second.Width) * 0.5;
        var minimumOverlapHeight = Math.Min(first.Height, second.Height) * 0.5;
        return overlap.Width >= minimumOverlapWidth && overlap.Height >= minimumOverlapHeight;
    }
}