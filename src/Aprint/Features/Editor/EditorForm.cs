using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Aprint.Features.Settings;

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
        Undo,
        Redo,
        Copy,
        Save,
        NewCapture,
    }

    private const int PenWidth = 3;
    private const int TextMinimumWidth = 100;
    private const int ToolPanelHeight = 50;
    private const int ToolIconBaseSize = 24;
    private const int ToolButtonPadding = 4;
    private const int ToolButtonMargin = 2;
    private const int ToolSeparatorMargin = 4;
    private const int ToolButtonSize = 32;
    private const int CropHandleSize = 8;

    [Flags]
    private enum CropEdge { None = 0, Left = 1, Top = 2, Right = 4, Bottom = 8 }

    private static readonly Dictionary<float, Font> CounterFonts = [];
    private static readonly object CounterFontSync = new();

    private readonly record struct ToolIconCacheKey(ToolIconKind Kind, int ColorArgb);

    private Bitmap _workingBitmap;
    private string _initialFolder;
    private readonly bool _closeAfterCopyOrSave;
    private readonly bool _saveWithoutPrompt;
    private readonly string _automaticSavePath;
    private readonly ImageList _toolIconImageList;
    private readonly int _toolIconSize;
    private readonly Dictionary<ToolIconKind, int> _colorizedToolIconColors = [];
    private readonly PictureBox _pictureBox;
    private readonly ToolStripButton _btnFreehand;
    private readonly ToolStripButton _btnLine;
    private readonly ToolStripButton _btnArrow;
    private readonly ToolStripButton _btnRectangle;
    private readonly ToolStripButton _btnCircle;
    private readonly ToolStripButton _btnCounter;
    private readonly ToolStripButton _btnHighlight;
    private readonly ToolStripDropDownButton _btnHighlightSizeDropdown;
    private readonly ToolStripButton _btnText;
    private readonly ToolStripButton _btnCrop;
    private readonly ToolStripButton _btnConfirmCrop;
    private readonly ToolStripButton _btnCancelCrop;
    private readonly ToolStripDropDownButton _btnTextFormatDropdown;
    private readonly ToolStripButton _btnBorrar;
    private readonly ToolStripDropDownButton _btnBorrarModeDropdown;
    private readonly ToolStripMenuItem _filledModeMenuItem;
    private readonly ToolStripMenuItem _pixelatedModeMenuItem;
    private readonly ToolStripSeparator _pixelSizeSeparator;
    private readonly ToolStripControlHost _pixelSizeControlHost;
    private readonly ToolStripButton _btnColor;
    private readonly ToolStripButton _btnTextColor;
    private readonly ToolStripButton _btnFillColor;
    private readonly ToolStripDropDownButton _btnFillColorDropdown;
    private readonly ToolStripButton _btnUndo;
    private readonly ToolStripButton _btnRedo;
    private readonly EditHistory _editHistory;
    private Font _textFont = new("Segoe UI", 12F);
    private readonly List<Font> _textFormatButtonFonts = [];
    private readonly ToolTip _textFormatToolTip = new();
    private readonly System.Windows.Forms.Timer _textCaretTimer = new() { Interval = 500 };
    private readonly System.Windows.Forms.Timer? _inactivityTimer;
    private readonly IMessageFilter? _activityMessageFilter;

    private Color _selectedColor = Color.Red;
    private Color _selectedTextColor = Color.Red;
    private Color? _selectedFillColor;
    private EditTool _currentTool = EditTool.Freehand;
    private EditTool _toolBeforeCrop = EditTool.Freehand;
    private BorrarMode _borrarMode = BorrarMode.Pixelate;
    private int _highlightWidth = 18;
    private int _borrarPixelSize = 6;
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
    private Rectangle _cropBounds;
    private CropEdge _cropDragEdge;
    private Point _cropDragStart;
    private Rectangle _cropDragBounds;

    public event Action<string>? Saved;
    public event Action? Copied;
    public event Action<BorrarMode>? BorrarModeChanged;
    public event Action<Color>? LineColorChanged;
    public event Action<Color>? TextColorChanged;
    public event Action<Color?>? FillColorChanged;
    public event Action<int>? BorrarPixelSizeChanged;
    public event Action? NewCaptureRequested;
    public event Action<string, float, FontStyle>? TextFormattingChanged;
    public event Action<int>? HighlightWidthChanged;

    public EditorForm(Bitmap bitmap, string initialFolder, BorrarMode borrarMode = BorrarMode.Pixelate, bool showToolLabels = true, bool closeAfterCopyOrSave = false, EditTool initialTool = EditTool.Freehand, bool saveWithoutPrompt = false, bool autoCloseWhenInactive = false, int inactivityTimeoutSeconds = 30, string textFontFamily = "Segoe UI", float textFontSize = 12F, FontStyle textFontStyle = FontStyle.Regular, int highlightWidth = 18, int lineColorArgb = -65536, int textColorArgb = -65536, int? fillColorArgb = null, int borrarPixelSize = 6)
    {
        _workingBitmap = (Bitmap)bitmap.Clone();
        _selectedColor = Color.FromArgb(lineColorArgb);
        _selectedTextColor = Color.FromArgb(textColorArgb);
        _selectedFillColor = fillColorArgb.HasValue ? Color.FromArgb(fillColorArgb.Value) : null;
        _borrarPixelSize = Math.Clamp(borrarPixelSize, 4, 32);
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
        _borrarMode = borrarMode;
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
            RenderMode = ToolStripRenderMode.System,
            Font = new Font("Segoe UI", 9F),
            BackColor = Color.FromArgb(245, 245, 247),
            ForeColor = Color.FromArgb(40, 44, 52),
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
        _btnFillColor.Click += (_, _) => PickFillColor();
        _btnFillColorDropdown = new ToolStripDropDownButton
        {
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            ImageKey = EnsureToolIconKey(ToolIconKind.FillColor, Color.Black),
            ImageScaling = ToolStripItemImageScaling.None,
            Margin = new Padding(0, 0, ToolButtonMargin, 0),
            AutoSize = false,
            Width = 14,
            Height = ToolButtonSize,
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

        _btnFreehand = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Freehand"),
            ToolIconKind.Freehand,
            Color.Black,
            isCheckable: true,
            isChecked: true,
            shortcut: "F2");
        _btnLine = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Line"),
            ToolIconKind.Line,
            Color.Black,
            isCheckable: true,
            shortcut: "F3");
        _btnArrow = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Arrow"),
            ToolIconKind.Arrow,
            Color.Black,
            isCheckable: true,
            shortcut: "F4");
        _btnRectangle = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Rectangle"),
            ToolIconKind.Rectangle,
            Color.Black,
            isCheckable: true,
            shortcut: "F5");
        _btnCircle = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Circle"),
            ToolIconKind.Circle,
            Color.Black,
            isCheckable: true,
            shortcut: "F6");
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
        _btnText = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Text"),
            ToolIconKind.Text,
            Color.Black,
            isCheckable: true,
            shortcut: "F10");
        _btnTextFormatDropdown = new ToolStripDropDownButton
        {
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            ImageKey = EnsureToolIconKey(ToolIconKind.Text, Color.Black),
            ImageScaling = ToolStripItemImageScaling.None,
            Margin = new Padding(0, 0, ToolButtonMargin, 0),
            AutoSize = false,
            Width = 14,
            Height = ToolButtonSize,
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
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            ImageKey = EnsureToolIconKey(ToolIconKind.Highlight, Color.Black),
            ImageScaling = ToolStripItemImageScaling.None,
            Margin = new Padding(0, 0, ToolButtonMargin, 0),
            AutoSize = false,
            Width = 14,
            Height = ToolButtonSize,
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

        _btnBorrar = CreateStandardToolButton(
            AppLocalization.Translate(_borrarMode == BorrarMode.Solid ? "Editor.Filled" : "Editor.Pixelated"),
            _borrarMode == BorrarMode.Solid ? ToolIconKind.Filled : ToolIconKind.Pixelated,
            Color.Black,
            isCheckable: true,
            shortcut: "F8");
        _btnBorrar.Click += (_, _) => SetActiveTool(
            _borrarMode == BorrarMode.Solid ? EditTool.Blur : EditTool.Pixelated);

        _btnBorrarModeDropdown = new ToolStripDropDownButton
        {
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            ImageKey = EnsureToolIconKey(
                _borrarMode == BorrarMode.Solid ? ToolIconKind.Filled : ToolIconKind.Pixelated,
                Color.Black),
            ImageScaling = ToolStripItemImageScaling.None,
            Margin = new Padding(0, 0, ToolButtonMargin, 0),
            AutoSize = false,
            Width = 14,
            Height = ToolButtonSize,
            AutoToolTip = false,
            ToolTipText = AppLocalization.Translate("Editor.PixelatedSize"),
        };
        _filledModeMenuItem = new ToolStripMenuItem(AppLocalization.Translate("Editor.Filled"));
        _filledModeMenuItem.Click += (_, _) =>
        {
            SetActiveTool(EditTool.Blur);
            _btnBorrarModeDropdown.DropDown.Close();
        };
        _pixelatedModeMenuItem = new ToolStripMenuItem(AppLocalization.Translate("Editor.Pixelated"));
        _pixelatedModeMenuItem.Click += (_, _) =>
        {
            SetActiveTool(EditTool.Pixelated);
            _btnBorrarModeDropdown.DropDown.Close();
        };
        _pixelSizeSeparator = new ToolStripSeparator();
        _btnBorrarModeDropdown.DropDownItems.AddRange([
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
            Value = _borrarPixelSize,
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
            Text = $"{_borrarPixelSize} px",
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(38, 22),
            Location = new Point(144, 13),
        };
        pixelSizeSlider.ValueChanged += (_, _) =>
        {
            _borrarPixelSize = pixelSizeSlider.Value;
            pixelSizeLabel.Text = $"{_borrarPixelSize} px";
            BorrarPixelSizeChanged?.Invoke(_borrarPixelSize);
        };
        pixelSizePanel.Controls.Add(pixelSizeSlider);
        pixelSizePanel.Controls.Add(pixelSizeLabel);
        _pixelSizeControlHost = new ToolStripControlHost(pixelSizePanel)
        {
            AutoSize = false,
            Size = pixelSizePanel.Size,
            Margin = Padding.Empty,
        };
        _btnBorrarModeDropdown.DropDownItems.Add(_pixelSizeControlHost);
        _btnBorrarModeDropdown.DropDown.AutoClose = false;
        var popupDismissMessageFilter = new PopupDismissMessageFilter(
            _btnTextFormatDropdown,
            _btnHighlightSizeDropdown,
            _btnBorrarModeDropdown);
        Shown += (_, _) => System.Windows.Forms.Application.AddMessageFilter(popupDismissMessageFilter);
        FormClosed += (_, _) => System.Windows.Forms.Application.RemoveMessageFilter(popupDismissMessageFilter);

        _btnFreehand.Click += (_, _) => SetActiveTool(EditTool.Freehand);
        _btnLine.Click += (_, _) => SetActiveTool(EditTool.Line);
        _btnArrow.Click += (_, _) => SetActiveTool(EditTool.Arrow);
        _btnRectangle.Click += (_, _) => SetActiveTool(EditTool.Rectangle);
        _btnCircle.Click += (_, _) => SetActiveTool(EditTool.Circle);
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
        _btnConfirmCrop.Click += (_, _) => ConfirmCrop();

        _btnCancelCrop = CreateStandardToolButton(
            AppLocalization.Translate("Editor.CancelCrop"),
            ToolIconKind.Cancel,
            Color.Black,
            shortcut: "Escape");
        _btnCancelCrop.Visible = false;
        _btnCancelCrop.Click += (_, _) => SetActiveTool(_toolBeforeCrop);

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

        toolStrip.Items.AddRange([
            btnNewCapture,
            CreateToolStripSeparator(),
            _btnColor,
            _btnTextColor,
            _btnFillColor,
            _btnFillColorDropdown,
            CreateToolStripSeparator(),
            _btnFreehand,
            _btnLine,
            _btnArrow,
            _btnRectangle,
            _btnCircle,
            _btnHighlight,
            _btnHighlightSizeDropdown,
            _btnBorrar,
            _btnBorrarModeDropdown,
            _btnCounter,
            _btnText,
            _btnTextFormatDropdown,
            _btnCrop,
            _btnConfirmCrop,
            _btnCancelCrop,
            CreateToolStripSeparator(),
            _btnUndo,
            _btnRedo,
            CreateToolStripSeparator(),
            btnCopy,
            btnSaveAs,
        ]);

        SetToolLabelsVisible(toolStrip, showToolLabels);

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

        scrollPanel.Controls.Add(_pictureBox);
        Controls.Add(scrollPanel);
        Controls.Add(toolStrip);

        CenterPictureBox(scrollPanel);
        SetActiveTool(initialTool);
    }

    /// <summary>Current selected tool in the editor.</summary>
    public EditTool SelectedTool => _currentTool;

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

    private static void SetToolLabelsVisible(ToolStrip toolStrip, bool visible)
    {
        foreach (ToolStripItem item in toolStrip.Items)
        {
            if (item is ToolStripButton button)
            {
                button.AutoSize = visible;
                if (!visible)
                {
                    button.Width = ToolButtonSize;
                }

                button.DisplayStyle = visible
                    ? ToolStripItemDisplayStyle.ImageAndText
                    : ToolStripItemDisplayStyle.Image;
            }
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

        using var icon = RenderToolIcon(kind, color, _toolIconSize);
        if (_toolIconImageList.Images.ContainsKey(imageKey))
        {
            _toolIconImageList.Images.RemoveByKey(imageKey);
        }

        _toolIconImageList.Images.Add(imageKey, icon);
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
            var mode = tool == EditTool.Blur ? BorrarMode.Solid : BorrarMode.Pixelate;
            if (_borrarMode != mode)
            {
                _borrarMode = mode;
                BorrarModeChanged?.Invoke(mode);
            }
        }
        _btnFreehand.Checked = tool == EditTool.Freehand;
        _btnLine.Checked = tool == EditTool.Line;
        _btnArrow.Checked = tool == EditTool.Arrow;
        _btnRectangle.Checked = tool == EditTool.Rectangle;
        _btnCircle.Checked = tool == EditTool.Circle;
        _btnCounter.Checked = tool == EditTool.Counter;
        _btnHighlight.Checked = tool == EditTool.Highlight;
        _btnText.Checked = tool == EditTool.Text;
        _btnCrop.Checked = tool == EditTool.Crop;
        _btnConfirmCrop.Visible = tool == EditTool.Crop;
        _btnCancelCrop.Visible = tool == EditTool.Crop;
        _btnBorrar.Checked = tool is EditTool.Blur or EditTool.Pixelated;
        UpdateBorrarToolAppearance();
        _pictureBox.Cursor = tool == EditTool.Text ? Cursors.IBeam : Cursors.Cross;
    }

    private void UpdateBorrarToolAppearance()
    {
        var isFilled = _borrarMode == BorrarMode.Solid;
        var iconKind = isFilled ? ToolIconKind.Filled : ToolIconKind.Pixelated;
        var localizedMode = AppLocalization.Translate(isFilled ? "Editor.Filled" : "Editor.Pixelated");
        var iconKey = EnsureToolIconKey(iconKind, Color.Black);

        _btnBorrar.Text = localizedMode;
        _btnBorrar.ImageKey = iconKey;
        _btnBorrar.ToolTipText = $"{localizedMode} (F8)";
        _btnBorrarModeDropdown.ImageKey = iconKey;
        _btnBorrarModeDropdown.ToolTipText = AppLocalization.Translate("Editor.PixelatedSize");
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
    }

    private void RedoLastEdit()
    {
        var replacement = _editHistory.Redo(_workingBitmap, _nextCounterValue);
        if (replacement is null)
        {
            return;
        }

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
    }

    private void PictureBox_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
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

        if (!_isDrawing)
        {
            return;
        }

        using var pen = new Pen(_selectedColor, PenWidth) { StartCap = LineCap.Round, EndCap = LineCap.Round };

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
                graphics.DrawLine(pen, _lastFreehandPoint, e.Location);
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
            var selection = ImageEditingService.CreateSelectionRectangle(_dragStart, e.Location);
            _textMinimumWidth = Math.Max(TextMinimumWidth, selection.Width);
            _textBounds = new Rectangle(_dragStart.X, e.Location.Y, _textMinimumWidth, _textFont.Height + 2);
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
            ApplyBorrarToSelection(selection);
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
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(_selectedColor, PenWidth) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                ImageEditingService.DrawShape(graphics, pen, _dragStart, e.Location, _currentTool, _selectedFillColor);
            }

            _pictureBox.Image = _workingBitmap;
            _shapePreviewVisible = false;
            _lastPreviewDirtyRect = Rectangle.Empty;
            InvalidatePictureBoxRegion(repaintRegion);
        }
    }

    private void PictureBox_Paint(object? sender, PaintEventArgs e)
    {
        if (_currentTool == EditTool.Crop)
        {
            DrawCropOverlay(e.Graphics);
            return;
        }

        if (_currentTool == EditTool.Highlight && _isDrawing)
        {
            ImageEditingService.DrawHighlighter(e.Graphics, _highlighterPoints, _selectedColor, _highlightWidth);
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

            return;
        }

        if (!_shapePreviewVisible)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        if (_currentTool is EditTool.Blur or EditTool.Pixelated)
        {
            using var selectionPen = new Pen(Color.Red, 2) { DashStyle = DashStyle.Dash };
            var selection = ImageEditingService.CreateSelectionRectangle(_dragStart, _shapePreviewEnd);
            e.Graphics.DrawRectangle(selectionPen, selection);
            return;
        }

        using var pen = new Pen(_selectedColor, PenWidth) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        ImageEditingService.DrawShape(e.Graphics, pen, _dragStart, _shapePreviewEnd, _currentTool, _selectedFillColor);
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
        }

        SetActiveTool(_toolBeforeCrop);
    }

    private void ApplyBorrarToSelection(Rectangle area)
    {
        ImageEditingService.ApplyBorrar(_workingBitmap, area, _borrarMode, _selectedColor, _borrarPixelSize);
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
        var contentSize = ImageEditingService.MeasureTextContent(_textBuffer, _textFont);
        var availableWidth = _workingBitmap.Width - _textBounds.Left;
        var availableHeight = _workingBitmap.Height - _textBounds.Top;
        var width = Math.Min(Math.Max(_textMinimumWidth, contentSize.Width + 3), availableWidth);
        var height = Math.Min(Math.Max(_textFont.Height, contentSize.Height) + 2, availableHeight);
        _textBounds = new Rectangle(_textBounds.X, _textBounds.Y, Math.Max(1, width), Math.Max(1, height));
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
        var value = _nextCounterValue;
        var text = value.ToString();
        var diameter = text.Length switch
        {
            >= 3 => 38,
            2 => 34,
            _ => 30,
        };

        var markerArea = new Rectangle(
            location.X - (diameter / 2),
            location.Y - (diameter / 2),
            diameter,
            diameter);

        using var graphics = Graphics.FromImage(_workingBitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var fillBrush = new SolidBrush(_selectedColor);
        using var borderPen = new Pen(Color.White, 2);
        graphics.FillEllipse(fillBrush, markerArea);
        graphics.DrawEllipse(borderPen, markerArea);

        var fontSize = diameter switch
        {
            >= 38 => 12F,
            >= 34 => 11F,
            _ => 10F,
        };

        var font = GetOrCreateCounterFont(fontSize);
        using var textBrush = new SolidBrush(GetContrastingTextColor(_selectedColor));
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        graphics.DrawString(text, font, textBrush, markerArea, format);

        _nextCounterValue++;
        _pictureBox.Image = _workingBitmap;
        InvalidatePictureBoxRegion(InflateRectangle(markerArea, 2));
    }

    private static Color GetContrastingTextColor(Color background)
    {
        var luminance = (0.299 * background.R) + (0.587 * background.G) + (0.114 * background.B);
        return luminance > 140 ? Color.Black : Color.White;
    }

    private void CopyToClipboard()
    {
        Clipboard.SetImage(_workingBitmap);
        Copied?.Invoke();

        if (_closeAfterCopyOrSave)
        {
            Close();
        }
    }

    private static Font GetOrCreateCounterFont(float fontSize)
    {
        lock (CounterFontSync)
        {
            if (!CounterFonts.TryGetValue(fontSize, out var font))
            {
                font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Point);
                CounterFonts[fontSize] = font;
            }

            return font;
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

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            var format = Path.GetExtension(destinationPath).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => ImageFormat.Jpeg,
                ".bmp" => ImageFormat.Bmp,
                _ => ImageFormat.Png,
            };

            using var saveCopy = (Bitmap)_workingBitmap.Clone();
            await Task.Run(() =>
            {
                using var output = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
                saveCopy.Save(output, format);
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
            MessageBox.Show(this, ex.Message, AppLocalization.Translate("App.Name"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
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
            var borrarToolIsActive = _currentTool is EditTool.Blur or EditTool.Pixelated;
            var tool = borrarToolIsActive
                ? (_currentTool == EditTool.Blur ? EditTool.Pixelated : EditTool.Blur)
                : (_borrarMode == BorrarMode.Solid ? EditTool.Blur : EditTool.Pixelated);
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

        if (keyData == Keys.T)
        {
            SetActiveTool(EditTool.Text);
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
            _toolIconImageList.Dispose();
            _editHistory.Dispose();
            _workingBitmap.Dispose();
        }

        base.Dispose(disposing);
    }
}
