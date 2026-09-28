using Aprint.Features.Editor;

namespace Aprint.Tests.Editor;

public sealed class ImageEditingServiceTests
{
    [Fact]
    public void ApplyBorrarFillsSelectionWithSolidColor()
    {
        using var bitmap = new Bitmap(4, 4);

        ImageEditingService.ApplyBorrar(
            bitmap,
            new Rectangle(1, 1, 2, 2),
            BorrarMode.Solid,
            Color.Red,
            pixelSize: 4);

        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(1, 1).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(2, 2).ToArgb());
        Assert.Equal(Color.FromArgb(0, 0, 0, 0).ToArgb(), bitmap.GetPixel(0, 0).ToArgb());
    }

    [Fact]
    public void CreateSelectionRectangleNormalizesReverseDrag()
    {
        var rectangle = ImageEditingService.CreateSelectionRectangle(new Point(8, 6), new Point(2, 1));

        Assert.Equal(new Rectangle(2, 1, 6, 5), rectangle);
    }

    [Fact]
    public void ApplyHighlighterDrawsTranslucentStroke()
    {
        using var bitmap = new Bitmap(32, 24);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);
        }

        ImageEditingService.ApplyHighlighter(bitmap, [new Point(4, 12), new Point(27, 12)], Color.Yellow, width: 18);

        var highlightedPixel = bitmap.GetPixel(15, 12);
        Assert.True(highlightedPixel.R > 240);
        Assert.InRange(highlightedPixel.G, 245, 255);
        Assert.InRange(highlightedPixel.B, 140, 190);
        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(15, 0).ToArgb());
    }

    [Fact]
    public void ApplyHighlighterUsesSelectedWidth()
    {
        using var thinBitmap = new Bitmap(32, 24);
        using var thickBitmap = new Bitmap(32, 24);
        using (var graphics = Graphics.FromImage(thinBitmap))
        {
            graphics.Clear(Color.White);
        }

        using (var graphics = Graphics.FromImage(thickBitmap))
        {
            graphics.Clear(Color.White);
        }

        Point[] points = [new Point(4, 16), new Point(27, 16)];
        ImageEditingService.ApplyHighlighter(thinBitmap, points, Color.Yellow, width: 4);
        ImageEditingService.ApplyHighlighter(thickBitmap, points, Color.Yellow, width: 24);

        Assert.Equal(Color.White.ToArgb(), thinBitmap.GetPixel(15, 7).ToArgb());
        Assert.NotEqual(Color.White.ToArgb(), thickBitmap.GetPixel(15, 7).ToArgb());
    }
}
