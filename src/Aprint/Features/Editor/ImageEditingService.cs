using System.Drawing.Drawing2D;

namespace Aprint.Features.Editor;

public static class ImageEditingService
{
    private const int HighlighterAlpha = 105;

    public static void DrawShape(Graphics graphics, Pen pen, Point start, Point end, EditTool tool)
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
                graphics.DrawRectangle(pen, rectangle);
                break;
            case EditTool.Circle:
                graphics.DrawEllipse(pen, rectangle);
                break;
        }
    }

    public static Rectangle CreateSelectionRectangle(Point start, Point end) => new(
        Math.Min(start.X, end.X),
        Math.Min(start.Y, end.Y),
        Math.Abs(start.X - end.X),
        Math.Abs(start.Y - end.Y));

    public static void ApplyHighlighter(Bitmap bitmap, IReadOnlyList<Point> points, Color color, int width)
    {
        using var graphics = Graphics.FromImage(bitmap);
        DrawHighlighter(graphics, points, color, width);
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

    public static void ApplyBorrar(Bitmap bitmap, Rectangle area, BorrarMode mode, Color color, int pixelSize)
    {
        var rectangle = Rectangle.Intersect(area, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
        if (rectangle.Width <= 0 || rectangle.Height <= 0)
        {
            return;
        }

        using var graphics = Graphics.FromImage(bitmap);
        if (mode == BorrarMode.Pixelate)
        {
            PixelateArea(bitmap, graphics, rectangle, pixelSize);
            return;
        }

        using var solidBrush = new SolidBrush(color);
        graphics.FillRectangle(solidBrush, rectangle);
    }

    private static void PixelateArea(Bitmap bitmap, Graphics graphics, Rectangle rectangle, int blockSize)
    {
        blockSize = Math.Max(4, blockSize);

        for (var y = rectangle.Top; y < rectangle.Bottom; y += blockSize)
        {
            for (var x = rectangle.Left; x < rectangle.Right; x += blockSize)
            {
                var blockWidth = Math.Min(blockSize, rectangle.Right - x);
                var blockHeight = Math.Min(blockSize, rectangle.Bottom - y);
                var averageColor = GetAverageColor(bitmap, new Rectangle(x, y, blockWidth, blockHeight));
                using var brush = new SolidBrush(averageColor);
                graphics.FillRectangle(brush, x, y, blockWidth, blockHeight);
            }
        }
    }

    private static Color GetAverageColor(Bitmap bitmap, Rectangle area)
    {
        long red = 0;
        long green = 0;
        long blue = 0;
        long count = 0;

        for (var y = area.Top; y < area.Bottom; y++)
        {
            for (var x = area.Left; x < area.Right; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                red += pixel.R;
                green += pixel.G;
                blue += pixel.B;
                count++;
            }
        }

        return count == 0
            ? Color.Black
            : Color.FromArgb((int)(red / count), (int)(green / count), (int)(blue / count));
    }
}
