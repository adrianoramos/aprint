using Aprint.Features.Editor;

namespace Aprint.Tests.Editor;

public sealed class ImageEditingServiceTests
{
    [Fact]
    public void CounterMarkerUsesFixedSizeAndFontThatFitTwoDigits()
    {
        Assert.Equal(40, ImageEditingService.CounterMarkerDiameter);
        Assert.Equal(12F, ImageEditingService.GetCounterMarkerFontSize(ImageEditingService.CounterMarkerDiameter));
        Assert.Equal(new Rectangle(30, 30, 40, 40), ImageEditingService.GetCounterMarkerBounds(new Point(50, 50), ImageEditingService.CounterMarkerDiameter));
    }

    [Fact]
    public void CounterMarkerCenterIsClampedSoTheCircleStaysInsideTheArea()
    {
        var area = new Rectangle(10, 20, 100, 80);
        var diameter = ImageEditingService.GetCounterMarkerDiameter(area.Size);
        var center = ImageEditingService.ClampCounterMarkerCenter(Point.Empty, area, diameter);
        var bounds = ImageEditingService.GetCounterMarkerBounds(center, diameter);

        Assert.Equal(new Point(30, 40), center);
        Assert.True(area.Contains(bounds));
    }

    [Fact]
    public void CreateCounterMarkerUsesCommonNumberAndBounds()
    {
        var area = new Rectangle(10, 20, 100, 80);

        var marker = ImageEditingService.CreateCounterMarker(Point.Empty, 99, area);

        Assert.Equal(99, marker.Value);
        Assert.Equal(ImageEditingService.CounterMarkerDiameter, marker.Diameter);
        Assert.True(area.Contains(marker.Bounds));
    }

