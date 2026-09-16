using Aprint.Features.Capture;
using Aprint.Features.Settings;
using Aprint.Infrastructure.Windows;

namespace Aprint.Application;

/// <summary>Executa o fluxo de captura e entrega o resultado pronto para apresentação.</summary>
public sealed class CaptureWorkflow
{
    public CaptureResult? Execute(AppSettings settings)
    {
        var captureMode = settings.CaptureMode == CaptureMode.SelectArea
            ? CaptureMode.AllScreens
            : settings.CaptureMode;
        using var fullBitmap = ScreenshotService.CaptureScreen(captureMode);

        Bitmap bitmap = fullBitmap;
        using var selectedRegionBitmap = GetSelectedRegionOrNull(fullBitmap, settings.CaptureMode, out var wasCancelled);
        if (wasCancelled)
        {
            return null;
        }

        if (selectedRegionBitmap is not null)
        {
            bitmap = selectedRegionBitmap;
        }

        if (settings.PlaySoundOnCapture)
        {
            NotificationSound.Play();
        }

        if (settings.CaptureAction is CaptureAction.ClipboardOnly or CaptureAction.Both)
        {
            Clipboard.SetImage(bitmap);
        }

        string? filePath = null;
        if (!settings.OpenEditorAfterCapture &&
            settings.CaptureAction is CaptureAction.SaveToDiskOnly or CaptureAction.Both)
        {
            filePath = ScreenshotService.SaveToDisk(bitmap, settings.SaveFolder);
        }

        return new CaptureResult((Bitmap)bitmap.Clone(), filePath);
    }

    private static Bitmap? GetSelectedRegionOrNull(Bitmap fullBitmap, CaptureMode captureMode, out bool wasCancelled)
    {
        wasCancelled = false;
        if (captureMode != CaptureMode.SelectArea)
        {
            return null;
        }

        using var selector = new RegionSelectorForm(fullBitmap);
        if (selector.ShowDialog() != DialogResult.OK || selector.SelectedRegion is not { } region)
        {
            wasCancelled = true;
            return null;
        }

        return fullBitmap.Clone(region, fullBitmap.PixelFormat);
    }
}

public sealed class CaptureResult(Bitmap bitmap, string? savedFilePath) : IDisposable
{
    public Bitmap Bitmap { get; } = bitmap;

    public string? SavedFilePath { get; } = savedFilePath;

    public void Dispose() => Bitmap.Dispose();
}
