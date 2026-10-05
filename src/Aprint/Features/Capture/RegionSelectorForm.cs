using Aprint.Features.Settings;
using Aprint.Features.Editor;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Aprint.Features.Capture;

/// <summary>Overlay em tela cheia (todas as telas) que permite ao usuário arrastar o mouse para selecionar uma área a capturar.</summary>
public sealed class RegionSelectorForm : Form
{
    private enum AnnotationTool
    {
        None,
        Freehand,
        Line,
        Arrow,
        Rectangle,
        Circle,
        Highlight,
        Counter,
        Text,
    }

    private const int SelectionBorderWidth = 2;
    private const int HandleSize = 10;
    private const int MinimumSelectionSize = 3;
    private const int FreehandPenWidth = ImageEditingService.AnnotationPenWidth;
    private const int ActionControlHeight = 34;
    private const int SplitDropdownWidth = 30;
    private const int IconOnlyActionWidth = 56;
    private const int IconOnlyDropdownWidth = 22;
    private static readonly Color ActiveToolBackColor = Color.FromArgb(232, 239, 244);
    private static readonly Color ActiveToolBorderColor = Color.FromArgb(183, 197, 207);
    private static readonly Color ActiveToolHoverColor = Color.FromArgb(222, 231, 237);
    private static readonly Color ActiveToolPressedColor = Color.FromArgb(211, 222, 230);
    private static AnnotationTool _lastSelectedAnnotationTool = AnnotationTool.Freehand;

    private readonly Bitmap _background;
    private readonly OutsideCaptureClickAction _outsideCaptureClickAction;
    private readonly SolidBrush _overlayBrush = new(Color.FromArgb(120, 0, 0, 0));
    private readonly Pen _borderPen = new(Color.White, SelectionBorderWidth);
    private readonly SolidBrush _handleBrush = new(Color.White);
    private readonly List<Image> _actionImages = [];
    private readonly ContextMenuStrip _drawingMenu = new();
    private readonly ContextMenuStrip _borrarMenu = new();
    private readonly ContextMenuStrip _captureActionsMenu = new();
    private readonly System.Windows.Forms.Timer _textCaretTimer = new() { Interval = 500 };
    private readonly ToolTip _actionToolTip = new();
    private readonly List<List<Point>> _freehandStrokes = [];
    private readonly List<List<Point>> _highlightStrokes = [];
    private readonly List<(Point Start, Point End, bool IsArrow)> _lineAnnotations = [];
    private readonly List<ImageEditingService.CounterMarker> _counterMarkers = [];
    private readonly List<Rectangle> _rectangles = [];
    private readonly List<Rectangle> _circles = [];
    private readonly Panel _actionBar;
    private Button? _drawingButton;
    private Button? _drawingDropdownButton;
    private Button? _highlightButton;
    private Image? _highlightIcon;
    private Button? _counterButton;
    private Button? _textButton;
    private Button? _borrarButton;
    private Button? _borrarDropdownButton;
    private Image? _pixelatedIcon;
    private Image? _filledIcon;
    private BorrarMode _borrarMode = BorrarMode.Pixelate;
    private Color _lineColor = Color.Red;
    private int _borrarPixelSize = 6;
    private int _highlightWidth = 18;
    private Color? _fillColor;
    private bool _showCaptureToolLabels = true;
    private int _nextCounterValue = 1;
    private Font _textFont = new("Segoe UI", 12F);
    private Color _textColor = Color.Red;
    private string _textBuffer = string.Empty;
    private Rectangle _textBounds;
    private int _textMinimumWidth;
    private bool _isTextSelected;
    private bool _textCaretVisible;
    private AnnotationTool _selectedAnnotationTool;
    private ToolStripMenuItem? _selectedDrawingMenuItem;
    private Image? _selectedDrawingIcon;
    private Point _startPoint;
    private Point _dragOrigin;
    private Rectangle _selection;
    private Rectangle _originalSelection;
    private ResizeEdges _resizeEdges;
    private bool _isSelecting;
    private bool _isMoving;
    private bool _isResizing;
    private AnnotationTool _annotationTool;
    private bool _isDrawingFreehand;
    private bool _isDrawingHighlight;
    private bool _isDrawingLine;
    private bool _isDrawingRectangle;
    private bool _isDrawingCircle;
    private bool _isDrawingTextSelection;
    private bool _isEditingText;
    private bool _isBorrarModeActive;
    private bool _isDrawingBorrarRegion;
    private Point _lineStart;
    private Point _lineEnd;
    private Point _rectangleStart;
    private Point _rectangleEnd;
    private Point _circleStart;
    private Point _circleEnd;
    private Point _borrarStart;
    private Point _borrarEnd;
    private Point _textStart;
    private Point _textEnd;

    public Rectangle? SelectedRegion { get; private set; }

    public RegionSelectionAction? SelectedAction { get; private set; }

