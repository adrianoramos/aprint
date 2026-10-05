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
}