using System.Drawing.Imaging;

namespace Aprint.Features.Capture;

public enum ScreenshotFileFormat
{
    Png,
    Jpeg,
    Bmp,
}

public static class ScreenshotFileFormats
{
    public static ScreenshotFileFormat Normalize(ScreenshotFileFormat format) =>
        Enum.IsDefined(format) ? format : ScreenshotFileFormat.Png;

    public static string GetExtension(ScreenshotFileFormat format) => Normalize(format) switch
    {
        ScreenshotFileFormat.Jpeg => ".jpg",
        ScreenshotFileFormat.Bmp => ".bmp",
        _ => ".png",
    };

    public static ImageFormat GetImageFormat(ScreenshotFileFormat format) => Normalize(format) switch
    {
        ScreenshotFileFormat.Jpeg => ImageFormat.Jpeg,
        ScreenshotFileFormat.Bmp => ImageFormat.Bmp,
        _ => ImageFormat.Png,
    };

    public static ImageFormat GetImageFormat(string filePath, ScreenshotFileFormat fallback) =>
        GetImageFormat(Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => ScreenshotFileFormat.Jpeg,
            ".bmp" => ScreenshotFileFormat.Bmp,
            ".png" => ScreenshotFileFormat.Png,
            _ => Normalize(fallback),
        });

    public static void ConfigureSaveFileDialog(
        SaveFileDialog dialog,
        string filter,
        string initialFolder,
        ScreenshotFileFormat defaultFormat)
    {
        defaultFormat = Normalize(defaultFormat);
        var extension = GetExtension(defaultFormat);
        dialog.Filter = filter;
        dialog.FilterIndex = (int)defaultFormat + 1;
        dialog.FileName = $"Aprint_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{extension}";
        dialog.DefaultExt = extension[1..];
        dialog.AddExtension = true;
        dialog.InitialDirectory = Directory.Exists(initialFolder) ? initialFolder : string.Empty;
    }
}