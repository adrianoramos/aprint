using System.Drawing.Imaging;
using Aprint.Features.Settings;

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

    /// <summary>Exibe o diálogo de gravação e salva a imagem no formato escolhido. Retorna null se cancelado.</summary>
    public static string? SaveToDiskWithPrompt(Bitmap bitmap, string initialFolder)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = AppLocalization.Translate("Editor.SaveFilter"),
            FileName = $"Aprint_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png",
            InitialDirectory = Directory.Exists(initialFolder) ? initialFolder : string.Empty,
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return null;
        }

        var format = Path.GetExtension(dialog.FileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => ImageFormat.Jpeg,
            ".bmp" => ImageFormat.Bmp,
            _ => ImageFormat.Png,
        };

        Directory.CreateDirectory(Path.GetDirectoryName(dialog.FileName)!);
        bitmap.Save(dialog.FileName, format);
        return dialog.FileName;
    }
}
