using System.Drawing.Imaging;
using Aprint.Infrastructure.Images;
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

    /// <summary>Salva o bitmap no formato informado na pasta escolhida e retorna o caminho do arquivo gerado.</summary>
    public static string SaveToDisk(
        Bitmap bitmap,
        string saveFolder,
        ScreenshotFileFormat format = ScreenshotFileFormat.Png)
    {
        format = ScreenshotFileFormats.Normalize(format);
        var fileName = $"Aprint_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{ScreenshotFileFormats.GetExtension(format)}";
        var filePath = Path.Combine(saveFolder, fileName);
        SaveToFile(bitmap, filePath, ScreenshotFileFormats.GetImageFormat(format));
        return filePath;
    }

    /// <summary>Salva a imagem em uma thread de trabalho. Mantenha o bitmap intacto até a conclusão.</summary>
    public static Task<string> SaveToDiskAsync(
        Bitmap bitmap,
        string saveFolder,
        ScreenshotFileFormat format = ScreenshotFileFormat.Png) =>
        Task.Run(() => SaveToDisk(bitmap, saveFolder, format));

    /// <summary>Exibe o diálogo de gravação e salva a imagem no formato escolhido. Retorna null se cancelado.</summary>
    public static string? SaveToDiskWithPrompt(
        Bitmap bitmap,
        string initialFolder,
        ScreenshotFileFormat defaultFormat = ScreenshotFileFormat.Png)
    {
        using var dialog = new SaveFileDialog();
        ScreenshotFileFormats.ConfigureSaveFileDialog(
            dialog,
            AppLocalization.Translate("Editor.SaveFilter"),
            initialFolder,
            defaultFormat);

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return null;
        }

        var filePath = dialog.FileName;
        SaveToFile(bitmap, filePath, ScreenshotFileFormats.GetImageFormat(filePath, defaultFormat));
        return dialog.FileName;
    }

    /// <summary>Shows the save dialog on the caller thread, then encodes and writes the image on a worker thread.</summary>
    public static async Task<string?> SaveToDiskWithPromptAsync(
        Bitmap bitmap,
        string initialFolder,
        ScreenshotFileFormat defaultFormat = ScreenshotFileFormat.Png)
    {
        using var dialog = new SaveFileDialog();
        ScreenshotFileFormats.ConfigureSaveFileDialog(
            dialog,
            AppLocalization.Translate("Editor.SaveFilter"),
            initialFolder,
            defaultFormat);

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return null;
        }

        var filePath = dialog.FileName;
        var format = ScreenshotFileFormats.GetImageFormat(filePath, defaultFormat);
        await Task.Run(() => SaveToFile(bitmap, filePath, format));
        return filePath;
    }

    private static void SaveToFile(Bitmap bitmap, string filePath, ImageFormat format)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        using var output = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        ImageMetadataWriter.SaveWithSoftwareMetadata(bitmap, output, format);
    }
}
