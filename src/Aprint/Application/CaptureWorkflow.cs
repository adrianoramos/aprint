using Aprint.Features.Capture;
using Aprint.Features.Editor;
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
        RegionSelectionAction? regionAction = null;
        var transferFullBitmap = false;
        var transferSelectedRegionBitmap = false;

        try
        {
            selectedRegionBitmap = GetSelectedRegionOrNull(fullBitmap, selectedMode, settings.OutsideCaptureClickAction, settings.MaskMode, settings.LastMaskPixelSize, settings.LastLineColorArgb, settings.LastHighlightWidth, settings.LastFillColorArgb, settings.LastTextFontFamily, settings.LastTextFontSize, settings.LastTextFontStyle, settings.LastTextColorArgb, settings.ShowCaptureToolLabels, settings.LastCaptureTool, out var lastCaptureTool, out var wasCancelled, out regionAction);
            settings.LastCaptureTool = lastCaptureTool;
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
            var openEditor = regionAction switch
            {
                RegionSelectionAction.Capture => true,
                RegionSelectionAction.Save or RegionSelectionAction.Copy => false,
                _ => settings.OpenEditorAfterCapture,
            };

            if (settings.PlaySoundOnCapture)
            {
                NotificationSound.Play();
            }

            var copyToClipboard = regionAction == RegionSelectionAction.Copy ||
                (regionAction is null && settings.CaptureAction is CaptureAction.ClipboardOnly or CaptureAction.Both);
            if (copyToClipboard)
            {
                Clipboard.SetImage(bitmap);
            }

            string? filePath = null;
            var saveToDisk = regionAction == RegionSelectionAction.Save ||
                (regionAction is null && !openEditor && settings.CaptureAction is CaptureAction.SaveToDiskOnly or CaptureAction.Both);
            if (saveToDisk)
            {
                if (regionAction == RegionSelectionAction.Save && !settings.SaveEditorImageWithoutPrompt)
                {
                    filePath = ScreenshotService.SaveToDiskWithPrompt(bitmap, settings.SaveFolder);
                    if (filePath is null)
                    {
                        return null;
                    }
                }
                else
                {
                    filePath = ScreenshotService.SaveToDisk(bitmap, settings.SaveFolder);
                }
            }

            var result = new CaptureResult(bitmap, filePath, openEditor, regionAction);
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

    private static Bitmap? GetSelectedRegionOrNull(Bitmap fullBitmap, CaptureMode captureMode, OutsideCaptureClickAction outsideCaptureClickAction, MaskMode maskMode, int maskPixelSize, int lineColorArgb, int highlightWidth, int? fillColorArgb, string textFontFamily, float textFontSize, FontStyle textFontStyle, int textColorArgb, bool showCaptureToolLabels, EditTool lastCaptureTool, out EditTool selectedCaptureTool, out bool wasCancelled, out RegionSelectionAction? selectedAction)
    {
        wasCancelled = false;
        selectedCaptureTool = lastCaptureTool;
        selectedAction = null;
        if (captureMode != CaptureMode.SelectArea)
        {
            return null;
        }

        using var selector = new RegionSelectorForm(fullBitmap, outsideCaptureClickAction, maskMode, maskPixelSize, lineColorArgb, highlightWidth, fillColorArgb, textFontFamily, textFontSize, textFontStyle, textColorArgb, showCaptureToolLabels, lastCaptureTool);
        var dialogResult = selector.ShowDialog();
        selectedCaptureTool = selector.LastUsedTool;
        if (dialogResult != DialogResult.OK || selector.SelectedRegion is not { } region || selector.SelectedAction is not { } action)
        {
            wasCancelled = true;
            return null;
        }

        selectedAction = action;

        var bitmapBounds = new Rectangle(Point.Empty, fullBitmap.Size);
        var boundedRegion = Rectangle.Intersect(region, bitmapBounds);
        if (boundedRegion.Width <= 0 || boundedRegion.Height <= 0)
        {
            wasCancelled = true;
            return null;
        }

        return fullBitmap.Clone(boundedRegion, fullBitmap.PixelFormat);
    }
}

public sealed class CaptureResult(Bitmap bitmap, string? savedFilePath, bool openEditor, RegionSelectionAction? selectionAction) : IDisposable
{
    public Bitmap Bitmap { get; } = bitmap;

    public string? SavedFilePath { get; } = savedFilePath;

    public bool OpenEditor { get; } = openEditor;

    public RegionSelectionAction? SelectionAction { get; } = selectionAction;

    public void Dispose() => Bitmap.Dispose();
}
