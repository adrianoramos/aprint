using System.Reflection;
using Aprint.Features.Capture;
using Aprint.Features.Editor;
using Aprint.Features.Settings;

namespace Aprint.Tests.Capture;

public sealed class RegionSelectorFormTests
{
    [Fact]
    public void DrawingButtonSelectsFreehandWhenLastToolWasNotADrawingTool()
    {
        using var bitmap = new Bitmap(4, 4);
        using var selector = new RegionSelectorForm(bitmap, lastCaptureTool: EditTool.Counter);
        var drawingButton = (ToolStripButton)typeof(RegionSelectorForm)
            .GetField("_drawingButton", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!;

        drawingButton.PerformClick();

        Assert.Equal(EditTool.Freehand, selector.LastUsedTool);
    }

    [Theory]
    [InlineData(EditTool.Blur, MaskMode.Solid)]
    [InlineData(EditTool.Pixelated, MaskMode.Pixelate)]
    public void LastMaskToolIsRestoredAsActive(EditTool lastCaptureTool, MaskMode expectedMode)
    {
        using var bitmap = new Bitmap(4, 4);
        using var selector = new RegionSelectorForm(bitmap, lastCaptureTool: lastCaptureTool);
        var isMaskModeActive = (bool)typeof(RegionSelectorForm)
            .GetField("_isMaskModeActive", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!;
        var maskDropdownButton = (ToolStripDropDownButton)typeof(RegionSelectorForm)
            .GetField("_maskDropdownButton", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!;
        var checkedMenuItem = Assert.Single(
            maskDropdownButton.DropDownItems.OfType<ToolStripMenuItem>(),
            item => item.Checked);

        Assert.True(isMaskModeActive);
        Assert.Equal(expectedMode, (MaskMode)checkedMenuItem.Tag!);
    }

    [Fact]
    public void SendingSelectionToEditorKeepsSelectorOpenForRepeatedCaptures()
    {
        using var background = new Bitmap(8, 8);
        using var selector = new RegionSelectorForm(background);
        var selection = new Rectangle(1, 1, 4, 4);
        typeof(RegionSelectorForm)
            .GetField("_selection", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(selector, selection);
        var sentImages = 0;
        selector.SendToEditorRequested += bitmap =>
        {
            sentImages++;
            Assert.Equal(new Size(4, 4), bitmap.Size);
        };
        var sendSelection = typeof(RegionSelectorForm)
            .GetMethod("SendSelectionToEditor", BindingFlags.Instance | BindingFlags.NonPublic)!;

        sendSelection.Invoke(selector, null);
        sendSelection.Invoke(selector, null);

        Assert.Equal(2, sentImages);
        Assert.Equal(selection, (Rectangle)typeof(RegionSelectorForm)
            .GetField("_selection", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!);
        Assert.True((bool)typeof(RegionSelectorForm)
            .GetField("_sendToEditorFlashVisible", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!);
        Assert.False(selector.IsDisposed);
        Assert.Equal(DialogResult.None, selector.DialogResult);
    }
}