using Aprint.Features.Capture;
using Aprint.Features.Settings;
using Aprint.Infrastructure.Windows;

namespace Aprint.Application;

/// <summary>Executa o fluxo de captura e entrega o resultado pronto para apresentação.</summary>
public sealed class CaptureWorkflow
{
    public CaptureResult? Execute(AppSettings settings, CaptureMode? captureModeOverride = null)
    {
        var selectedMode = captureModeOverride ?? settings.CaptureMode;
        var captureMode = selectedMode == CaptureMode.SelectArea
            ? CaptureMode.AllScreens
            : selectedMode;
        Bitmap? fullBitmap = ScreenshotService.CaptureScreen(captureMode);
        Bitmap? selectedRegionBitmap = null;
        var transferFullBitmap = false;
        var transferSelectedRegionBitmap = false;

        try
        {
            selectedRegionBitmap = GetSelectedRegionOrNull(fullBitmap, selectedMode, out var wasCancelled);
            if (wasCancelled)
            {
                return null;
            }

            if (selectedRegionBitmap is not null)
            {
                fullBitmap.Dispose();
                fullBitmap = null;
            }

            var bitmap = selectedRegionBitmap ?? fullBitmap!;

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

            var result = new CaptureResult(bitmap, filePath);
            transferSelectedRegionBitmap = selectedRegionBitmap is not null;
            transferFullBitmap = selectedRegionBitmap is null;
            return result;
        }
        finally
        {
            if (!transferSelectedRegionBitmap)
            {
                selectedRegionBitmap?.Dispose();
            }

            if (!transferFullBitmap)
            {
                fullBitmap?.Dispose();
            }
        }
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
