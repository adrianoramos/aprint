using System.Drawing.Imaging;
using System.Text;
using Aprint.Infrastructure.Images;

namespace Aprint.Tests.Capture;

public sealed class ImageMetadataWriterTests
{
    [Fact]
    public void PngContainsSoftwareTextMetadata()
    {
        using var bitmap = new Bitmap(2, 2);
        using var output = new MemoryStream();

        ImageMetadataWriter.SaveWithSoftwareMetadata(bitmap, output, ImageFormat.Png);

        var bytes = output.ToArray();
        Assert.True(bytes.AsSpan().IndexOf("eXIf"u8) >= 0);
        Assert.True(bytes.AsSpan().IndexOf(new byte[] { 0x31, 0x01, 0x02 }) >= 0);
        Assert.True(bytes.AsSpan().IndexOf(Encoding.ASCII.GetBytes("Software\0Aprint")) >= 0);
        using var encodedImage = new MemoryStream(bytes);
        using var decodedImage = Image.FromStream(encodedImage);
        Assert.Equal(bitmap.Size, decodedImage.Size);
    }

    [Fact]
    public void JpegContainsExifSoftwareMetadata()
    {
        using var bitmap = new Bitmap(2, 2);
        using var output = new MemoryStream();

        ImageMetadataWriter.SaveWithSoftwareMetadata(bitmap, output, ImageFormat.Jpeg);

        var bytes = output.ToArray();
        Assert.True(bytes.AsSpan().IndexOf(Encoding.ASCII.GetBytes("Exif\0\0")) >= 0);
        Assert.True(bytes.AsSpan().IndexOf(new byte[] { 0x31, 0x01, 0x02 }) >= 0);
        Assert.True(bytes.AsSpan().IndexOf(Encoding.ASCII.GetBytes("Aprint\0")) >= 0);
        using var encodedImage = new MemoryStream(bytes);
        using var decodedImage = Image.FromStream(encodedImage);
        Assert.Equal(bitmap.Size, decodedImage.Size);
    }
}