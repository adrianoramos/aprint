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

        Assert.Equal(5, editorDropdowns.Length);
        Assert.Equal(6, captureDropdowns.Length);
        Assert.All(editorDropdowns, button => Assert.Equal(14, button.Width));
        Assert.All(editorDropdowns, button => Assert.Equal(captureDropdowns[0].Width, button.Width));
        Assert.All(captureDropdowns, button => Assert.Equal(34, button.Height));
        Assert.All(editorDropdowns, button => Assert.Equal(captureDropdowns[0].Height, button.Height));
        var editorToolStrip = editor.Controls.OfType<ToolStrip>().Single();
        var drawingButton = (ToolStripButton)typeof(EditorForm)
            .GetField("_btnDrawing", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var drawingDropdown = (ToolStripDropDownButton)typeof(EditorForm)
            .GetField("_btnDrawingDropdown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        Assert.Equal(captureDropdowns[0].Height, drawingButton.Height);
        Assert.All(
            editorToolStrip.Items.OfType<ToolStripButton>(),
            button => Assert.Equal(captureDropdowns[0].Height, button.Height));
        if (!showLabels)
        {
            const int iconOnlyActionWidth = 52;
            Assert.Equal(iconOnlyActionWidth, drawingButton.Width + drawingDropdown.Width);
            for (var index = 0; index < editorToolStrip.Items.Count; index++)
            {
                if (editorToolStrip.Items[index] is ToolStripButton button)
                {
                    if (index + 1 < editorToolStrip.Items.Count
                        && editorToolStrip.Items[index + 1] is ToolStripDropDownButton dropdownButton)
                    {
                        Assert.Equal(iconOnlyActionWidth, button.Width + dropdownButton.Width);
                        index++;
                    }
                    else
                    {
                        Assert.Equal(iconOnlyActionWidth, button.Width);
                    }
                }
            }
        }

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
    [InlineData(EditTool.Freehand, "_btnDrawing", "_btnDrawingDropdown")]
    [InlineData(EditTool.Line, "_btnDrawing", "_btnDrawingDropdown")]
    [InlineData(EditTool.Arrow, "_btnDrawing", "_btnDrawingDropdown")]
    [InlineData(EditTool.Rectangle, "_btnDrawing", "_btnDrawingDropdown")]
    [InlineData(EditTool.Circle, "_btnDrawing", "_btnDrawingDropdown")]
    [InlineData(EditTool.Highlight, "_btnHighlight", "_btnHighlightSizeDropdown")]
    [InlineData(EditTool.Counter, "_btnCounter", "_btnCounterDropdown")]
    [InlineData(EditTool.Text, "_btnText", "_btnTextFormatDropdown")]
    [InlineData(EditTool.Pixelated, "_btnMask", "_btnMaskModeDropdown")]
    public void SelectedSplitToolActivatesMatchingToolbarGroup(EditTool tool, string buttonFieldName, string dropdownFieldName)
    {
        using var bitmap = new Bitmap(20, 20);
        using var editor = new EditorForm(bitmap, string.Empty);
        var setActiveTool = typeof(EditorForm).GetMethod("SetActiveTool", BindingFlags.Instance | BindingFlags.NonPublic)!;
        setActiveTool.Invoke(editor, [tool]);

        var mainButton = typeof(EditorForm).GetField(buttonFieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor);
        var dropdownButton = typeof(EditorForm).GetField(dropdownFieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor);
        var renderer = typeof(EditorForm).GetField("_toolStripRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;

        Assert.Same(mainButton, renderer.GetType().GetField("_activeMainButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(renderer));
        Assert.Same(dropdownButton, renderer.GetType().GetField("_activeDropdownButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(renderer));
    }

    [Fact]
    public void ClosingMaskMenuOutsideGroupKeepsOnlyTheActiveToolHighlighted()
    {
        using var bitmap = new Bitmap(20, 20);
        using var editor = new EditorForm(bitmap, string.Empty, initialTool: EditTool.Highlight);
        editor.Show();
        System.Windows.Forms.Application.DoEvents();

        var maskButton = (ToolStripButton)typeof(EditorForm)
            .GetField("_btnMask", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var maskDropdown = (ToolStripDropDownButton)typeof(EditorForm)
            .GetField("_btnMaskModeDropdown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var renderer = typeof(EditorForm)
            .GetField("_toolStripRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var originalCursorPosition = Cursor.Position;

        try
        {
            Cursor.Position = editor.PointToScreen(new Point(editor.ClientSize.Width - 5, editor.ClientSize.Height - 5));
            maskDropdown.DropDown.Show(
                maskDropdown.Owner!,
                new Point(maskDropdown.Bounds.Left, maskDropdown.Bounds.Bottom));
            System.Windows.Forms.Application.DoEvents();
            renderer.GetType().GetMethod("SetHoveredGroup", BindingFlags.Instance | BindingFlags.Public)!
                .Invoke(renderer, [maskButton, maskDropdown]);

            maskDropdown.DropDown.Close();

            Assert.Same(
                typeof(EditorForm).GetField("_btnHighlight", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor),
                renderer.GetType().GetField("_activeMainButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(renderer));
            Assert.Null(renderer.GetType().GetField("_hoveredMainButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(renderer));
            Assert.Null(renderer.GetType().GetField("_hoveredDropdownButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(renderer));
        }
        finally
        {
            Cursor.Position = originalCursorPosition;
        }
    }

    [Fact]
    public void DrawingDropdownUpdatesMainIconAndRetainsLastDrawingToolWhenSwitchingModes()
    {
        using var bitmap = new Bitmap(20, 20);
        using var editor = new EditorForm(bitmap, string.Empty, initialTool: EditTool.Crop);
        var drawingButton = (ToolStripButton)typeof(EditorForm)
            .GetField("_btnDrawing", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var drawingDropdown = (ToolStripDropDownButton)typeof(EditorForm)
            .GetField("_btnDrawingDropdown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var freehandImageKey = drawingButton.ImageKey;
        var lineMenuItem = Assert.Single(
            drawingDropdown.DropDownItems.OfType<ToolStripMenuItem>(),
            item => item.Tag is EditTool tool && tool == EditTool.Line);
        var freehandMenuItem = Assert.Single(
            drawingDropdown.DropDownItems.OfType<ToolStripMenuItem>(),
            item => item.Tag is EditTool tool && tool == EditTool.Freehand);

        var renderer = typeof(EditorForm).GetField("_toolStripRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        Assert.False(drawingButton.Checked);
        Assert.True(freehandMenuItem.Checked);

        lineMenuItem.PerformClick();

        var lineImageKey = drawingButton.ImageKey;
        Assert.Equal(EditTool.Line, editor.SelectedTool);
        Assert.NotEqual(freehandImageKey, lineImageKey);
        Assert.True(drawingButton.Checked);
        Assert.True(lineMenuItem.Checked);

        typeof(EditorForm).GetMethod("SetActiveTool", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(editor, [EditTool.Crop]);

        Assert.False(drawingButton.Checked);
        Assert.Equal(lineImageKey, drawingButton.ImageKey);
        Assert.Equal(EditTool.Crop, editor.SelectedTool);
    }

    [Fact]
    public void DrawingDropdownOffersLineAndFillColorOptionsWithPreviews()
    {
        using var bitmap = new Bitmap(20, 20);
        using var editor = new EditorForm(bitmap, string.Empty, fillColorArgb: Color.Magenta.ToArgb());
        var drawingDropdown = (ToolStripDropDownButton)typeof(EditorForm)
            .GetField("_btnDrawingDropdown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var lineColorItem = (ToolStripMenuItem)typeof(EditorForm)
            .GetField("_drawingLineColorMenuItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var fillColorItem = (ToolStripMenuItem)typeof(EditorForm)
            .GetField("_drawingFillColorMenuItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var noFillItem = (ToolStripMenuItem)typeof(EditorForm)
            .GetField("_drawingNoFillMenuItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;

        Assert.Contains(lineColorItem, drawingDropdown.DropDownItems.Cast<ToolStripItem>());
        Assert.Contains(fillColorItem, drawingDropdown.DropDownItems.Cast<ToolStripItem>());
        Assert.Contains(noFillItem, drawingDropdown.DropDownItems.Cast<ToolStripItem>());
        Assert.NotNull(lineColorItem.Image);
        Assert.NotNull(fillColorItem.Image);
        Assert.Equal(Color.Red.ToArgb(), ((Bitmap)lineColorItem.Image!).GetPixel(
            lineColorItem.Image.Width / 2,
            lineColorItem.Image.Height / 2).ToArgb());
        Assert.Equal(Color.Magenta.ToArgb(), ((Bitmap)fillColorItem.Image!).GetPixel(
            fillColorItem.Image.Width / 2,
            fillColorItem.Image.Height / 2).ToArgb());
        Assert.Contains(Color.Red.ToArgb().ToString("x8"), lineColorItem.ToolTipText, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(fillColorItem.ToolTipText));
        var selectedFillToolTip = fillColorItem.ToolTipText;

        noFillItem.PerformClick();

        Assert.Null(typeof(EditorForm)
            .GetField("_selectedFillColor", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor));
        Assert.NotEqual(selectedFillToolTip, fillColorItem.ToolTipText);
    }

    [Fact]
    public void DrawingDropdownConfiguresStrokeWidth()
    {
        using var bitmap = new Bitmap(20, 20);
        using var editor = new EditorForm(bitmap, string.Empty);
        var drawingDropdown = (ToolStripDropDownButton)typeof(EditorForm)
            .GetField("_btnDrawingDropdown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var widthMenu = (ToolStripMenuItem)typeof(EditorForm)
            .GetField("_drawingLineWidthMenuItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var changedWidth = 0;
        editor.LineWidthChanged += width => changedWidth = width;

        Assert.Contains(widthMenu, drawingDropdown.DropDownItems.Cast<ToolStripItem>());
        Assert.Equal(new[] { 1, 2, 3, 4, 6, 8, 12, 16 }, widthMenu.DropDownItems.OfType<ToolStripMenuItem>()
            .Where(item => item.Tag is int)
            .Select(item => (int)item.Tag!));
        Assert.Contains(widthMenu.DropDownItems.OfType<ToolStripMenuItem>(), item => item.Tag?.ToString() == "custom-size");

        Assert.Single(widthMenu.DropDownItems.OfType<ToolStripMenuItem>(), item => item.Tag is int width && width == 8).PerformClick();

        Assert.Equal(8, changedWidth);
        Assert.Equal(8, typeof(EditorForm)
            .GetField("_lineWidth", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor));
    }

    [Fact]
    public void CounterDropdownOffersSizeTextColorAndCircleColor()
    {
        using var bitmap = new Bitmap(20, 20);
        using var editor = new EditorForm(bitmap, string.Empty, initialTool: EditTool.Counter);
        var counterDropdown = (ToolStripDropDownButton)typeof(EditorForm)
            .GetField("_btnCounterDropdown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var sizeMenu = (ToolStripMenuItem)typeof(EditorForm)
            .GetField("_counterSizeMenuItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var textColorMenuItem = (ToolStripMenuItem)typeof(EditorForm)
            .GetField("_counterTextColorMenuItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var circleColorMenuItem = (ToolStripMenuItem)typeof(EditorForm)
            .GetField("_counterCircleColorMenuItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;

        Assert.Contains(sizeMenu, counterDropdown.DropDownItems.Cast<ToolStripItem>());
        Assert.Contains(textColorMenuItem, counterDropdown.DropDownItems.Cast<ToolStripItem>());
        Assert.Contains(circleColorMenuItem, counterDropdown.DropDownItems.Cast<ToolStripItem>());
        Assert.Equal(new[] { 24, 32, 40, 48, 64, 80 }, sizeMenu.DropDownItems
            .OfType<ToolStripMenuItem>()
            .Where(item => item.Tag is int)
            .Select(item => (int)item.Tag!));
        Assert.Contains(sizeMenu.DropDownItems.OfType<ToolStripMenuItem>(), item => item.Tag?.ToString() == "custom-size");
        Assert.NotNull(textColorMenuItem.Image);
        Assert.NotNull(circleColorMenuItem.Image);

        var sizePreset = Assert.Single(
            sizeMenu.DropDownItems.OfType<ToolStripMenuItem>(),
            item => item.Tag is int size && size == 64);
        sizePreset.PerformClick();
        Assert.Equal(64, typeof(EditorForm)
            .GetField("_counterSize", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor));
    }

    [Fact]
    public void TextDropdownShowsCurrentTextColorPreview()
    {
        using var bitmap = new Bitmap(20, 20);
        using var editor = new EditorForm(bitmap, string.Empty, textColorArgb: Color.Red.ToArgb());
        var textDropdown = (ToolStripDropDownButton)typeof(EditorForm)
            .GetField("_btnTextFormatDropdown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var textColorItem = (ToolStripMenuItem)typeof(EditorForm)
            .GetField("_textFormatColorMenuItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;

        Assert.Contains(textColorItem, textDropdown.DropDownItems.Cast<ToolStripItem>());
        Assert.NotNull(textColorItem.Image);
        var originalPreview = textColorItem.Image;

        typeof(EditorForm).GetField("_selectedTextColor", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(editor, Color.Lime);
        typeof(EditorForm).GetMethod("UpdateColorMenuPreviews", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(editor, null);

        Assert.NotSame(originalPreview, textColorItem.Image);
        Assert.EndsWith(Color.Lime.Name, textColorItem.ToolTipText);
    }

    [Fact]
    public void CounterMarkerUsesConfiguredSizeAndIndependentTextAndCircleColors()
    {
        using var bitmap = new Bitmap(100, 100);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);
        }

        using var editor = new EditorForm(
            bitmap,
            string.Empty,
            initialTool: EditTool.Counter,
            counterSize: 64,
            counterTextColorArgb: Color.Lime.ToArgb(),
            counterCircleColorArgb: Color.Blue.ToArgb());
        typeof(EditorForm).GetMethod("AddCounterMarker", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(editor, [new Point(50, 50)]);
        var workingBitmap = (Bitmap)typeof(EditorForm)
            .GetField("_workingBitmap", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var blueCirclePixelFound = false;
        var greenTextPixelFound = false;
        for (var y = 0; y < workingBitmap.Height; y++)
        {
            for (var x = 0; x < workingBitmap.Width; x++)
            {
                var pixel = workingBitmap.GetPixel(x, y);
                blueCirclePixelFound |= pixel.B > pixel.R && pixel.B > pixel.G;
                greenTextPixelFound |= pixel.G > pixel.R && pixel.G > pixel.B;
            }
        }

        Assert.True(blueCirclePixelFound);
        Assert.True(greenTextPixelFound);
        Assert.Equal(64, typeof(EditorForm)
            .GetField("_counterSize", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor));
    }

    [Theory]
    [InlineData(EditTool.Freehand)]
    [InlineData(EditTool.Line)]
    [InlineData(EditTool.Arrow)]
    [InlineData(EditTool.Rectangle)]
    [InlineData(EditTool.Circle)]
    public void EditorDrawingToolTipsMatchFloatingToolbar(EditTool tool)
    {
        using var editorBitmap = new Bitmap(20, 20);
        using var captureBitmap = new Bitmap(20, 20);
        using var editor = new EditorForm(editorBitmap, string.Empty, initialTool: tool);
        using var capture = new RegionSelectorForm(captureBitmap, lastCaptureTool: tool);
        var editorButton = (ToolStripButton)typeof(EditorForm)
            .GetField("_btnDrawing", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var captureButton = (ToolStripButton)typeof(RegionSelectorForm)
            .GetField("_drawingButton", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(capture)!;

        Assert.Equal(captureButton.ToolTipText, editorButton.ToolTipText);
    }

    [Fact]
    public void HistoryButtonsStayNearToolsAndExportsStayAtToolbarRightEdge()
    {
        using var bitmap = new Bitmap(1100, 600);
        using var editor = new EditorForm(bitmap, string.Empty, showToolLabels: false);
        editor.Show();
        System.Windows.Forms.Application.DoEvents();

        var toolStrip = editor.Controls.OfType<ToolStrip>().Single();
        var buttons = toolStrip.Items.OfType<ToolStripButton>().ToArray();
        var undoButton = buttons.Single(button => (button.ToolTipText ?? string.Empty).EndsWith("(Ctrl+Z)", StringComparison.Ordinal));
        var redoButton = buttons.Single(button => (button.ToolTipText ?? string.Empty).EndsWith("(Ctrl+Y)", StringComparison.Ordinal));
        var copyButton = buttons.Single(button => (button.ToolTipText ?? string.Empty).EndsWith("(Ctrl+C)", StringComparison.Ordinal));
        var saveButton = buttons.Single(button => (button.ToolTipText ?? string.Empty).EndsWith("(Ctrl+S)", StringComparison.Ordinal));
        var cropButton = (ToolStripButton)typeof(EditorForm).GetField("_btnCrop", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;

        Assert.Equal(ToolStripItemAlignment.Left, undoButton.Alignment);
        Assert.Equal(ToolStripItemAlignment.Left, redoButton.Alignment);
        Assert.Equal(ToolStripItemAlignment.Right, copyButton.Alignment);
        Assert.Equal(ToolStripItemAlignment.Right, saveButton.Alignment);
        Assert.True(cropButton.Bounds.Right < undoButton.Bounds.Left);
        Assert.True(undoButton.Bounds.Left < redoButton.Bounds.Left);
        Assert.Equal(ToolStripItemPlacement.Main, saveButton.Placement);
        if (toolStrip.OverflowButton.Visible)
        {
            Assert.Equal(ToolStripItemPlacement.Overflow, copyButton.Placement);
            Assert.Equal(toolStrip.OverflowButton.Bounds.Left, saveButton.Bounds.Right + saveButton.Margin.Right);
        }
        else
        {
            Assert.Equal(ToolStripItemPlacement.Main, copyButton.Placement);
            Assert.True(copyButton.Bounds.Left < saveButton.Bounds.Left);
            Assert.Equal(toolStrip.DisplayRectangle.Right, saveButton.Bounds.Right + saveButton.Margin.Right);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EditingToolsStayCenteredBetweenCaptureAndExportActions(bool showLabels)
    {
        using var bitmap = new Bitmap(1600, 600);
        using var editor = new EditorForm(bitmap, string.Empty, showToolLabels: showLabels);
        editor.Show();
        System.Windows.Forms.Application.DoEvents();

        var toolStrip = editor.Controls.OfType<ToolStrip>().Single();
        var buttons = toolStrip.Items.OfType<ToolStripButton>().ToArray();
        var captureButton = buttons.Single(button => (button.ToolTipText ?? string.Empty).EndsWith("(Ctrl+N)", StringComparison.Ordinal));
        var copyButton = buttons.Single(button => (button.ToolTipText ?? string.Empty).EndsWith("(Ctrl+C)", StringComparison.Ordinal));
        var saveButton = buttons.Single(button => (button.ToolTipText ?? string.Empty).EndsWith("(Ctrl+S)", StringComparison.Ordinal));
        var firstTool = (ToolStripButton)typeof(EditorForm).GetField("_btnDrawing", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        var lastTool = (ToolStripButton)typeof(EditorForm).GetField("_btnRedo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        var toolGroupCenter = (firstTool.Bounds.Left + lastTool.Bounds.Right) / 2;
        var toolbarCenter = toolStrip.DisplayRectangle.Left + (toolStrip.DisplayRectangle.Width / 2);

        Assert.Equal(ToolStripItemAlignment.Left, captureButton.Alignment);
        Assert.InRange(Math.Abs(toolGroupCenter - toolbarCenter), 0, 1);
        Assert.True(captureButton.Bounds.Right < firstTool.Bounds.Left);
        Assert.True(lastTool.Bounds.Right < copyButton.Bounds.Left);
        Assert.True(copyButton.Bounds.Left < saveButton.Bounds.Left);
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

        using var editor = new EditorForm(bitmap, string.Empty, maskMode: MaskMode.Solid, maskColorArgb: Color.Black.ToArgb());
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
        using var cancellation = new CancellationTokenSource();
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);
        }

        using var editor = new EditorForm(bitmap, string.Empty, maskMode: MaskMode.Solid);
        SetMaskRegions(editor, bitmap.Size, new Rectangle(1, 1, 2, 2));
        typeof(EditorForm).GetField("_isAutomaticMaskSelectionActive", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, true);
        typeof(EditorForm).GetField("_textRegionDetectionInProgress", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, true);
        typeof(EditorForm).GetField("_textRegionDetectionCancellation", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, cancellation);
        typeof(EditorForm).GetMethod("CancelAutomaticMask", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, null);
        var sourceImage = (Bitmap)typeof(EditorForm).GetField("_workingBitmap", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;

        Assert.Equal(Color.White.ToArgb(), sourceImage.GetPixel(1, 1).ToArgb());
        Assert.False((bool)typeof(EditorForm).GetField("_isAutomaticMaskSelectionActive", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!);
        Assert.False((bool)typeof(EditorForm).GetField("_textRegionDetectionInProgress", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!);
        Assert.True(cancellation.IsCancellationRequested);
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

        using var editor = new EditorForm(bitmap, string.Empty, maskMode: MaskMode.Solid, maskColorArgb: Color.Black.ToArgb());
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

    [Theory]
    [InlineData(EditTool.Blur)]
    [InlineData(EditTool.Pixelated)]
    public void AutomaticMaskMenuItemIsSeparatedAndAvailable(EditTool initialTool)
    {
        using var bitmap = new Bitmap(2, 2);
        using var editor = new EditorForm(bitmap, string.Empty, initialTool: initialTool);
        var menuItem = (ToolStripMenuItem)typeof(EditorForm)
            .GetField("_automaticMaskMenuItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var separator = (ToolStripSeparator)typeof(EditorForm)
            .GetField("_automaticMaskSeparator", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var maskDropdown = (ToolStripDropDownButton)typeof(EditorForm)
            .GetField("_btnMaskModeDropdown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var pixelSizeSeparator = (ToolStripSeparator)typeof(EditorForm)
            .GetField("_pixelSizeSeparator", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var pixelSizeMenuItem = (ToolStripMenuItem)typeof(EditorForm)
            .GetField("_pixelSizeMenuItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var maskColorSeparator = (ToolStripSeparator)typeof(EditorForm)
            .GetField("_maskColorSeparator", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var maskColorMenuItem = (ToolStripMenuItem)typeof(EditorForm)
            .GetField("_maskColorMenuItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;

        Assert.True(menuItem.Enabled);
        Assert.NotNull(menuItem.Image);
        var menuItemIndex = maskDropdown.DropDownItems.IndexOf(menuItem);
        Assert.Same(separator, maskDropdown.DropDownItems[menuItemIndex - 1]);
        Assert.Same(maskColorMenuItem, maskDropdown.DropDownItems[menuItemIndex - 2]);
        Assert.Same(maskColorSeparator, maskDropdown.DropDownItems[menuItemIndex - 3]);
        Assert.Same(pixelSizeMenuItem, maskDropdown.DropDownItems[menuItemIndex - 4]);
        Assert.Same(pixelSizeSeparator, maskDropdown.DropDownItems[menuItemIndex - 5]);
        Assert.False((bool)typeof(EditorForm).GetField("_textRegionDetectionReady", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!);
        Assert.False((bool)typeof(EditorForm).GetField("_textRegionDetectionInProgress", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!);
    }

    [Fact]
    public void SizeAndFontMenusUsePresetsAndCustomizationCommands()
    {
        using var bitmap = new Bitmap(2, 2);
        using var editor = new EditorForm(bitmap, string.Empty);
        var highlightDropdown = (ToolStripDropDownButton)typeof(EditorForm)
            .GetField("_btnHighlightSizeDropdown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var maskDropdown = (ToolStripDropDownButton)typeof(EditorForm)
            .GetField("_btnMaskModeDropdown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var pixelSizeMenu = (ToolStripMenuItem)typeof(EditorForm)
            .GetField("_pixelSizeMenuItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var textDropdown = (ToolStripDropDownButton)typeof(EditorForm)
            .GetField("_btnTextFormatDropdown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var textFontSizeMenu = (ToolStripMenuItem)typeof(EditorForm)
            .GetField("_textFontSizeMenuItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;

        Assert.Empty(highlightDropdown.DropDownItems.OfType<ToolStripControlHost>());
        Assert.Empty(maskDropdown.DropDownItems.OfType<ToolStripControlHost>());
        Assert.Empty(pixelSizeMenu.DropDownItems.OfType<ToolStripControlHost>());
        Assert.Empty(textDropdown.DropDownItems.OfType<ToolStripControlHost>());
        Assert.Equal(
            new[] { 4, 8, 16, 32 },
            highlightDropdown.DropDownItems.OfType<ToolStripMenuItem>()
                .Where(item => item.Tag is int)
                .Select(item => (int)item.Tag!));
        Assert.Contains(
            highlightDropdown.DropDownItems.OfType<ToolStripMenuItem>(),
            item => item.Tag?.ToString() == "custom-size");
        Assert.Contains(
            pixelSizeMenu.DropDownItems.OfType<ToolStripMenuItem>(),
            item => item.Tag?.ToString() == "custom-size");
        Assert.Contains(
            textDropdown.DropDownItems.OfType<ToolStripMenuItem>(),
            item => item.Tag?.ToString() == "font-dialog");
        Assert.DoesNotContain(
            textFontSizeMenu.DropDownItems.OfType<ToolStripMenuItem>(),
            item => item.Tag?.ToString() == "custom-size");

        var highlightPreset = Assert.Single(
            highlightDropdown.DropDownItems.OfType<ToolStripMenuItem>(),
            item => item.Tag is int size && size == 16);
        highlightPreset.PerformClick();
        Assert.Equal(16, typeof(EditorForm)
            .GetField("_highlightWidth", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor));

        var pixelSizePreset = Assert.Single(
            pixelSizeMenu.DropDownItems.OfType<ToolStripMenuItem>(),
            item => item.Tag is int size && size == 12);
        pixelSizePreset.PerformClick();
        Assert.Equal(12, typeof(EditorForm)
            .GetField("_maskPixelSize", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor));

        var fontSizePreset = Assert.Single(
            textFontSizeMenu.DropDownItems.OfType<ToolStripMenuItem>(),
            item => item.Tag is float size && size == 16F);
        fontSizePreset.PerformClick();
        Assert.Equal(16F, typeof(EditorForm)
            .GetField("_textFontSize", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor));
    }

    [Fact]
    public void HighlightMenuColorIsIndependentFromStrokeColor()
    {
        using var bitmap = new Bitmap(2, 2);
        using var editor = new EditorForm(
            bitmap,
            string.Empty,
            initialTool: EditTool.Highlight,
            lineColorArgb: Color.Red.ToArgb(),
            highlightColorArgb: Color.Blue.ToArgb());
        var highlightDropdown = (ToolStripDropDownButton)typeof(EditorForm)
            .GetField("_btnHighlightSizeDropdown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var highlightMenuItem = (ToolStripMenuItem)typeof(EditorForm)
            .GetField("_highlightColorMenuItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var strokeMenuItem = (ToolStripMenuItem)typeof(EditorForm)
            .GetField("_drawingLineColorMenuItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;

        Assert.Contains(highlightMenuItem, highlightDropdown.DropDownItems.Cast<ToolStripItem>());
        Assert.NotNull(highlightMenuItem.Image);
        Assert.Equal(Color.Red.ToArgb(), ((Color)typeof(EditorForm)
            .GetField("_selectedColor", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!).ToArgb());
        Assert.Equal(Color.Blue.ToArgb(), ((Color)typeof(EditorForm)
            .GetField("_selectedHighlightColor", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!).ToArgb());
        var originalPreviewColor = GetCenterSwatchColor(highlightMenuItem.Image!);
        var originalLineColor = GetCenterSwatchColor(strokeMenuItem.Image!);
        typeof(EditorForm).GetField("_selectedColor", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(editor, Color.Lime);
        typeof(EditorForm).GetMethod("UpdateColorMenuPreviews", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(editor, null);

        var updatedPreviewColor = GetCenterSwatchColor(highlightMenuItem.Image!);
        var updatedLineColor = GetCenterSwatchColor(strokeMenuItem.Image!);
        Assert.Equal(originalPreviewColor, updatedPreviewColor);
        Assert.NotEqual(originalLineColor, updatedLineColor);
        Assert.True(updatedLineColor.G > updatedLineColor.R);
        Assert.True(updatedLineColor.G > updatedLineColor.B);
        var highlightColor = (Color)typeof(EditorForm)
            .GetField("_selectedHighlightColor", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var lineColor = (Color)typeof(EditorForm)
            .GetField("_selectedColor", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        Assert.EndsWith(highlightColor.Name, highlightMenuItem.ToolTipText);
        Assert.Equal(Color.Lime.ToArgb(), lineColor.ToArgb());

        static Color GetCenterSwatchColor(Image image) =>
            ((Bitmap)image).GetPixel(image.Width / 2, image.Height / 2);
    }

    [Fact]
    public void CoverMaskUsesItsOwnColorWithoutChangingLineColor()
    {
        using var bitmap = new Bitmap(4, 4);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);
        }

        using var editor = new EditorForm(
            bitmap,
            string.Empty,
            initialTool: EditTool.Blur,
            lineColorArgb: Color.Red.ToArgb(),
            maskColorArgb: Color.Blue.ToArgb());
        var maskDropdown = (ToolStripDropDownButton)typeof(EditorForm)
            .GetField("_btnMaskModeDropdown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var maskColorMenuItem = (ToolStripMenuItem)typeof(EditorForm)
            .GetField("_maskColorMenuItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        Assert.Contains(maskColorMenuItem, maskDropdown.DropDownItems.Cast<ToolStripItem>());
        Assert.NotNull(maskColorMenuItem.Image);

        typeof(EditorForm).GetMethod("ApplyMaskToSelection", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(editor, [new Rectangle(0, 0, 4, 4)]);

        var workingBitmap = (Bitmap)typeof(EditorForm)
            .GetField("_workingBitmap", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        Assert.Equal(Color.Blue.ToArgb(), workingBitmap.GetPixel(1, 1).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), ((Color)typeof(EditorForm)
            .GetField("_selectedColor", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!).ToArgb());
    }

    [Fact]
    public void CtrlF8StartsAutomaticMaskAndIsShownInMenu()
    {
        using var bitmap = new Bitmap(2, 2);
        using var editor = new EditorForm(bitmap, string.Empty);
        var menuItem = (ToolStripMenuItem)typeof(EditorForm)
            .GetField("_automaticMaskMenuItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        typeof(EditorForm)
            .GetField("_textRegionDetectionInProgress", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(editor, true);
        var processCmdKey = typeof(EditorForm).GetMethod("ProcessCmdKey", BindingFlags.Instance | BindingFlags.NonPublic)!;
        object?[] arguments =
        [
            Message.Create(IntPtr.Zero, 0x0100, IntPtr.Zero, IntPtr.Zero),
            Keys.Control | Keys.F8,
        ];

        var handled = (bool)processCmdKey.Invoke(editor, arguments)!;

        Assert.Equal("Ctrl+F8", menuItem.ShortcutKeyDisplayString);
        Assert.True(handled);
    }

    [Fact]
    public void AutomaticMaskModeShowsCenteredApplyAndCancelButtons()
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
        var spacerAfter = (ToolStripLabel)typeof(EditorForm).GetField("_actionSpacerAfter", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        var automaticMaskMenuItem = (ToolStripMenuItem)typeof(EditorForm).GetField("_automaticMaskMenuItem", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        var applyButton = (ToolStripButton)typeof(EditorForm).GetField("_btnConfirmCrop", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        var cancelButton = (ToolStripButton)typeof(EditorForm).GetField("_btnCancelCrop", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;

        Assert.Equal(new ToolStripItem[] { spacerBefore, applyButton, cancelButton, spacerAfter }, visibleItems);
        Assert.False(automaticMaskMenuItem.Enabled);
        Assert.EndsWith("(Enter)", applyButton.ToolTipText);
        Assert.EndsWith("(Esc)", cancelButton.ToolTipText);
        var actionCenter = (applyButton.Bounds.Left + cancelButton.Bounds.Right) / 2;
        Assert.InRange(Math.Abs(actionCenter - (toolStrip.DisplayRectangle.Left + (toolStrip.DisplayRectangle.Width / 2))), 0, 1);

        cancelButton.PerformClick();
        Assert.True(automaticMaskMenuItem.Enabled);
        Assert.False(applyButton.Visible);
        Assert.False(cancelButton.Visible);

        activateSelection.Invoke(editor, [true]);
        var processCmdKey = typeof(EditorForm).GetMethod("ProcessCmdKey", BindingFlags.Instance | BindingFlags.NonPublic)!;
        object?[] arguments =
        [
            Message.Create(IntPtr.Zero, 0x0100, IntPtr.Zero, IntPtr.Zero),
            Keys.Escape,
        ];
        Assert.True((bool)processCmdKey.Invoke(editor, arguments)!);
        Assert.True(automaticMaskMenuItem.Enabled);
        Assert.False(applyButton.Visible);
        Assert.False(cancelButton.Visible);
    }

    [Fact]
    public void CropModeShowsCenteredConfirmAndCancelButtons()
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
        var spacerAfter = (ToolStripLabel)typeof(EditorForm).GetField("_actionSpacerAfter", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        var confirmButton = (ToolStripButton)typeof(EditorForm).GetField("_btnConfirmCrop", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        var cancelButton = (ToolStripButton)typeof(EditorForm).GetField("_btnCancelCrop", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;

        Assert.Equal(new ToolStripItem[] { spacerBefore, confirmButton, cancelButton, spacerAfter }, visibleItems);
        Assert.EndsWith("(Enter)", confirmButton.ToolTipText);
        Assert.EndsWith("(Esc)", cancelButton.ToolTipText);
        var actionCenter = (confirmButton.Bounds.Left + cancelButton.Bounds.Right) / 2;
        Assert.InRange(Math.Abs(actionCenter - (toolStrip.DisplayRectangle.Left + (toolStrip.DisplayRectangle.Width / 2))), 0, 1);

        cancelButton.PerformClick();
        Assert.Equal(EditTool.Freehand, editor.SelectedTool);
        Assert.False(confirmButton.Visible);
        Assert.False(cancelButton.Visible);
        Assert.Contains(toolStrip.Items.OfType<ToolStripButton>(), button => button.Visible && button.Enabled);

        setActiveTool.Invoke(editor, [EditTool.Crop]);
        var processCmdKey = typeof(EditorForm).GetMethod("ProcessCmdKey", BindingFlags.Instance | BindingFlags.NonPublic)!;
        object?[] arguments =
        [
            Message.Create(IntPtr.Zero, 0x0100, IntPtr.Zero, IntPtr.Zero),
            Keys.Escape,
        ];
        Assert.True((bool)processCmdKey.Invoke(editor, arguments)!);
        Assert.Equal(EditTool.Freehand, editor.SelectedTool);
        Assert.False(confirmButton.Visible);
        Assert.False(cancelButton.Visible);
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