    public RegionSelectorForm(Bitmap background, OutsideCaptureClickAction outsideCaptureClickAction = OutsideCaptureClickAction.SelectNewArea, BorrarMode borrarMode = BorrarMode.Pixelate, int borrarPixelSize = 6, int lineColorArgb = -65536, int highlightWidth = 18, int? fillColorArgb = null, string textFontFamily = "Segoe UI", float textFontSize = 12F, FontStyle textFontStyle = FontStyle.Regular, int textColorArgb = -65536, bool showCaptureToolLabels = true)
    {
        _background = background;
        _outsideCaptureClickAction = outsideCaptureClickAction;
        _borrarMode = borrarMode;
        _borrarPixelSize = Math.Clamp(borrarPixelSize, 4, 32);
        _lineColor = Color.FromArgb(lineColorArgb);
        _highlightWidth = Math.Clamp(highlightWidth, 4, 48);
        _fillColor = fillColorArgb is int selectedFillColorArgb ? Color.FromArgb(selectedFillColorArgb) : null;
        _textFont.Dispose();
        _textFont = CreateTextFont(textFontFamily, textFontSize, textFontStyle);
        _textColor = Color.FromArgb(textColorArgb);
        _showCaptureToolLabels = showCaptureToolLabels;
        _textCaretTimer.Tick += (_, _) =>
        {
            if (_isEditingText)
            {
                _textCaretVisible = !_textCaretVisible;
                Invalidate(_textBounds);
            }
        };

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = SystemInformation.VirtualScreen;
        TopMost = true;
        ShowInTaskbar = false;
        KeyPreview = true;
        DoubleBuffered = true;
        Cursor = Cursors.Default;
        BackColor = Color.Black;

        _actionBar = CreateActionBar();
        Controls.Add(_actionBar);
        _actionBar.Visible = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.DrawImageUnscaled(_background, Point.Empty);
        e.Graphics.FillRectangle(_overlayBrush, ClientRectangle);

        if (_selection is { Width: > 0, Height: > 0 })
        {
            // Revela o conteúdo original (sem escurecer) dentro da área selecionada.
            e.Graphics.DrawImage(_background, _selection, _selection, GraphicsUnit.Pixel);
            DrawFreehandStrokes(e.Graphics);
            DrawHighlightStrokes(e.Graphics);
            DrawLines(e.Graphics);
            DrawRectangles(e.Graphics);
            DrawCircles(e.Graphics);
            DrawCounterMarkers(e.Graphics);
            DrawTextOverlay(e.Graphics);
            if (_isDrawingBorrarRegion)
            {
                using var previewPen = new Pen(Color.Red, 2) { DashStyle = DashStyle.Dash };
                e.Graphics.DrawRectangle(previewPen, GetRectangle(_borrarStart, _borrarEnd));
            }
            e.Graphics.DrawRectangle(_borderPen, _selection);
            DrawHandles(e.Graphics);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (_isEditingText)
            {
                CommitTextEdit();
            }

            if (_selection.Width > 0 && _selection.Height > 0)
            {
                _resizeEdges = GetResizeEdges(e.Location);
                if (_resizeEdges != ResizeEdges.None)
                {
                    CommitAnnotations();
                    _isResizing = true;
                    Cursor = GetResizeCursor(_resizeEdges);
                    _dragOrigin = e.Location;
                    _originalSelection = _selection;
                    return;
                }

                if (_annotationTool == AnnotationTool.Counter && _selection.Contains(e.Location))
                {
                    AddCounterMarker(ClampToSelection(e.Location));
                    return;
                }

                if (_annotationTool == AnnotationTool.Text && _selection.Contains(e.Location))
                {
                    _isDrawingTextSelection = true;
                    _textStart = ClampToSelection(e.Location);
                    _textEnd = _textStart;
                    Capture = true;
                    Invalidate(GetTextPreviewBounds(_textStart, _textEnd));
                    return;
                }

                if (_annotationTool == AnnotationTool.Freehand && _selection.Contains(e.Location))
                {
                    _isDrawingFreehand = true;
                    _freehandStrokes.Add([ClampToSelection(e.Location)]);
                    Capture = true;
                    return;
                }

                if (_annotationTool == AnnotationTool.Highlight && _selection.Contains(e.Location))
                {
                    var point = ClampToSelection(e.Location);
                    _isDrawingHighlight = true;
                    _highlightStrokes.Add([point]);
                    Capture = true;
                    Invalidate(GetHighlightDirtyRectangle(point, point));
                    return;
                }

                if (_annotationTool is AnnotationTool.Line or AnnotationTool.Arrow && _selection.Contains(e.Location))
                {
                    _isDrawingLine = true;
                    _lineStart = ClampToSelection(e.Location);
                    _lineEnd = _lineStart;
                    Capture = true;
                    return;
                }

                if (_annotationTool == AnnotationTool.Rectangle && _selection.Contains(e.Location))
                {
                    _isDrawingRectangle = true;
                    _rectangleStart = ClampToSelection(e.Location);
                    _rectangleEnd = _rectangleStart;
                    Capture = true;
                    return;
                }

                if (_annotationTool == AnnotationTool.Circle && _selection.Contains(e.Location))
                {
                    _isDrawingCircle = true;
                    _circleStart = ClampToSelection(e.Location);
                    _circleEnd = _circleStart;
                    Capture = true;
                    return;
                }

                if (_isBorrarModeActive && _selection.Contains(e.Location))
                {
                    _isDrawingBorrarRegion = true;
                    _borrarStart = ClampToSelection(e.Location);
                    _borrarEnd = _borrarStart;
                    Capture = true;
                    return;
                }

                if (_selection.Contains(e.Location))
                {
                    CommitAnnotations();
                    _isMoving = true;
                    Cursor = Cursors.SizeAll;
                    _dragOrigin = e.Location;
                    _originalSelection = _selection;
                    return;
                }

                if (_outsideCaptureClickAction == OutsideCaptureClickAction.ExitCaptureMode)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                    return;
                }

                if (_outsideCaptureClickAction == OutsideCaptureClickAction.None)
                {
                    return;
                }
            }

            _actionBar.Visible = false;
            CommitAnnotations();
            _nextCounterValue = 1;
            _isSelecting = true;
            Cursor = Cursors.Cross;
            _startPoint = e.Location;
            _selection = new Rectangle(e.Location, Size.Empty);
            Invalidate();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_isDrawingFreehand)
        {
            AddFreehandPoint(e.Location);
            return;
        }

        if (_isDrawingHighlight)
        {
            AddHighlightPoint(e.Location);
            return;
        }

        if (_isDrawingTextSelection)
        {
            var previousBounds = GetTextPreviewBounds(_textStart, _textEnd);
            _textEnd = ClampToSelection(e.Location);
            Invalidate(Rectangle.Union(previousBounds, GetTextPreviewBounds(_textStart, _textEnd)));
            return;
        }

        if (_isDrawingLine)
        {
            var previousEnd = _lineEnd;
            _lineEnd = ClampToSelection(e.Location);
            InvalidateLine(previousEnd, _lineEnd);
            return;
        }

        if (_isDrawingRectangle)
        {
            var previousEnd = _rectangleEnd;
            _rectangleEnd = ClampToSelection(e.Location);
            InvalidateShapePreview(_rectangleStart, previousEnd, _rectangleEnd);
            return;
        }

        if (_isDrawingCircle)
        {
            var previousEnd = _circleEnd;
            _circleEnd = ClampToSelection(e.Location);
            InvalidateShapePreview(_circleStart, previousEnd, _circleEnd);
            return;
        }

        if (_isDrawingBorrarRegion)
        {
            var previousEnd = _borrarEnd;
            _borrarEnd = ClampToSelection(e.Location);
            InvalidateShapePreview(_borrarStart, previousEnd, _borrarEnd);
            return;
        }

        if (_isSelecting || _isResizing || _isMoving)
        {
            var nextSelection = _isSelecting
                ? GetRectangle(_startPoint, e.Location)
                : _isResizing
                    ? ResizeSelection(e.Location)
                    : MoveSelection(e.Location);
            if (nextSelection == _selection)
            {
                return;
            }

            var invalidRectangle = Rectangle.Union(_selection, nextSelection);
            invalidRectangle.Inflate(HandleSize, HandleSize);
            _selection = nextSelection;
            Invalidate(invalidRectangle);
        }

