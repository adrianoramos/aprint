using System.Reflection;
using Aprint.Features.Capture;
using Aprint.Features.Editor;

namespace Aprint.Tests.Editor;

public sealed class EditorFormTests
{
    [Fact]
    public void InitialToolDefaultsToFreehandWhenNotProvided()
    {
        using var bitmap = new Bitmap(2, 2);
        using var editor = new EditorForm(bitmap, string.Empty);

        Assert.Equal(EditTool.Freehand, editor.SelectedTool);
    }

    [Theory]
    [InlineData((EditTool)int.MaxValue, EditTool.Freehand)]
    [InlineData(EditTool.Line, EditTool.Line)]
    public void InitialToolFallsBackToFreehandOnlyWhenUndefined(EditTool initialTool, EditTool expectedTool)
    {
        using var bitmap = new Bitmap(2, 2);
        using var editor = new EditorForm(bitmap, string.Empty, initialTool: initialTool);

        Assert.Equal(expectedTool, editor.SelectedTool);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EditorToolbarButtonsMatchCaptureDropdownHeight(bool showLabels)
    {
        using var editorBitmap = new Bitmap(2, 2);
        using var captureBitmap = new Bitmap(2, 2);
        using var editor = new EditorForm(editorBitmap, string.Empty, showToolLabels: showLabels);
        using var capture = new RegionSelectorForm(captureBitmap);
        var editorDropdowns = typeof(EditorForm)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(field => typeof(ToolStripDropDownButton).IsAssignableFrom(field.FieldType))
            .Select(field => (ToolStripDropDownButton)field.GetValue(editor)!)
            .ToArray();
        var captureActionBar = (ToolStrip)typeof(RegionSelectorForm)
            .GetField("_actionBar", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(capture)!;
        var captureDropdowns = captureActionBar.Items.OfType<ToolStripDropDownButton>().ToArray();

        Assert.Equal(4, editorDropdowns.Length);
        Assert.Equal(3, captureDropdowns.Length);
        Assert.All(editorDropdowns, button => Assert.Equal(14, button.Width));
        Assert.All(editorDropdowns, button => Assert.Equal(captureDropdowns[0].Width, button.Width));
        Assert.All(captureDropdowns, button => Assert.Equal(34, button.Height));
        Assert.All(editorDropdowns, button => Assert.Equal(captureDropdowns[0].Height, button.Height));
        var editorToolStrip = editor.Controls.OfType<ToolStrip>().Single();
        Assert.All(
            editorToolStrip.Items.OfType<ToolStripButton>(),
            button => Assert.Equal(captureDropdowns[0].Height, button.Height));
        if (showLabels)
        {
            Assert.All(
                editorToolStrip.Items.OfType<ToolStripButton>()
                    .Where(button => button.DisplayStyle == ToolStripItemDisplayStyle.ImageAndText),
                button => Assert.True(
                    button.Width >= TextRenderer.MeasureText(button.Text, editorToolStrip.Font).Width,
                    $"The '{button.Text}' label must fit its button."));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShowWithoutActivationIsEnabledOnlyWhenRequested(bool showWithoutActivation)
    {
        using var bitmap = new Bitmap(2, 2);
        using var editor = new EditorForm(bitmap, string.Empty, showWithoutActivation: showWithoutActivation);
        var property = typeof(EditorForm).GetProperty("ShowWithoutActivation", BindingFlags.Instance | BindingFlags.NonPublic)!;

        Assert.Equal(showWithoutActivation, property.GetValue(editor));
    }

    [Fact]
    public void ApplyingTextMasksBakesPixelsAndEndsSelectionMode()
    {
        using var bitmap = new Bitmap(4, 4);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);
        }

        using var editor = new EditorForm(bitmap, string.Empty, maskMode: MaskMode.Solid, lineColorArgb: Color.Black.ToArgb());
        SetMaskRegions(editor, bitmap.Size, new Rectangle(1, 1, 2, 2));
        typeof(EditorForm).GetMethod("SetAutomaticMaskSelectionActive", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, [true]);
        var pictureBox = (PictureBox)typeof(EditorForm).GetField("_pictureBox", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        var tooltip = (ToolTip)typeof(EditorForm).GetField("_automaticMaskToolTip", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        Assert.NotEmpty(tooltip.GetToolTip(pictureBox) ?? string.Empty);

        typeof(EditorForm).GetMethod("ApplyAutomaticMask", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, null);

        var exportFactory = typeof(EditorForm).GetMethod("CreateProtectedImage", BindingFlags.Instance | BindingFlags.NonPublic)!;
        using var protectedImage = (Bitmap)exportFactory.Invoke(editor, null)!;
        var sourceImage = (Bitmap)typeof(EditorForm).GetField("_workingBitmap", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;

        Assert.Equal(Color.Black.ToArgb(), protectedImage.GetPixel(1, 1).ToArgb());
        Assert.Equal(Color.White.ToArgb(), protectedImage.GetPixel(0, 0).ToArgb());
        Assert.Equal(Color.Black.ToArgb(), sourceImage.GetPixel(1, 1).ToArgb());
        Assert.False((bool)typeof(EditorForm).GetField("_isAutomaticMaskSelectionActive", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!);
        Assert.False((bool)typeof(EditorForm).GetField("_textRegionDetectionReady", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!);
        Assert.Empty(tooltip.GetToolTip(pictureBox) ?? string.Empty);

        typeof(EditorForm).GetMethod("UndoLastEdit", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, null);
        var undoneImage = (Bitmap)typeof(EditorForm).GetField("_workingBitmap", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        Assert.Equal(Color.White.ToArgb(), undoneImage.GetPixel(1, 1).ToArgb());
    }

    [Fact]
    public void CancellingAutomaticMaskDoesNotChangeImage()
    {
        using var bitmap = new Bitmap(4, 4);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);
        }

        using var editor = new EditorForm(bitmap, string.Empty, maskMode: MaskMode.Solid);
        SetMaskRegions(editor, bitmap.Size, new Rectangle(1, 1, 2, 2));
        typeof(EditorForm).GetField("_isAutomaticMaskSelectionActive", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, true);
        typeof(EditorForm).GetMethod("CancelAutomaticMask", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, null);
        var sourceImage = (Bitmap)typeof(EditorForm).GetField("_workingBitmap", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;

        Assert.Equal(Color.White.ToArgb(), sourceImage.GetPixel(1, 1).ToArgb());
        Assert.False((bool)typeof(EditorForm).GetField("_isAutomaticMaskSelectionActive", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!);
        Assert.False((bool)typeof(EditorForm).GetField("_textRegionDetectionReady", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!);
    }

    [Fact]
    public void DraggingAddsManualMaskRegionDuringAutomaticMaskSelection()
    {
        using var bitmap = new Bitmap(20, 20);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);
        }

        using var editor = new EditorForm(bitmap, string.Empty, maskMode: MaskMode.Solid, lineColorArgb: Color.Black.ToArgb());
        SetMaskRegions(editor, bitmap.Size, new Rectangle(1, 1, 3, 3));
        typeof(EditorForm).GetField("_isAutomaticMaskSelectionActive", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, true);
        var pictureBox = (PictureBox)typeof(EditorForm).GetField("_pictureBox", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        var mouseDown = typeof(EditorForm).GetMethod("PictureBox_MouseDown", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var mouseMove = typeof(EditorForm).GetMethod("PictureBox_MouseMove", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var mouseUp = typeof(EditorForm).GetMethod("PictureBox_MouseUp", BindingFlags.Instance | BindingFlags.NonPublic)!;

        mouseDown.Invoke(editor, [pictureBox, new MouseEventArgs(MouseButtons.Left, 1, 4, 5, 0)]);
        mouseMove.Invoke(editor, [pictureBox, new MouseEventArgs(MouseButtons.Left, 1, 15, 16, 0)]);
        mouseUp.Invoke(editor, [pictureBox, new MouseEventArgs(MouseButtons.Left, 1, 15, 16, 0)]);

        var regionsField = typeof(EditorForm).GetField("_maskRegions", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var regions = regionsField.GetValue(editor)!;
        var count = (int)regionsField.FieldType.GetProperty("Count")!.GetValue(regions)!;
        var addedRegion = regionsField.FieldType.GetProperty("Item")!.GetValue(regions, [1])!;

        Assert.Equal(2, count);
        Assert.Equal(new Rectangle(4, 5, 11, 11), addedRegion.GetType().GetProperty("Bounds")!.GetValue(addedRegion));
        Assert.True((bool)addedRegion.GetType().GetProperty("ShouldMask")!.GetValue(addedRegion)!);

        typeof(EditorForm).GetMethod("ApplyAutomaticMask", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, null);
        var maskedImage = (Bitmap)typeof(EditorForm).GetField("_workingBitmap", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        Assert.Equal(Color.Black.ToArgb(), maskedImage.GetPixel(5, 6).ToArgb());
    }

    [Fact]
    public void ExportRemainsUnmodifiedUntilAutomaticMaskStarts()
    {
        using var bitmap = new Bitmap(2, 2);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);
        }

        using var editor = new EditorForm(bitmap, string.Empty);
        var exportFactory = typeof(EditorForm).GetMethod("CreateProtectedImage", BindingFlags.Instance | BindingFlags.NonPublic)!;
        using var export = (Bitmap)exportFactory.Invoke(editor, null)!;

        Assert.Equal(Color.White.ToArgb(), export.GetPixel(0, 0).ToArgb());
    }

    [Fact]
    public void AutomaticMaskToolIsAvailableButInactiveAtStartup()
    {
        using var bitmap = new Bitmap(2, 2);
        using var editor = new EditorForm(bitmap, string.Empty);
        var button = (ToolStripButton)typeof(EditorForm)
            .GetField("_btnAutomaticMask", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;

        Assert.True(button.Enabled);
        Assert.False((bool)typeof(EditorForm).GetField("_textRegionDetectionReady", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!);
        Assert.False((bool)typeof(EditorForm).GetField("_textRegionDetectionInProgress", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!);
    }

    [Fact]
    public void AutomaticMaskModeShowsOnlyCenteredApplyAndCancelButtons()
    {
        using var bitmap = new Bitmap(800, 600);
        using var editor = new EditorForm(bitmap, string.Empty);
        var activateSelection = typeof(EditorForm).GetMethod("SetAutomaticMaskSelectionActive", BindingFlags.Instance | BindingFlags.NonPublic)!;
        activateSelection.Invoke(editor, [true]);
        editor.Show();
        System.Windows.Forms.Application.DoEvents();

        var toolStrip = editor.Controls.OfType<ToolStrip>().Single();
        var visibleItems = toolStrip.Items.Cast<ToolStripItem>().Where(item => item.Visible).ToArray();
        var spacerBefore = (ToolStripLabel)typeof(EditorForm).GetField("_actionSpacerBefore", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        var automaticMaskButton = (ToolStripButton)typeof(EditorForm).GetField("_btnAutomaticMask", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        var applyButton = (ToolStripButton)typeof(EditorForm).GetField("_btnConfirmCrop", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        var cancelButton = (ToolStripButton)typeof(EditorForm).GetField("_btnCancelCrop", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        var spacerAfter = (ToolStripLabel)typeof(EditorForm).GetField("_actionSpacerAfter", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;

        Assert.Equal(new ToolStripItem[] { spacerBefore, applyButton, cancelButton, spacerAfter }, visibleItems);
        var actionsCenter = (applyButton.Bounds.Left + cancelButton.Bounds.Right) / 2;
        Assert.InRange(Math.Abs(actionsCenter - (toolStrip.DisplayRectangle.Left + (toolStrip.DisplayRectangle.Width / 2))), 0, 1);

        activateSelection.Invoke(editor, [false]);
        Assert.True(automaticMaskButton.Enabled);
        Assert.False(applyButton.Visible);
        Assert.False(cancelButton.Visible);
    }

    [Fact]
    public void CropModeShowsOnlyCenteredConfirmAndCancelButtons()
    {
        using var bitmap = new Bitmap(800, 600);
        using var editor = new EditorForm(bitmap, string.Empty);
        var setActiveTool = typeof(EditorForm).GetMethod("SetActiveTool", BindingFlags.Instance | BindingFlags.NonPublic)!;
        setActiveTool.Invoke(editor, [EditTool.Crop]);
        editor.Show();
        System.Windows.Forms.Application.DoEvents();

        var toolStrip = editor.Controls.OfType<ToolStrip>().Single();
        var visibleItems = toolStrip.Items.Cast<ToolStripItem>().Where(item => item.Visible).ToArray();
        var spacerBefore = (ToolStripLabel)typeof(EditorForm).GetField("_actionSpacerBefore", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        var confirmButton = (ToolStripButton)typeof(EditorForm).GetField("_btnConfirmCrop", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        var cancelButton = (ToolStripButton)typeof(EditorForm).GetField("_btnCancelCrop", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        var spacerAfter = (ToolStripLabel)typeof(EditorForm).GetField("_actionSpacerAfter", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;

        Assert.Equal(new ToolStripItem[] { spacerBefore, confirmButton, cancelButton, spacerAfter }, visibleItems);
        var actionsCenter = (confirmButton.Bounds.Left + cancelButton.Bounds.Right) / 2;
        Assert.InRange(Math.Abs(actionsCenter - (toolStrip.DisplayRectangle.Left + (toolStrip.DisplayRectangle.Width / 2))), 0, 1);

        setActiveTool.Invoke(editor, [EditTool.Freehand]);
        Assert.False(confirmButton.Visible);
        Assert.False(cancelButton.Visible);
        Assert.Contains(toolStrip.Items.OfType<ToolStripButton>(), button => button.Visible && button.Enabled);
    }

    [Fact]
    public void EditorDoesNotAddTextMaskStatusBar()
    {
        using var bitmap = new Bitmap(20, 20);
        using var editor = new EditorForm(bitmap, string.Empty);

        Assert.DoesNotContain(editor.Controls.OfType<Control>(), control => control is Label);
        Assert.Equal(bitmap.Height + 50, editor.ClientSize.Height);
    }

    private static void SetMaskRegions(EditorForm editor, Size imageSize, params Rectangle[] bounds)
    {
        var regionType = typeof(EditorForm).GetNestedType("MaskRegion", BindingFlags.NonPublic)!;
        var regionsField = typeof(EditorForm).GetField("_maskRegions", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var regions = regionsField.GetValue(editor)!;
        var addRegion = regionsField.FieldType.GetMethod("Add")!;
        foreach (var rectangle in bounds)
        {
            var region = Activator.CreateInstance(
                regionType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                [rectangle],
                null)!;
            addRegion.Invoke(regions, [region]);
        }

        typeof(EditorForm).GetField("_maskDetectionImageSize", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, imageSize);
        typeof(EditorForm).GetField("_textRegionDetectionReady", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, true);
    }
}