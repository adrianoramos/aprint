using System.Drawing.Imaging;

namespace Aprint.Features.Capture;

/// <summary>Responsável por capturar a tela (todos os monitores ou apenas o monitor do cursor).</summary>
public static class ScreenshotService
{
    /// <summary>Captura a tela e retorna o bitmap resultante. O chamador é responsável por descartá-lo.</summary>
    public static Bitmap CaptureScreen(CaptureMode mode)
    {
        var bounds = mode == CaptureMode.ScreenWithCursor
            ? Screen.FromPoint(Cursor.Position).Bounds
            : SystemInformation.VirtualScreen;

        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        }

        return bitmap;
    }

    /// <summary>Salva o bitmap como PNG na pasta informada. Retorna o caminho do arquivo gerado.</summary>
    public static string SaveToDisk(Bitmap bitmap, string saveFolder)
    {
        Directory.CreateDirectory(saveFolder);

        var fileName = $"Aprint_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        var filePath = Path.Combine(saveFolder, fileName);
        bitmap.Save(filePath, ImageFormat.Png);

        return filePath;
    }
}