        if (!_isSelecting && !_isResizing && !_isMoving)
        {
            Cursor = GetCursorForLocation(e.Location);
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        if (_isDrawingTextSelection)
        {
            _textEnd = ClampToSelection(e.Location);
            _isDrawingTextSelection = false;
            Capture = false;
            BeginTextEdit(_textStart, _textEnd);
            return;
        }

        if (_isDrawingFreehand)
        {
            AddFreehandPoint(e.Location);
            _isDrawingFreehand = false;
            Capture = false;
            return;
        }

        if (_isDrawingHighlight)
        {
            AddHighlightPoint(e.Location);
            _isDrawingHighlight = false;
            Capture = false;
            return;
        }

        if (_isDrawingLine)
        {
            var previousEnd = _lineEnd;
            _lineEnd = ClampToSelection(e.Location);
            _lineAnnotations.Add((_lineStart, _lineEnd, _annotationTool == AnnotationTool.Arrow));
            _isDrawingLine = false;
            Capture = false;
            InvalidateLine(previousEnd, _lineEnd);
            return;
        }

        if (_isDrawingRectangle)
        {
            var previousEnd = _rectangleEnd;
            _rectangleEnd = ClampToSelection(e.Location);
            _rectangles.Add(GetRectangle(_rectangleStart, _rectangleEnd));
            _isDrawingRectangle = false;
            Capture = false;
            InvalidateShapePreview(_rectangleStart, previousEnd, _rectangleEnd);
            return;
        }

        if (_isDrawingCircle)
        {
            var previousEnd = _circleEnd;
            _circleEnd = ClampToSelection(e.Location);
            _circles.Add(GetRectangle(_circleStart, _circleEnd));
            _isDrawingCircle = false;
            Capture = false;
            InvalidateShapePreview(_circleStart, previousEnd, _circleEnd);
            return;
        }

        if (_isDrawingBorrarRegion)
        {
            var previousEnd = _borrarEnd;
            _borrarEnd = ClampToSelection(e.Location);
            var area = GetRectangle(_borrarStart, _borrarEnd);
            _isDrawingBorrarRegion = false;
            Capture = false;
            if (area.Width > 0 && area.Height > 0)
            {
                ImageEditingService.ApplyBorrar(_background, area, _borrarMode, _lineColor, _borrarPixelSize);
            }

            InvalidateShapePreview(_borrarStart, previousEnd, _borrarEnd);
            return;
        }

        if (_isResizing || _isMoving)
        {
            _isResizing = false;
            _isMoving = false;
            UpdateActionBarLocation();
            Cursor = Cursors.Default;
            return;
        }

        if (!_isSelecting)
        {
            return;
        }

        _isSelecting = false;
    Cursor = Cursors.Default;
        _selection = GetRectangle(_startPoint, e.Location);

        if (_selection.Width >= MinimumSelectionSize && _selection.Height >= MinimumSelectionSize)
        {
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                CompleteSelection(RegionSelectionAction.Capture);
                return;
            }

            _actionBar.Visible = true;
            UpdateActionBarLocation();
            Invalidate();
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (_isEditingText)
        {
            if (keyData == Keys.Escape)
            {
                CancelTextEdit();
                return true;
            }

            if ((keyData & Keys.Control) != 0 && (keyData & Keys.KeyCode) == Keys.A)
            {
                _isTextSelected = _textBuffer.Length > 0;
                _textCaretVisible = false;
                Invalidate(_textBounds);
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

            if (keyData == (Keys.Control | Keys.Enter))
            {
                CommitTextEdit();
                return true;
            }

            if (keyData == Keys.Enter)
            {
                AppendTextNewLine();
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

        if (TryHandleEditorToolShortcut(keyData))
        {
            return true;
        }

        if (keyData == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return true;
        }

        if (_actionBar.Visible)
        {
            var action = keyData switch
            {
                Keys.Enter => RegionSelectionAction.Capture,
                Keys.Control | Keys.S => RegionSelectionAction.Save,
                Keys.Control | Keys.C => RegionSelectionAction.Copy,
                _ => (RegionSelectionAction?)null,
            };

            if (action is { } selectedAction)
            {
                CompleteSelection(selectedAction);
                return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        if (!_isEditingText)
        {
            base.OnKeyPress(e);
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _overlayBrush.Dispose();
            _borderPen.Dispose();
            _handleBrush.Dispose();
            _drawingMenu.Dispose();
            _borrarMenu.Dispose();
            _captureActionsMenu.Dispose();
            _textCaretTimer.Stop();
            _textCaretTimer.Dispose();
            _actionToolTip.Dispose();
            _textFont.Dispose();
            foreach (var image in _actionImages)
            {
                image.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private static Rectangle GetRectangle(Point p1, Point p2) => new(
        Math.Min(p1.X, p2.X),
        Math.Min(p1.Y, p2.Y),
        Math.Abs(p1.X - p2.X),
        Math.Abs(p1.Y - p2.Y));

    private Panel CreateActionBar()
    {
        var panel = new Panel
        {
            BackColor = Color.FromArgb(246, 247, 249),
            BorderStyle = BorderStyle.FixedSingle,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(0, 52),
            Padding = new Padding(6),
        };

        var captureLabel = AppLocalization.Translate("Capture.Region.Capture");
        var captureButton = CreateActionButton(GetCaptureToolButtonText(captureLabel), true, LoadActionIcon("capture.png", Color.White));
        SetSplitMainButtonSize(captureButton);
        captureButton.Margin = Padding.Empty;
        captureButton.FlatAppearance.BorderSize = 1;
        captureButton.FlatAppearance.BorderColor = Color.FromArgb(0, 88, 155);
        captureButton.AccessibleName = captureLabel;
        _actionToolTip.SetToolTip(captureButton, $"{captureLabel} (Enter)");
        captureButton.Click += (_, _) => CompleteSelection(RegionSelectionAction.Capture);

        var captureDropdownButton = CreateActionButton("\u25BE", true, null);
        captureDropdownButton.AutoSize = false;
        captureDropdownButton.MinimumSize = new Size(SplitDropdownWidth, ActionControlHeight);
        captureDropdownButton.Size = new Size(SplitDropdownWidth, ActionControlHeight);
        captureDropdownButton.Margin = new Padding(-1, 0, 0, 0);
        captureDropdownButton.Padding = Padding.Empty;
        captureDropdownButton.FlatAppearance.BorderSize = 1;
        captureDropdownButton.FlatAppearance.BorderColor = Color.FromArgb(0, 88, 155);
        captureDropdownButton.AccessibleName = AppLocalization.Translate("Capture.Region.Actions");
        captureDropdownButton.Click += (_, _) => _captureActionsMenu.Show(captureDropdownButton, new Point(0, captureDropdownButton.Height));

        var captureActionsPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor = AnchorStyles.None,
            BackColor = panel.BackColor,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(3),
            Padding = Padding.Empty,
            WrapContents = false,
        };
        ApplyCaptureSplitButtonSize(captureActionsPanel, captureButton, captureDropdownButton);
        captureActionsPanel.Controls.Add(captureButton);
        captureActionsPanel.Controls.Add(captureDropdownButton);

        var captureMenuItem = new ToolStripMenuItem(captureLabel, LoadActionIcon("capture.png", Color.FromArgb(45, 49, 54)))
        {
            ShortcutKeyDisplayString = "Enter",
        };
        captureMenuItem.Click += (_, _) => CompleteSelection(RegionSelectionAction.Capture);
        var copyMenuItem = new ToolStripMenuItem(AppLocalization.Translate("Capture.Region.Copy"), LoadActionIcon("copy.png", Color.FromArgb(45, 49, 54)))
        {
            ShortcutKeyDisplayString = "Ctrl+C",
        };
        copyMenuItem.Click += (_, _) => CompleteSelection(RegionSelectionAction.Copy);
        var saveMenuItem = new ToolStripMenuItem(AppLocalization.Translate("Capture.Region.Save"), LoadActionIcon("save.png", Color.FromArgb(45, 49, 54)))
        {
            ShortcutKeyDisplayString = "Ctrl+S",
        };
        saveMenuItem.Click += (_, _) => CompleteSelection(RegionSelectionAction.Save);
        _captureActionsMenu.Items.AddRange([captureMenuItem, copyMenuItem, saveMenuItem]);

        var drawingLabel = AppLocalization.Translate("Capture.Region.Drawing");
        var drawingButton = CreateActionButton(GetCaptureToolButtonText(drawingLabel), false, null);
        SetSplitMainButtonSize(drawingButton);
        _drawingButton = drawingButton;
        drawingButton.AccessibleName = drawingLabel;
        _actionToolTip.SetToolTip(drawingButton, drawingLabel);
        drawingButton.Margin = Padding.Empty;
        drawingButton.FlatAppearance.BorderSize = 0;
        drawingButton.Click += (_, _) => ToggleDrawingTool();

        var drawingDropdownButton = CreateActionButton("\u25BE", false, null);
        _drawingDropdownButton = drawingDropdownButton;
        drawingDropdownButton.AutoSize = false;
        drawingDropdownButton.MinimumSize = new Size(SplitDropdownWidth, ActionControlHeight);
        drawingDropdownButton.Size = new Size(SplitDropdownWidth, ActionControlHeight);
        drawingDropdownButton.Margin = new Padding(-1, 0, 0, 0);
        drawingDropdownButton.Padding = Padding.Empty;
        drawingDropdownButton.FlatAppearance.BorderSize = 0;
        drawingDropdownButton.AccessibleName = drawingLabel;
        drawingDropdownButton.Click += (_, _) => _drawingMenu.Show(drawingDropdownButton, new Point(0, drawingDropdownButton.Height));

        var drawingActionsPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor = AnchorStyles.None,
            BackColor = panel.BackColor,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(3),
            Padding = Padding.Empty,
            WrapContents = false,
        };
        ApplyCaptureSplitButtonSize(drawingActionsPanel, drawingButton, drawingDropdownButton);
        drawingActionsPanel.Controls.Add(drawingButton);
        drawingActionsPanel.Controls.Add(drawingDropdownButton);

        var highlightLabel = AppLocalization.Translate("Editor.Highlight");
        _highlightIcon = LoadActionIcon("highlight.png", Color.FromArgb(45, 49, 54));
        var highlightButton = CreateActionButton(GetCaptureToolButtonText(highlightLabel), false, _highlightIcon);
        ApplyCaptureToolButtonSize(highlightButton);
        _highlightButton = highlightButton;
        highlightButton.AccessibleName = highlightLabel;
        _actionToolTip.SetToolTip(highlightButton, $"{highlightLabel} (F7)");
        highlightButton.Click += (_, _) => ToggleStandaloneTool(AnnotationTool.Highlight);

        var counterLabel = AppLocalization.Translate("Editor.Counter");
        var counterIcon = LoadActionIcon("counter.png", Color.FromArgb(45, 49, 54));
        var counterButton = CreateActionButton(GetCaptureToolButtonText(counterLabel), false, counterIcon);
        ApplyCaptureToolButtonSize(counterButton);
        _counterButton = counterButton;
        counterButton.AccessibleName = counterLabel;
        _actionToolTip.SetToolTip(counterButton, $"{counterLabel} (F9)");
        counterButton.Click += (_, _) => ToggleStandaloneTool(AnnotationTool.Counter);

        var textLabel = AppLocalization.Translate("Editor.Text");
        var textButton = CreateActionButton(GetCaptureToolButtonText(textLabel), false, LoadActionIcon("text.png", Color.FromArgb(45, 49, 54)));
        ApplyCaptureToolButtonSize(textButton);
        _textButton = textButton;
        textButton.AccessibleName = textLabel;
        _actionToolTip.SetToolTip(textButton, $"{textLabel} (F10)");
        textButton.Click += (_, _) => ToggleStandaloneTool(AnnotationTool.Text);

        _pixelatedIcon = LoadActionIcon("pixelated.png", Color.FromArgb(45, 49, 54));
        _filledIcon = LoadActionIcon("filled.png", Color.FromArgb(45, 49, 54));
        var borrarLabel = GetBorrarModeLabel(_borrarMode);
        var borrarButton = CreateActionButton(GetCaptureToolButtonText(borrarLabel), false, _borrarMode == BorrarMode.Pixelate ? _pixelatedIcon : _filledIcon);
        SetSplitMainButtonSize(borrarButton);
        _borrarButton = borrarButton;
        borrarButton.AccessibleName = AppLocalization.Translate("Editor.Mask");
        borrarButton.AccessibleDescription = borrarLabel;
        _actionToolTip.SetToolTip(borrarButton, $"{AppLocalization.Translate("Editor.Mask")}: {borrarLabel} (F8)");
        borrarButton.Margin = Padding.Empty;
        borrarButton.FlatAppearance.BorderSize = 0;
        borrarButton.Click += (_, _) => ToggleBorrarMode();

        var borrarDropdownButton = CreateActionButton("\u25BE", false, null);
        _borrarDropdownButton = borrarDropdownButton;
        borrarDropdownButton.AutoSize = false;
        borrarDropdownButton.MinimumSize = new Size(SplitDropdownWidth, ActionControlHeight);
        borrarDropdownButton.Size = new Size(SplitDropdownWidth, ActionControlHeight);
        borrarDropdownButton.Margin = new Padding(-1, 0, 0, 0);
        borrarDropdownButton.Padding = Padding.Empty;
        borrarDropdownButton.FlatAppearance.BorderSize = 0;
        borrarDropdownButton.AccessibleName = AppLocalization.Translate("Editor.MaskOptions");
        borrarDropdownButton.Click += (_, _) => _borrarMenu.Show(borrarDropdownButton, new Point(0, borrarDropdownButton.Height));

        var borrarActionsPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor = AnchorStyles.None,
            BackColor = panel.BackColor,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(3),
            Padding = Padding.Empty,
            WrapContents = false,
        };
        ApplyCaptureSplitButtonSize(borrarActionsPanel, borrarButton, borrarDropdownButton);
        borrarActionsPanel.Controls.Add(borrarButton);
        borrarActionsPanel.Controls.Add(borrarDropdownButton);

        AddAnnotationMenuItem(drawingButton, AnnotationTool.Freehand, "Capture.Region.Freehand", "freehand.png");
        AddAnnotationMenuItem(drawingButton, AnnotationTool.Line, "Capture.Region.Line", "line.png");
        AddAnnotationMenuItem(drawingButton, AnnotationTool.Arrow, "Capture.Region.Arrow", "arrow.png");
        AddAnnotationMenuItem(drawingButton, AnnotationTool.Rectangle, "Capture.Region.Rectangle", "square.png");
        AddAnnotationMenuItem(drawingButton, AnnotationTool.Circle, "Capture.Region.Circle", "circle.png");
        RestoreLastAnnotationTool(drawingButton);
        AddBorrarMenuItem(BorrarMode.Pixelate, "Editor.Pixelated", _pixelatedIcon);
        AddBorrarMenuItem(BorrarMode.Solid, "Editor.Filled", _filledIcon);

        var layout = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = panel.BackColor,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            WrapContents = false,
        };
        layout.Controls.Add(drawingActionsPanel);
        layout.Controls.Add(highlightButton);
        layout.Controls.Add(borrarActionsPanel);
        layout.Controls.Add(counterButton);
        layout.Controls.Add(textButton);
        layout.Controls.Add(captureActionsPanel);
        layout.Location = new Point(panel.Padding.Left, panel.Padding.Top);
        panel.Controls.Add(layout);
        return panel;
    }

    private void AddAnnotationMenuItem(Button drawingButton, AnnotationTool tool, string localizationKey, string iconFileName)
    {
        var icon = LoadActionIcon(iconFileName, Color.FromArgb(45, 49, 54));
        var label = AppLocalization.Translate(localizationKey);
        var menuItem = new ToolStripMenuItem(label, icon)
        {
            Tag = tool,
            ShortcutKeyDisplayString = GetDrawingToolShortcut(tool),
        };
        menuItem.Click += (_, _) => SelectAnnotationTool(tool, drawingButton, menuItem, icon);
        _drawingMenu.Items.Add(menuItem);
    }

    private static string GetDrawingToolShortcut(AnnotationTool tool) => tool switch
    {
        AnnotationTool.Freehand => "F2",
        AnnotationTool.Line => "F3",
        AnnotationTool.Arrow => "F4",
        AnnotationTool.Rectangle => "F5",
        AnnotationTool.Circle => "F6",
        _ => string.Empty,
    };

    private void RestoreLastAnnotationTool(Button drawingButton)
    {
        var menuItem = _drawingMenu.Items
            .OfType<ToolStripMenuItem>()
            .FirstOrDefault(item => item.Tag is AnnotationTool tool && tool == _lastSelectedAnnotationTool);
        if (menuItem is null || menuItem.Tag is not AnnotationTool selectedTool || menuItem.Image is not Image selectedIcon)
        {
            return;
        }

        SelectAnnotationTool(selectedTool, drawingButton, menuItem, selectedIcon);
    }

    private void AddBorrarMenuItem(BorrarMode mode, string localizationKey, Image icon)
    {
        var menuItem = new ToolStripMenuItem(AppLocalization.Translate(localizationKey), icon)
        {
            Checked = mode == _borrarMode,
            Tag = mode,
        };
        menuItem.Click += (_, _) => SelectBorrarMode(mode, menuItem, icon);
        _borrarMenu.Items.Add(menuItem);
    }

    private Button CreateActionButton(string text, bool isPrimary, Image? image)
    {
        var button = new Button
        {
            Text = text,
            Image = image,
            ImageAlign = ContentAlignment.MiddleCenter,
            TextAlign = ContentAlignment.MiddleCenter,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            AutoSize = true,
            Anchor = AnchorStyles.None,
            MinimumSize = new Size(0, ActionControlHeight),
            Margin = new Padding(3),
            Padding = new Padding(6, 3, 6, 3),
            FlatStyle = FlatStyle.Flat,
            BackColor = isPrimary ? Color.FromArgb(0, 103, 192) : Color.White,
            ForeColor = isPrimary ? Color.White : Color.FromArgb(45, 49, 54),
            UseVisualStyleBackColor = false,
            TabStop = false,
        };

        button.FlatAppearance.BorderSize = isPrimary ? 0 : 1;
        button.FlatAppearance.BorderColor = Color.FromArgb(214, 219, 225);
        button.FlatAppearance.MouseOverBackColor = isPrimary
            ? Color.FromArgb(0, 90, 158)
            : Color.FromArgb(241, 244, 247);
        button.FlatAppearance.MouseDownBackColor = isPrimary
            ? Color.FromArgb(0, 75, 132)
            : Color.FromArgb(230, 235, 240);
        return button;
    }

    private string GetCaptureToolButtonText(string label) => _showCaptureToolLabels ? label : string.Empty;

    private void ApplyCaptureToolButtonSize(Button button)
    {
        if (_showCaptureToolLabels)
        {
            return;
        }

        button.AutoSize = false;
        button.MinimumSize = new Size(IconOnlyActionWidth, ActionControlHeight);
        button.Size = new Size(IconOnlyActionWidth, ActionControlHeight);
    }

    private void ApplyCaptureSplitButtonSize(FlowLayoutPanel panel, Button mainButton, Button dropdownButton)
    {
        if (_showCaptureToolLabels)
        {
            return;
        }

        var mainButtonWidth = IconOnlyActionWidth - IconOnlyDropdownWidth + 1;
        mainButton.AutoSize = false;
        mainButton.MinimumSize = new Size(mainButtonWidth, ActionControlHeight);
        mainButton.Size = new Size(mainButtonWidth, ActionControlHeight);
        dropdownButton.MinimumSize = new Size(IconOnlyDropdownWidth, ActionControlHeight);
        dropdownButton.Size = new Size(IconOnlyDropdownWidth, ActionControlHeight);
        panel.AutoSize = true;
        panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
    }

    private static void SetSplitMainButtonSize(Button button)
    {
        button.AutoSize = true;
        button.MinimumSize = new Size(0, ActionControlHeight);
    }

    private Bitmap LoadActionIcon(string fileName, Color color)
    {
        var resourceName = $"Aprint.Resources.Icons.{fileName}";
        using var stream = typeof(RegionSelectorForm).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Recurso incorporado '{resourceName}' não encontrado.");
        using var sourceImage = Image.FromStream(stream);

        var icon = new Bitmap(18, 18, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(icon))
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(sourceImage, new Rectangle(0, 0, icon.Width, icon.Height));
        }

        for (var y = 0; y < icon.Height; y++)
        {
            for (var x = 0; x < icon.Width; x++)
            {
                var pixel = icon.GetPixel(x, y);
                if (pixel.A > 0)
                {
                    icon.SetPixel(x, y, Color.FromArgb(pixel.A, color));
                }
            }
        }

        _actionImages.Add(icon);
        return icon;
    }

    private void CompleteSelection(RegionSelectionAction action)
    {
        CommitTextEdit();
        CommitAnnotations();

        SelectedRegion = _selection;
        SelectedAction = action;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void CommitAnnotations()
    {
        if (_freehandStrokes.Count > 0 || _highlightStrokes.Count > 0 || _lineAnnotations.Count > 0 || _counterMarkers.Count > 0 || _rectangles.Count > 0 || _circles.Count > 0 || _isDrawingLine || _isDrawingRectangle || _isDrawingCircle)
        {
            using var graphics = Graphics.FromImage(_background);
            DrawFreehandStrokes(graphics);
            DrawHighlightStrokes(graphics);
            DrawLines(graphics);
            DrawRectangles(graphics);
            DrawCircles(graphics);
            DrawCounterMarkers(graphics);
        }

        _freehandStrokes.Clear();
        _highlightStrokes.Clear();
        _lineAnnotations.Clear();
        _rectangles.Clear();
        _circles.Clear();
        _counterMarkers.Clear();
    }

    private void BeginTextEdit(Point start, Point end)
    {
        _textBounds = ImageEditingService.CreateTextBounds(start, end, _selection, _textFont, out _textMinimumWidth);
        _textBuffer = string.Empty;
        _isTextSelected = false;
        _isEditingText = true;
        _textCaretVisible = true;
        _textCaretTimer.Start();
        Cursor = Cursors.IBeam;
        Focus();
        Invalidate(_textBounds);
    }

    private void ResizeTextBoundsToContent()
    {
        if (!_isEditingText)
        {
            return;
        }

        var previousBounds = _textBounds;
        var contentSize = ImageEditingService.MeasureTextContent(_textBuffer, _textFont);
        _textBounds = ImageEditingService.ResizeTextBounds(_textBounds, _textMinimumWidth, _textBuffer, _textFont, _selection);
        Invalidate(Rectangle.Union(previousBounds, _textBounds));
    }

    private void AppendTextNewLine()
    {
        if (_isTextSelected)
        {
            _textBuffer = string.Empty;
            _isTextSelected = false;
        }

        _textBuffer += Environment.NewLine;
        _textCaretVisible = true;
        ResizeTextBoundsToContent();
    }

    private void CommitTextEdit()
    {
        if (!_isEditingText)
        {
            return;
        }

        _textCaretTimer.Stop();
        _isEditingText = false;
        _isTextSelected = false;
        if (_textBuffer.Length > 0)
        {
            ImageEditingService.ApplyText(_background, _textBounds, _textBuffer, _textColor, _textFont);
        }

        _textBuffer = string.Empty;
        _textCaretVisible = false;
        Cursor = GetCursorForLocation(PointToClient(Cursor.Position));
        Invalidate(_textBounds);
    }

    private void CancelTextEdit()
    {
        _isEditingText = false;
        _textCaretTimer.Stop();
        _isTextSelected = false;
        _textBuffer = string.Empty;
        _textCaretVisible = false;
        Cursor = GetCursorForLocation(PointToClient(Cursor.Position));
        Invalidate(_textBounds);
    }

    private void DrawTextOverlay(Graphics graphics)
    {
        if (_isDrawingTextSelection)
        {
            using var selectionPen = new Pen(Color.Gray) { DashStyle = DashStyle.Dash };
            graphics.DrawRectangle(selectionPen, GetRectangle(_textStart, _textEnd));
            return;
        }

        if (!_isEditingText)
        {
            return;
        }

        if (_isTextSelected)
        {
            using var selectionBrush = new SolidBrush(SystemColors.Highlight);
            graphics.FillRectangle(selectionBrush, Rectangle.Inflate(_textBounds, -1, -1));
        }

        var textColor = _isTextSelected ? SystemColors.HighlightText : _textColor;
        ImageEditingService.DrawText(graphics, _textBounds, _textBuffer, textColor, _textFont);
        using var borderPen = new Pen(Color.Gray) { DashStyle = DashStyle.Dash };
        graphics.DrawRectangle(borderPen, _textBounds.X, _textBounds.Y, _textBounds.Width - 1, _textBounds.Height - 1);

        if (!_textCaretVisible || _isTextSelected)
        {
            return;
        }

        var lines = _textBuffer.Split('\n');
        var lastLine = lines[^1].TrimEnd('\r');
        var caretSize = TextRenderer.MeasureText(lastLine, _textFont, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        var caretX = Math.Min(_textBounds.Right - 1, _textBounds.Left + 1 + caretSize.Width);
        var caretY = Math.Min(_textBounds.Bottom - 1, _textBounds.Top + 1 + ((lines.Length - 1) * _textFont.Height));
        using var caretPen = new Pen(_textColor);
        graphics.DrawLine(caretPen, caretX, caretY, caretX, Math.Min(_textBounds.Bottom - 1, caretY + _textFont.Height));
    }

    private Rectangle GetTextPreviewBounds(Point start, Point end) => Rectangle.Inflate(GetRectangle(start, end), 2, 2);

    private static Font CreateTextFont(string family, float size, FontStyle style)
    {
        size = float.IsFinite(size) ? Math.Clamp(size, 6F, 72F) : 12F;
        style &= FontStyle.Bold | FontStyle.Italic | FontStyle.Underline | FontStyle.Strikeout;
        var familyName = string.IsNullOrWhiteSpace(family) ? "Segoe UI" : family;
        try
        {
            return new Font(familyName, size, style, GraphicsUnit.Point);
        }
        catch (ArgumentException)
        {
            return new Font("Segoe UI", size, style, GraphicsUnit.Point);
        }
    }

    private void AddFreehandPoint(Point location)
    {
        if (_freehandStrokes.Count == 0)
        {
            return;
        }

        var point = ClampToSelection(location);
        var stroke = _freehandStrokes[^1];
        var previousPoint = stroke[^1];
        if (point == previousPoint)
        {
            return;
        }

        stroke.Add(point);
        var dirtyRectangle = Rectangle.FromLTRB(
            Math.Min(previousPoint.X, point.X),
            Math.Min(previousPoint.Y, point.Y),
            Math.Max(previousPoint.X, point.X) + 1,
            Math.Max(previousPoint.Y, point.Y) + 1);
        dirtyRectangle.Inflate(FreehandPenWidth + 2, FreehandPenWidth + 2);
        Invalidate(Rectangle.Intersect(dirtyRectangle, _selection));
    }

    private void AddHighlightPoint(Point location)
    {
        if (_highlightStrokes.Count == 0)
        {
            return;
        }

        var point = ClampToSelection(location);
        var stroke = _highlightStrokes[^1];
        var previousPoint = stroke[^1];
        if (point == previousPoint)
        {
            return;
        }

        stroke.Add(point);
        Invalidate(Rectangle.Intersect(GetHighlightDirtyRectangle(previousPoint, point), _selection));
    }

    private Rectangle GetHighlightDirtyRectangle(Point start, Point end)
    {
        var dirtyRectangle = CreateLineRectangle(start, end);
        dirtyRectangle.Inflate((_highlightWidth / 2) + 2, (_highlightWidth / 2) + 2);
        return dirtyRectangle;
    }

    private void InvalidateLine(Point start, Point end)
    {
        var dirtyRectangle = Rectangle.Union(
            CreateLineRectangle(_lineStart, start),
            CreateLineRectangle(_lineStart, end));
        dirtyRectangle.Inflate(FreehandPenWidth + 2, FreehandPenWidth + 2);
        Invalidate(Rectangle.Intersect(dirtyRectangle, _selection));
    }

    private static Rectangle CreateLineRectangle(Point start, Point end) => Rectangle.FromLTRB(
            Math.Min(start.X, end.X),
            Math.Min(start.Y, end.Y),
            Math.Max(start.X, end.X) + 1,
            Math.Max(start.Y, end.Y) + 1);

    private void SelectAnnotationTool(AnnotationTool tool, Button drawingButton, ToolStripMenuItem? selectedMenuItem, Image selectedIcon)
        => SelectAnnotationTool(tool, drawingButton, selectedMenuItem, selectedIcon, toggleIfAlreadyActive: true);

    private void SelectAnnotationTool(AnnotationTool tool, Button drawingButton, ToolStripMenuItem? selectedMenuItem, Image selectedIcon, bool toggleIfAlreadyActive)
    {
        if (_isEditingText)
        {
            CommitTextEdit();
        }

        if (tool is AnnotationTool.Highlight or AnnotationTool.Counter)
        {
            ToggleStandaloneTool(tool);
            return;
        }

        var isTogglingOff = toggleIfAlreadyActive && _annotationTool == tool && _selectedAnnotationTool == tool;
        _lastSelectedAnnotationTool = tool;
        _selectedAnnotationTool = tool;
        _selectedDrawingMenuItem = selectedMenuItem;
        _selectedDrawingIcon = selectedIcon;
        _annotationTool = isTogglingOff ? AnnotationTool.None : tool;
        _isBorrarModeActive = false;
        Cursor = GetCursorForLocation(PointToClient(Cursor.Position));
        UpdateBorrarButtonAppearance();
        foreach (ToolStripMenuItem menuItem in _drawingMenu.Items)
        {
            menuItem.Checked = menuItem == _selectedDrawingMenuItem;
        }

        drawingButton.Image = _selectedDrawingIcon;
        _actionToolTip.SetToolTip(drawingButton, $"{AppLocalization.Translate("Capture.Region.Drawing")}: {selectedMenuItem?.Text ?? tool.ToString()} ({GetDrawingToolShortcut(tool)})");
        UpdateDrawingButtonAppearance();
    }

    private void ToggleStandaloneTool(AnnotationTool tool)
        => SetStandaloneTool(tool, toggleIfAlreadyActive: true);

    private void SetStandaloneTool(AnnotationTool tool, bool toggleIfAlreadyActive)
    {
        if (_isEditingText)
        {
            CommitTextEdit();
        }

        _annotationTool = toggleIfAlreadyActive && _annotationTool == tool
            ? AnnotationTool.None
            : tool;
        _isBorrarModeActive = false;
        Cursor = GetCursorForLocation(PointToClient(Cursor.Position));
        UpdateBorrarButtonAppearance();
        UpdateDrawingButtonAppearance();
    }

    private bool TryHandleEditorToolShortcut(Keys keyData)
    {
        if (!_actionBar.Visible)
        {
            return false;
        }

        var drawingTool = keyData switch
        {
            Keys.F2 => AnnotationTool.Freehand,
            Keys.F3 => AnnotationTool.Line,
            Keys.F4 => AnnotationTool.Arrow,
            Keys.F5 => AnnotationTool.Rectangle,
            Keys.F6 => AnnotationTool.Circle,
            _ => AnnotationTool.None,
        };

        if (drawingTool != AnnotationTool.None && _drawingButton is not null)
        {
            var menuItem = _drawingMenu.Items
                .OfType<ToolStripMenuItem>()
                .FirstOrDefault(item => item.Tag is AnnotationTool tool && tool == drawingTool);
            if (menuItem?.Image is Image icon)
            {
                SelectAnnotationTool(drawingTool, _drawingButton, menuItem, icon, toggleIfAlreadyActive: false);
                return true;
            }
        }

        switch (keyData)
        {
            case Keys.F7:
                SetStandaloneTool(AnnotationTool.Highlight, toggleIfAlreadyActive: false);
                return true;
            case Keys.F8:
                var mode = _isBorrarModeActive
                    ? (_borrarMode == BorrarMode.Solid ? BorrarMode.Pixelate : BorrarMode.Solid)
                    : _borrarMode;
                var modeMenuItem = _borrarMenu.Items
                    .OfType<ToolStripMenuItem>()
                    .FirstOrDefault(item => item.Tag is BorrarMode menuMode && menuMode == mode);
                if (modeMenuItem?.Image is Image modeIcon)
                {
                    SelectBorrarMode(mode, modeMenuItem, modeIcon);
                    return true;
                }
                return false;
            case Keys.F9:
                SetStandaloneTool(AnnotationTool.Counter, toggleIfAlreadyActive: false);
                return true;
            case Keys.F10:
                SetStandaloneTool(AnnotationTool.Text, toggleIfAlreadyActive: false);
                return true;
            default:
                return false;
        }
    }

    private Cursor GetCursorForLocation(Point location)
    {
        if (_selection.Contains(location))
        {
            if (_annotationTool == AnnotationTool.Text)
            {
                return Cursors.IBeam;
            }

            if (_annotationTool != AnnotationTool.None || _isBorrarModeActive)
            {
                return Cursors.Cross;
            }
        }

        return GetResizeCursor(GetResizeEdges(location));
    }

    private void ToggleDrawingTool()
    {
        if (_isEditingText)
        {
            CommitTextEdit();
        }

        if (_selectedAnnotationTool == AnnotationTool.None)
        {
            if (_drawingDropdownButton is not null)
            {
                _drawingMenu.Show(_drawingDropdownButton, new Point(0, _drawingDropdownButton.Height));
            }

            return;
        }

        if (_annotationTool == _selectedAnnotationTool)
        {
            _annotationTool = AnnotationTool.None;
        }
        else
        {
            _annotationTool = _selectedAnnotationTool;
            _isBorrarModeActive = false;
            UpdateBorrarButtonAppearance();
        }

        Cursor = GetCursorForLocation(PointToClient(Cursor.Position));
        UpdateDrawingButtonAppearance();
    }

    private void UpdateDrawingButtonAppearance()
    {
        var isActive = _annotationTool is AnnotationTool.Freehand or AnnotationTool.Line or AnnotationTool.Arrow or AnnotationTool.Rectangle or AnnotationTool.Circle;
        if (_drawingButton is not null)
        {
            SetToolButtonAppearance(_drawingButton, isActive);
        }

        if (_drawingDropdownButton is not null)
        {
            SetToolButtonAppearance(_drawingDropdownButton, isActive);
        }

        if (_highlightButton is not null)
        {
            SetToolButtonAppearance(_highlightButton, _annotationTool == AnnotationTool.Highlight);
        }

        if (_counterButton is not null)
        {
            SetToolButtonAppearance(_counterButton, _annotationTool == AnnotationTool.Counter);
        }

        if (_textButton is not null)
        {
            SetToolButtonAppearance(_textButton, _annotationTool == AnnotationTool.Text);
        }
    }

    private static void SetToolButtonAppearance(Button button, bool isActive)
    {
        button.BackColor = isActive ? ActiveToolBackColor : Color.White;
        button.ForeColor = Color.FromArgb(45, 49, 54);
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = isActive ? ActiveToolBorderColor : Color.FromArgb(214, 219, 225);
        button.FlatAppearance.MouseOverBackColor = isActive ? ActiveToolHoverColor : Color.FromArgb(241, 244, 247);
        button.FlatAppearance.MouseDownBackColor = isActive ? ActiveToolPressedColor : Color.FromArgb(230, 235, 240);
    }

    private void SelectBorrarMode(BorrarMode mode, ToolStripMenuItem selectedMenuItem, Image selectedIcon)
    {
        if (_isEditingText)
        {
            CommitTextEdit();
        }

        var isTogglingOff = _isBorrarModeActive && _borrarMode == mode;
        _annotationTool = AnnotationTool.None;
        Cursor = GetCursorForLocation(PointToClient(Cursor.Position));

        _borrarMode = mode;
        _isBorrarModeActive = !isTogglingOff;
        foreach (ToolStripMenuItem menuItem in _borrarMenu.Items)
        {
            menuItem.Checked = _isBorrarModeActive && menuItem == selectedMenuItem;
        }

        if (_borrarButton is not null)
        {
            _borrarButton.Image = selectedIcon;
            var modeLabel = GetBorrarModeLabel(mode);
            _borrarButton.Text = GetCaptureToolButtonText(modeLabel);
            _borrarButton.AccessibleDescription = modeLabel;
        }

        UpdateDrawingButtonAppearance();
        UpdateBorrarButtonAppearance();
    }

    private void ToggleBorrarMode()
    {
        if (_isEditingText)
        {
            CommitTextEdit();
        }

        if (!_isBorrarModeActive)
        {
            _annotationTool = AnnotationTool.None;
        }

        _isBorrarModeActive = !_isBorrarModeActive;
        Cursor = GetCursorForLocation(PointToClient(Cursor.Position));
        UpdateDrawingButtonAppearance();
        UpdateBorrarButtonAppearance();
    }

    private static string GetBorrarModeLabel(BorrarMode mode) =>
        AppLocalization.Translate(mode == BorrarMode.Pixelate ? "Editor.Pixelated" : "Editor.Filled");

    private void UpdateBorrarButtonAppearance()
    {
        if (_borrarButton is null)
        {
            return;
        }

        var maskLabel = AppLocalization.Translate("Editor.Mask");
        var modeLabel = GetBorrarModeLabel(_borrarMode);
        _actionToolTip.SetToolTip(_borrarButton, $"{maskLabel}: {modeLabel} (F8)");
        _borrarButton.AccessibleDescription = modeLabel;
        SetToolButtonAppearance(_borrarButton, _isBorrarModeActive);

        if (_borrarDropdownButton is null)
        {
            return;
        }

        SetToolButtonAppearance(_borrarDropdownButton, _isBorrarModeActive);
    }

    private Point ClampToSelection(Point point) => new(
        Math.Clamp(point.X, _selection.Left, _selection.Right - 1),
        Math.Clamp(point.Y, _selection.Top, _selection.Bottom - 1));

    private void DrawFreehandStrokes(Graphics graphics)
    {
        var state = graphics.Save();
        graphics.SetClip(_selection);
        foreach (var stroke in _freehandStrokes)
        {
            ImageEditingService.DrawFreehandStroke(graphics, stroke, _lineColor, FreehandPenWidth);
        }

        graphics.Restore(state);
    }

    private void DrawHighlightStrokes(Graphics graphics)
    {
        foreach (var stroke in _highlightStrokes)
        {
            ImageEditingService.DrawHighlighter(graphics, stroke, _lineColor, _highlightWidth, _selection);
        }
    }

    private void AddCounterMarker(Point location)
    {
        var marker = ImageEditingService.CreateCounterMarker(location, _nextCounterValue++, _selection);
        _counterMarkers.Add(marker);
        Invalidate(Rectangle.Intersect(marker.Bounds, _selection));
    }

    private void DrawCounterMarkers(Graphics graphics)
    {
        var state = graphics.Save();
        graphics.SetClip(_selection);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        foreach (var marker in _counterMarkers)
        {
            ImageEditingService.DrawCounterMarker(graphics, marker, _lineColor, _fillColor);
        }

        graphics.Restore(state);
    }

    private void DrawLines(Graphics graphics)
    {
        var state = graphics.Save();
        graphics.SetClip(_selection);

        foreach (var annotation in _lineAnnotations)
        {
            var tool = annotation.IsArrow ? EditTool.Arrow : EditTool.Line;
            ImageEditingService.DrawAnnotation(graphics, annotation.Start, annotation.End, tool, _lineColor, _fillColor);
        }

        if (_isDrawingLine)
        {
            var tool = _annotationTool == AnnotationTool.Arrow ? EditTool.Arrow : EditTool.Line;
            ImageEditingService.DrawAnnotation(graphics, _lineStart, _lineEnd, tool, _lineColor, _fillColor);
        }

        graphics.Restore(state);
    }

    private void InvalidateShapePreview(Point start, Point previousEnd, Point nextEnd)
    {
        var dirtyRectangle = Rectangle.Union(
            GetRectangle(start, previousEnd),
            GetRectangle(start, nextEnd));
        dirtyRectangle.Inflate(FreehandPenWidth + 2, FreehandPenWidth + 2);
        Invalidate(Rectangle.Intersect(dirtyRectangle, _selection));
    }

    private void DrawRectangles(Graphics graphics)
    {
        var state = graphics.Save();
        graphics.SetClip(_selection);

        foreach (var rectangle in _rectangles)
        {
            ImageEditingService.DrawAnnotation(
                graphics,
                rectangle.Location,
                new Point(rectangle.Right, rectangle.Bottom),
                EditTool.Rectangle,
                _lineColor,
                _fillColor);
        }

        if (_isDrawingRectangle)
        {
            ImageEditingService.DrawAnnotation(graphics, _rectangleStart, _rectangleEnd, EditTool.Rectangle, _lineColor, _fillColor);
        }

        graphics.Restore(state);
    }

    private void DrawCircles(Graphics graphics)
    {
        var state = graphics.Save();
        graphics.SetClip(_selection);

        foreach (var circle in _circles)
        {
            ImageEditingService.DrawAnnotation(
                graphics,
                circle.Location,
                new Point(circle.Right, circle.Bottom),
                EditTool.Circle,
                _lineColor,
                _fillColor);
        }

        if (_isDrawingCircle)
        {
            ImageEditingService.DrawAnnotation(graphics, _circleStart, _circleEnd, EditTool.Circle, _lineColor, _fillColor);
        }

        graphics.Restore(state);
    }

    private void UpdateActionBarLocation()
    {
        var left = Math.Clamp(_selection.Left + (_selection.Width - _actionBar.Width) / 2, 0, Math.Max(0, ClientSize.Width - _actionBar.Width));
        var top = _selection.Bottom + 18;
        if (top + _actionBar.Height > ClientSize.Height)
        {
            top = _selection.Top - _actionBar.Height - 18;
        }

        _actionBar.Location = new Point(left, Math.Clamp(top, 0, Math.Max(0, ClientSize.Height - _actionBar.Height)));
    }

    private void DrawHandles(Graphics graphics)
    {
        foreach (var handle in GetHandleRectangles(_selection))
        {
            graphics.FillRectangle(_handleBrush, handle);
        }
    }

    private ResizeEdges GetResizeEdges(Point point)
    {
        if (_selection.Width <= 0 || _selection.Height <= 0)
        {
            return ResizeEdges.None;
        }

        var handles = GetHandleRectangles(_selection);
        var edges = new[]
        {
            ResizeEdges.NorthWest, ResizeEdges.North, ResizeEdges.NorthEast,
            ResizeEdges.West, ResizeEdges.None, ResizeEdges.East,
            ResizeEdges.SouthWest, ResizeEdges.South, ResizeEdges.SouthEast,
        };

        for (var index = 0; index < handles.Length; index++)
        {
            if (handles[index].Contains(point))
            {
                return edges[index];
            }
        }

        return ResizeEdges.None;
    }

    private static Cursor GetResizeCursor(ResizeEdges edges) => edges switch
    {
        ResizeEdges.North or ResizeEdges.South => Cursors.SizeNS,
        ResizeEdges.East or ResizeEdges.West => Cursors.SizeWE,
        ResizeEdges.NorthEast or ResizeEdges.SouthWest => Cursors.SizeNESW,
        ResizeEdges.NorthWest or ResizeEdges.SouthEast => Cursors.SizeNWSE,
        _ => Cursors.Default,
    };

    private static Rectangle[] GetHandleRectangles(Rectangle rectangle)
    {
        var half = HandleSize / 2;
        var middleX = rectangle.Left + rectangle.Width / 2;
        var middleY = rectangle.Top + rectangle.Height / 2;
        return
        [
            new Rectangle(rectangle.Left - half, rectangle.Top - half, HandleSize, HandleSize),
            new Rectangle(middleX - half, rectangle.Top - half, HandleSize, HandleSize),
            new Rectangle(rectangle.Right - half, rectangle.Top - half, HandleSize, HandleSize),
            new Rectangle(rectangle.Left - half, middleY - half, HandleSize, HandleSize),
            Rectangle.Empty,
            new Rectangle(rectangle.Right - half, middleY - half, HandleSize, HandleSize),
            new Rectangle(rectangle.Left - half, rectangle.Bottom - half, HandleSize, HandleSize),
            new Rectangle(middleX - half, rectangle.Bottom - half, HandleSize, HandleSize),
            new Rectangle(rectangle.Right - half, rectangle.Bottom - half, HandleSize, HandleSize),
        ];
    }

    private Rectangle ResizeSelection(Point point)
    {
        var deltaX = point.X - _dragOrigin.X;
        var deltaY = point.Y - _dragOrigin.Y;
        var left = _originalSelection.Left;
        var top = _originalSelection.Top;
        var right = _originalSelection.Right;
        var bottom = _originalSelection.Bottom;

        if (_resizeEdges.HasFlag(ResizeEdges.West))
        {
            left = Math.Clamp(left + deltaX, 0, right - MinimumSelectionSize);
        }
        if (_resizeEdges.HasFlag(ResizeEdges.East))
        {
            right = Math.Clamp(right + deltaX, left + MinimumSelectionSize, ClientSize.Width);
        }
        if (_resizeEdges.HasFlag(ResizeEdges.North))
        {
            top = Math.Clamp(top + deltaY, 0, bottom - MinimumSelectionSize);
        }
        if (_resizeEdges.HasFlag(ResizeEdges.South))
        {
            bottom = Math.Clamp(bottom + deltaY, top + MinimumSelectionSize, ClientSize.Height);
        }

        return Rectangle.FromLTRB(left, top, right, bottom);
    }

    private Rectangle MoveSelection(Point point)
    {
        var left = Math.Clamp(_originalSelection.Left + point.X - _dragOrigin.X, 0, ClientSize.Width - _originalSelection.Width);
        var top = Math.Clamp(_originalSelection.Top + point.Y - _dragOrigin.Y, 0, ClientSize.Height - _originalSelection.Height);
        return new Rectangle(left, top, _originalSelection.Width, _originalSelection.Height);
    }

    [Flags]
    private enum ResizeEdges
    {
        None = 0,
        North = 1,
        South = 2,
        East = 4,
        West = 8,
        NorthEast = North | East,
        NorthWest = North | West,
        SouthEast = South | East,
        SouthWest = South | West,
    }
}

public enum RegionSelectionAction
{
    Capture,
    Save,
    Copy,
}
