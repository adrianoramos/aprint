using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Aprint.Features.Capture;
using Aprint.Features.Settings;
using Aprint.Infrastructure.Images;

namespace Aprint.Features.Editor;

/// <summary>Janela de edição simples exibida após a captura, permitindo desenhar sobre a imagem antes de copiar/salvar.</summary>
public sealed class EditorForm : Form
{
    private sealed class EditorActivityMessageFilter(EditorForm editor, System.Windows.Forms.Timer timer) : IMessageFilter
    {
        public bool PreFilterMessage(ref Message message)
        {
            if (!IsActivityMessage(message.Msg))
            {
                return false;
            }

            var target = Control.FromHandle(message.HWnd) ?? Control.FromChildHandle(message.HWnd);
            if (target == editor || target?.FindForm() == editor)
            {
                timer.Stop();
                timer.Start();
            }

            return false;
        }

        private static bool IsActivityMessage(int message) => message is
            0x0100 or 0x0101 or 0x0102 or 0x0104 or 0x0105 or 0x0106 or
            0x0200 or 0x0201 or 0x0202 or 0x0204 or 0x0205 or 0x0207 or
            0x0208 or 0x020A or 0x020B or 0x020C or 0x020E;
    }

    private sealed class PopupDismissMessageFilter(params ToolStripDropDownButton[] buttons) : IMessageFilter
    {
        public bool PreFilterMessage(ref Message message)
        {
            if (message.Msg is not (0x0201 or 0x0204 or 0x0207))
            {
                return false;
            }

            var cursorPosition = Control.MousePosition;
            var clickedButton = buttons.FirstOrDefault(button =>
                button.Owner is not null &&
                button.Owner.RectangleToScreen(button.Bounds).Contains(cursorPosition));

            if (clickedButton?.DropDown.Visible == true)
            {
                clickedButton.DropDown.Close();
                return true;
            }

            foreach (var button in buttons)
            {
                if (button.DropDown.Visible && !button.DropDown.Bounds.Contains(cursorPosition))
                {
                    button.DropDown.Close();
                }
            }

            return false;
        }
    }

    private sealed class EditorToolStripRenderer() : ToolStripProfessionalRenderer(ToolStripColorPalette.CreateColorTable())
    {
        private ToolStripItem? _activeMainButton;
        private ToolStripItem? _activeDropdownButton;
        private ToolStripItem? _hoveredMainButton;
        private ToolStripItem? _hoveredDropdownButton;

        public bool SetActiveGroup(ToolStripItem? mainButton, ToolStripItem? dropdownButton)
        {
            if (ReferenceEquals(_activeMainButton, mainButton) && ReferenceEquals(_activeDropdownButton, dropdownButton))
            {
                return false;
            }

            _activeMainButton = mainButton;
            _activeDropdownButton = dropdownButton;
            return true;
        }

        public bool SetHoveredGroup(ToolStripItem? mainButton, ToolStripItem? dropdownButton)
        {
            if (ReferenceEquals(_hoveredMainButton, mainButton) && ReferenceEquals(_hoveredDropdownButton, dropdownButton))
            {
                return false;
            }

            _hoveredMainButton = mainButton;
            _hoveredDropdownButton = dropdownButton;
            return true;
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (IsActive(e.Item) || e.Item.Selected || e.Item.Pressed)
            {
                DrawBackground(e, omitRightBorder: ReferenceEquals(e.Item, _activeMainButton) || ReferenceEquals(e.Item, _hoveredMainButton));
                return;
            }

            base.OnRenderButtonBackground(e);
        }

        protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (IsActive(e.Item) || e.Item.Selected || e.Item.Pressed)
            {
                DrawBackground(e);
                return;
            }

            base.OnRenderDropDownButtonBackground(e);
        }

        private bool IsActive(ToolStripItem item) =>
            ReferenceEquals(item, _activeMainButton) ||
            ReferenceEquals(item, _activeDropdownButton) ||
            ReferenceEquals(item, _hoveredMainButton) ||
            ReferenceEquals(item, _hoveredDropdownButton) ||
            item is ToolStripButton { Checked: true };

