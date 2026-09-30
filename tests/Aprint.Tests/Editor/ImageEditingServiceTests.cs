using Aprint.Features.Editor;

namespace Aprint.Tests.Editor;

public sealed class ImageEditingServiceTests
{
    [Theory]
    [InlineData(EditTool.Rectangle)]
    [InlineData(EditTool.Circle)]
    public void DrawShapeKeepsOutlineAndFillColorsIndependent(EditTool tool)
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        using var pen = new Pen(Color.Red);

        ImageEditingService.DrawShape(graphics, pen, new Point(4, 4), new Point(26, 26), tool, Color.Blue);

        Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(15, 15).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(15, 4).ToArgb());
        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(0, 0).ToArgb());
    }

    [Theory]
    [InlineData(EditTool.Rectangle)]
    [InlineData(EditTool.Circle)]
    public void DrawShapeRemainsHollowWithoutFillColor(EditTool tool)
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        using var pen = new Pen(Color.Red);

        ImageEditingService.DrawShape(graphics, pen, new Point(4, 4), new Point(26, 26), tool);

        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(15, 15).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(15, 4).ToArgb());
    }

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
    public void ApplyBorrarPixelatesSelectionWithoutFillingIt()
    {
        using var bitmap = new Bitmap(4, 4);
        for (var row = 0; row < bitmap.Height; row++)
        {
            for (var column = 0; column < bitmap.Width; column++)
            {
                bitmap.SetPixel(column, row, Color.FromArgb(column * 40, row * 40, 0));
            }
        }

        ImageEditingService.ApplyBorrar(bitmap, new Rectangle(0, 0, 4, 4), BorrarMode.Pixelate, Color.Red, pixelSize: 4);

        Assert.Equal(Color.FromArgb(60, 60, 0).ToArgb(), bitmap.GetPixel(0, 0).ToArgb());
        Assert.Equal(bitmap.GetPixel(0, 0).ToArgb(), bitmap.GetPixel(3, 3).ToArgb());
        Assert.NotEqual(Color.Red.ToArgb(), bitmap.GetPixel(0, 0).ToArgb());
    }

    [Fact]
    public void CreateSelectionRectangleNormalizesReverseDrag()
    {
        var rectangle = ImageEditingService.CreateSelectionRectangle(new Point(8, 6), new Point(2, 1));

        Assert.Equal(new Rectangle(2, 1, 6, 5), rectangle);
    }

    [Fact]
    public void CropPreservesPixelsWithoutResizingSource()
    {
        using var bitmap = new Bitmap(4, 3);
        for (var row = 0; row < bitmap.Height; row++)
        {
            for (var column = 0; column < bitmap.Width; column++)
            {
                bitmap.SetPixel(column, row, Color.FromArgb(column * 40, row * 70, column + row));
            }
        }

        using var cropped = ImageEditingService.Crop(bitmap, new Rectangle(1, 1, 2, 2));

        Assert.Equal(new Size(2, 2), cropped.Size);
        Assert.Equal(new Size(4, 3), bitmap.Size);
        for (var row = 0; row < cropped.Height; row++)
        {
            for (var column = 0; column < cropped.Width; column++)
            {
                Assert.Equal(bitmap.GetPixel(column + 1, row + 1).ToArgb(), cropped.GetPixel(column, row).ToArgb());
            }
        }
    }

    [Theory]
    [InlineData(0, 0, 0, 2)]
    [InlineData(3, 0, 2, 2)]
    public void CropRejectsEmptyOrOutOfBoundsAreas(int x, int y, int width, int height)
    {
        using var bitmap = new Bitmap(4, 3);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ImageEditingService.Crop(bitmap, new Rectangle(x, y, width, height)));
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

    [Fact]
    public void ApplyTextDrawsWithinTheSelectedBounds()
    {
        using var bitmap = new Bitmap(120, 60);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);
        }

        using var font = new Font("Segoe UI", 12F);
        var textBounds = new Rectangle(10, 10, 80, 30);
        ImageEditingService.ApplyText(bitmap, textBounds, "Text", Color.Black, font);

        var changedPixels = 0;
        for (var y = textBounds.Top; y < textBounds.Bottom; y++)
        {
            for (var x = textBounds.Left; x < textBounds.Right; x++)
            {
                if (bitmap.GetPixel(x, y).ToArgb() != Color.White.ToArgb())
                {
                    changedPixels++;
                }
            }
        }

        Assert.True(changedPixels > 0);
        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(100, 50).ToArgb());
    }

    [Fact]
    public void MeasureTextContentGrowsForLongerLinesAndLineBreaks()
    {
        using var font = new Font("Segoe UI", 12F);

        var caretSize = ImageEditingService.MeasureTextContent(string.Empty, font);
        var shortLine = ImageEditingService.MeasureTextContent("Hi", font);
        var longLine = ImageEditingService.MeasureTextContent("A much longer line", font);
        var twoLines = ImageEditingService.MeasureTextContent("Hi\nthere", font);

        Assert.Equal(font.Height, caretSize.Height);
        Assert.True(longLine.Width > shortLine.Width);
        Assert.Equal(shortLine.Height * 2, twoLines.Height);
    }
}
