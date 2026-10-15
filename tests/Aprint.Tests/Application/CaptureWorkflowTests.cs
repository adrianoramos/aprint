using System.Reflection;
using Aprint.Application;
using Aprint.Features.Capture;
using Aprint.Features.Editor;
using Aprint.Features.Settings;

namespace Aprint.Tests.Application;

public sealed class CaptureWorkflowTests
{
    [Theory]
    [InlineData(CaptureAction.ClipboardOnly, false, false, true, false)]
    [InlineData(CaptureAction.SaveToDiskOnly, false, false, false, true)]
    [InlineData(CaptureAction.Both, false, false, true, true)]
    [InlineData(CaptureAction.SaveToDiskOnly, true, true, false, false)]
    [InlineData(CaptureAction.Both, true, true, true, false)]
    public void FloatingCaptureFollowsConfiguredPostCaptureFlow(
        CaptureAction captureAction,
        bool openEditorAfterCapture,
        bool expectedOpenEditor,
        bool expectedCopyToClipboard,
        bool expectedSaveToDisk)
    {
        var settings = new AppSettings
        {
            CaptureAction = captureAction,
            OpenEditorAfterCapture = openEditorAfterCapture,
        };

        var actions = ResolveActions(settings, RegionSelectionAction.Capture);

        Assert.Equal(expectedOpenEditor, actions.OpenEditor);
        Assert.Equal(expectedCopyToClipboard, actions.CopyToClipboard);
        Assert.Equal(expectedSaveToDisk, actions.SaveToDisk);
    }

    [Fact]
    public void ExplicitCopyAndSaveActionsOverrideConfiguredPostCaptureFlow()
    {
        var settings = new AppSettings
        {
            CaptureAction = CaptureAction.Both,
            OpenEditorAfterCapture = true,
        };

        Assert.Equal((false, true, false), ResolveActions(settings, RegionSelectionAction.Copy));
        Assert.Equal((false, false, true), ResolveActions(settings, RegionSelectionAction.Save));
    }

    [Fact]
    public void CaptureResultTransfersBitmapOwnership()
    {
        var bitmap = new Bitmap(2, 2);
        bitmap.SetPixel(0, 0, Color.Red);
        using var result = new CaptureResult(bitmap, null, openEditor: true, selectionAction: null);
        using var transferredBitmap = result.TakeBitmap();

        Assert.Same(bitmap, transferredBitmap);
        Assert.Equal(Color.Red.ToArgb(), transferredBitmap.GetPixel(0, 0).ToArgb());
    }

    [Fact]
    public void FloatingToolbarChangesUpdateSharedEditorSettings()
    {
        using var bitmap = new Bitmap(20, 20);
        using var selector = new RegionSelectorForm(bitmap);
        var settings = new AppSettings();
        var saveCount = 0;
        var bindSettings = typeof(CaptureWorkflow).GetMethod("BindSelectorSettings", BindingFlags.Static | BindingFlags.NonPublic)!;
        bindSettings.Invoke(null, [selector, settings, (Action)(() => saveCount++)]);

        void InvokeSelector(string methodName, params object?[] arguments) =>
            typeof(RegionSelectorForm).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(selector, arguments);

        typeof(RegionSelectorForm).GetField("_textFontFamily", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(selector, "Arial");
        InvokeSelector("SetLineColor", Color.Lime);
        InvokeSelector("SetLineWidth", 12);
        InvokeSelector("SetFillColor", Color.Blue);
        InvokeSelector("SetHighlightColor", Color.Yellow);
        InvokeSelector("SetHighlightWidth", 32);
        InvokeSelector("SetMaskColor", Color.Cyan);
        InvokeSelector("SetMaskPixelSize", 24);
        InvokeSelector("SetCounterSize", 64);
        InvokeSelector("SetCounterTextColor", Color.Magenta);
        InvokeSelector("SetCounterCircleColor", Color.Green);
        InvokeSelector("SetTextColor", Color.Purple);
        InvokeSelector("SetTextFontSize", 18F);
        InvokeSelector("SetTextFontStyle", FontStyle.Bold, true);

        var maskDropdown = (ToolStripDropDownButton)typeof(RegionSelectorForm)
            .GetField("_maskDropdownButton", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!;
        var solidModeItem = Assert.Single(
            maskDropdown.DropDownItems.OfType<ToolStripMenuItem>(),
            item => item.Tag is MaskMode mode && mode == MaskMode.Solid);
        InvokeSelector("SelectMaskMode", MaskMode.Solid, solidModeItem, solidModeItem.Image!);

        Assert.Equal(Color.Lime.ToArgb(), settings.LastLineColorArgb);
        Assert.Equal(12, settings.LastLineWidth);
        Assert.Equal(Color.Blue.ToArgb(), settings.LastFillColorArgb);
        Assert.Equal(Color.Yellow.ToArgb(), settings.LastHighlightColorArgb);
        Assert.Equal(32, settings.LastHighlightWidth);
        Assert.Equal(Color.Cyan.ToArgb(), settings.LastMaskColorArgb);
        Assert.Equal(24, settings.LastMaskPixelSize);
        Assert.Equal(MaskMode.Solid, settings.MaskMode);
        Assert.Equal(64, settings.LastCounterSize);
        Assert.Equal(Color.Magenta.ToArgb(), settings.LastCounterTextColorArgb);
        Assert.Equal(Color.Green.ToArgb(), settings.LastCounterCircleColorArgb);
        Assert.Equal(Color.Purple.ToArgb(), settings.LastTextColorArgb);
        Assert.Equal("Arial", settings.LastTextFontFamily);
        Assert.Equal(18F, settings.LastTextFontSize);
        Assert.Equal(FontStyle.Bold, settings.LastTextFontStyle);
        Assert.Equal(14, saveCount);
    }

    private static (bool OpenEditor, bool CopyToClipboard, bool SaveToDisk) ResolveActions(
        AppSettings settings,
        RegionSelectionAction? selectionAction)
    {
        var method = typeof(CaptureWorkflow).GetMethod("ResolveActions", BindingFlags.Static | BindingFlags.NonPublic)!;
        return ((bool OpenEditor, bool CopyToClipboard, bool SaveToDisk))method.Invoke(null, [settings, selectionAction])!;
    }
}