        private static void DrawBackground(ToolStripItemRenderEventArgs e, bool omitRightBorder = false)
        {
            var bounds = new Rectangle(Point.Empty, e.Item.Size);
            using var fill = new SolidBrush(ToolStripColorPalette.ActiveBackground);
            using var border = new Pen(ToolStripColorPalette.ActiveBorder);
            e.Graphics.FillRectangle(fill, bounds);
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            var right = bounds.Right - 1;
            var bottom = bounds.Bottom - 1;
            e.Graphics.DrawLine(border, bounds.Left, bounds.Top, right, bounds.Top);
            e.Graphics.DrawLine(border, bounds.Left, bottom, right, bottom);
            e.Graphics.DrawLine(border, bounds.Left, bounds.Top, bounds.Left, bottom);
            if (!omitRightBorder)
            {
                e.Graphics.DrawLine(border, right, bounds.Top, right, bottom);
            }
        }
    }

    private enum ToolIconKind
    {
        LineColor,
        TextColor,
        FillColor,
        Freehand,
        Line,
        Arrow,
        Rectangle,
        Circle,
        Counter,
        Highlight,
        Text,
        Crop,
        Confirm,
        Cancel,
        Filled,
        Pixelated,
        AutomaticMask,
        Undo,
        Redo,
        Copy,
        Save,
        NewCapture,
    }

    private const int PenWidth = ImageEditingService.AnnotationPenWidth;
    private const int ToolPanelHeight = 50;
    private const int ToolIconBaseSize = 24;
    private const int ToolButtonPadding = 4;
    private const int ToolButtonMargin = 2;
    private const int ToolSeparatorMargin = 4;
    private const int ToolButtonSize = 32;
    private const int CropHandleSize = 8;

    [Flags]
    private enum CropEdge { None = 0, Left = 1, Top = 2, Right = 4, Bottom = 8 }

    private sealed class MaskRegion(Rectangle bounds)
    {
        public Rectangle Bounds { get; } = bounds;

        public bool ShouldMask { get; set; } = true;
    }

    private Bitmap _workingBitmap;
    private readonly List<MaskRegion> _maskRegions = [];
    private readonly ToolTip _automaticMaskToolTip = new();
    private Size _maskDetectionImageSize;
    private int _textRegionDetectionGeneration;
    private bool _textRegionDetectionReady;
    private bool _textRegionDetectionInProgress;
    private bool _isAutomaticMaskSelectionActive;
    private bool _isAutomaticMaskRevealActive;
    private string _initialFolder;
    private readonly bool _closeAfterCopyOrSave;
    private readonly bool _saveWithoutPrompt;
    private readonly bool _showWithoutActivation;
    private readonly string _automaticSavePath;
    private readonly ImageList _toolIconImageList;
    private readonly int _toolIconSize;
    private readonly Dictionary<string, Bitmap> _toolIconBitmaps = [];
    private readonly Dictionary<ToolIconKind, int> _colorizedToolIconColors = [];
    private readonly PictureBox _pictureBox;
    private readonly ToolStripButton _btnDrawing;
    private readonly ToolStripDropDownButton _btnDrawingDropdown;
    private readonly ToolStripButton _btnCounter;
    private readonly ToolStripButton _btnHighlight;
    private readonly ToolStripDropDownButton _btnHighlightSizeDropdown;
    private readonly ToolStripButton _btnText;
    private readonly ToolStripButton _btnCrop;
    private readonly ToolStripButton _btnConfirmCrop;
    private readonly ToolStripButton _btnCancelCrop;
    private readonly ToolStripDropDownButton _btnTextFormatDropdown;
    private readonly ToolStripButton _btnMask;
    private readonly ToolStripDropDownButton _btnMaskModeDropdown;
    private readonly ToolStripMenuItem _filledModeMenuItem;
    private readonly ToolStripMenuItem _pixelatedModeMenuItem;
    private readonly ToolStripSeparator _pixelSizeSeparator;
    private readonly ToolStripSeparator _automaticMaskSeparator;
    private readonly ToolStripMenuItem _automaticMaskMenuItem;
    private readonly ToolStripControlHost _pixelSizeControlHost;
    private readonly ToolStripButton _btnColor;
    private readonly ToolStripButton _btnTextColor;
    private readonly ToolStripButton _btnFillColor;
    private readonly ToolStripDropDownButton _btnFillColorDropdown;
    private readonly ToolStripButton _btnUndo;
    private readonly ToolStripButton _btnRedo;
    private readonly ToolStripLabel _centerToolsSpacer;
    private readonly EditorToolStripRenderer _toolStripRenderer = new();
    private readonly ToolStripLabel _actionSpacerBefore;
    private readonly ToolStripLabel _actionSpacerAfter;
    private readonly EditHistory _editHistory;
    private Font _textFont = new("Segoe UI", 12F);
    private readonly List<Font> _textFormatButtonFonts = [];
    private EditTool _selectedDrawingTool = EditTool.Freehand;
    private readonly ToolTip _textFormatToolTip = new();
    private readonly System.Windows.Forms.Timer _textCaretTimer = new() { Interval = 500 };
    private readonly System.Windows.Forms.Timer? _inactivityTimer;
    private readonly IMessageFilter? _activityMessageFilter;

    private Color _selectedColor = Color.Red;
    private Color _selectedTextColor = Color.Red;
    private Color? _selectedFillColor;
    private EditTool _currentTool = EditTool.Freehand;
    private EditTool _toolBeforeCrop = EditTool.Freehand;
    private MaskMode _maskMode = MaskMode.Pixelate;
    private int _highlightWidth = 18;
    private int _maskPixelSize = 6;
    private int _nextCounterValue = 1;
    private bool _isDrawing;
    private Point _dragStart;
    private Point _lastFreehandPoint;
    private readonly List<Point> _highlighterPoints = [];
    private Rectangle _textBounds;
    private int _textMinimumWidth;
    private string _textBuffer = string.Empty;
    private string _textFontFamily = "Segoe UI";
    private float _textFontSize = 12F;
    private FontStyle _textFontStyle = FontStyle.Regular;
    private bool _isEditingText;
    private bool _isTextSelected;
    private bool _isTextFormatMenuOpen;
    private bool _textCaretVisible;
    private Rectangle _lastPreviewDirtyRect;
    private Point _shapePreviewEnd;
    private bool _shapePreviewVisible;
    private bool _isDrawingManualMaskRegion;
    private Point _manualMaskStart;
    private Point _manualMaskEnd;
    private Rectangle _cropBounds;
    private CropEdge _cropDragEdge;
    private Point _cropDragStart;
    private Rectangle _cropDragBounds;

    public event Action<string>? Saved;
    public event Action? Copied;
    public event Action<MaskMode>? MaskModeChanged;
    public event Action<Color>? LineColorChanged;
    public event Action<Color>? TextColorChanged;
    public event Action<Color?>? FillColorChanged;
    public event Action<int>? MaskPixelSizeChanged;
    public event Action? NewCaptureRequested;
    public event Action<string, float, FontStyle>? TextFormattingChanged;
    public event Action<int>? HighlightWidthChanged;

    public EditorForm(Bitmap bitmap, string initialFolder, MaskMode maskMode = MaskMode.Pixelate, bool showToolLabels = true, bool closeAfterCopyOrSave = false, EditTool initialTool = EditTool.Freehand, bool saveWithoutPrompt = false, bool autoCloseWhenInactive = false, int inactivityTimeoutSeconds = 30, string textFontFamily = "Segoe UI", float textFontSize = 12F, FontStyle textFontStyle = FontStyle.Regular, int highlightWidth = 18, int lineColorArgb = -65536, int textColorArgb = -65536, int? fillColorArgb = null, int maskPixelSize = 6, bool showWithoutActivation = false)
    {
        _workingBitmap = (Bitmap)bitmap.Clone();
        _showWithoutActivation = showWithoutActivation;
        _selectedColor = Color.FromArgb(lineColorArgb);
        _selectedTextColor = Color.FromArgb(textColorArgb);
        _selectedFillColor = fillColorArgb.HasValue ? Color.FromArgb(fillColorArgb.Value) : null;
        _maskPixelSize = Math.Clamp(maskPixelSize, 4, 32);
        _highlightWidth = Math.Clamp(highlightWidth, 4, 48);
        var installedFontFamilies = FontFamily.Families;
        _textFontFamily = installedFontFamilies
            .Select(fontFamily => fontFamily.Name)
            .FirstOrDefault(fontFamilyName => string.Equals(fontFamilyName, textFontFamily, StringComparison.OrdinalIgnoreCase))
            ?? "Segoe UI";
        foreach (var fontFamily in installedFontFamilies)
        {
            fontFamily.Dispose();
        }

        _textFontSize = float.IsFinite(textFontSize) ? Math.Clamp(textFontSize, 6F, 72F) : 12F;
        const FontStyle supportedTextStyles = FontStyle.Bold | FontStyle.Italic | FontStyle.Underline | FontStyle.Strikeout;
        _textFontStyle = textFontStyle & supportedTextStyles;
        _textFont.Dispose();
        _textFont = new Font(_textFontFamily, _textFontSize, _textFontStyle, GraphicsUnit.Point);
        _closeAfterCopyOrSave = closeAfterCopyOrSave;
        _saveWithoutPrompt = saveWithoutPrompt;
        if (autoCloseWhenInactive)
        {
            var timeoutSeconds = inactivityTimeoutSeconds is 60 or 180 or 300 ? inactivityTimeoutSeconds : 30;
            var inactivityTimer = new System.Windows.Forms.Timer { Interval = timeoutSeconds * 1000 };
            inactivityTimer.Tick += (_, _) => Close();
            _inactivityTimer = inactivityTimer;
            _activityMessageFilter = new EditorActivityMessageFilter(this, inactivityTimer);
            Shown += (_, _) =>
            {
                System.Windows.Forms.Application.AddMessageFilter(_activityMessageFilter);
                inactivityTimer.Start();
            };
            FormClosed += (_, _) => System.Windows.Forms.Application.RemoveMessageFilter(_activityMessageFilter);
        }
        _editHistory = new EditHistory(_workingBitmap, _nextCounterValue);
        _initialFolder = initialFolder;
        var saveFolder = string.IsNullOrWhiteSpace(_initialFolder)
            ? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            : _initialFolder;
        _automaticSavePath = Path.Combine(saveFolder, $"Aprint_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png");
        _maskMode = maskMode;
        _toolIconSize = GetToolIconSizeForDpi(DeviceDpi);
        _toolIconImageList = CreateToolIconImageList(_toolIconSize);

        Text = AppLocalization.Translate("Editor.FormTitle");
        Icon = Program.AppIcon;
        KeyPreview = true;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = true;
        MinimizeBox = true;

        // A janela nasce do tamanho exato da captura (limitada apenas pela área útil da tela).
        var workingArea = Screen.FromPoint(Cursor.Position).WorkingArea;
        var desiredWidth = _workingBitmap.Width;
        var desiredHeight = _workingBitmap.Height + ToolPanelHeight;
        ClientSize = new Size(Math.Min(desiredWidth, workingArea.Width), Math.Min(desiredHeight, workingArea.Height));

        // Se a captura não couber na área útil da tela, abre já maximizada.
        if (desiredWidth > workingArea.Width || desiredHeight > workingArea.Height)
        {
            WindowState = FormWindowState.Maximized;
        }

        var toolStrip = new ToolStrip
        {
            Dock = DockStyle.Top,
            GripStyle = ToolStripGripStyle.Hidden,
            AutoSize = false,
            Height = ToolPanelHeight,
            ShowItemToolTips = true,
            Renderer = _toolStripRenderer,
            Font = new Font("Segoe UI", 9F),
            BackColor = ToolStripColorPalette.BarBackground,
            ForeColor = ToolStripColorPalette.ButtonForeground,
            Padding = new Padding(ToolButtonPadding, ToolButtonPadding, ToolButtonPadding, ToolButtonPadding),
            ImageList = _toolIconImageList,
        };

        _btnColor = CreateStandardToolButton(
            AppLocalization.Translate("Editor.LineColor"),
            ToolIconKind.LineColor,
            _selectedColor);
        UpdateColorButtonAppearance();
        _btnColor.Click += (_, _) => PickColor();

        _btnTextColor = CreateStandardToolButton(
            AppLocalization.Translate("Editor.TextColor"),
            ToolIconKind.TextColor,
            _selectedTextColor);
        UpdateTextColorButtonAppearance();
        _btnTextColor.Click += (_, _) => PickTextColor();

        _btnFillColor = CreateStandardToolButton(
            AppLocalization.Translate("Editor.FillColor"),
            ToolIconKind.FillColor,
            Color.Black);
        _btnFillColor.Margin = new Padding(ToolButtonMargin, 0, 0, 0);
        _btnFillColor.Click += (_, _) => PickFillColor();
        _btnFillColorDropdown = new ToolStripDropDownButton
        {
            ImageAlign = ContentAlignment.MiddleCenter,
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            ImageKey = EnsureToolIconKey(ToolIconKind.FillColor, Color.Black),
            ImageScaling = ToolStripItemImageScaling.None,
            Margin = new Padding(0, 0, ToolButtonMargin, 0),
            AutoSize = false,
            Width = ToolStripMetrics.DropdownButtonWidth,
            Height = ToolStripMetrics.DropdownButtonHeight,
            AutoToolTip = false,
            ToolTipText = AppLocalization.Translate("Editor.FillColor"),
        };
        _btnFillColorDropdown.DropDownItems.Add(AppLocalization.Translate("Editor.NoFill"), null, (_, _) =>
        {
            _selectedFillColor = null;
            UpdateFillColorButtonAppearance();
            FillColorChanged?.Invoke(null);
        });
        UpdateFillColorButtonAppearance();

        _btnDrawing = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Freehand"),
            ToolIconKind.Freehand,
            Color.Black,
            isCheckable: true,
            isChecked: true,
            shortcut: "F2");
        _btnDrawing.Margin = new Padding(ToolButtonMargin, 0, 0, 0);
        _btnDrawing.Click += (_, _) => SetActiveTool(_selectedDrawingTool);
        _btnDrawingDropdown = new ToolStripDropDownButton
        {
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            ImageScaling = ToolStripItemImageScaling.None,
            Margin = new Padding(0, 0, ToolButtonMargin, 0),
            AutoSize = false,
            Width = ToolStripMetrics.DropdownButtonWidth,
            Height = ToolStripMetrics.DropdownButtonHeight,
            AutoToolTip = false,
            ToolTipText = AppLocalization.Translate("Capture.Region.Drawing"),
            AccessibleName = AppLocalization.Translate("Capture.Region.Drawing"),
        };
        AddDrawingToolMenuItem(EditTool.Freehand, ToolIconKind.Freehand, "Editor.Freehand", "F2");
        AddDrawingToolMenuItem(EditTool.Line, ToolIconKind.Line, "Editor.Line", "F3");
        AddDrawingToolMenuItem(EditTool.Arrow, ToolIconKind.Arrow, "Editor.Arrow", "F4");
        AddDrawingToolMenuItem(EditTool.Rectangle, ToolIconKind.Rectangle, "Editor.Rectangle", "F5");
        AddDrawingToolMenuItem(EditTool.Circle, ToolIconKind.Circle, "Editor.Circle", "F6");
        _btnCounter = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Counter"),
            ToolIconKind.Counter,
            Color.Black,
            isCheckable: true,
            shortcut: "F9");
        _btnHighlight = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Highlight"),
            ToolIconKind.Highlight,
            Color.Black,
            isCheckable: true,
            shortcut: "F7");
        _btnHighlight.Margin = new Padding(ToolButtonMargin, 0, 0, 0);
        _btnText = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Text"),
            ToolIconKind.Text,
            Color.Black,
            isCheckable: true,
            shortcut: "F10");
        _btnText.Margin = new Padding(ToolButtonMargin, 0, 0, 0);
        _btnTextFormatDropdown = new ToolStripDropDownButton
        {
            ImageAlign = ContentAlignment.MiddleCenter,
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            ImageKey = EnsureToolIconKey(ToolIconKind.Text, Color.Black),
            ImageScaling = ToolStripItemImageScaling.None,
            Margin = new Padding(0, 0, ToolButtonMargin, 0),
            AutoSize = false,
            Width = ToolStripMetrics.DropdownButtonWidth,
            Height = ToolStripMetrics.DropdownButtonHeight,
            AutoToolTip = false,
            ToolTipText = AppLocalization.Translate("Editor.TextFormatting"),
        };
        var textFormatPanel = new Panel
        {
            Size = new Size(400, 42),
            Padding = new Padding(4),
            BackColor = SystemColors.Control,
        };
        var textFontFamilyCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(4, 8),
            Size = new Size(170, 25),
        };
        var textFontFamilies = FontFamily.Families;
        var fontFamilyNames = textFontFamilies
            .Select(fontFamily => fontFamily.Name)
            .OrderBy(fontFamilyName => fontFamilyName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        foreach (var fontFamily in textFontFamilies)
        {
            fontFamily.Dispose();
        }

        textFontFamilyCombo.Items.AddRange(fontFamilyNames);
        textFontFamilyCombo.SelectedItem = _textFontFamily;
        if (textFontFamilyCombo.SelectedIndex < 0 && textFontFamilyCombo.Items.Count > 0)
        {
            textFontFamilyCombo.SelectedIndex = 0;
        }

        textFontFamilyCombo.SelectedIndexChanged += (_, _) =>
        {
            if (textFontFamilyCombo.SelectedItem is string fontFamilyName)
            {
                _textFontFamily = fontFamilyName;
                UpdateTextFont();
            }
        };
        var textFontSizeInput = new NumericUpDown
        {
            Minimum = 6,
            Maximum = 72,
            Value = (decimal)_textFontSize,
            Location = new Point(180, 8),
            Size = new Size(56, 25),
        };
        textFontSizeInput.ValueChanged += (_, _) =>
        {
            _textFontSize = (float)textFontSizeInput.Value;
            UpdateTextFont();
        };
        var textFontSizeUnit = new Label
        {
            AutoSize = false,
            Text = "pt",
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(238, 9),
            Size = new Size(20, 22),
        };
        var boldButton = CreateTextFormatButton("B", "Editor.TextBold", FontStyle.Bold, new Point(268, 8));
        var italicButton = CreateTextFormatButton("I", "Editor.TextItalic", FontStyle.Italic, new Point(300, 8));
        var underlineButton = CreateTextFormatButton("U", "Editor.TextUnderline", FontStyle.Underline, new Point(332, 8));
        var strikeoutButton = CreateTextFormatButton("S", "Editor.TextStrikeout", FontStyle.Strikeout, new Point(364, 8));
        textFormatPanel.Controls.AddRange([
            textFontFamilyCombo,
            textFontSizeInput,
            textFontSizeUnit,
            boldButton,
            italicButton,
            underlineButton,
            strikeoutButton,
        ]);
        _btnTextFormatDropdown.DropDownItems.Add(new ToolStripControlHost(textFormatPanel)
        {
            AutoSize = false,
            Size = textFormatPanel.Size,
            Margin = Padding.Empty,
        });
        _btnTextFormatDropdown.DropDown.AutoClose = false;
        _btnTextFormatDropdown.DropDownOpening += (_, _) => _isTextFormatMenuOpen = true;
        _btnTextFormatDropdown.DropDownClosed += (_, _) =>
        {
            _isTextFormatMenuOpen = false;
            if (_isEditingText)
            {
                _pictureBox?.Focus();
            }
        };
        _btnHighlightSizeDropdown = new ToolStripDropDownButton
        {
            ImageAlign = ContentAlignment.MiddleCenter,
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            ImageKey = EnsureToolIconKey(ToolIconKind.Highlight, Color.Black),
            ImageScaling = ToolStripItemImageScaling.None,
            Margin = new Padding(0, 0, ToolButtonMargin, 0),
            AutoSize = false,
            Width = ToolStripMetrics.DropdownButtonWidth,
            Height = ToolStripMetrics.DropdownButtonHeight,
            AutoToolTip = false,
            ToolTipText = AppLocalization.Translate("Editor.HighlightSize"),
        };
        var highlightSizePanel = new Panel
        {
            Size = new Size(186, 48),
            Padding = new Padding(4),
            BackColor = SystemColors.Control,
        };
        var highlightSizeSlider = new TrackBar
        {
            Minimum = 4,
            Maximum = 48,
            Value = _highlightWidth,
            TickFrequency = 4,
            SmallChange = 1,
            LargeChange = 4,
            TickStyle = TickStyle.BottomRight,
            AutoSize = false,
            Size = new Size(138, 40),
            Location = new Point(4, 4),
        };
        var highlightSizeLabel = new Label
        {
            AutoSize = false,
            Text = $"{_highlightWidth} px",
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(38, 22),
            Location = new Point(144, 13),
        };
        highlightSizeSlider.ValueChanged += (_, _) =>
        {
            _highlightWidth = highlightSizeSlider.Value;
            highlightSizeLabel.Text = $"{_highlightWidth} px";
            HighlightWidthChanged?.Invoke(_highlightWidth);
        };
        highlightSizePanel.Controls.Add(highlightSizeSlider);
        highlightSizePanel.Controls.Add(highlightSizeLabel);
        _btnHighlightSizeDropdown.DropDownItems.Add(new ToolStripControlHost(highlightSizePanel)
        {
            AutoSize = false,
            Size = highlightSizePanel.Size,
            Margin = Padding.Empty,
        });
        _btnHighlightSizeDropdown.DropDown.AutoClose = false;

        _btnMask = CreateStandardToolButton(
            AppLocalization.Translate(_maskMode == MaskMode.Solid ? "Editor.Filled" : "Editor.Pixelated"),
            _maskMode == MaskMode.Solid ? ToolIconKind.Filled : ToolIconKind.Pixelated,
            Color.Black,
            isCheckable: true,
            shortcut: "F8");
        _btnMask.Margin = new Padding(ToolButtonMargin, 0, 0, 0);
        _btnMask.Click += (_, _) => SetActiveTool(
            _maskMode == MaskMode.Solid ? EditTool.Blur : EditTool.Pixelated);

        _btnMaskModeDropdown = new ToolStripDropDownButton
        {
            ImageAlign = ContentAlignment.MiddleCenter,
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            ImageKey = EnsureToolIconKey(
                _maskMode == MaskMode.Solid ? ToolIconKind.Filled : ToolIconKind.Pixelated,
                Color.Black),
            ImageScaling = ToolStripItemImageScaling.None,
            Margin = new Padding(0, 0, ToolButtonMargin, 0),
            AutoSize = false,
            Width = ToolStripMetrics.DropdownButtonWidth,
            Height = ToolStripMetrics.DropdownButtonHeight,
            AutoToolTip = false,
            ToolTipText = AppLocalization.Translate("Editor.PixelatedSize"),
        };
        _filledModeMenuItem = new ToolStripMenuItem(AppLocalization.Translate("Editor.Filled"));
        _filledModeMenuItem.Click += (_, _) =>
        {
            SetActiveTool(EditTool.Blur);
            _btnMaskModeDropdown.DropDown.Close();
        };
        _pixelatedModeMenuItem = new ToolStripMenuItem(AppLocalization.Translate("Editor.Pixelated"));
        _pixelatedModeMenuItem.Click += (_, _) =>
        {
            SetActiveTool(EditTool.Pixelated);
            _btnMaskModeDropdown.DropDown.Close();
        };
        _pixelSizeSeparator = new ToolStripSeparator();
        _btnMaskModeDropdown.DropDownItems.AddRange([
            _filledModeMenuItem,
            _pixelatedModeMenuItem,
            _pixelSizeSeparator,
        ]);
        var pixelSizePanel = new Panel
        {
            Size = new Size(186, 48),
            Padding = new Padding(4),
            BackColor = SystemColors.Control,
        };
        var pixelSizeSlider = new TrackBar
        {
            Minimum = 4,
            Maximum = 32,
            Value = _maskPixelSize,
            TickFrequency = 4,
            SmallChange = 1,
            LargeChange = 4,
            TickStyle = TickStyle.BottomRight,
            AutoSize = false,
            Size = new Size(138, 40),
            Location = new Point(4, 4),
        };
        var pixelSizeLabel = new Label
        {
            AutoSize = false,
            Text = $"{_maskPixelSize} px",
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(38, 22),
            Location = new Point(144, 13),
        };
        pixelSizeSlider.ValueChanged += (_, _) =>
        {
            _maskPixelSize = pixelSizeSlider.Value;
            pixelSizeLabel.Text = $"{_maskPixelSize} px";
            MaskPixelSizeChanged?.Invoke(_maskPixelSize);
        };
        pixelSizePanel.Controls.Add(pixelSizeSlider);
        pixelSizePanel.Controls.Add(pixelSizeLabel);
        _pixelSizeControlHost = new ToolStripControlHost(pixelSizePanel)
        {
            AutoSize = false,
            Size = pixelSizePanel.Size,
            Margin = Padding.Empty,
        };
        _automaticMaskSeparator = new ToolStripSeparator();
        _automaticMaskMenuItem = new ToolStripMenuItem(
            AppLocalization.Translate("Editor.AutomaticMask"),
            _toolIconImageList.Images[EnsureToolIconKey(ToolIconKind.AutomaticMask, Color.Black)])
        {
            ToolTipText = AppLocalization.Translate("Editor.AutomaticMask"),
        };
        _automaticMaskMenuItem.Click += async (_, _) =>
        {
            _btnMaskModeDropdown.DropDown.Close();
            await StartAutomaticMaskAsync();
        };
        _btnMaskModeDropdown.DropDownItems.AddRange([
            _pixelSizeControlHost,
            _automaticMaskSeparator,
            _automaticMaskMenuItem,
        ]);
        _btnMaskModeDropdown.DropDown.AutoClose = false;
        var popupDismissMessageFilter = new PopupDismissMessageFilter(
            _btnTextFormatDropdown,
            _btnHighlightSizeDropdown,
            _btnMaskModeDropdown);
        Shown += (_, _) => System.Windows.Forms.Application.AddMessageFilter(popupDismissMessageFilter);
        FormClosed += (_, _) => System.Windows.Forms.Application.RemoveMessageFilter(popupDismissMessageFilter);

        _btnCounter.Click += (_, _) => SetActiveTool(EditTool.Counter);
        _btnHighlight.Click += (_, _) => SetActiveTool(EditTool.Highlight);
        _btnText.Click += (_, _) => SetActiveTool(EditTool.Text);

        _btnCrop = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Crop"),
            ToolIconKind.Crop,
            Color.Black,
            isCheckable: true,
            shortcut: "F11");
        _btnCrop.Click += (_, _) => SetActiveTool(EditTool.Crop);

        _btnConfirmCrop = CreateStandardToolButton(
            AppLocalization.Translate("Editor.ConfirmCrop"),
            ToolIconKind.Confirm,
            Color.Black,
            shortcut: "Enter");
        _btnConfirmCrop.Visible = false;
        _btnConfirmCrop.Click += (_, _) =>
        {
            if (_isAutomaticMaskSelectionActive)
            {
                ApplyAutomaticMask();
            }
            else
            {
                ConfirmCrop();
            }
        };

        _btnCancelCrop = CreateStandardToolButton(
            AppLocalization.Translate("Editor.CancelCrop"),
            ToolIconKind.Cancel,
            Color.Black,
            shortcut: "Escape");
        _btnCancelCrop.Visible = false;
        _btnCancelCrop.Click += (_, _) =>
        {
            if (_isAutomaticMaskSelectionActive)
            {
                CancelAutomaticMask();
            }
            else
            {
                SetActiveTool(_toolBeforeCrop);
            }
        };

        _actionSpacerBefore = new ToolStripLabel
        {
            AutoSize = false,
            Visible = false,
            BackColor = ToolStripColorPalette.BarBackground,
            ForeColor = ToolStripColorPalette.ButtonForeground,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            Width = 0,
        };
        _actionSpacerAfter = new ToolStripLabel
        {
            AutoSize = false,
            Visible = false,
            BackColor = ToolStripColorPalette.BarBackground,
            ForeColor = ToolStripColorPalette.ButtonForeground,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            Width = 0,
        };

        _btnUndo = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Undo"),
            ToolIconKind.Undo,
            Color.Black,
            shortcut: "Ctrl+Z");
        _btnUndo.Click += (_, _) => UndoLastEdit();

        _btnRedo = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Redo"),
            ToolIconKind.Redo,
            Color.Black,
            shortcut: "Ctrl+Y");
        _btnRedo.Click += (_, _) => RedoLastEdit();

        var btnCopy = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Copy"),
            ToolIconKind.Copy,
            Color.Black,
            shortcut: "Ctrl+C");
        btnCopy.Click += (_, _) => CopyToClipboard();

        var btnNewCapture = CreateStandardToolButton(
            AppLocalization.Translate("Editor.NewCapture"),
            ToolIconKind.NewCapture,
            Color.Black,
            shortcut: "Ctrl+N");
        btnNewCapture.Click += (_, _) =>
        {
            NewCaptureRequested?.Invoke();
            Close();
        };

        var btnSaveAs = CreateStandardToolButton(
            AppLocalization.Translate("Editor.SaveAs"),
            ToolIconKind.Save,
            Color.Black,
            shortcut: "Ctrl+S");
        btnSaveAs.Click += async (_, _) => await SaveAsAsync();

        var copyGroupSeparator = CreateToolStripSeparator();
        copyGroupSeparator.Alignment = ToolStripItemAlignment.Right;
        foreach (var actionButton in new[] { btnCopy, btnSaveAs })
        {
            actionButton.Alignment = ToolStripItemAlignment.Right;
        }
        _centerToolsSpacer = new ToolStripLabel
        {
            AutoSize = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            Width = 0,
        };
        toolStrip.Items.AddRange([
            btnNewCapture,
            CreateToolStripSeparator(),
            _centerToolsSpacer,
            _btnColor,
            _btnTextColor,
            _btnFillColor,
            _btnFillColorDropdown,
            CreateToolStripSeparator(),
            _btnDrawing,
            _btnDrawingDropdown,
            _btnHighlight,
            _btnHighlightSizeDropdown,
            _btnMask,
            _btnMaskModeDropdown,
            _btnCounter,
            _btnText,
            _btnTextFormatDropdown,
            _btnCrop,
            _btnUndo,
            _btnRedo,
            _actionSpacerBefore,
            _btnConfirmCrop,
            _btnCancelCrop,
            _actionSpacerAfter,
            btnSaveAs,
            btnCopy,
            copyGroupSeparator,
        ]);

        TrackToolGroupHover(toolStrip, _btnDrawing, _btnDrawingDropdown);
        TrackToolGroupHover(toolStrip, _btnFillColor, _btnFillColorDropdown);
        TrackToolGroupHover(toolStrip, _btnHighlight, _btnHighlightSizeDropdown);
        TrackToolGroupHover(toolStrip, _btnText, _btnTextFormatDropdown);
        TrackToolGroupHover(toolStrip, _btnMask, _btnMaskModeDropdown);

        foreach (ToolStripItem item in toolStrip.Items)
        {
            if (item is ToolStripButton or ToolStripDropDownButton)
            {
                item.BackColor = ToolStripColorPalette.ButtonBackground;
                item.ForeColor = ToolStripColorPalette.ButtonForeground;
            }
        }

        var scrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        scrollPanel.Resize += (_, _) => CenterPictureBox(scrollPanel);

        _pictureBox = new PictureBox
        {
            Image = _workingBitmap,
            SizeMode = PictureBoxSizeMode.AutoSize,
            Cursor = Cursors.Cross,
            TabStop = true,
        };
        _pictureBox.MouseDown += PictureBox_MouseDown;
        _pictureBox.MouseMove += PictureBox_MouseMove;
        _pictureBox.MouseUp += PictureBox_MouseUp;
        _pictureBox.Paint += PictureBox_Paint;
        _pictureBox.LostFocus += (_, _) =>
        {
            if (_isEditingText && !_isTextFormatMenuOpen)
            {
                CommitTextEdit();
            }
        };
        _textCaretTimer.Tick += (_, _) =>
        {
            _textCaretVisible = !_textCaretVisible;
            if (_isEditingText)
            {
                _pictureBox.Invalidate(InflateRectangle(_textBounds, 2));
            }
        };
        KeyPress += EditorForm_KeyPress;
        KeyDown += EditorForm_KeyDown;
        KeyUp += EditorForm_KeyUp;
        Deactivate += (_, _) => SetAutomaticMaskRevealActive(false);

        scrollPanel.Controls.Add(_pictureBox);
        Controls.Add(scrollPanel);
        Controls.Add(toolStrip);
        toolStrip.Resize += (_, _) =>
        {
            CenterActionButtons(toolStrip);
            CenterEditorTools(toolStrip);
        };

        SetToolLabelsVisible(toolStrip, showToolLabels);
        CenterPictureBox(scrollPanel);
        SetActiveTool(Enum.IsDefined(initialTool) ? initialTool : EditTool.Freehand);
    }

    /// <summary>Current selected tool in the editor.</summary>
    public EditTool SelectedTool => _currentTool;

    protected override bool ShowWithoutActivation => _showWithoutActivation;

    private static void CenterPictureBox(Panel scrollPanel)
    {
        if (scrollPanel.Controls.Count == 0)
        {
            return;
        }

        var pictureBox = (PictureBox)scrollPanel.Controls[0];
        var x = Math.Max(0, (scrollPanel.ClientSize.Width - pictureBox.Width) / 2);
        var y = Math.Max(0, (scrollPanel.ClientSize.Height - pictureBox.Height) / 2);
        pictureBox.Location = new Point(x, y);
    }

    private void TrackToolGroupHover(ToolStrip toolStrip, ToolStripItem mainButton, ToolStripItem dropdownButton)
    {
        void SetHovered(bool hovered)
        {
            if (_toolStripRenderer.SetHoveredGroup(hovered ? mainButton : null, hovered ? dropdownButton : null))
            {
                toolStrip.Invalidate();
            }
        }

        void ClearHoverIfOutsideGroup()
        {
            var point = toolStrip.PointToClient(Control.MousePosition);
            var hoveredItem = toolStrip.GetItemAt(point);
            if (!ReferenceEquals(hoveredItem, mainButton) && !ReferenceEquals(hoveredItem, dropdownButton))
            {
                SetHovered(false);
            }
        }

        mainButton.MouseEnter += (_, _) => SetHovered(true);
        dropdownButton.MouseEnter += (_, _) => SetHovered(true);
        mainButton.MouseLeave += (_, _) => ClearHoverIfOutsideGroup();
        dropdownButton.MouseLeave += (_, _) => ClearHoverIfOutsideGroup();
    }

    private void AddDrawingToolMenuItem(EditTool tool, ToolIconKind iconKind, string localizationKey, string shortcut)
    {
        var iconKey = EnsureToolIconKey(iconKind, Color.Black);
        var label = AppLocalization.Translate(localizationKey);
        var menuItem = new ToolStripMenuItem(label, _toolIconImageList.Images[iconKey])
        {
            Tag = tool,
            AccessibleName = label,
            Checked = tool == _selectedDrawingTool,
            ShortcutKeyDisplayString = shortcut,
        };
        menuItem.Click += (_, _) => SetActiveTool(tool);
        _btnDrawingDropdown.DropDownItems.Add(menuItem);
    }

    private static bool IsDrawingTool(EditTool tool) => tool is
        EditTool.Freehand or EditTool.Line or EditTool.Arrow or EditTool.Rectangle or EditTool.Circle;

    private void UpdateDrawingToolButton(EditTool tool)
    {
        var (iconKind, localizationKey, tooltipLocalizationKey, shortcut) = tool switch
        {
            EditTool.Freehand => (ToolIconKind.Freehand, "Editor.Freehand", "Capture.Region.Freehand", "F2"),
            EditTool.Line => (ToolIconKind.Line, "Editor.Line", "Capture.Region.Line", "F3"),
            EditTool.Arrow => (ToolIconKind.Arrow, "Editor.Arrow", "Capture.Region.Arrow", "F4"),
            EditTool.Rectangle => (ToolIconKind.Rectangle, "Editor.Rectangle", "Capture.Region.Rectangle", "F5"),
            EditTool.Circle => (ToolIconKind.Circle, "Editor.Circle", "Capture.Region.Circle", "F6"),
            _ => throw new ArgumentOutOfRangeException(nameof(tool)),
        };
        var label = AppLocalization.Translate(localizationKey);
        var tooltipToolLabel = AppLocalization.Translate(tooltipLocalizationKey);
        var drawingLabel = AppLocalization.Translate("Capture.Region.Drawing");
        _selectedDrawingTool = tool;
        _btnDrawing.Text = label;
        _btnDrawing.ImageKey = EnsureToolIconKey(iconKind, Color.Black);
        _btnDrawing.ToolTipText = $"{drawingLabel}: {tooltipToolLabel} ({shortcut})";
        _btnDrawing.AccessibleName = label;

        foreach (ToolStripMenuItem menuItem in _btnDrawingDropdown.DropDownItems)
        {
            menuItem.Checked = menuItem.Tag is EditTool menuTool && menuTool == tool;
        }
    }

    private void SetToolLabelsVisible(ToolStrip toolStrip, bool visible)
    {
        var buttons = toolStrip.Items.OfType<ToolStripButton>().ToArray();
        foreach (var button in buttons)
        {
            button.AutoSize = visible;
            button.DisplayStyle = visible
                ? ToolStripItemDisplayStyle.ImageAndText
                : ToolStripItemDisplayStyle.Image;
        }

        foreach (var button in buttons)
        {
            var preferredWidth = button.GetPreferredSize(Size.Empty).Width;
            var itemIndex = toolStrip.Items.IndexOf(button);
            var isSplitButton = itemIndex + 1 < toolStrip.Items.Count
                && toolStrip.Items[itemIndex + 1] is ToolStripDropDownButton;
            var width = visible
                ? preferredWidth
                : ToolStripMetrics.IconOnlyActionWidth - (isSplitButton ? ToolStripMetrics.DropdownButtonWidth : 0);
            button.AutoSize = false;
            button.Size = new Size(width, ToolStripMetrics.DropdownButtonHeight);
        }
    }

    private ToolStripButton CreateStandardToolButton(
        string text,
        ToolIconKind iconKind,
        Color iconColor,
        bool isCheckable = false,
        bool isChecked = false,
        string? shortcut = null)
    {
        return new ToolStripButton(text)
        {
            CheckOnClick = isCheckable,
            Checked = isChecked,
            ImageAlign = ContentAlignment.MiddleCenter,
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            ImageKey = EnsureToolIconKey(iconKind, iconColor),
            ImageScaling = ToolStripItemImageScaling.None,
            Margin = new Padding(ToolButtonMargin, 0, ToolButtonMargin, 0),
            AutoSize = false,
            Width = ToolButtonSize,
            Height = ToolButtonSize,
            AutoToolTip = true,
            ToolTipText = shortcut is null ? text : $"{text} ({shortcut})",
        };
    }

    private string EnsureToolIconKey(ToolIconKind kind, Color color)
    {
        var isColorizedIcon = kind is ToolIconKind.LineColor or ToolIconKind.TextColor or ToolIconKind.FillColor;
        var imageKey = $"{_toolIconSize}:{(int)kind}";

        if (!isColorizedIcon && _toolIconImageList.Images.ContainsKey(imageKey))
        {
            return imageKey;
        }

        if (isColorizedIcon && _colorizedToolIconColors.TryGetValue(kind, out var currentColor) && currentColor == color.ToArgb())
        {
            return imageKey;
        }

        var icon = RenderToolIcon(kind, color, _toolIconSize);
        if (_toolIconImageList.Images.ContainsKey(imageKey))
        {
            _toolIconImageList.Images.RemoveByKey(imageKey);
            if (_toolIconBitmaps.Remove(imageKey, out var previousIcon))
            {
                previousIcon.Dispose();
            }
        }

        try
        {
            _toolIconImageList.Images.Add(imageKey, icon);
            _toolIconBitmaps[imageKey] = icon;
        }
        catch
        {
            icon.Dispose();
            throw;
        }

        if (isColorizedIcon)
        {
            _colorizedToolIconColors[kind] = color.ToArgb();
        }

        return imageKey;
    }

    private static int GetToolIconSizeForDpi(int dpi)
    {
        var scaled = (int)Math.Round(ToolIconBaseSize * (dpi / 96F));
        return scaled switch
        {
            <= 22 => 20,
            <= 26 => 24,
            <= 30 => 28,
            _ => 32,
        };
    }

    private static ImageList CreateToolIconImageList(int iconSize)
    {
        return new ImageList
        {
            ColorDepth = ColorDepth.Depth32Bit,
            ImageSize = new Size(iconSize, iconSize),
            TransparentColor = Color.Transparent,
        };
    }

    private static Bitmap RenderToolIcon(ToolIconKind kind, Color color, int iconSize)
    {
        var resourceName = GetToolIconResourceName(kind);
        if (resourceName is null)
        {
            return new Bitmap(iconSize, iconSize, PixelFormat.Format32bppArgb);
        }

        using var sourceStream = typeof(EditorForm).Assembly.GetManifestResourceStream(resourceName);
        if (sourceStream is null)
        {
            return new Bitmap(iconSize, iconSize, PixelFormat.Format32bppArgb);
        }

        // Carrega a imagem PNG diretamente
        using var loadedImage = Image.FromStream(sourceStream);
        var baseBitmap = new Bitmap(loadedImage);

        try
        {
            // Aplica colorização apenas no ícone de "Cor"
            if (kind is ToolIconKind.LineColor or ToolIconKind.TextColor or ToolIconKind.FillColor && color != Color.Black)
            {
                var colorizedBitmap = ColorizeIcon(baseBitmap, color);
                baseBitmap.Dispose();
                baseBitmap = colorizedBitmap;
            }

            // Redimensiona se necessário
            if (baseBitmap.Width != iconSize || baseBitmap.Height != iconSize)
            {
                var resized = new Bitmap(iconSize, iconSize, PixelFormat.Format32bppArgb);
                using var graphics = Graphics.FromImage(resized);
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(baseBitmap, 0, 0, iconSize, iconSize);
                baseBitmap.Dispose();
                return resized;
            }

            return baseBitmap;
        }
        catch
        {
            baseBitmap?.Dispose();
            throw;
        }
    }

    private static Bitmap ColorizeIcon(Bitmap bitmap, Color targetColor)
    {
        var result = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);

        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        var resultData = result.LockBits(
            new Rectangle(0, 0, result.Width, result.Height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);

        try
        {
            unsafe
            {
                byte* srcPtr = (byte*)bitmapData.Scan0.ToPointer();
                byte* dstPtr = (byte*)resultData.Scan0.ToPointer();
                int stride = bitmapData.Stride;
                int width = bitmap.Width;
                int height = bitmap.Height;

                for (int y = 0; y < height; y++)
                {
                    byte* srcRow = srcPtr + (y * stride);
                    byte* dstRow = dstPtr + (y * stride);

                    for (int x = 0; x < width; x++)
                    {
                        int srcOffset = x * 4;
                        byte alpha = srcRow[srcOffset + 3];

                        if (alpha > 0)
                        {
                            dstRow[srcOffset] = targetColor.B;
                            dstRow[srcOffset + 1] = targetColor.G;
                            dstRow[srcOffset + 2] = targetColor.R;
                            dstRow[srcOffset + 3] = alpha;
                        }
                    }
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
            result.UnlockBits(resultData);
        }

        return result;
    }

    private static string? GetToolIconResourceName(ToolIconKind kind)
    {
        return kind switch
        {
            ToolIconKind.LineColor => "Aprint.Resources.Icons.colorline.png",
            ToolIconKind.TextColor => "Aprint.Resources.Icons.colortext.png",
            ToolIconKind.FillColor => "Aprint.Resources.Icons.colorfill.png",
            ToolIconKind.Freehand => "Aprint.Resources.Icons.freehand.png",
            ToolIconKind.Line => "Aprint.Resources.Icons.line.png",
            ToolIconKind.Arrow => "Aprint.Resources.Icons.arrow.png",
            ToolIconKind.Rectangle => "Aprint.Resources.Icons.square.png",
            ToolIconKind.Circle => "Aprint.Resources.Icons.circle.png",
            ToolIconKind.Counter => "Aprint.Resources.Icons.counter.png",
            ToolIconKind.Highlight => "Aprint.Resources.Icons.highlight.png",
            ToolIconKind.Text => "Aprint.Resources.Icons.text.png",
            ToolIconKind.Crop => "Aprint.Resources.Icons.crop.png",
            ToolIconKind.Confirm => "Aprint.Resources.Icons.confirm.png",
            ToolIconKind.Cancel => "Aprint.Resources.Icons.cancel.png",
            ToolIconKind.Filled => "Aprint.Resources.Icons.filled.png",
            ToolIconKind.Pixelated => "Aprint.Resources.Icons.pixelated.png",
            ToolIconKind.AutomaticMask => "Aprint.Resources.Icons.automaticmask.png",
            ToolIconKind.Undo => "Aprint.Resources.Icons.undo.png",
            ToolIconKind.Redo => "Aprint.Resources.Icons.redo.png",
            ToolIconKind.Copy => "Aprint.Resources.Icons.copy.png",
            ToolIconKind.Save => "Aprint.Resources.Icons.save.png",
            ToolIconKind.NewCapture => "Aprint.Resources.Icons.new.png",
            _ => null,
        };
    }

    private static ToolStripSeparator CreateToolStripSeparator()
    {
        return new ToolStripSeparator
        {
            AutoSize = false,
            Width = 8,
            Margin = new Padding(ToolSeparatorMargin, 0, ToolSeparatorMargin, 0),
        };
    }

    private void SetActiveTool(EditTool tool)
    {
        if (_isAutomaticMaskSelectionActive && tool != _currentTool)
        {
            return;
        }

        if (_isEditingText && tool != EditTool.Text)
        {
            CommitTextEdit();
        }

        if (_currentTool == EditTool.Crop && tool != EditTool.Crop)
        {
            _cropDragEdge = CropEdge.None;
            _pictureBox.Capture = false;
            _pictureBox.Invalidate();
        }

        if (tool == EditTool.Crop && _currentTool != EditTool.Crop)
        {
            _toolBeforeCrop = _currentTool;
            _cropBounds = new Rectangle(Point.Empty, _workingBitmap.Size);
            _pictureBox.Invalidate();
        }

        _currentTool = tool;
        if (tool is EditTool.Blur or EditTool.Pixelated)
        {
            var mode = tool == EditTool.Blur ? MaskMode.Solid : MaskMode.Pixelate;
            if (_maskMode != mode)
            {
                _maskMode = mode;
                MaskModeChanged?.Invoke(mode);
                _pictureBox.Invalidate();
            }
        }
        var isDrawingTool = IsDrawingTool(tool);
        if (isDrawingTool)
        {
            UpdateDrawingToolButton(tool);
        }
        _btnDrawing.Checked = isDrawingTool;
        _btnCounter.Checked = tool == EditTool.Counter;
        _btnHighlight.Checked = tool == EditTool.Highlight;
        _btnText.Checked = tool == EditTool.Text;
        _btnCrop.Checked = tool == EditTool.Crop;
        UpdateActionToolbarMode();
        _btnMask.Checked = tool is EditTool.Blur or EditTool.Pixelated;
        UpdateMaskToolAppearance();
        ToolStripItem? activeGroupButton = null;
        ToolStripItem? activeGroupDropdown = null;
        if (isDrawingTool)
        {
            activeGroupButton = _btnDrawing;
            activeGroupDropdown = _btnDrawingDropdown;
        }
        else if (tool == EditTool.Highlight)
        {
            activeGroupButton = _btnHighlight;
            activeGroupDropdown = _btnHighlightSizeDropdown;
        }
        else if (tool == EditTool.Text)
        {
            activeGroupButton = _btnText;
            activeGroupDropdown = _btnTextFormatDropdown;
        }
        else if (tool is EditTool.Blur or EditTool.Pixelated)
        {
            activeGroupButton = _btnMask;
            activeGroupDropdown = _btnMaskModeDropdown;
        }

        if (_toolStripRenderer.SetActiveGroup(activeGroupButton, activeGroupDropdown) &&
            Controls.OfType<ToolStrip>().FirstOrDefault() is { } toolStrip)
        {
            toolStrip.Invalidate();
        }

        _pictureBox.Cursor = tool == EditTool.Text ? Cursors.IBeam : Cursors.Cross;
    }

    private void UpdateMaskToolAppearance()
    {
        var isFilled = _maskMode == MaskMode.Solid;
        var iconKind = isFilled ? ToolIconKind.Filled : ToolIconKind.Pixelated;
        var localizedMode = AppLocalization.Translate(isFilled ? "Editor.Filled" : "Editor.Pixelated");
        var iconKey = EnsureToolIconKey(iconKind, Color.Black);

        _btnMask.Text = localizedMode;
        _btnMask.ImageKey = iconKey;
        _btnMask.ToolTipText = $"{AppLocalization.Translate("Editor.Mask")}: {localizedMode} (F8)";
        _btnMaskModeDropdown.ImageKey = iconKey;
        _btnMaskModeDropdown.ToolTipText = AppLocalization.Translate("Editor.PixelatedSize");
        _filledModeMenuItem.Checked = isFilled;
        _pixelatedModeMenuItem.Checked = !isFilled;
        _pixelSizeSeparator.Visible = !isFilled;
        _pixelSizeControlHost.Visible = !isFilled;
    }

    private void PickColor()
    {
        using var dialog = new ColorDialog { Color = _selectedColor, FullOpen = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _selectedColor = dialog.Color;
            UpdateColorButtonAppearance();
            LineColorChanged?.Invoke(_selectedColor);
        }
    }

    private void PickTextColor()
    {
        using var dialog = new ColorDialog { Color = _selectedTextColor, FullOpen = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _selectedTextColor = dialog.Color;
            UpdateTextColorButtonAppearance();
            TextColorChanged?.Invoke(_selectedTextColor);
            _pictureBox.Invalidate();
        }
    }

    private void UpdateColorButtonAppearance()
    {
        _btnColor.ImageKey = EnsureToolIconKey(ToolIconKind.LineColor, _selectedColor);
        _btnColor.ToolTipText = $"{AppLocalization.Translate("Editor.LineColor")}: {_selectedColor.Name}";
    }

    private void UpdateTextColorButtonAppearance()
    {
        _btnTextColor.ImageKey = EnsureToolIconKey(ToolIconKind.TextColor, _selectedTextColor);
        _btnTextColor.ToolTipText = $"{AppLocalization.Translate("Editor.TextColor")}: {_selectedTextColor.Name}";
    }

    private void PickFillColor()
    {
        using var dialog = new ColorDialog { Color = _selectedFillColor ?? Color.White, FullOpen = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _selectedFillColor = dialog.Color;
            UpdateFillColorButtonAppearance();
            FillColorChanged?.Invoke(_selectedFillColor);
        }
    }

    private void UpdateFillColorButtonAppearance()
    {
        var iconKey = EnsureToolIconKey(ToolIconKind.FillColor, _selectedFillColor ?? Color.Black);
        _btnFillColor.ImageKey = iconKey;
        _btnFillColorDropdown.ImageKey = iconKey;
        var colorName = _selectedFillColor?.Name ?? AppLocalization.Translate("Editor.NoFill");
        _btnFillColor.ToolTipText = $"{AppLocalization.Translate("Editor.FillColor")}: {colorName}";
    }

    private void SaveUndoState()
    {
        _editHistory.Capture(_workingBitmap, _nextCounterValue);
    }

    private void UndoLastEdit()
    {
        var replacement = _editHistory.Undo(_workingBitmap, _nextCounterValue);
        if (replacement is null)
        {
            return;
        }

        var dimensionsChanged = replacement.Value.Bitmap.Size != _maskDetectionImageSize;
        var current = _workingBitmap;
        _workingBitmap = replacement.Value.Bitmap;
        _nextCounterValue = replacement.Value.NextCounterValue;
        current.Dispose();

        _pictureBox.Image = _workingBitmap;
        if (_currentTool == EditTool.Crop)
        {
            _cropBounds = new Rectangle(Point.Empty, _workingBitmap.Size);
        }
        CenterPictureBox((Panel)_pictureBox.Parent!);
        _pictureBox.Invalidate();
        if (dimensionsChanged)
        {
            ResetTextRegionDetection();
        }
    }

    private void RedoLastEdit()
    {
        var replacement = _editHistory.Redo(_workingBitmap, _nextCounterValue);
        if (replacement is null)
        {
            return;
        }

        var dimensionsChanged = replacement.Value.Bitmap.Size != _maskDetectionImageSize;
        var current = _workingBitmap;
        _workingBitmap = replacement.Value.Bitmap;
        _nextCounterValue = replacement.Value.NextCounterValue;
        current.Dispose();

        _pictureBox.Image = _workingBitmap;
        if (_currentTool == EditTool.Crop)
        {
            _cropBounds = new Rectangle(Point.Empty, _workingBitmap.Size);
        }
        CenterPictureBox((Panel)_pictureBox.Parent!);
        _pictureBox.Invalidate();
        if (dimensionsChanged)
        {
            ResetTextRegionDetection();
        }
    }

    private void PictureBox_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        if (_isAutomaticMaskSelectionActive)
        {
            if (_isAutomaticMaskRevealActive)
            {
                var regionIndex = _maskRegions.FindLastIndex(region => region.Bounds.Contains(e.Location));
                if (regionIndex >= 0)
                {
                    _maskRegions[regionIndex].ShouldMask = !_maskRegions[regionIndex].ShouldMask;
                    _pictureBox.Invalidate(_maskRegions[regionIndex].Bounds);
                }
            }
            else
            {
                _isDrawingManualMaskRegion = true;
                _manualMaskStart = ClampToImage(e.Location);
                _manualMaskEnd = _manualMaskStart;
                _pictureBox.Invalidate();
            }

            return;
        }

        if (_currentTool == EditTool.Crop)
        {
            _cropDragEdge = HitTestCropEdge(e.Location);
            _cropDragStart = e.Location;
            _cropDragBounds = _cropBounds;
            return;
        }

        if (_isEditingText)
        {
            CommitTextEdit();
        }

        _isDrawing = true;
        _dragStart = e.Location;
        _lastFreehandPoint = e.Location;
        if (_currentTool == EditTool.Highlight)
        {
            _highlighterPoints.Clear();
            _highlighterPoints.Add(e.Location);
        }

        SaveUndoState();

        if (_currentTool == EditTool.Counter)
        {
            AddCounterMarker(e.Location);
            _isDrawing = false;
            return;
        }

        if (_currentTool != EditTool.Freehand)
        {
            _lastPreviewDirtyRect = Rectangle.Empty;
            _shapePreviewVisible = false;
        }
    }

    private void PictureBox_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_currentTool == EditTool.Crop)
        {
            if (_cropDragEdge != CropEdge.None && (e.Button & MouseButtons.Left) != 0)
            {
                var deltaX = e.X - _cropDragStart.X;
                var deltaY = e.Y - _cropDragStart.Y;
                var left = _cropDragBounds.Left;
                var top = _cropDragBounds.Top;
                var right = _cropDragBounds.Right;
                var bottom = _cropDragBounds.Bottom;
                if (_cropDragEdge.HasFlag(CropEdge.Left)) left = Math.Clamp(left + deltaX, 0, right - 1);
                if (_cropDragEdge.HasFlag(CropEdge.Right)) right = Math.Clamp(right + deltaX, left + 1, _workingBitmap.Width);
                if (_cropDragEdge.HasFlag(CropEdge.Top)) top = Math.Clamp(top + deltaY, 0, bottom - 1);
                if (_cropDragEdge.HasFlag(CropEdge.Bottom)) bottom = Math.Clamp(bottom + deltaY, top + 1, _workingBitmap.Height);
                var newBounds = Rectangle.FromLTRB(left, top, right, bottom);
                if (newBounds != _cropBounds)
                {
                    var previousBounds = _cropBounds;
                    _cropBounds = newBounds;
                    InvalidateCropPreview(previousBounds);
                }
            }
            else
            {
                var edge = HitTestCropEdge(e.Location);
                _pictureBox.Cursor = edge switch
                {
                    CropEdge.Left or CropEdge.Right => Cursors.SizeWE,
                    CropEdge.Top or CropEdge.Bottom => Cursors.SizeNS,
                    CropEdge.Left | CropEdge.Top or CropEdge.Right | CropEdge.Bottom => Cursors.SizeNWSE,
                    CropEdge.Right | CropEdge.Top or CropEdge.Left | CropEdge.Bottom => Cursors.SizeNESW,
                    _ => Cursors.Default,
                };
            }
            return;
        }

        if (_isAutomaticMaskSelectionActive && _isDrawingManualMaskRegion)
        {
            var previousBounds = ImageEditingService.CreateSelectionRectangle(_manualMaskStart, _manualMaskEnd);
            _manualMaskEnd = ClampToImage(e.Location);
            var currentBounds = ImageEditingService.CreateSelectionRectangle(_manualMaskStart, _manualMaskEnd);
            _pictureBox.Invalidate(Rectangle.Union(previousBounds, currentBounds));
            return;
        }

        if (!_isDrawing)
        {
            return;
        }

        if (_currentTool == EditTool.Highlight)
        {
            if (_highlighterPoints[^1] != e.Location)
            {
                var previousPoint = _highlighterPoints[^1];
                _highlighterPoints.Add(e.Location);
                InvalidatePictureBoxRegion(CreateLineDirtyRectangle(previousPoint, e.Location, (_highlightWidth / 2) + 2));
            }
        }
        else if (_currentTool == EditTool.Text)
        {
            var currentDirtyRect = CreateToolDirtyRectangle(_dragStart, e.Location, _currentTool, 2);
            _shapePreviewEnd = e.Location;
            _shapePreviewVisible = true;
            var repaintRegion = _lastPreviewDirtyRect.IsEmpty
                ? currentDirtyRect
                : Rectangle.Union(_lastPreviewDirtyRect, currentDirtyRect);
            _lastPreviewDirtyRect = currentDirtyRect;
            InvalidatePictureBoxRegion(repaintRegion);
        }
        else if (_currentTool == EditTool.Freehand)
        {
            using (var graphics = Graphics.FromImage(_workingBitmap))
            {
                ImageEditingService.DrawFreehandSegment(graphics, _lastFreehandPoint, e.Location, _selectedColor, PenWidth);
            }

            var dirtyRect = CreateLineDirtyRectangle(_lastFreehandPoint, e.Location, PenWidth + 2);
            _lastFreehandPoint = e.Location;
            InvalidatePictureBoxRegion(dirtyRect);
        }
        else
        {
            var currentDirtyRect = CreateToolDirtyRectangle(_dragStart, e.Location, _currentTool, PenWidth + 4);
            _shapePreviewEnd = e.Location;
            _shapePreviewVisible = true;

            var repaintRegion = _lastPreviewDirtyRect.IsEmpty
                ? currentDirtyRect
                : Rectangle.Union(_lastPreviewDirtyRect, currentDirtyRect);
            _lastPreviewDirtyRect = currentDirtyRect;
            InvalidatePictureBoxRegion(repaintRegion);
        }
    }

    private void PictureBox_MouseUp(object? sender, MouseEventArgs e)
    {
        if (_isAutomaticMaskSelectionActive && _isDrawingManualMaskRegion)
        {
            _manualMaskEnd = ClampToImage(e.Location);
            _isDrawingManualMaskRegion = false;
            var bounds = ImageEditingService.CreateSelectionRectangle(_manualMaskStart, _manualMaskEnd);
            if (bounds.Width > 1 && bounds.Height > 1)
            {
                _maskRegions.Add(new MaskRegion(bounds));
            }

            _pictureBox.Invalidate();
            return;
        }

        if (_currentTool == EditTool.Crop)
        {
            _cropDragEdge = CropEdge.None;
            return;
        }

        if (!_isDrawing)
        {
            return;
        }

        _isDrawing = false;

        if (_currentTool == EditTool.Highlight)
        {
            if (_highlighterPoints.Count == 0 || _highlighterPoints[^1] != e.Location)
            {
                _highlighterPoints.Add(e.Location);
            }

            ImageEditingService.ApplyHighlighter(_workingBitmap, _highlighterPoints, _selectedColor, _highlightWidth);
            _highlighterPoints.Clear();
            _pictureBox.Image = _workingBitmap;
            _pictureBox.Invalidate();
        }
        else if (_currentTool == EditTool.Text)
        {
            _textBounds = ImageEditingService.CreateTextBounds(
                _dragStart,
                e.Location,
                new Rectangle(Point.Empty, _workingBitmap.Size),
                _textFont,
                out _textMinimumWidth);
            _textBuffer = string.Empty;
            _isTextSelected = false;
            ResizeTextBoundsToContent();
            _isEditingText = true;
            _textCaretVisible = true;
            _shapePreviewVisible = false;
            _lastPreviewDirtyRect = Rectangle.Empty;
            _textCaretTimer.Start();
            _pictureBox.Focus();
            _pictureBox.Invalidate(_textBounds);
        }
        else if (_currentTool is EditTool.Blur or EditTool.Pixelated)
        {
            var selection = ImageEditingService.CreateSelectionRectangle(_dragStart, e.Location);
            var repaintRegion = _lastPreviewDirtyRect.IsEmpty
                ? selection
                : Rectangle.Union(_lastPreviewDirtyRect, selection);
            ApplyMaskToSelection(selection);
            _shapePreviewVisible = false;
            _lastPreviewDirtyRect = Rectangle.Empty;
            InvalidatePictureBoxRegion(repaintRegion);
        }
        else if (_currentTool != EditTool.Freehand)
        {
            var dirtyRect = CreateToolDirtyRectangle(_dragStart, e.Location, _currentTool, PenWidth + 4);
            var repaintRegion = _lastPreviewDirtyRect.IsEmpty
                ? dirtyRect
                : Rectangle.Union(_lastPreviewDirtyRect, dirtyRect);
            using (var graphics = Graphics.FromImage(_workingBitmap))
            {
                ImageEditingService.DrawAnnotation(graphics, _dragStart, e.Location, _currentTool, _selectedColor, _selectedFillColor);
            }

            _pictureBox.Image = _workingBitmap;
            _shapePreviewVisible = false;
            _lastPreviewDirtyRect = Rectangle.Empty;
            InvalidatePictureBoxRegion(repaintRegion);
        }
    }

    private void PictureBox_Paint(object? sender, PaintEventArgs e)
    {
        if (_isAutomaticMaskSelectionActive && _isDrawingManualMaskRegion)
        {
            DrawAutomaticMaskPreview(e.Graphics, e.ClipRectangle);
            var selection = ImageEditingService.CreateSelectionRectangle(_manualMaskStart, _manualMaskEnd);
            using var selectionPen = new Pen(Color.Yellow, 2) { DashStyle = DashStyle.Dash };
            e.Graphics.DrawRectangle(
                selectionPen,
                selection.X,
                selection.Y,
                Math.Max(0, selection.Width - 1),
                Math.Max(0, selection.Height - 1));
            return;
        }

        if (_currentTool == EditTool.Crop)
        {
            DrawCropOverlay(e.Graphics);
            DrawAutomaticMaskPreview(e.Graphics, e.ClipRectangle);
            return;
        }

        if (_currentTool == EditTool.Highlight && _isDrawing)
        {
            ImageEditingService.DrawHighlighter(
                e.Graphics,
                _highlighterPoints,
                _selectedColor,
                _highlightWidth,
                new Rectangle(Point.Empty, _workingBitmap.Size));
            DrawAutomaticMaskPreview(e.Graphics, e.ClipRectangle);
            return;
        }

        if (_currentTool == EditTool.Text)
        {
            if (_isEditingText)
            {
                if (_isTextSelected)
                {
                    using var selectionBrush = new SolidBrush(SystemColors.Highlight);
                    e.Graphics.FillRectangle(selectionBrush, Rectangle.Inflate(_textBounds, -1, -1));
                }

                var textColor = _isTextSelected ? SystemColors.HighlightText : _selectedTextColor;
                ImageEditingService.DrawText(e.Graphics, _textBounds, _textBuffer, textColor, _textFont);
                using var borderPen = new Pen(Color.Gray) { DashStyle = DashStyle.Dash };
                e.Graphics.DrawRectangle(borderPen, _textBounds.X, _textBounds.Y, _textBounds.Width - 1, _textBounds.Height - 1);
                if (_textCaretVisible && !_isTextSelected)
                {
                    DrawTextCaret(e.Graphics);
                }
            }
            else if (_isDrawing && _shapePreviewVisible)
            {
                var selection = ImageEditingService.CreateSelectionRectangle(_dragStart, _shapePreviewEnd);
                using var selectionPen = new Pen(Color.Gray) { DashStyle = DashStyle.Dash };
                e.Graphics.DrawRectangle(selectionPen, selection);
            }

            DrawAutomaticMaskPreview(e.Graphics, e.ClipRectangle);
            return;
        }

        if (!_shapePreviewVisible)
        {
            DrawAutomaticMaskPreview(e.Graphics, e.ClipRectangle);
            return;
        }

        if (_currentTool is EditTool.Blur or EditTool.Pixelated)
        {
            using var selectionPen = new Pen(Color.Red, 2) { DashStyle = DashStyle.Dash };
            var selection = ImageEditingService.CreateSelectionRectangle(_dragStart, _shapePreviewEnd);
            e.Graphics.DrawRectangle(selectionPen, selection);
            DrawAutomaticMaskPreview(e.Graphics, e.ClipRectangle);
            return;
        }

        ImageEditingService.DrawAnnotation(e.Graphics, _dragStart, _shapePreviewEnd, _currentTool, _selectedColor, _selectedFillColor);
        DrawAutomaticMaskPreview(e.Graphics, e.ClipRectangle);
    }

    private CropEdge HitTestCropEdge(Point location)
    {
        const int tolerance = CropHandleSize;
        var onLeft = Math.Abs(location.X - _cropBounds.Left) <= tolerance;
        var onRight = Math.Abs(location.X - (_cropBounds.Right - 1)) <= tolerance;
        var onTop = Math.Abs(location.Y - _cropBounds.Top) <= tolerance;
        var onBottom = Math.Abs(location.Y - (_cropBounds.Bottom - 1)) <= tolerance;
        var insideX = location.X >= _cropBounds.Left - tolerance && location.X < _cropBounds.Right + tolerance;
        var insideY = location.Y >= _cropBounds.Top - tolerance && location.Y < _cropBounds.Bottom + tolerance;
        var edge = CropEdge.None;
        if (insideY && onLeft) edge |= CropEdge.Left;
        else if (insideY && onRight) edge |= CropEdge.Right;
        if (insideX && onTop) edge |= CropEdge.Top;
        else if (insideX && onBottom) edge |= CropEdge.Bottom;
        return edge;
    }

    private void DrawCropOverlay(Graphics graphics)
    {
        var bounds = _cropBounds;
        var image = new Rectangle(Point.Empty, _workingBitmap.Size);
        using var shade = new SolidBrush(Color.FromArgb(110, Color.Black));
        graphics.FillRectangle(shade, 0, 0, image.Width, bounds.Top);
        graphics.FillRectangle(shade, 0, bounds.Top, bounds.Left, bounds.Height);
        graphics.FillRectangle(shade, bounds.Right, bounds.Top, image.Width - bounds.Right, bounds.Height);
        graphics.FillRectangle(shade, 0, bounds.Bottom, image.Width, image.Height - bounds.Bottom);
        using var border = new Pen(Color.White) { DashStyle = DashStyle.Dash };
        graphics.DrawRectangle(border, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);

        using var handleFill = new SolidBrush(Color.White);
        using var handleBorder = new Pen(Color.FromArgb(40, 44, 52));
        foreach (var point in GetCropHandlePoints(bounds))
        {
            var handle = GetCropHandleBounds(point);
            graphics.FillRectangle(handleFill, handle);
            graphics.DrawRectangle(handleBorder, handle.X, handle.Y, handle.Width - 1, handle.Height - 1);
        }
    }

    private void InvalidateCropPreview(Rectangle previousBounds)
    {
        using var dirtyRegion = new Region(previousBounds);
        dirtyRegion.Xor(_cropBounds);
        IncludeCropDecoration(dirtyRegion, previousBounds);
        IncludeCropDecoration(dirtyRegion, _cropBounds);
        _pictureBox.Invalidate(dirtyRegion);
    }

    private void IncludeCropDecoration(Region region, Rectangle bounds)
    {
        region.Union(new Rectangle(bounds.Left - 1, bounds.Top - 1, bounds.Width + 2, 3));
        region.Union(new Rectangle(bounds.Left - 1, bounds.Bottom - 2, bounds.Width + 2, 3));
        region.Union(new Rectangle(bounds.Left - 1, bounds.Top - 1, 3, bounds.Height + 2));
        region.Union(new Rectangle(bounds.Right - 2, bounds.Top - 1, 3, bounds.Height + 2));
        foreach (var point in GetCropHandlePoints(bounds))
        {
            region.Union(GetCropHandleBounds(point));
        }
    }

    private Rectangle GetCropHandleBounds(Point point) => new(
        Math.Clamp(point.X - CropHandleSize / 2, 0, Math.Max(0, _workingBitmap.Width - CropHandleSize)),
        Math.Clamp(point.Y - CropHandleSize / 2, 0, Math.Max(0, _workingBitmap.Height - CropHandleSize)),
        Math.Min(CropHandleSize, _workingBitmap.Width), Math.Min(CropHandleSize, _workingBitmap.Height));

    private static Point[] GetCropHandlePoints(Rectangle bounds)
    {
        var centerX = bounds.Left + (bounds.Width - 1) / 2;
        var centerY = bounds.Top + (bounds.Height - 1) / 2;
        return [
            new(bounds.Left, bounds.Top), new(centerX, bounds.Top), new(bounds.Right - 1, bounds.Top),
            new(bounds.Left, centerY), new(bounds.Right - 1, centerY),
            new(bounds.Left, bounds.Bottom - 1), new(centerX, bounds.Bottom - 1), new(bounds.Right - 1, bounds.Bottom - 1),
        ];
    }

    private void ConfirmCrop()
    {
        if (_cropBounds != new Rectangle(Point.Empty, _workingBitmap.Size))
        {
            var cropped = ImageEditingService.Crop(_workingBitmap, _cropBounds);
            SaveUndoState();
            _pictureBox.Image = cropped;
            _workingBitmap.Dispose();
            _workingBitmap = cropped;
            CenterPictureBox((Panel)_pictureBox.Parent!);
            ResetTextRegionDetection();
        }

        SetActiveTool(_toolBeforeCrop);
    }

    private void ApplyMaskToSelection(Rectangle area)
    {
        ImageEditingService.ApplyMask(_workingBitmap, area, _maskMode, _selectedColor, _maskPixelSize);
        _pictureBox.Image = _workingBitmap;
        InvalidatePictureBoxRegion(InflateRectangle(area, 2));
    }

    private void EditorForm_KeyPress(object? sender, KeyPressEventArgs e)
    {
        if (!_isEditingText)
        {
            return;
        }

        if (e.KeyChar == '\b')
        {
            if (_isTextSelected)
            {
                _textBuffer = string.Empty;
                _isTextSelected = false;
            }
            else if (_textBuffer.Length > 0)
            {
                _textBuffer = _textBuffer[..^1];
            }
        }
        else if (e.KeyChar is '\r' or '\n')
        {
            if (_isTextSelected)
            {
                _textBuffer = string.Empty;
                _isTextSelected = false;
            }

            _textBuffer += Environment.NewLine;
        }
        else if (!char.IsControl(e.KeyChar))
        {
            if (_isTextSelected)
            {
                _textBuffer = string.Empty;
                _isTextSelected = false;
            }

            _textBuffer += e.KeyChar;
        }
        else
        {
            return;
        }

        e.Handled = true;
        _textCaretVisible = true;
        ResizeTextBoundsToContent();
    }

    private CheckBox CreateTextFormatButton(string text, string tooltipKey, FontStyle style, Point location)
    {
        var buttonFont = new Font("Segoe UI", 9F, style, GraphicsUnit.Point);
        var button = new CheckBox
        {
            Appearance = Appearance.Button,
            AutoSize = false,
            Font = buttonFont,
            Text = text,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = location,
            Size = new Size(28, 25),
            TabStop = false,
            Checked = (_textFontStyle & style) != 0,
        };
        _textFormatButtonFonts.Add(buttonFont);
        _textFormatToolTip.SetToolTip(button, AppLocalization.Translate(tooltipKey));
        button.CheckedChanged += (_, _) => SetTextFontStyle(style, button.Checked);
        return button;
    }

    private void SetTextFontStyle(FontStyle style, bool enabled)
    {
        _textFontStyle = enabled ? _textFontStyle | style : _textFontStyle & ~style;
        UpdateTextFont();
    }

    private void UpdateTextFont()
    {
        var previousFont = _textFont;
        _textFont = new Font(_textFontFamily, _textFontSize, _textFontStyle, GraphicsUnit.Point);
        previousFont.Dispose();

        TextFormattingChanged?.Invoke(_textFontFamily, _textFontSize, _textFontStyle);

        if (_isEditingText)
        {
            _textCaretVisible = true;
            ResizeTextBoundsToContent();
        }
    }

    private void ResizeTextBoundsToContent()
    {
        var previousBounds = _textBounds;
        _textBounds = ImageEditingService.ResizeTextBounds(
            _textBounds,
            _textMinimumWidth,
            _textBuffer,
            _textFont,
            new Rectangle(Point.Empty, _workingBitmap.Size));
        _pictureBox.Invalidate(Rectangle.Union(previousBounds, _textBounds));
    }

    private void DrawTextCaret(Graphics graphics)
    {
        var lines = _textBuffer.Split('\n');
        var lastLine = lines[^1].TrimEnd('\r');
        var caretSize = TextRenderer.MeasureText(
            lastLine,
            _textFont,
            Size.Empty,
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        var x = Math.Min(_textBounds.Right - 1, _textBounds.Left + 1 + caretSize.Width);
        var y = Math.Min(_textBounds.Bottom - 1, _textBounds.Top + 1 + ((lines.Length - 1) * _textFont.Height));
        using var caretPen = new Pen(_selectedTextColor);
        graphics.DrawLine(caretPen, x, y, x, Math.Min(_textBounds.Bottom - 1, y + _textFont.Height));
    }

    private void CommitTextEdit()
    {
        _textCaretTimer.Stop();
        _isEditingText = false;
        _isTextSelected = false;
        ImageEditingService.ApplyText(_workingBitmap, _textBounds, _textBuffer, _selectedTextColor, _textFont);
        _textBuffer = string.Empty;
        _textCaretVisible = false;
        _pictureBox.Image = _workingBitmap;
        _pictureBox.Invalidate(InflateRectangle(_textBounds, 2));
    }

    private void CancelTextEdit()
    {
        _textCaretTimer.Stop();
        _isEditingText = false;
        _isTextSelected = false;
        _textBuffer = string.Empty;
        _textCaretVisible = false;
        _pictureBox.Invalidate(InflateRectangle(_textBounds, 2));
    }

    private void AddCounterMarker(Point location)
    {
        var imageArea = new Rectangle(Point.Empty, _workingBitmap.Size);
        var marker = ImageEditingService.CreateCounterMarker(location, _nextCounterValue, imageArea);

        using var graphics = Graphics.FromImage(_workingBitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        ImageEditingService.DrawCounterMarker(graphics, marker, _selectedColor, _selectedFillColor, _selectedTextColor);

        _nextCounterValue++;
        _pictureBox.Image = _workingBitmap;
        InvalidatePictureBoxRegion(InflateRectangle(marker.Bounds, 2));
    }

    private void CopyToClipboard()
    {
        using var protectedImage = CreateProtectedImage();
        if (protectedImage is null)
        {
            return;
        }

        Clipboard.SetImage(protectedImage);
        Copied?.Invoke();

        if (_closeAfterCopyOrSave)
        {
            Close();
        }
    }

    private static Rectangle InflateRectangle(Rectangle rectangle, int amount)
    {
        if (rectangle.IsEmpty)
        {
            return rectangle;
        }

        rectangle.Inflate(amount, amount);
        return rectangle;
    }

    private Point ClampToImage(Point point) => new(
        Math.Clamp(point.X, 0, _workingBitmap.Width - 1),
        Math.Clamp(point.Y, 0, _workingBitmap.Height - 1));

    private static Rectangle CreateLineDirtyRectangle(Point start, Point end, int padding)
    {
        var left = Math.Min(start.X, end.X);
        var top = Math.Min(start.Y, end.Y);
        var width = Math.Abs(end.X - start.X);
        var height = Math.Abs(end.Y - start.Y);
        var dirtyRect = new Rectangle(left, top, width, height);
        return InflateRectangle(dirtyRect, padding);
    }

    private static Rectangle CreateToolDirtyRectangle(Point start, Point end, EditTool tool, int padding)
    {
        var baseRect = tool is EditTool.Blur or EditTool.Pixelated
            ? ImageEditingService.CreateSelectionRectangle(start, end)
            : CreateLineDirtyRectangle(start, end, 0);

        return InflateRectangle(baseRect, padding);
    }

    private void InvalidatePictureBoxRegion(Rectangle region)
    {
        if (region.IsEmpty)
        {
            _pictureBox.Invalidate();
            return;
        }

        var bounded = Rectangle.Intersect(new Rectangle(Point.Empty, _workingBitmap.Size), region);
        if (bounded.IsEmpty)
        {
            return;
        }

        _pictureBox.Invalidate(bounded);
    }

    private async Task SaveAsAsync()
    {
        if (_textRegionDetectionInProgress)
        {
            return;
        }

        string destinationPath;
        if (_saveWithoutPrompt)
        {
            destinationPath = _automaticSavePath;
        }
        else
        {
            using var dialog = new SaveFileDialog
            {
                Filter = AppLocalization.Translate("Editor.SaveFilter"),
                FileName = $"Aprint_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png",
                InitialDirectory = Directory.Exists(_initialFolder) ? _initialFolder : string.Empty,
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            destinationPath = dialog.FileName;
        }

        using var saveCopy = CreateProtectedImage();
        if (saveCopy is null)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            var format = Path.GetExtension(destinationPath).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => ImageFormat.Jpeg,
                ".bmp" => ImageFormat.Bmp,
                _ => ImageFormat.Png,
            };

            await Task.Run(() =>
            {
                using var output = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
                ImageMetadataWriter.SaveWithSoftwareMetadata(saveCopy, output, format);
            });

            var savedDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(savedDirectory) && Directory.Exists(savedDirectory))
            {
                _initialFolder = savedDirectory;
            }

            Saved?.Invoke(destinationPath);

            if (_closeAfterCopyOrSave)
            {
                Close();
            }
        }
        catch (Exception ex)
        {
            if (Debugger.IsAttached)
            {
                throw;
            }

            MessageBox.Show(this, ex.Message, AppLocalization.Translate("App.Name"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (_isAutomaticMaskSelectionActive)
        {
            if (keyData == Keys.Enter)
            {
                ApplyAutomaticMask();
            }
            else if (keyData == Keys.Escape)
            {
                CancelAutomaticMask();
            }
            else if (keyData == Keys.Space)
            {
                SetAutomaticMaskRevealActive(true);
            }

            return true;
        }

        if (_currentTool == EditTool.Crop)
        {
            if (keyData == Keys.Enter)
            {
                ConfirmCrop();
                return true;
            }

            if (keyData == Keys.Escape)
            {
                SetActiveTool(_toolBeforeCrop);
                return true;
            }
        }

        if (_isEditingText)
        {
            if ((keyData & Keys.Control) != 0 && (keyData & Keys.KeyCode) == Keys.A)
            {
                _isTextSelected = _textBuffer.Length > 0;
                _textCaretVisible = false;
                _pictureBox.Invalidate(_textBounds);
                return true;
            }

            if (keyData == Keys.Delete && _isTextSelected)
            {
                _textBuffer = string.Empty;
                _isTextSelected = false;
                _textCaretVisible = true;
                ResizeTextBoundsToContent();
                return true;
            }

            if (keyData == Keys.Escape)
            {
                CancelTextEdit();
                return true;
            }

            if (keyData == (Keys.Control | Keys.Enter))
            {
                CommitTextEdit();
                return true;
            }

            if ((keyData & Keys.Control) != 0 && (keyData & Keys.KeyCode) != Keys.ControlKey)
            {
                CommitTextEdit();
            }
            else
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        if (keyData == (Keys.Control | Keys.N))
        {
            NewCaptureRequested?.Invoke();
            Close();
            return true;
        }

        if (keyData == Keys.F2)
        {
            SetActiveTool(EditTool.Freehand);
            return true;
        }

        if (keyData == Keys.F3)
        {
            SetActiveTool(EditTool.Line);
            return true;
        }

        if (keyData == Keys.F4)
        {
            SetActiveTool(EditTool.Arrow);
            return true;
        }

        if (keyData == Keys.F5)
        {
            SetActiveTool(EditTool.Rectangle);
            return true;
        }

        if (keyData == Keys.F6)
        {
            SetActiveTool(EditTool.Circle);
            return true;
        }

        if (keyData == Keys.F7)
        {
            SetActiveTool(EditTool.Highlight);
            return true;
        }

        if (keyData == Keys.F8)
        {
            var maskToolIsActive = _currentTool is EditTool.Blur or EditTool.Pixelated;
            var tool = maskToolIsActive
                ? (_currentTool == EditTool.Blur ? EditTool.Pixelated : EditTool.Blur)
                : (_maskMode == MaskMode.Solid ? EditTool.Blur : EditTool.Pixelated);
            SetActiveTool(tool);
            return true;
        }

        if (keyData == Keys.F9)
        {
            SetActiveTool(EditTool.Counter);
            return true;
        }

        if (keyData == Keys.F10)
        {
            SetActiveTool(EditTool.Text);
            return true;
        }

        if (keyData == Keys.F11)
        {
            SetActiveTool(EditTool.Crop);
            return true;
        }

        if (keyData == Keys.Oemcomma)
        {
            SetActiveTool(EditTool.Crop);
            return true;
        }

        if (keyData == (Keys.Control | Keys.Z))
        {
            UndoLastEdit();
            return true;
        }

        if (keyData == (Keys.Control | Keys.Y))
        {
            RedoLastEdit();
            return true;
        }

        if (keyData == (Keys.Control | Keys.C))
        {
            CopyToClipboard();
            return true;
        }

        if (keyData == (Keys.Control | Keys.S))
        {
            _ = SaveAsAsync();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void EditorForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space && !_isEditingText && _textRegionDetectionReady)
        {
            SetAutomaticMaskRevealActive(true);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void EditorForm_KeyUp(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space && _isAutomaticMaskRevealActive)
        {
            SetAutomaticMaskRevealActive(false);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void SetAutomaticMaskRevealActive(bool active)
    {
        if (_isAutomaticMaskRevealActive == active || !_isAutomaticMaskSelectionActive || !_textRegionDetectionReady)
        {
            return;
        }

        _isAutomaticMaskRevealActive = active;
        _automaticMaskToolTip.SetToolTip(
            _pictureBox,
            AppLocalization.Translate(active ? "Editor.AutomaticMaskRevealActive" : "Editor.AutomaticMaskReviewHint"));
        _pictureBox.Invalidate();
    }

    private async Task DetectTextRegionsAsync()
    {
        if (_textRegionDetectionInProgress)
        {
            return;
        }

        var generation = ++_textRegionDetectionGeneration;
        _textRegionDetectionReady = false;
        _textRegionDetectionInProgress = true;
        _isAutomaticMaskRevealActive = false;
        _maskRegions.Clear();
        SetAutomaticMaskSelectionActive(true);
        _btnConfirmCrop.Enabled = false;
        _btnCancelCrop.Enabled = false;
        _automaticMaskMenuItem.ToolTipText = AppLocalization.Translate("Editor.AutomaticMaskScanning");
        _pictureBox.Invalidate();

        try
        {
            using var source = (Bitmap)_workingBitmap.Clone();
            var detectedRegions = await WindowsTextRegionDetector.DetectRegionsAsync(source);
            if (IsDisposed || generation != _textRegionDetectionGeneration)
            {
                return;
            }

            if (detectedRegions is null)
            {
                _textRegionDetectionInProgress = false;
                _textRegionDetectionReady = true;
                SetAutomaticMaskSelectionActive(true);
                _automaticMaskMenuItem.ToolTipText = AppLocalization.Translate("Editor.AutomaticMaskUnavailable");
                _pictureBox.Invalidate();
                return;
            }

            _maskRegions.Clear();
            _maskRegions.AddRange(detectedRegions.Select(region => new MaskRegion(region)));
            _maskDetectionImageSize = source.Size;
            _textRegionDetectionInProgress = false;
            _textRegionDetectionReady = true;
            _automaticMaskMenuItem.ToolTipText = AppLocalization.Translate(_maskRegions.Count == 0
                ? "Editor.AutomaticMaskNoRegions"
                : "Editor.AutomaticMaskReviewHint");
            SetAutomaticMaskSelectionActive(true);

            _pictureBox.Invalidate();
        }
        catch
        {
            if (!IsDisposed && generation == _textRegionDetectionGeneration)
            {
                _textRegionDetectionInProgress = false;
                _textRegionDetectionReady = true;
                SetAutomaticMaskSelectionActive(true);
                _automaticMaskMenuItem.ToolTipText = AppLocalization.Translate("Editor.AutomaticMaskUnavailable");
                _pictureBox.Invalidate();
            }
        }
    }

    private void ResetTextRegionDetection()
    {
        _textRegionDetectionGeneration++;
        _maskRegions.Clear();
        _textRegionDetectionReady = false;
        _textRegionDetectionInProgress = false;
        _isAutomaticMaskRevealActive = false;
        SetAutomaticMaskSelectionActive(false);
        _automaticMaskMenuItem.Enabled = true;
        _automaticMaskMenuItem.ToolTipText = AppLocalization.Translate("Editor.AutomaticMask");
        _pictureBox.Invalidate();
    }

    private async Task StartAutomaticMaskAsync()
    {
        if (_textRegionDetectionInProgress || _isAutomaticMaskSelectionActive)
        {
            return;
        }

        if (_currentTool == EditTool.Crop)
        {
            SetActiveTool(_toolBeforeCrop);
        }

        await DetectTextRegionsAsync();
    }

    private void SetAutomaticMaskSelectionActive(bool active)
    {
        _isAutomaticMaskSelectionActive = active;
        _automaticMaskToolTip.SetToolTip(
            _pictureBox,
            active && _textRegionDetectionReady
                ? AppLocalization.Translate("Editor.AutomaticMaskReviewHint")
                : string.Empty);
        UpdateActionToolbarMode();
    }

    private void UpdateActionToolbarMode()
    {
        var centeredActionMode = _isAutomaticMaskSelectionActive || _currentTool == EditTool.Crop;
        var toolStrip = Controls.OfType<ToolStrip>().FirstOrDefault();
        if (toolStrip is null)
        {
            return;
        }

        foreach (ToolStripItem item in toolStrip.Items)
        {
            if (item == _actionSpacerBefore || item == _actionSpacerAfter)
            {
                item.Visible = centeredActionMode;
                continue;
            }

            item.Visible = item == _btnConfirmCrop || item == _btnCancelCrop
                ? centeredActionMode
                : !centeredActionMode;

            if (item is ToolStripButton or ToolStripDropDownButton)
            {
                item.Enabled = !_isAutomaticMaskSelectionActive;
            }
        }

        _btnConfirmCrop.Enabled = centeredActionMode;
        _btnCancelCrop.Enabled = centeredActionMode;
        _automaticMaskMenuItem.Enabled = !_isAutomaticMaskSelectionActive;
        UpdateAutomaticMaskActionButtons();
        toolStrip.PerformLayout();
        CenterActionButtons(toolStrip);
        CenterEditorTools(toolStrip);
    }

    private void CenterEditorTools(ToolStrip toolStrip)
    {
        if (_isAutomaticMaskSelectionActive || _currentTool == EditTool.Crop)
        {
            return;
        }

        var center = toolStrip.DisplayRectangle.Left + (toolStrip.DisplayRectangle.Width / 2F);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var toolGroupCenter = (_btnColor.Bounds.Left + _btnRedo.Bounds.Right) / 2F;
            var adjustment = (int)Math.Round(center - toolGroupCenter);
            if (adjustment == 0)
            {
                break;
            }

            _centerToolsSpacer.Width = Math.Max(0, _centerToolsSpacer.Width + adjustment);
            toolStrip.PerformLayout();
        }
    }

    private void CenterActionButtons(ToolStrip toolStrip)
    {
        if (!_isAutomaticMaskSelectionActive && _currentTool != EditTool.Crop)
        {
            return;
        }

        var availableWidth = toolStrip.DisplayRectangle.Width -
            _btnConfirmCrop.Width - _btnConfirmCrop.Margin.Horizontal -
            _btnCancelCrop.Width - _btnCancelCrop.Margin.Horizontal;
        var spacerWidth = Math.Max(0, availableWidth / 2);
        _actionSpacerBefore.Width = spacerWidth;
        _actionSpacerAfter.Width = availableWidth - spacerWidth;
    }

    private void UpdateAutomaticMaskActionButtons()
    {
        var automaticMaskActive = _isAutomaticMaskSelectionActive;
        _btnConfirmCrop.Text = AppLocalization.Translate(automaticMaskActive ? "Editor.ApplyAutomaticMask" : "Editor.ConfirmCrop");
        _btnConfirmCrop.ToolTipText = AppLocalization.Translate(automaticMaskActive ? "Editor.ApplyAutomaticMaskToolTip" : "Editor.ConfirmCropToolTip");
        _btnCancelCrop.Text = AppLocalization.Translate(automaticMaskActive ? "Editor.CancelAutomaticMask" : "Editor.CancelCrop");
        _btnCancelCrop.ToolTipText = AppLocalization.Translate(automaticMaskActive ? "Editor.CancelAutomaticMaskToolTip" : "Editor.CancelCropToolTip");
    }

    private void ApplyAutomaticMask()
    {
        if (!_isAutomaticMaskSelectionActive || !_textRegionDetectionReady || _textRegionDetectionInProgress)
        {
            return;
        }

        var selectedRegions = _maskRegions.Where(region => region.ShouldMask).ToArray();
        if (selectedRegions.Length > 0)
        {
            SaveUndoState();
            foreach (var region in selectedRegions)
            {
                ImageEditingService.ApplyMask(_workingBitmap, region.Bounds, _maskMode, _selectedColor, _maskPixelSize);
            }

            _pictureBox.Image = _workingBitmap;
        }

        _maskRegions.Clear();
        _maskDetectionImageSize = Size.Empty;
        _textRegionDetectionReady = false;
        _isAutomaticMaskRevealActive = false;
        SetAutomaticMaskSelectionActive(false);
        _automaticMaskMenuItem.ToolTipText = AppLocalization.Translate(selectedRegions.Length > 0
            ? "Editor.AutomaticMaskApplied"
            : "Editor.AutomaticMaskNoSelection");
        _pictureBox.Invalidate();
    }

    private void CancelAutomaticMask()
    {
        if (!_isAutomaticMaskSelectionActive || _textRegionDetectionInProgress)
        {
            return;
        }

        _maskRegions.Clear();
        _maskDetectionImageSize = Size.Empty;
        _textRegionDetectionReady = false;
        _isAutomaticMaskRevealActive = false;
        SetAutomaticMaskSelectionActive(false);
        _automaticMaskMenuItem.ToolTipText = AppLocalization.Translate("Editor.AutomaticMask");
        _pictureBox.Invalidate();
    }

    private void DrawAutomaticMaskPreview(Graphics graphics, Rectangle clipBounds)
    {
        if (!_textRegionDetectionReady)
        {
            if (_textRegionDetectionInProgress)
            {
                using var pendingBrush = new SolidBrush(ToolStripColorPalette.BarBackground);
                graphics.FillRectangle(pendingBrush, clipBounds);
            }

            return;
        }

        var imageBounds = new Rectangle(Point.Empty, _workingBitmap.Size);
        foreach (var region in _maskRegions)
        {
            var bounds = Rectangle.Intersect(region.Bounds, imageBounds);
            if (bounds.IsEmpty || !bounds.IntersectsWith(clipBounds))
            {
                continue;
            }

            if (_isAutomaticMaskRevealActive)
            {
                using var outline = new Pen(region.ShouldMask ? Color.Red : Color.LimeGreen, 2)
                {
                    DashStyle = DashStyle.Dash,
                };
                graphics.DrawRectangle(outline, bounds.X, bounds.Y, Math.Max(0, bounds.Width - 1), Math.Max(0, bounds.Height - 1));
                continue;
            }

            if (!region.ShouldMask)
            {
                continue;
            }

            using var maskedRegion = _workingBitmap.Clone(bounds, PixelFormat.Format32bppArgb);
            ImageEditingService.ApplyMask(
                maskedRegion,
                new Rectangle(Point.Empty, maskedRegion.Size),
                _maskMode,
                _selectedColor,
                _maskPixelSize);
            graphics.DrawImageUnscaled(maskedRegion, bounds.Location);
        }
    }

    private Bitmap? CreateProtectedImage()
    {
        if (_textRegionDetectionInProgress)
        {
            return null;
        }

        var protectedImage = (Bitmap)_workingBitmap.Clone();
        if (!_textRegionDetectionReady)
        {
            return protectedImage;
        }

        foreach (var region in _maskRegions.Where(region => region.ShouldMask))
        {
            ImageEditingService.ApplyMask(
                protectedImage,
                region.Bounds,
                _maskMode,
                _selectedColor,
                _maskPixelSize);
        }

        return protectedImage;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inactivityTimer?.Stop();
            _inactivityTimer?.Dispose();
            _textCaretTimer.Stop();
            _textCaretTimer.Dispose();
            _textFont.Dispose();
            foreach (var buttonFont in _textFormatButtonFonts)
            {
                buttonFont.Dispose();
            }

            _textFormatToolTip.Dispose();
            _automaticMaskToolTip.Dispose();
            _toolIconImageList.Dispose();
            foreach (var icon in _toolIconBitmaps.Values)
            {
                icon.Dispose();
            }

            _toolIconBitmaps.Clear();
            _editHistory.Dispose();
            _workingBitmap.Dispose();
        }

        base.Dispose(disposing);
    }
}
