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
}