    [Fact]
    public void DrawCounterMarkerUsesLineAndFillColorsIndependently()
    {
        using var bitmap = new Bitmap(40, 40);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        using var font = new Font("Segoe UI", 8F, FontStyle.Bold);

        ImageEditingService.DrawCounterMarker(graphics, new Rectangle(5, 5, 30, 30), "1", font, Color.Red, Color.Blue);

        Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(10, 20).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(20, 7).ToArgb());
        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(0, 0).ToArgb());
    }

    [Fact]
    public void DrawCounterMarkerUsesConfiguredTextColor()
    {
        using var bitmap = new Bitmap(40, 40);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        using var font = new Font("Segoe UI", 12F, FontStyle.Bold);

        ImageEditingService.DrawCounterMarker(
            graphics,
            new Rectangle(0, 0, 40, 40),
            "1",
            font,
            Color.Red,
            null,
            Color.Lime);

        Assert.Contains(Enumerable.Range(0, bitmap.Height)
            .SelectMany(y => Enumerable.Range(0, bitmap.Width)
                .Select(x => bitmap.GetPixel(x, y))),
            pixel => pixel.ToArgb() == Color.Lime.ToArgb());
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(20, 2).ToArgb());
    }

    [Fact]
    public void DrawCounterMarkerLeavesFillTransparentWhenNoFillColorIsSelected()
    {
        using var bitmap = new Bitmap(40, 40);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        using var font = new Font("Segoe UI", 8F, FontStyle.Bold);

        ImageEditingService.DrawCounterMarker(graphics, new Rectangle(5, 5, 30, 30), "1", font, Color.Red, null);

        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(10, 20).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(20, 7).ToArgb());
    }

    [Fact]
    public void DrawCounterMarkerKeepsTheWholeOutlineInsideItsClippedBounds()
    {
        using var bitmap = new Bitmap(40, 40);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        graphics.SetClip(new Rectangle(0, 0, 40, 40));
        using var font = new Font("Segoe UI", 12F, FontStyle.Bold);

        ImageEditingService.DrawCounterMarker(graphics, new Rectangle(0, 0, 40, 40), "99", font, Color.Red, null);

        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(20, 2).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(38, 20).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(20, 38).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(2, 20).ToArgb());
    }

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
    public void DrawAnnotationUsesSharedOutlineAndFillColors(EditTool tool)
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);

        ImageEditingService.DrawAnnotation(graphics, new Point(4, 4), new Point(26, 26), tool, Color.Red, Color.Blue);

        Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(15, 15).ToArgb());
        Assert.NotEqual(Color.White.ToArgb(), bitmap.GetPixel(15, 5).ToArgb());
        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(0, 0).ToArgb());
    }

    [Fact]
    public void DrawFreehandSegmentUsesSharedAnnotationColor()
    {
        using var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);

        ImageEditingService.DrawFreehandSegment(graphics, new Point(4, 12), new Point(20, 12), Color.Red);

        Assert.NotEqual(Color.White.ToArgb(), bitmap.GetPixel(12, 12).ToArgb());
        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(12, 2).ToArgb());
    }

    [Fact]
    public void DrawFreehandStrokeUsesSharedAnnotationColor()
    {
        using var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);

        ImageEditingService.DrawFreehandStroke(graphics, [new Point(4, 12), new Point(20, 12)], Color.Red);

        Assert.NotEqual(Color.White.ToArgb(), bitmap.GetPixel(12, 12).ToArgb());
        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(12, 2).ToArgb());
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
    public void ApplyMaskFillsSelectionWithSolidColor()
    {
        using var bitmap = new Bitmap(4, 4);

        ImageEditingService.ApplyMask(
            bitmap,
            new Rectangle(1, 1, 2, 2),
            MaskMode.Solid,
            Color.Red,
            pixelSize: 4);

        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(1, 1).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(2, 2).ToArgb());
        Assert.Equal(Color.FromArgb(0, 0, 0, 0).ToArgb(), bitmap.GetPixel(0, 0).ToArgb());
    }

    [Fact]
    public void ApplyMaskPixelatesSelectionWithoutFillingIt()
    {
        using var bitmap = new Bitmap(4, 4);
        for (var row = 0; row < bitmap.Height; row++)
        {
            for (var column = 0; column < bitmap.Width; column++)
            {
                bitmap.SetPixel(column, row, Color.FromArgb(column * 40, row * 40, 0));
            }
        }

        ImageEditingService.ApplyMask(bitmap, new Rectangle(0, 0, 4, 4), MaskMode.Pixelate, Color.Red, pixelSize: 4);

        Assert.Equal(Color.FromArgb(60, 60, 0).ToArgb(), bitmap.GetPixel(0, 0).ToArgb());
        Assert.Equal(bitmap.GetPixel(0, 0).ToArgb(), bitmap.GetPixel(3, 3).ToArgb());
        Assert.NotEqual(Color.Red.ToArgb(), bitmap.GetPixel(0, 0).ToArgb());
    }

    [Fact]
    public void ApplyMaskPixelatesSelectionAtOffset()
    {
        using var bitmap = new Bitmap(8, 8);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Black);
        }

        for (var row = 2; row < 6; row++)
        {
            for (var column = 2; column < 6; column++)
            {
                bitmap.SetPixel(column, row, Color.FromArgb((column - 2) * 40, (row - 2) * 40, 0));
            }
        }

        ImageEditingService.ApplyMask(bitmap, new Rectangle(2, 2, 4, 4), MaskMode.Pixelate, Color.Red, pixelSize: 4);

        Assert.Equal(Color.FromArgb(60, 60, 0).ToArgb(), bitmap.GetPixel(2, 2).ToArgb());
        Assert.Equal(bitmap.GetPixel(2, 2).ToArgb(), bitmap.GetPixel(5, 5).ToArgb());
        Assert.Equal(Color.Black.ToArgb(), bitmap.GetPixel(0, 0).ToArgb());
    }

    [Fact]
    public void ApplyMaskPixelatesPartialBlocksWithTheExactAverage()
    {
        using var bitmap = new Bitmap(11, 7);
        for (var row = 0; row < bitmap.Height; row++)
        {
            for (var column = 0; column < bitmap.Width; column++)
            {
                bitmap.SetPixel(column, row, Color.FromArgb(column * 20, row * 30, (column + row) * 10));
            }
        }

        var area = new Rectangle(2, 1, 7, 5);
        var blockSize = 4;
        var expectedColors = new int[2, 2];
        for (var blockY = 0; blockY < expectedColors.GetLength(0); blockY++)
        {
            for (var blockX = 0; blockX < expectedColors.GetLength(1); blockX++)
            {
                var blockWidth = Math.Min(blockSize, area.Width - (blockX * blockSize));
                var blockHeight = Math.Min(blockSize, area.Height - (blockY * blockSize));
                long red = 0;
                long green = 0;
                long blue = 0;
                for (var y = 0; y < blockHeight; y++)
                {
                    for (var x = 0; x < blockWidth; x++)
                    {
                        var color = bitmap.GetPixel(area.Left + (blockX * blockSize) + x, area.Top + (blockY * blockSize) + y);
                        red += color.R;
                        green += color.G;
                        blue += color.B;
                    }
                }

                expectedColors[blockY, blockX] = Color.FromArgb(
                    (int)(red / (blockWidth * blockHeight)),
                    (int)(green / (blockWidth * blockHeight)),
                    (int)(blue / (blockWidth * blockHeight))).ToArgb();
            }
        }

        ImageEditingService.ApplyMask(bitmap, area, MaskMode.Pixelate, Color.Red, blockSize);

        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var expected = area.Contains(x, y)
                    ? expectedColors[(y - area.Top) / blockSize, (x - area.Left) / blockSize]
                    : Color.FromArgb(x * 20, y * 30, (x + y) * 10).ToArgb();
                Assert.Equal(expected, bitmap.GetPixel(x, y).ToArgb());
            }
        }
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
    public void DrawHighlighterDoesNotPaintOutsideClipBounds()
    {
        using var bitmap = new Bitmap(40, 24);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);
            ImageEditingService.DrawHighlighter(
                graphics,
                [new Point(2, 12), new Point(37, 12)],
                Color.Yellow,
                width: 18,
                clipBounds: new Rectangle(14, 0, 12, 24));
        }

        Assert.NotEqual(Color.White.ToArgb(), bitmap.GetPixel(20, 12).ToArgb());
        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(13, 12).ToArgb());
        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(26, 12).ToArgb());
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

    [Fact]
    public void CreateTextBoundsUsesSharedEditorPlacementForReverseDrag()
    {
        using var font = new Font("Segoe UI", 12F);
        var bounds = ImageEditingService.CreateTextBounds(
            new Point(80, 70),
            new Point(30, 20),
            new Rectangle(0, 0, 200, 120),
            font,
            out var minimumWidth);

        Assert.Equal(new Rectangle(80, 20, 100, font.Height + 2), bounds);
        Assert.Equal(100, minimumWidth);
    }
}
