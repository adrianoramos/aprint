using System.Drawing.Drawing2D;

namespace Aprint.Features.Editor;

public static class ImageEditingService
{
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
