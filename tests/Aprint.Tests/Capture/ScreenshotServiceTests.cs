using Aprint.Features.Capture;

namespace Aprint.Tests.Capture;

public sealed class ScreenshotServiceTests
{
    [Fact]
    public async Task SaveToDiskAsyncWritesReadablePng()
    {
        var saveFolder = Path.Combine(Path.GetTempPath(), "Aprint.Tests", Guid.NewGuid().ToString("N"));
        try
        {
            using var bitmap = new Bitmap(8, 6);
            bitmap.SetPixel(2, 3, Color.Red);

            var filePath = await ScreenshotService.SaveToDiskAsync(bitmap, saveFolder);

            Assert.True(File.Exists(filePath));
            using var savedImage = Image.FromFile(filePath);
            Assert.Equal(bitmap.Size, savedImage.Size);
            Assert.Equal(Color.Red.ToArgb(), ((Bitmap)savedImage).GetPixel(2, 3).ToArgb());
        }
        finally
        {
            if (Directory.Exists(saveFolder))
            {
                Directory.Delete(saveFolder, recursive: true);
            }
        }
    }
}