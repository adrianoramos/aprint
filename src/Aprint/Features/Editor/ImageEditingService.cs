using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Aprint.Features.Editor;

public static class ImageEditingService
{
    public readonly record struct CounterMarker(Point Center, int Value, int Diameter)
    {
        public Rectangle Bounds => GetCounterMarkerBounds(Center, Diameter);
    }

    public const int AnnotationPenWidth = 3;
    public const int TextMinimumWidth = 100;
    public const int CounterMarkerDiameter = 40;
    public const float CounterMarkerFontSize = 12F;
    private const int HighlighterAlpha = 105;
    private static readonly Dictionary<float, Font> CounterFonts = [];
    private static readonly object CounterFontSync = new();

    public static CounterMarker CreateCounterMarker(Point requestedCenter, int value, Rectangle availableArea)
    {
        var diameter = GetCounterMarkerDiameter(availableArea.Size);
        var center = ClampCounterMarkerCenter(requestedCenter, availableArea, diameter);
        return new CounterMarker(center, value, diameter);
    }

    public static int GetCounterMarkerDiameter(Size availableSize)
    {
        if (availableSize.Width <= 0 || availableSize.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(availableSize));
        }

        return Math.Min(CounterMarkerDiameter, Math.Min(availableSize.Width, availableSize.Height));
    }

    public static Point ClampCounterMarkerCenter(Point center, Rectangle availableArea, int diameter)
    {
        if (availableArea.Width <= 0 || availableArea.Height <= 0 || diameter <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(availableArea));
        }

        diameter = Math.Min(diameter, Math.Min(availableArea.Width, availableArea.Height));
        var halfDiameter = diameter / 2;
        var rightInset = diameter - halfDiameter;
        var minimumX = availableArea.Left + halfDiameter;
        var maximumX = availableArea.Right - rightInset;
        var minimumY = availableArea.Top + halfDiameter;
        var maximumY = availableArea.Bottom - rightInset;

        return new Point(
            Math.Clamp(center.X, minimumX, maximumX),
            Math.Clamp(center.Y, minimumY, maximumY));
    }

    public static Rectangle GetCounterMarkerBounds(Point center, int diameter) => new(
        center.X - (diameter / 2),
        center.Y - (diameter / 2),
        diameter,
        diameter);

    public static float GetCounterMarkerFontSize(int diameter) => Math.Clamp(
        CounterMarkerFontSize * diameter / CounterMarkerDiameter,
        6F,
        CounterMarkerFontSize);

    public static void DrawCounterMarker(Graphics graphics, CounterMarker marker, Color lineColor, Color? fillColor, Color? textColor = null)
    {
        var font = GetCounterMarkerFont(marker.Diameter);
        DrawCounterMarker(graphics, marker.Bounds, marker.Value.ToString(), font, lineColor, fillColor, textColor);
    }

    public static Font GetCounterMarkerFont(int diameter)
    {
        var fontSize = GetCounterMarkerFontSize(diameter);
        lock (CounterFontSync)
        {
            if (!CounterFonts.TryGetValue(fontSize, out var font))
            {
                font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Point);
                CounterFonts[fontSize] = font;
            }

            return font;
        }
    }

    public static void ClearCounterMarkerFontCache()
    {
        lock (CounterFontSync)
        {
            foreach (var font in CounterFonts.Values)
            {
                font.Dispose();
            }

            CounterFonts.Clear();
        }
    }

    public static void DrawCounterMarker(Graphics graphics, Rectangle bounds, string text, Font font, Color lineColor, Color? fillColor, Color? textColor = null)
    {
        if (fillColor is Color markerFillColor)
        {
            using var fillBrush = new SolidBrush(markerFillColor);
            graphics.FillEllipse(fillBrush, bounds);
        }

        using var borderPen = new Pen(lineColor, 2);
        graphics.DrawEllipse(borderPen, Rectangle.Inflate(bounds, -2, -2));

        var textBackgroundColor = fillColor ?? lineColor;
        var resolvedTextColor = textColor ?? GetContrastingTextColor(textBackgroundColor);
        using var textBrush = new SolidBrush(resolvedTextColor);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        graphics.DrawString(text, font, textBrush, bounds, format);
    }

    public static void DrawShape(Graphics graphics, Pen pen, Point start, Point end, EditTool tool, Color? fillColor = null)
    {
        var rectangle = CreateSelectionRectangle(start, end);

        switch (tool)
        {
            case EditTool.Line:
                graphics.DrawLine(pen, start, end);
                break;
            case EditTool.Arrow:
                using (var arrowPen = (Pen)pen.Clone())
                using (var arrowCap = new AdjustableArrowCap(5, 7, true))
                {
                    arrowPen.CustomEndCap = arrowCap;
                    graphics.DrawLine(arrowPen, start, end);
                }
                break;
            case EditTool.Rectangle:
                if (fillColor is Color rectangleColor)
                {
                    using var brush = new SolidBrush(rectangleColor);
                    graphics.FillRectangle(brush, rectangle);
                }
                graphics.DrawRectangle(pen, rectangle);
                break;
            case EditTool.Circle:
                if (fillColor is Color circleColor)
                {
                    using var brush = new SolidBrush(circleColor);
                    graphics.FillEllipse(brush, rectangle);
                }
                graphics.DrawEllipse(pen, rectangle);
                break;
        }
    }

    public static void DrawAnnotation(Graphics graphics, Point start, Point end, EditTool tool, Color lineColor, Color? fillColor = null)
    {
        var state = graphics.Save();
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = CreateAnnotationPen(lineColor, AnnotationPenWidth);
        DrawShape(graphics, pen, start, end, tool, fillColor);
        graphics.Restore(state);
    }

    public static void DrawFreehandSegment(Graphics graphics, Point start, Point end, Color color, int width = AnnotationPenWidth)
    {
        var state = graphics.Save();
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = CreateAnnotationPen(color, width);
        graphics.DrawLine(pen, start, end);
        graphics.Restore(state);
    }

    public static void DrawFreehandStroke(Graphics graphics, IReadOnlyList<Point> points, Color color, int width = AnnotationPenWidth)
    {
        if (points.Count < 2)
        {
            return;
        }

        var state = graphics.Save();
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = CreateAnnotationPen(color, width);
        graphics.DrawLines(pen, points.ToArray());
        graphics.Restore(state);
    }

    public static Rectangle CreateSelectionRectangle(Point start, Point end) => new(
        Math.Min(start.X, end.X),
        Math.Min(start.Y, end.Y),
        Math.Abs(start.X - end.X),
        Math.Abs(start.Y - end.Y));

    public static Bitmap Crop(Bitmap bitmap, Rectangle area)
    {
        if (area.Width <= 0 || area.Height <= 0 ||
            !new Rectangle(Point.Empty, bitmap.Size).Contains(area))
        {
            throw new ArgumentOutOfRangeException(nameof(area));
        }

        return bitmap.Clone(area, bitmap.PixelFormat);
    }

    public static void ApplyHighlighter(Bitmap bitmap, IReadOnlyList<Point> points, Color color, int width)
    {
        using var graphics = Graphics.FromImage(bitmap);
        DrawHighlighter(graphics, points, color, width, new Rectangle(Point.Empty, bitmap.Size));
    }

    public static void DrawHighlighter(Graphics graphics, IReadOnlyList<Point> points, Color color, int width, Rectangle clipBounds)
    {
        var state = graphics.Save();
        try
        {
            graphics.SetClip(clipBounds, CombineMode.Intersect);
            DrawHighlighter(graphics, points, color, width);
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    public static void DrawHighlighter(Graphics graphics, IReadOnlyList<Point> points, Color color, int width)
    {
        if (points.Count == 0)
        {
            return;
        }

        width = Math.Max(1, width);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Color.FromArgb(HighlighterAlpha, color), width)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };

        if (points.Count == 1)
        {
            using var brush = new SolidBrush(pen.Color);
            var point = points[0];
            graphics.FillEllipse(brush, point.X - width / 2, point.Y - width / 2, width, width);
            return;
        }

        graphics.DrawLines(pen, points.ToArray());
    }

    public static void ApplyText(Bitmap bitmap, Rectangle bounds, string text, Color color, Font font)
    {
        if (string.IsNullOrEmpty(text) || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        using var graphics = Graphics.FromImage(bitmap);
        DrawText(graphics, bounds, text, color, font);
    }

    public static Size MeasureTextContent(string text, Font font)
    {
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
        var width = lines.Max(line => TextRenderer.MeasureText(line, font, Size.Empty, flags).Width);

        return new Size(width, lines.Length * font.Height);
    }

    public static Rectangle CreateTextBounds(Point dragStart, Point dragEnd, Rectangle availableArea, Font font, out int minimumWidth)
    {
        if (availableArea.Width <= 0 || availableArea.Height <= 0)
        {
            minimumWidth = 0;
            return Rectangle.Empty;
        }

        var left = Math.Clamp(dragStart.X, availableArea.Left, availableArea.Right - 1);
        var top = Math.Clamp(dragEnd.Y, availableArea.Top, availableArea.Bottom - 1);
        minimumWidth = Math.Max(TextMinimumWidth, Math.Abs(dragStart.X - dragEnd.X));
        var width = Math.Min(minimumWidth, availableArea.Right - left);
        var height = Math.Min(font.Height + 2, availableArea.Bottom - top);
        return new Rectangle(left, top, Math.Max(1, width), Math.Max(1, height));
    }

    public static Rectangle ResizeTextBounds(Rectangle currentBounds, int minimumWidth, string text, Font font, Rectangle availableArea)
    {
        if (availableArea.Width <= 0 || availableArea.Height <= 0)
        {
            return Rectangle.Empty;
        }

        var availableWidth = Math.Max(1, availableArea.Right - currentBounds.Left);
        var availableHeight = Math.Max(1, availableArea.Bottom - currentBounds.Top);
        var contentSize = MeasureTextContent(text, font);
        var width = Math.Min(Math.Max(minimumWidth, contentSize.Width + 3), availableWidth);
        var height = Math.Min(Math.Max(font.Height, contentSize.Height) + 2, availableHeight);
        return new Rectangle(currentBounds.Left, currentBounds.Top, Math.Max(1, width), Math.Max(1, height));
    }

    public static void DrawText(Graphics graphics, Rectangle bounds, string text, Color color, Font font)
    {
        if (string.IsNullOrEmpty(text) || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var state = graphics.Save();
        var textBounds = Rectangle.Inflate(bounds, -1, -1);
        graphics.SetClip(textBounds);
        using var brush = new SolidBrush(color);
        using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
        format.FormatFlags |= StringFormatFlags.NoWrap;
        graphics.DrawString(text, font, brush, textBounds, format);
        graphics.Restore(state);
    }

    public static void ApplyMask(Bitmap bitmap, Rectangle area, MaskMode mode, Color color, int pixelSize)
    {
        var rectangle = Rectangle.Intersect(area, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
        if (rectangle.Width <= 0 || rectangle.Height <= 0)
        {
            return;
        }

        if (mode == MaskMode.Pixelate)
        {
            PixelateArea(bitmap, rectangle, pixelSize);
            return;
        }

        using var graphics = Graphics.FromImage(bitmap);
        using var solidBrush = new SolidBrush(color);
        graphics.FillRectangle(solidBrush, rectangle);
    }

    private static void PixelateArea(Bitmap bitmap, Rectangle rectangle, int blockSize)
    {
        blockSize = Math.Max(4, blockSize);
        var blocksWide = (rectangle.Width + blockSize - 1) / blockSize;
        var blocksHigh = (rectangle.Height + blockSize - 1) / blockSize;
        var blockColors = new int[blocksWide * blocksHigh];
        BitmapData? bitmapData = null;

        try
        {
            bitmapData = bitmap.LockBits(
                new Rectangle(Point.Empty, bitmap.Size),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            unsafe
            {
                var scan0 = (byte*)bitmapData.Scan0;

                for (var blockY = 0; blockY < blocksHigh; blockY++)
                {
                    var blockTop = blockY * blockSize;
                    var blockHeight = Math.Min(blockSize, rectangle.Height - blockTop);

                    for (var blockX = 0; blockX < blocksWide; blockX++)
                    {
                        var blockLeft = blockX * blockSize;
                        var blockWidth = Math.Min(blockSize, rectangle.Width - blockLeft);
                        long red = 0;
                        long green = 0;
                        long blue = 0;

                        for (var y = 0; y < blockHeight; y++)
                        {
                            var row = scan0 + ((rectangle.Top + blockTop + y) * bitmapData.Stride);
                            for (var x = 0; x < blockWidth; x++)
                            {
                                var pixel = row + ((rectangle.Left + blockLeft + x) * 4);
                                blue += pixel[0];
                                green += pixel[1];
                                red += pixel[2];
                            }
                        }

                        var pixelCount = blockWidth * blockHeight;
                        blockColors[(blockY * blocksWide) + blockX] = Color.FromArgb(
                            (int)(red / pixelCount),
                            (int)(green / pixelCount),
                            (int)(blue / pixelCount)).ToArgb();
                    }
                }
            }
        }
        finally
        {
            if (bitmapData is not null)
            {
                bitmap.UnlockBits(bitmapData);
            }
        }

        BitmapData? outputData = null;
        try
        {
            outputData = bitmap.LockBits(
                new Rectangle(Point.Empty, bitmap.Size),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);

            unsafe
            {
                var scan0 = (byte*)outputData.Scan0;

                for (var blockY = 0; blockY < blocksHigh; blockY++)
                {
                    var blockTop = blockY * blockSize;
                    var blockHeight = Math.Min(blockSize, rectangle.Height - blockTop);

                    for (var blockX = 0; blockX < blocksWide; blockX++)
                    {
                        var blockLeft = blockX * blockSize;
                        var blockWidth = Math.Min(blockSize, rectangle.Width - blockLeft);
                        var argb = (uint)blockColors[(blockY * blocksWide) + blockX];
                        var blue = (byte)argb;
                        var green = (byte)(argb >> 8);
                        var red = (byte)(argb >> 16);
                        var alpha = (byte)(argb >> 24);

                        for (var y = 0; y < blockHeight; y++)
                        {
                            var destination = scan0 + (((rectangle.Top + blockTop + y) * outputData.Stride) + ((rectangle.Left + blockLeft) * 4));
                            for (var x = 0; x < blockWidth; x++)
                            {
                                destination[0] = blue;
                                destination[1] = green;
                                destination[2] = red;
                                destination[3] = alpha;
                                destination += 4;
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            if (outputData is not null)
            {
                bitmap.UnlockBits(outputData);
            }
        }
    }

    private static Color GetContrastingTextColor(Color background)
    {
        var luminance = (0.299 * background.R) + (0.587 * background.G) + (0.114 * background.B);
        return luminance > 140 ? Color.Black : Color.White;
    }

    private static Pen CreateAnnotationPen(Color color, int width) => new(color, Math.Max(1, width))
    {
        StartCap = LineCap.Round,
        EndCap = LineCap.Round,
        LineJoin = LineJoin.Round,
    };
}
