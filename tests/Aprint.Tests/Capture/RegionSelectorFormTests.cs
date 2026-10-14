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

    [Fact]
    public void HighlightStrokesUseTheirOwnConfiguredColor()
    {
        using var bitmap = new Bitmap(40, 40);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);
        }

        using var selector = new RegionSelectorForm(
            bitmap,
            lineColorArgb: Color.Red.ToArgb(),
            highlightColorArgb: Color.Blue.ToArgb(),
            highlightWidth: 12);
        typeof(RegionSelectorForm)
            .GetField("_selection", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(selector, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
        var highlightStrokes = (List<List<Point>>)typeof(RegionSelectorForm)
            .GetField("_highlightStrokes", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!;
        highlightStrokes.Add([new Point(4, 20), new Point(36, 20)]);

        using (var graphics = Graphics.FromImage(bitmap))
        {
            typeof(RegionSelectorForm)
                .GetMethod("DrawHighlightStrokes", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(selector, [graphics]);
        }

        var highlightedPixel = bitmap.GetPixel(20, 20);
        Assert.True(highlightedPixel.B > highlightedPixel.R);
        Assert.True(highlightedPixel.B > highlightedPixel.G);
        Assert.Equal(Color.Red.ToArgb(), ((Color)typeof(RegionSelectorForm)
            .GetField("_lineColor", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!).ToArgb());
    }

    [Fact]
    public void CounterUsesConfiguredSizeAndIndependentTextAndCircleColors()
    {
        using var bitmap = new Bitmap(100, 100);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);
        }

        using var selector = new RegionSelectorForm(
            bitmap,
            counterSize: 64,
            counterTextColorArgb: Color.Lime.ToArgb(),
            counterCircleColorArgb: Color.Blue.ToArgb());
        typeof(RegionSelectorForm).GetField("_selection", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(selector, new Rectangle(0, 0, 100, 100));
        typeof(RegionSelectorForm).GetMethod("AddCounterMarker", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(selector, [new Point(50, 50)]);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            typeof(RegionSelectorForm).GetMethod("DrawCounterMarkers", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(selector, [graphics]);
        }

        var markers = (List<ImageEditingService.CounterMarker>)typeof(RegionSelectorForm)
            .GetField("_counterMarkers", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!;
        Assert.Equal(64, Assert.Single(markers).Diameter);
        var hasBlueCirclePixels = false;
        var hasGreenTextPixels = false;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                hasBlueCirclePixels |= pixel.B > pixel.R && pixel.B > pixel.G;
                hasGreenTextPixels |= pixel.G > pixel.R && pixel.G > pixel.B;
            }
        }

        Assert.True(hasBlueCirclePixels);
        Assert.True(hasGreenTextPixels);
    }

    [Fact]
    public void CoverMaskUsesItsOwnConfiguredColor()
    {
        using var bitmap = new Bitmap(8, 8);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);
        }

        using var selector = new RegionSelectorForm(
            bitmap,
            maskMode: MaskMode.Solid,
            lineColorArgb: Color.Red.ToArgb(),
            maskColorArgb: Color.Blue.ToArgb());
        typeof(RegionSelectorForm).GetField("_selection", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(selector, new Rectangle(0, 0, 8, 8));
        typeof(RegionSelectorForm).GetField("_maskStart", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(selector, new Point(1, 1));
        typeof(RegionSelectorForm).GetField("_maskEnd", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(selector, new Point(5, 5));
        typeof(RegionSelectorForm).GetField("_isDrawingMaskRegion", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(selector, true);

        typeof(RegionSelectorForm).GetMethod("OnMouseUp", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(selector, [new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0)]);

        Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(2, 2).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), ((Color)typeof(RegionSelectorForm)
            .GetField("_lineColor", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!).ToArgb());
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
    public void InitialScreenSelectionBarOffersFullScreenCurrentScreenAndEachDisplay()
    {
        var cursorScreenBounds = Screen.FromPoint(Cursor.Position).Bounds;
        var virtualBounds = SystemInformation.VirtualScreen;
        using var bitmap = new Bitmap(4, 4);
        using var selector = new RegionSelectorForm(bitmap);
        selector.Show();
        var screenSelectionBar = (ToolStrip)typeof(RegionSelectorForm)
            .GetField("_screenSelectionBar", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!;
        var buttons = screenSelectionBar.Items.OfType<ToolStripButton>().ToArray();
        var itemsWidth = screenSelectionBar.Items
            .Cast<ToolStripItem>()
            .Sum(item => item.GetPreferredSize(Size.Empty).Width + item.Margin.Horizontal)
            + screenSelectionBar.Padding.Horizontal;

        Assert.True(screenSelectionBar.Visible);
        Assert.Equal(DockStyle.None, screenSelectionBar.Dock);
        Assert.Equal(52, screenSelectionBar.MinimumSize.Height);
        Assert.Equal(itemsWidth, screenSelectionBar.MinimumSize.Width);
        Assert.True(screenSelectionBar.Width >= screenSelectionBar.MinimumSize.Width);
        var expectedScreenCenter = cursorScreenBounds.Left - virtualBounds.Left + (cursorScreenBounds.Width / 2);
        var actualBarCenter = screenSelectionBar.Left + (screenSelectionBar.Width / 2);
        Assert.InRange(
            Math.Abs(actualBarCenter - expectedScreenCenter),
            0,
            1);
        Assert.Equal(cursorScreenBounds.Top - virtualBounds.Top + 12, screenSelectionBar.Top);
        var hasMultipleScreens = Screen.AllScreens.Length > 1;
        Assert.Equal(hasMultipleScreens ? Screen.AllScreens.Length + 1 : 1, buttons.Length);
        var fullScreenButton = Assert.Single(buttons, button => button.Text == AppLocalization.Translate("Capture.Region.FullScreen"));
        Assert.NotNull(fullScreenButton.Image);
        Assert.Equal(ToolStripItemDisplayStyle.ImageAndText, fullScreenButton.DisplayStyle);
        if (hasMultipleScreens)
        {
            var currentScreenButton = Assert.Single(buttons, button => button.Text == AppLocalization.Translate("Capture.Region.CurrentScreen"));
            Assert.NotNull(currentScreenButton.Image);
            Assert.Equal(ToolStripItemDisplayStyle.ImageAndText, currentScreenButton.DisplayStyle);
            for (var index = 1; index <= Screen.AllScreens.Length; index++)
            {
                var label = $"{AppLocalization.Translate("Capture.Region.Screen")} {index}";
                if (Screen.AllScreens[index - 1].Bounds == cursorScreenBounds)
                {
                    Assert.DoesNotContain(buttons, button => button.Text == label);
                }
                else
                {
                    Assert.Contains(buttons, button => button.Text == label);
                }
            }
        }

        buttons.Single(button => button.Text == AppLocalization.Translate("Capture.Region.FullScreen")).PerformClick();

        Assert.Equal(new Rectangle(Point.Empty, bitmap.Size), selector.SelectedRegion);
        Assert.Equal(RegionSelectionAction.Capture, selector.SelectedAction);
    }

    [Fact]
    public void NumberedScreenButtonSelectsTheScreenBoundsInsideTheVirtualDesktop()
    {
        var virtualBounds = SystemInformation.VirtualScreen;
        using var bitmap = new Bitmap(Math.Max(4, virtualBounds.Width), Math.Max(4, virtualBounds.Height));
        using var selector = new RegionSelectorForm(bitmap);
        var screenSelectionBar = (ToolStrip)typeof(RegionSelectorForm)
            .GetField("_screenSelectionBar", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!;
        if (Screen.AllScreens.Length == 1)
        {
            Assert.Single(screenSelectionBar.Items.OfType<ToolStripButton>());
            return;
        }
        var cursorScreenBounds = (Rectangle)typeof(RegionSelectorForm)
            .GetField("_screenUnderCursorAtStart", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!;
        var screenIndex = Array.FindIndex(Screen.AllScreens, screen => screen.Bounds != cursorScreenBounds);
        var firstScreenButton = screenSelectionBar.Items
            .OfType<ToolStripButton>()
            .Single(button => button.Text == $"{AppLocalization.Translate("Capture.Region.Screen")} {screenIndex + 1}");

        firstScreenButton.PerformClick();

        var screenBounds = Screen.AllScreens[screenIndex].Bounds;
        var expectedSelection = Rectangle.Intersect(
            new Rectangle(
                screenBounds.X - virtualBounds.X,
                screenBounds.Y - virtualBounds.Y,
                screenBounds.Width,
                screenBounds.Height),
            new Rectangle(Point.Empty, bitmap.Size));
        Assert.Equal(expectedSelection, selector.SelectedRegion);
        Assert.Equal(RegionSelectionAction.Capture, selector.SelectedAction);
    }

    [Theory]
    [InlineData(1, 0, true)]
    [InlineData(1, 0, false)]
    [InlineData(2, 0, true)]
    [InlineData(2, 0, false)]
    [InlineData(2, 1, true)]
    [InlineData(2, 1, false)]
    [InlineData(3, 0, true)]
    [InlineData(3, 0, false)]
    [InlineData(3, 1, true)]
    [InlineData(3, 1, false)]
    [InlineData(3, 2, true)]
    [InlineData(3, 2, false)]
    public void ScreenSelectionBarOffersMonitorChoicesOnlyForMultipleScreens(int screenCount, int currentScreenIndex, bool showToolLabels)
    {
        using var bitmap = new Bitmap(4, 4);
        using var selector = new RegionSelectorForm(bitmap, showToolLabels: showToolLabels);
        var cursorScreenBounds = (Rectangle)typeof(RegionSelectorForm)
            .GetField("_screenUnderCursorAtStart", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!;
        var screens = Enumerable.Range(0, screenCount)
            .Select(index => new Rectangle(cursorScreenBounds.Right + (index + 1) * 100, cursorScreenBounds.Top, 100, 100))
            .ToArray();
        screens[currentScreenIndex] = cursorScreenBounds;
        using var screenSelectionBar = (ToolStrip)typeof(RegionSelectorForm)
            .GetMethod("CreateScreenSelectionBar", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(selector, new object[] { screens })!;
        var expectedLabels = new List<string> { AppLocalization.Translate("Capture.Region.FullScreen") };
        if (screenCount > 1)
        {
            expectedLabels.Add(AppLocalization.Translate("Capture.Region.CurrentScreen"));
            expectedLabels.AddRange(Enumerable.Range(1, screenCount)
                .Where(index => index != currentScreenIndex + 1)
                .Select(index => $"{AppLocalization.Translate("Capture.Region.Screen")} {index}"));
        }

        var buttons = screenSelectionBar.Items.OfType<ToolStripButton>().ToArray();
        var expectedTooltips = new List<string> { AppLocalization.Translate("Capture.Region.FullScreenTooltip") };
        if (screenCount > 1)
        {
            expectedTooltips.Add(AppLocalization.Translate("Capture.Region.CurrentScreenTooltip"));
            expectedTooltips.AddRange(Enumerable.Range(1, screenCount)
                .Where(index => index != currentScreenIndex + 1)
                .Select(index => AppLocalization.Format("Capture.Region.ScreenTooltip", index)));
        }
        Assert.Equal(expectedTooltips, buttons.Select(button => button.ToolTipText));
        Assert.Equal(expectedLabels, buttons.Select(button => button.AccessibleName));
        var expectedTexts = expectedLabels.Take(2).Concat(Enumerable.Range(1, screenCount)
            .Where(index => screenCount > 1 && index != currentScreenIndex + 1)
            .Select(index => showToolLabels ? $"{AppLocalization.Translate("Capture.Region.Screen")} {index}" : $"{index}"));
        Assert.Equal(expectedTexts, buttons.Select(button => button.Text));
        Assert.Equal(screenCount > 1 ? 1 : 0, screenSelectionBar.Items.OfType<ToolStripSeparator>().Count());
        for (var index = 0; index < buttons.Length; index++)
        {
            var button = buttons[index];
            Assert.True(button.AutoSize);
            Assert.Equal(index >= 2 ? new Padding(3, 3, 3, 3) : new Padding(8, 3, 8, 3), button.Padding);
            Assert.NotNull(button.Image);
            Assert.Equal(showToolLabels || index >= 2 ? ToolStripItemDisplayStyle.ImageAndText : ToolStripItemDisplayStyle.Image, button.DisplayStyle);
            Assert.Equal(ContentAlignment.MiddleCenter, button.ImageAlign);
            Assert.Equal(ContentAlignment.MiddleCenter, button.TextAlign);
            Assert.Equal(TextImageRelation.ImageBeforeText, button.TextImageRelation);
            Assert.Equal(ToolStripItemImageScaling.None, button.ImageScaling);
        }
    }

    [Fact]
    public void CurrentScreenButtonSelectsTheMonitorUnderTheCursor()
    {
        var virtualBounds = SystemInformation.VirtualScreen;
        using var bitmap = new Bitmap(Math.Max(4, virtualBounds.Width), Math.Max(4, virtualBounds.Height));
        var cursorScreenBounds = Screen.FromPoint(Cursor.Position).Bounds;
        using var selector = new RegionSelectorForm(bitmap);
        var screenSelectionBar = (ToolStrip)typeof(RegionSelectorForm)
            .GetField("_screenSelectionBar", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!;
        if (Screen.AllScreens.Length == 1)
        {
            Assert.Single(screenSelectionBar.Items.OfType<ToolStripButton>());
            return;
        }
        var currentScreenButton = screenSelectionBar.Items
            .OfType<ToolStripButton>()
            .Single(button => button.Text == AppLocalization.Translate("Capture.Region.CurrentScreen"));

        currentScreenButton.PerformClick();

        var expectedSelection = Rectangle.Intersect(
            new Rectangle(
                cursorScreenBounds.X - virtualBounds.X,
                cursorScreenBounds.Y - virtualBounds.Y,
                cursorScreenBounds.Width,
                cursorScreenBounds.Height),
            new Rectangle(Point.Empty, bitmap.Size));
        Assert.Equal(expectedSelection, selector.SelectedRegion);
        Assert.Equal(RegionSelectionAction.Capture, selector.SelectedAction);
    }

    [Theory]
    [InlineData("pt-BR", true, "Capturar tela inteira", "Capturar tela atual", "Capturar tela 1")]
    [InlineData("pt-BR", false, "Capturar tela inteira", "Capturar tela atual", "Capturar tela 1")]
    [InlineData("en-US", true, "Capture full screen", "Capture current screen", "Capture screen 1")]
    [InlineData("en-US", false, "Capture full screen", "Capture current screen", "Capture screen 1")]
    public void ScreenSelectionTooltipsAreLocalizedRegardlessOfLabels(string language, bool showToolLabels, string fullScreenTooltip, string currentScreenTooltip, string numberedScreenTooltip)
    {
        var previousCulture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(language);
            using var bitmap = new Bitmap(4, 4);
            using var selector = new RegionSelectorForm(bitmap, showToolLabels: showToolLabels);
            var cursorScreenBounds = (Rectangle)typeof(RegionSelectorForm)
                .GetField("_screenUnderCursorAtStart", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(selector)!;
            var screens = new[] { new Rectangle(cursorScreenBounds.Right + 100, cursorScreenBounds.Top, 100, 100), cursorScreenBounds };
            using var bar = (ToolStrip)typeof(RegionSelectorForm)
                .GetMethod("CreateScreenSelectionBar", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(selector, new object[] { screens })!;

            Assert.Equal(new[] { fullScreenTooltip, currentScreenTooltip, numberedScreenTooltip },
                bar.Items.OfType<ToolStripButton>().Select(button => button.ToolTipText));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NumberedScreenButtonsRenderIconAndTextAsACenteredGroup(bool showToolLabels)
    {
        using var bitmap = new Bitmap(4, 4);
        using var selector = new RegionSelectorForm(bitmap, showToolLabels: showToolLabels);
        var screens = new[] { new Rectangle(100000, 0, 100, 100), new Rectangle(100100, 0, 100, 100) };
        using var bar = (ToolStrip)typeof(RegionSelectorForm)
            .GetMethod("CreateScreenSelectionBar", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(selector, new object[] { screens })!;
        selector.Controls.Add(bar);
        selector.Show();
        var imageBounds = new Dictionary<ToolStripItem, Rectangle>();
        var textBounds = new Dictionary<ToolStripItem, Rectangle>();
        bar.Renderer.RenderItemImage += (_, args) => imageBounds[args.Item] = args.ImageRectangle;
        bar.Renderer.RenderItemText += (_, args) =>
        {
            textBounds[args.Item] = args.TextRectangle;
            Assert.True(args.TextFormat.HasFlag(TextFormatFlags.NoPadding));
        };
        using var rendered = new Bitmap(bar.Width, bar.Height);
        bar.DrawToBitmap(rendered, new Rectangle(Point.Empty, rendered.Size));

        foreach (var button in bar.Items.OfType<ToolStripButton>().Skip(2))
        {
            Assert.True(imageBounds.ContainsKey(button));
            Assert.True(textBounds.ContainsKey(button));
            var content = Rectangle.Union(imageBounds[button], textBounds[button]);
            Assert.InRange(Math.Abs(content.Left - (button.Width - content.Right)), 0, 1);
            Assert.InRange(Math.Abs(imageBounds[button].Top - (button.Height - imageBounds[button].Bottom)), 0, 1);
            Assert.InRange(Math.Abs(textBounds[button].Top - (button.Height - textBounds[button].Bottom)), 0, 1);
            Assert.True(textBounds[button].Left > imageBounds[button].Right);
        }
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

        foreach (var menuItem in maskDropdownButton.DropDownItems
            .OfType<ToolStripMenuItem>()
            .Where(item => item.Tag is MaskMode))
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

        Assert.Equal(6, dropdownButtons.Length);
        Assert.All(dropdownButtons, button => Assert.Equal(14, button.Width));
        Assert.All(dropdownButtons, button => Assert.Equal(34, button.Height));
        var splitButtons = dropdownButtons
            .Select(button => (ToolStripButton)actionBar.Items[actionBar.Items.IndexOf(button) - 1]);
        Assert.All(splitButtons, button => Assert.Equal(34, button.Height));
        Assert.All(splitButtons, button => Assert.False(button.AutoSize));
    }

    [Fact]
    public void FloatingToolbarOffersEditorConfigurationsForAllAnnotationTools()
    {
        using var bitmap = new Bitmap(20, 20);
        using var selector = new RegionSelectorForm(bitmap);
        var drawingDropdown = GetField<ToolStripDropDownButton>(selector, "_drawingDropdownButton");
        var highlightDropdown = GetField<ToolStripDropDownButton>(selector, "_highlightDropdownButton");
        var maskDropdown = GetField<ToolStripDropDownButton>(selector, "_maskDropdownButton");
        var counterDropdown = GetField<ToolStripDropDownButton>(selector, "_counterDropdownButton");
        var textDropdown = GetField<ToolStripDropDownButton>(selector, "_textDropdownButton");

        Assert.Contains(drawingDropdown.DropDownItems.OfType<ToolStripMenuItem>(), item => item.Text == AppLocalization.Translate("Editor.LineColor"));
        Assert.Contains(drawingDropdown.DropDownItems.OfType<ToolStripMenuItem>(), item => item.Text == AppLocalization.Translate("Editor.FillColor"));
        Assert.Contains(drawingDropdown.DropDownItems.OfType<ToolStripMenuItem>(), item => item.Text == AppLocalization.Translate("Editor.NoFill"));

        Assert.Equal(new[] { 4, 8, 16, 32 }, highlightDropdown.DropDownItems.OfType<ToolStripMenuItem>()
            .Where(item => item.Tag is int)
            .Select(item => (int)item.Tag!));
        Assert.Contains(highlightDropdown.DropDownItems.OfType<ToolStripMenuItem>(), item => item.Tag?.ToString() == "custom-size");
        Assert.NotNull(GetField<ToolStripMenuItem>(selector, "_highlightColorMenuItem").Image);
        Assert.Equal(2, maskDropdown.DropDownItems.OfType<ToolStripMenuItem>().Count(item => item.Tag is MaskMode));
        Assert.Equal(new[] { 4, 6, 8, 12, 16, 24, 32 }, GetField<ToolStripMenuItem>(selector, "_maskPixelSizeMenuItem")
            .DropDownItems.OfType<ToolStripMenuItem>()
            .Where(item => item.Tag is int)
            .Select(item => (int)item.Tag!));
        Assert.NotNull(GetField<ToolStripMenuItem>(selector, "_maskColorMenuItem").Image);

        var counterSizeMenu = GetField<ToolStripMenuItem>(selector, "_counterSizeMenuItem");
        Assert.Equal(new[] { 24, 32, 40, 48, 64, 80 }, counterSizeMenu.DropDownItems.OfType<ToolStripMenuItem>()
            .Where(item => item.Tag is int)
            .Select(item => (int)item.Tag!));
        Assert.NotNull(GetField<ToolStripMenuItem>(selector, "_counterTextColorMenuItem").Image);
        Assert.NotNull(GetField<ToolStripMenuItem>(selector, "_counterCircleColorMenuItem").Image);
        Assert.Contains(counterDropdown.DropDownItems.OfType<ToolStripMenuItem>(), item => item == counterSizeMenu);

        var textFontSizeMenu = GetField<ToolStripMenuItem>(selector, "_textFontSizeMenuItem");
        Assert.Equal(new[] { 8F, 10F, 12F, 14F, 16F, 18F, 24F, 32F, 48F, 72F }, textFontSizeMenu.DropDownItems.OfType<ToolStripMenuItem>()
            .Where(item => item.Tag is float)
            .Select(item => (float)item.Tag!));
        Assert.Contains(textDropdown.DropDownItems.OfType<ToolStripMenuItem>(), item => item.Text == AppLocalization.Translate("Editor.Font"));
        Assert.NotNull(GetField<ToolStripMenuItem>(selector, "_textColorMenuItem").Image);
        Assert.Equal(4, textDropdown.DropDownItems.OfType<ToolStripMenuItem>().Count(item => item.Tag is FontStyle));

        var maskSizeSeparator = GetField<ToolStripSeparator>(selector, "_maskPixelSizeSeparator");
        var maskColorSeparator = GetField<ToolStripSeparator>(selector, "_maskColorSeparator");
        var actionBar = GetField<ToolStrip>(selector, "_actionBar");
        selector.Show();
        actionBar.Visible = true;
        System.Windows.Forms.Application.DoEvents();
        maskDropdown.DropDown.Show(
            maskDropdown.Owner!,
            new Point(maskDropdown.Bounds.Left, maskDropdown.Bounds.Bottom));
        System.Windows.Forms.Application.DoEvents();
        Assert.True(maskSizeSeparator.Visible);
        Assert.False(maskColorSeparator.Visible);
        maskDropdown.DropDown.Close();

        var solidMaskMode = Assert.Single(maskDropdown.DropDownItems.OfType<ToolStripMenuItem>(), item => item.Tag is MaskMode mode && mode == MaskMode.Solid);
        solidMaskMode.PerformClick();
        maskDropdown.DropDown.Show(
            maskDropdown.Owner!,
            new Point(maskDropdown.Bounds.Left, maskDropdown.Bounds.Bottom));
        System.Windows.Forms.Application.DoEvents();
        Assert.False(maskSizeSeparator.Visible);
        Assert.True(maskColorSeparator.Visible);
        maskDropdown.DropDown.Close();

        static T GetField<T>(RegionSelectorForm form, string fieldName) =>
            (T)typeof(RegionSelectorForm)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(form)!;
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

    [Fact]
    public void ActionBarStaysInsideMonitorBoundsNearBottomEdge()
    {
        using var background = new Bitmap(100, 100);
        using var selector = new RegionSelectorForm(background);
        selector.Show();
        var screenBounds = selector.RectangleToClient(Screen.FromPoint(Cursor.Position).Bounds);
        var selection = new Rectangle(screenBounds.Left + 20, screenBounds.Bottom - 30, 40, 30);
        typeof(RegionSelectorForm)
            .GetField("_selection", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(selector, selection);
        var actionBar = (ToolStrip)typeof(RegionSelectorForm)
            .GetField("_actionBar", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!;

        typeof(RegionSelectorForm)
            .GetMethod("UpdateActionBarLocation", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(selector, null);

        Assert.True(actionBar.Left >= screenBounds.Left);
        Assert.True(actionBar.Right <= screenBounds.Right);
        Assert.True(actionBar.Top >= screenBounds.Top);
        Assert.True(actionBar.Bottom <= screenBounds.Bottom);
        Assert.True(actionBar.Bottom <= selection.Top);
    }

    [Fact]
    public void RightButtonDragMovesSelectionAndRestoresActionBar()
    {
        using var background = new Bitmap(100, 100);
        using var selector = new RegionSelectorForm(background);
        selector.Show();
        var annotationToolField = typeof(RegionSelectorForm)
            .GetField("_annotationTool", BindingFlags.Instance | BindingFlags.NonPublic)!;
        annotationToolField.SetValue(selector, Enum.Parse(annotationToolField.FieldType, "None"));
        var selectionField = typeof(RegionSelectorForm)
            .GetField("_selection", BindingFlags.Instance | BindingFlags.NonPublic)!;
        selectionField.SetValue(selector, new Rectangle(20, 20, 40, 40));
        var actionBar = (ToolStrip)typeof(RegionSelectorForm)
            .GetField("_actionBar", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!;
        actionBar.Visible = true;
        var mouseDown = typeof(RegionSelectorForm).GetMethod("OnMouseDown", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var mouseMove = typeof(RegionSelectorForm).GetMethod("OnMouseMove", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var mouseUp = typeof(RegionSelectorForm).GetMethod("OnMouseUp", BindingFlags.Instance | BindingFlags.NonPublic)!;

        mouseDown.Invoke(selector, [new MouseEventArgs(MouseButtons.Right, 1, 40, 40, 0)]);
        Assert.True((bool)typeof(RegionSelectorForm)
            .GetField("_isMoving", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!);
        Assert.False(actionBar.Visible);

        mouseMove.Invoke(selector, [new MouseEventArgs(MouseButtons.Right, 1, 50, 50, 0)]);
        Assert.Equal(new Rectangle(30, 30, 40, 40), (Rectangle)selectionField.GetValue(selector)!);

        mouseUp.Invoke(selector, [new MouseEventArgs(MouseButtons.Right, 1, 50, 50, 0)]);

        Assert.False((bool)typeof(RegionSelectorForm)
            .GetField("_isMoving", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(selector)!);
        Assert.True(actionBar.Visible);
    }
}