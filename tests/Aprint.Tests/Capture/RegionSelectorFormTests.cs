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
    public void MaskButtonLabelRemainsStableWhenMaskModeChanges()
    {
        using var bitmap = new Bitmap(4, 4);
        using var selector = new RegionSelectorForm(bitmap, showToolLabels: true);
        var maskButton = (ToolStripButton)typeof(RegionSelectorForm)
            .GetField("_maskButton", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!;
        var maskDropdownButton = (ToolStripDropDownButton)typeof(RegionSelectorForm)
            .GetField("_maskDropdownButton", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!;
        var expectedLabel = AppLocalization.Translate("Editor.Mask");

        foreach (var menuItem in maskDropdownButton.DropDownItems.OfType<ToolStripMenuItem>())
        {
            menuItem.PerformClick();
            Assert.Equal(expectedLabel, maskButton.Text);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DropdownButtonsKeepCompactWidthWhenLabelsAreEnabled(bool showLabels)
    {
        using var bitmap = new Bitmap(4, 4);
        using var selector = new RegionSelectorForm(bitmap, showToolLabels: showLabels);
        var actionBar = (ToolStrip)typeof(RegionSelectorForm)
            .GetField("_actionBar", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!;
        var dropdownButtons = actionBar.Items.OfType<ToolStripDropDownButton>().ToArray();

        Assert.Equal(3, dropdownButtons.Length);
        Assert.All(dropdownButtons, button => Assert.Equal(14, button.Width));
        Assert.All(dropdownButtons, button => Assert.Equal(34, button.Height));
        var splitButtons = dropdownButtons
            .Select(button => (ToolStripButton)actionBar.Items[actionBar.Items.IndexOf(button) - 1]);
        Assert.All(splitButtons, button => Assert.Equal(34, button.Height));
        Assert.All(splitButtons, button => Assert.False(button.AutoSize));
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

    [Fact]
    public void MovingSelectionHidesActionBarUntilDragEnds()
    {
        using var background = new Bitmap(100, 100);
        using var selector = new RegionSelectorForm(background);
        selector.Show();
        var annotationToolField = typeof(RegionSelectorForm)
            .GetField("_annotationTool", BindingFlags.Instance | BindingFlags.NonPublic)!;
        annotationToolField.SetValue(selector, Enum.Parse(annotationToolField.FieldType, "None"));
        typeof(RegionSelectorForm)
            .GetField("_selection", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(selector, new Rectangle(20, 20, 40, 40));
        var actionBar = (ToolStrip)typeof(RegionSelectorForm)
            .GetField("_actionBar", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!;
        actionBar.Visible = true;
        var mouseDown = typeof(RegionSelectorForm).GetMethod("OnMouseDown", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var mouseMove = typeof(RegionSelectorForm).GetMethod("OnMouseMove", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var mouseUp = typeof(RegionSelectorForm).GetMethod("OnMouseUp", BindingFlags.Instance | BindingFlags.NonPublic)!;

        mouseDown.Invoke(selector, [new MouseEventArgs(MouseButtons.Left, 1, 40, 40, 0)]);

        Assert.True((bool)typeof(RegionSelectorForm)
            .GetField("_isMoving", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!);
        Assert.False(actionBar.Visible);

        mouseMove.Invoke(selector, [new MouseEventArgs(MouseButtons.Left, 1, 50, 50, 0)]);
        mouseUp.Invoke(selector, [new MouseEventArgs(MouseButtons.Left, 1, 50, 50, 0)]);

        Assert.True(actionBar.Visible);
    }
}