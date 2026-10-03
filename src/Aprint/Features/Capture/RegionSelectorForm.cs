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
    private const int FreehandPenWidth = 3;
    private const int TextMinimumWidth = 100;
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
    private readonly List<List<Point>> _freehandStrokes = [];
    private readonly List<List<Point>> _highlightStrokes = [];
    private readonly List<(Point Start, Point End, bool IsArrow)> _lineAnnotations = [];
    private readonly List<(Point Center, int Value, int Diameter)> _counterMarkers = [];
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
    private Color _borrarColor = Color.Red;
    private int _borrarPixelSize = 6;
    private int _highlightWidth = 18;
    private Color? _counterFillColor;
    private int _nextCounterValue = 1;
    private Font _textFont = new("Segoe UI", 12F);
    private Color _textColor = Color.Red;
    private string _textBuffer = string.Empty;
    private Rectangle _textBounds;
    private int _textMinimumWidth;
    private int _textMinimumHeight;
    private AnnotationTool _selectedAnnotationTool;
    private ToolStripMenuItem? _selectedDrawingMenuItem;
    private Image? _selectedDrawingIcon;
    private string? _selectedDrawingLabel;
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

    public RegionSelectorForm(Bitmap background, OutsideCaptureClickAction outsideCaptureClickAction = OutsideCaptureClickAction.SelectNewArea, BorrarMode borrarMode = BorrarMode.Pixelate, int borrarPixelSize = 6, int borrarColorArgb = -65536, int highlightWidth = 18, int? counterFillColorArgb = null, string textFontFamily = "Segoe UI", float textFontSize = 12F, FontStyle textFontStyle = FontStyle.Regular, int textColorArgb = -65536)
    {
        _background = background;
        _outsideCaptureClickAction = outsideCaptureClickAction;
        _borrarMode = borrarMode;
        _borrarPixelSize = Math.Clamp(borrarPixelSize, 4, 32);
        _borrarColor = Color.FromArgb(borrarColorArgb);
        _highlightWidth = Math.Clamp(highlightWidth, 4, 48);
        _counterFillColor = counterFillColorArgb is int fillColorArgb ? Color.FromArgb(fillColorArgb) : null;
        _textFont.Dispose();
        _textFont = CreateTextFont(textFontFamily, textFontSize, textFontStyle);
        _textColor = Color.FromArgb(textColorArgb);

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = SystemInformation.VirtualScreen;
        TopMost = true;
        ShowInTaskbar = false;
        KeyPreview = true;
        DoubleBuffered = true;
        Cursor = Cursors.Cross;
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
            _freehandStrokes.Clear();
            _highlightStrokes.Clear();
            _lineAnnotations.Clear();
            _rectangles.Clear();
            _circles.Clear();
            _counterMarkers.Clear();
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
            Cursor = _annotationTool == AnnotationTool.Text && _selection.Contains(e.Location)
                ? Cursors.IBeam
                : GetResizeCursor(GetResizeEdges(e.Location));
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
                ImageEditingService.ApplyBorrar(_background, area, _borrarMode, _borrarColor, _borrarPixelSize);
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
            if (_textBuffer.Length > 0)
            {
                _textBuffer = _textBuffer[..^1];
            }
        }
        else if (!char.IsControl(e.KeyChar))
        {
            _textBuffer += e.KeyChar;
        }
        else
        {
            return;
        }

        e.Handled = true;
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
            Size = new Size(900, 56),
            Padding = new Padding(6),
        };

        var captureLabel = AppLocalization.Translate("Capture.Region.Capture");
        var captureButton = CreateActionButton(captureLabel, true, LoadActionIcon("capture.png", Color.White));
        captureButton.Margin = Padding.Empty;
        captureButton.FlatAppearance.BorderSize = 0;
        captureButton.AccessibleName = captureLabel;
        captureButton.Click += (_, _) => CompleteSelection(RegionSelectionAction.Capture);

        var captureDropdownButton = CreateActionButton("\u25BE", true, null);
        captureDropdownButton.AutoSize = false;
        captureDropdownButton.MinimumSize = new Size(30, 34);
        captureDropdownButton.Size = new Size(30, 34);
        captureDropdownButton.Margin = Padding.Empty;
        captureDropdownButton.Padding = Padding.Empty;
        captureDropdownButton.FlatAppearance.BorderSize = 0;
        captureDropdownButton.AccessibleName = AppLocalization.Translate("Capture.Region.Actions");
        captureDropdownButton.Click += (_, _) => _captureActionsMenu.Show(captureDropdownButton, new Point(0, captureDropdownButton.Height));

        var captureActionsPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor = AnchorStyles.None,
            BackColor = panel.BackColor,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            WrapContents = false,
        };
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
        var drawingButton = CreateActionButton(drawingLabel, false, null);
        _drawingButton = drawingButton;
        _selectedDrawingLabel = drawingLabel;
        drawingButton.AccessibleName = drawingLabel;
        drawingButton.Margin = Padding.Empty;
        drawingButton.FlatAppearance.BorderSize = 0;
        drawingButton.Click += (_, _) => ToggleDrawingTool();

        var drawingDropdownButton = CreateActionButton("\u25BE", false, null);
        _drawingDropdownButton = drawingDropdownButton;
        drawingDropdownButton.AutoSize = false;
        drawingDropdownButton.MinimumSize = new Size(30, 34);
        drawingDropdownButton.Size = new Size(30, 34);
        drawingDropdownButton.Margin = Padding.Empty;
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
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            WrapContents = false,
        };
        drawingActionsPanel.Controls.Add(drawingButton);
        drawingActionsPanel.Controls.Add(drawingDropdownButton);

        var highlightLabel = AppLocalization.Translate("Editor.Highlight");
        _highlightIcon = LoadActionIcon("highlight.png", Color.FromArgb(45, 49, 54));
        var highlightButton = CreateActionButton(highlightLabel, false, _highlightIcon);
        _highlightButton = highlightButton;
        highlightButton.AccessibleName = highlightLabel;
        highlightButton.Click += (_, _) => ToggleStandaloneTool(AnnotationTool.Highlight);

        var counterLabel = AppLocalization.Translate("Editor.Counter");
        var counterIcon = LoadActionIcon("counter.png", Color.FromArgb(45, 49, 54));
        var counterButton = CreateActionButton(counterLabel, false, counterIcon);
        _counterButton = counterButton;
        counterButton.AccessibleName = counterLabel;
        counterButton.Click += (_, _) => ToggleStandaloneTool(AnnotationTool.Counter);

        var textLabel = AppLocalization.Translate("Editor.Text");
        var textButton = CreateActionButton(textLabel, false, LoadActionIcon("text.png", Color.FromArgb(45, 49, 54)));
        _textButton = textButton;
        textButton.AccessibleName = textLabel;
        textButton.Click += (_, _) => ToggleStandaloneTool(AnnotationTool.Text);

        _pixelatedIcon = LoadActionIcon("pixelated.png", Color.FromArgb(45, 49, 54));
        _filledIcon = LoadActionIcon("filled.png", Color.FromArgb(45, 49, 54));
        var borrarLabel = GetBorrarModeLabel(_borrarMode);
        var borrarButton = CreateActionButton(borrarLabel, false, _borrarMode == BorrarMode.Pixelate ? _pixelatedIcon : _filledIcon);
        _borrarButton = borrarButton;
        borrarButton.AccessibleName = borrarLabel;
        borrarButton.Margin = Padding.Empty;
        borrarButton.FlatAppearance.BorderSize = 0;
        borrarButton.Click += (_, _) => ToggleBorrarMode();

        var borrarDropdownButton = CreateActionButton("\u25BE", false, null);
        _borrarDropdownButton = borrarDropdownButton;
        borrarDropdownButton.AutoSize = false;
        borrarDropdownButton.MinimumSize = new Size(30, 34);
        borrarDropdownButton.Size = new Size(30, 34);
        borrarDropdownButton.Margin = Padding.Empty;
        borrarDropdownButton.Padding = Padding.Empty;
        borrarDropdownButton.FlatAppearance.BorderSize = 0;
        borrarDropdownButton.AccessibleName = AppLocalization.Translate("Capture.Region.Actions");
        borrarDropdownButton.Click += (_, _) => _borrarMenu.Show(borrarDropdownButton, new Point(0, borrarDropdownButton.Height));

        var borrarActionsPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor = AnchorStyles.None,
            BackColor = panel.BackColor,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            WrapContents = false,
        };
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

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 1,
            BackColor = panel.BackColor,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        for (var column = 0; column < 6; column++)
        {
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.667F));
        }

        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.Controls.Add(drawingActionsPanel, 0, 0);
        layout.Controls.Add(highlightButton, 1, 0);
        layout.Controls.Add(counterButton, 2, 0);
        layout.Controls.Add(textButton, 3, 0);
        layout.Controls.Add(borrarActionsPanel, 4, 0);
        layout.Controls.Add(captureActionsPanel, 5, 0);
        panel.Controls.Add(layout);
        return panel;
    }

    private void AddAnnotationMenuItem(Button drawingButton, AnnotationTool tool, string localizationKey, string iconFileName)
    {
        var icon = LoadActionIcon(iconFileName, Color.FromArgb(45, 49, 54));
        var label = AppLocalization.Translate(localizationKey);
        var menuItem = new ToolStripMenuItem(label, icon) { Tag = tool };
        menuItem.Click += (_, _) => SelectAnnotationTool(tool, drawingButton, menuItem, icon, label);
        _drawingMenu.Items.Add(menuItem);
    }

    private void RestoreLastAnnotationTool(Button drawingButton)
    {
        var menuItem = _drawingMenu.Items
            .OfType<ToolStripMenuItem>()
            .FirstOrDefault(item => item.Tag is AnnotationTool tool && tool == _lastSelectedAnnotationTool);
        if (menuItem is null || menuItem.Tag is not AnnotationTool selectedTool || menuItem.Image is not Image selectedIcon || menuItem.Text is not { } label)
        {
            return;
        }

        SelectAnnotationTool(selectedTool, drawingButton, menuItem, selectedIcon, label);
    }

    private void AddBorrarMenuItem(BorrarMode mode, string localizationKey, Image icon)
    {
        var menuItem = new ToolStripMenuItem(AppLocalization.Translate(localizationKey), icon)
        {
            Checked = mode == _borrarMode,
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
            MinimumSize = new Size(82, 34),
            Margin = new Padding(5, 3, 5, 3),
            Padding = new Padding(8, 3, 8, 3),
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

        SelectedRegion = _selection;
        SelectedAction = action;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void BeginTextEdit(Point start, Point end)
    {
        var draggedBounds = GetRectangle(start, end);
        var left = Math.Clamp(draggedBounds.Left, _selection.Left, _selection.Right - 1);
        var top = Math.Clamp(draggedBounds.Top, _selection.Top, _selection.Bottom - 1);
        var availableWidth = _selection.Right - left;
        var availableHeight = _selection.Bottom - top;
        _textMinimumWidth = Math.Min(availableWidth, Math.Max(TextMinimumWidth, draggedBounds.Width));
        _textMinimumHeight = Math.Min(availableHeight, Math.Max(_textFont.Height + 4, draggedBounds.Height));
        _textBounds = new Rectangle(left, top, _textMinimumWidth, _textMinimumHeight);
        _textBuffer = string.Empty;
        _isEditingText = true;
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
        var availableWidth = Math.Max(1, _selection.Right - _textBounds.Left);
        var availableHeight = Math.Max(1, _selection.Bottom - _textBounds.Top);
        var width = Math.Min(Math.Max(_textMinimumWidth, contentSize.Width + 4), availableWidth);
        var height = Math.Min(Math.Max(_textMinimumHeight, contentSize.Height + 4), availableHeight);
        _textBounds = new Rectangle(_textBounds.Left, _textBounds.Top, Math.Max(1, width), Math.Max(1, height));
        Invalidate(Rectangle.Union(previousBounds, _textBounds));
    }

    private void AppendTextNewLine()
    {
        _textBuffer += Environment.NewLine;
        ResizeTextBoundsToContent();
    }

    private void CommitTextEdit()
    {
        if (!_isEditingText)
        {
            return;
        }

        _isEditingText = false;
        Cursor = Cursors.Cross;
        if (_textBuffer.Length > 0)
        {
            ImageEditingService.ApplyText(_background, _textBounds, _textBuffer, _textColor, _textFont);
        }

        _textBuffer = string.Empty;
        Invalidate(_textBounds);
    }

    private void CancelTextEdit()
    {
        _isEditingText = false;
        _textBuffer = string.Empty;
        Cursor = Cursors.Cross;
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

        ImageEditingService.DrawText(graphics, _textBounds, _textBuffer, _textColor, _textFont);
        using var borderPen = new Pen(Color.Gray) { DashStyle = DashStyle.Dash };
        graphics.DrawRectangle(borderPen, _textBounds.X, _textBounds.Y, _textBounds.Width - 1, _textBounds.Height - 1);

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

    private void SelectAnnotationTool(AnnotationTool tool, Button drawingButton, ToolStripMenuItem? selectedMenuItem, Image selectedIcon, string label)
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

        var isTogglingOff = _annotationTool == tool && _selectedAnnotationTool == tool;
        _lastSelectedAnnotationTool = tool;
        _selectedAnnotationTool = tool;
        _selectedDrawingMenuItem = selectedMenuItem;
        _selectedDrawingIcon = selectedIcon;
        _selectedDrawingLabel = label;
        _annotationTool = isTogglingOff ? AnnotationTool.None : tool;
        _isBorrarModeActive = false;
        UpdateBorrarButtonAppearance();
        foreach (ToolStripMenuItem menuItem in _drawingMenu.Items)
        {
            menuItem.Checked = menuItem == _selectedDrawingMenuItem;
        }

        drawingButton.Text = _selectedDrawingLabel;
        drawingButton.Image = _selectedDrawingIcon;
        drawingButton.AccessibleName = _selectedDrawingLabel;
        UpdateDrawingButtonAppearance();
    }

    private void ToggleStandaloneTool(AnnotationTool tool)
    {
        if (_isEditingText)
        {
            CommitTextEdit();
        }

        _annotationTool = _annotationTool == tool
            ? AnnotationTool.None
            : tool;
        _isBorrarModeActive = false;
        Cursor = _annotationTool == AnnotationTool.Text ? Cursors.IBeam : Cursors.Cross;
        UpdateBorrarButtonAppearance();
        UpdateDrawingButtonAppearance();
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

        UpdateDrawingButtonAppearance();
    }

    private void UpdateDrawingButtonAppearance()
    {
        var isActive = _annotationTool is AnnotationTool.Freehand or AnnotationTool.Line or AnnotationTool.Arrow or AnnotationTool.Rectangle or AnnotationTool.Circle;
        if (_drawingButton is not null)
        {
            _drawingButton.BackColor = isActive ? Color.FromArgb(0, 103, 192) : Color.White;
            _drawingButton.ForeColor = isActive ? Color.White : Color.FromArgb(45, 49, 54);
            _drawingButton.FlatAppearance.BorderSize = 0;
            _drawingButton.FlatAppearance.MouseOverBackColor = isActive
                ? Color.FromArgb(0, 90, 158)
                : Color.FromArgb(241, 244, 247);
            _drawingButton.FlatAppearance.MouseDownBackColor = isActive
                ? Color.FromArgb(0, 75, 132)
                : Color.FromArgb(230, 235, 240);
        }

        if (_drawingDropdownButton is not null)
        {
            _drawingDropdownButton.BackColor = isActive ? Color.FromArgb(0, 103, 192) : Color.White;
            _drawingDropdownButton.ForeColor = isActive ? Color.White : Color.FromArgb(45, 49, 54);
            _drawingDropdownButton.FlatAppearance.BorderSize = 0;
            _drawingDropdownButton.FlatAppearance.MouseOverBackColor = isActive
                ? Color.FromArgb(0, 90, 158)
                : Color.FromArgb(241, 244, 247);
            _drawingDropdownButton.FlatAppearance.MouseDownBackColor = isActive
                ? Color.FromArgb(0, 75, 132)
                : Color.FromArgb(230, 235, 240);
        }

        if (_highlightButton is not null)
        {
            _highlightButton.BackColor = _annotationTool == AnnotationTool.Highlight ? Color.FromArgb(0, 103, 192) : Color.White;
            _highlightButton.ForeColor = _annotationTool == AnnotationTool.Highlight ? Color.White : Color.FromArgb(45, 49, 54);
            _highlightButton.FlatAppearance.BorderSize = 0;
            _highlightButton.FlatAppearance.MouseOverBackColor = _annotationTool == AnnotationTool.Highlight
                ? Color.FromArgb(0, 90, 158)
                : Color.FromArgb(241, 244, 247);
            _highlightButton.FlatAppearance.MouseDownBackColor = _annotationTool == AnnotationTool.Highlight
                ? Color.FromArgb(0, 75, 132)
                : Color.FromArgb(230, 235, 240);
        }

        if (_counterButton is not null)
        {
            _counterButton.BackColor = _annotationTool == AnnotationTool.Counter ? Color.FromArgb(0, 103, 192) : Color.White;
            _counterButton.ForeColor = _annotationTool == AnnotationTool.Counter ? Color.White : Color.FromArgb(45, 49, 54);
            _counterButton.FlatAppearance.BorderSize = 0;
            _counterButton.FlatAppearance.MouseOverBackColor = _annotationTool == AnnotationTool.Counter
                ? Color.FromArgb(0, 90, 158)
                : Color.FromArgb(241, 244, 247);
            _counterButton.FlatAppearance.MouseDownBackColor = _annotationTool == AnnotationTool.Counter
                ? Color.FromArgb(0, 75, 132)
                : Color.FromArgb(230, 235, 240);
        }

        if (_textButton is not null)
        {
            _textButton.BackColor = _annotationTool == AnnotationTool.Text ? Color.FromArgb(0, 103, 192) : Color.White;
            _textButton.ForeColor = _annotationTool == AnnotationTool.Text ? Color.White : Color.FromArgb(45, 49, 54);
            _textButton.FlatAppearance.BorderSize = 0;
            _textButton.FlatAppearance.MouseOverBackColor = _annotationTool == AnnotationTool.Text
                ? Color.FromArgb(0, 90, 158)
                : Color.FromArgb(241, 244, 247);
            _textButton.FlatAppearance.MouseDownBackColor = _annotationTool == AnnotationTool.Text
                ? Color.FromArgb(0, 75, 132)
                : Color.FromArgb(230, 235, 240);
        }
    }

    private void SelectBorrarMode(BorrarMode mode, ToolStripMenuItem selectedMenuItem, Image selectedIcon)
    {
        if (_isEditingText)
        {
            CommitTextEdit();
        }

        var isTogglingOff = _isBorrarModeActive && _borrarMode == mode;
        _annotationTool = AnnotationTool.None;

        _borrarMode = mode;
        _isBorrarModeActive = !isTogglingOff;
        foreach (ToolStripMenuItem menuItem in _borrarMenu.Items)
        {
            menuItem.Checked = _isBorrarModeActive && menuItem == selectedMenuItem;
        }

        if (_borrarButton is not null)
        {
            _borrarButton.Image = selectedIcon;
            _borrarButton.Text = GetBorrarModeLabel(mode);
            _borrarButton.AccessibleName = _borrarButton.Text;
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

        _borrarButton.BackColor = _isBorrarModeActive ? Color.FromArgb(0, 103, 192) : Color.White;
        _borrarButton.ForeColor = _isBorrarModeActive ? Color.White : Color.FromArgb(45, 49, 54);
        _borrarButton.FlatAppearance.BorderSize = _isBorrarModeActive ? 0 : 1;
        _borrarButton.FlatAppearance.MouseOverBackColor = _isBorrarModeActive
            ? Color.FromArgb(0, 90, 158)
            : Color.FromArgb(241, 244, 247);
        _borrarButton.FlatAppearance.MouseDownBackColor = _isBorrarModeActive
            ? Color.FromArgb(0, 75, 132)
            : Color.FromArgb(230, 235, 240);

        if (_borrarDropdownButton is null)
        {
            return;
        }

        _borrarDropdownButton.BackColor = _isBorrarModeActive ? Color.FromArgb(0, 103, 192) : Color.White;
        _borrarDropdownButton.ForeColor = _isBorrarModeActive ? Color.White : Color.FromArgb(45, 49, 54);
        _borrarDropdownButton.FlatAppearance.BorderSize = 0;
        _borrarDropdownButton.FlatAppearance.MouseOverBackColor = _isBorrarModeActive
            ? Color.FromArgb(0, 90, 158)
            : Color.FromArgb(241, 244, 247);
        _borrarDropdownButton.FlatAppearance.MouseDownBackColor = _isBorrarModeActive
            ? Color.FromArgb(0, 75, 132)
            : Color.FromArgb(230, 235, 240);
    }

    private Point ClampToSelection(Point point) => new(
        Math.Clamp(point.X, _selection.Left, _selection.Right - 1),
        Math.Clamp(point.Y, _selection.Top, _selection.Bottom - 1));

    private void DrawFreehandStrokes(Graphics graphics)
    {
        var state = graphics.Save();
        graphics.SetClip(_selection);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Color.Red, FreehandPenWidth)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };

        foreach (var stroke in _freehandStrokes)
        {
            if (stroke.Count > 1)
            {
                graphics.DrawLines(pen, stroke.ToArray());
            }
        }

        graphics.Restore(state);
    }

    private void DrawHighlightStrokes(Graphics graphics)
    {
        var state = graphics.Save();
        graphics.SetClip(_selection);
        foreach (var stroke in _highlightStrokes)
        {
            ImageEditingService.DrawHighlighter(graphics, stroke, _borrarColor, _highlightWidth);
        }

        graphics.Restore(state);
    }

    private void AddCounterMarker(Point location)
    {
        var diameter = ImageEditingService.GetCounterMarkerDiameter(_selection.Size);
        var center = ImageEditingService.ClampCounterMarkerCenter(location, _selection, diameter);
        var value = _nextCounterValue++;
        _counterMarkers.Add((center, value, diameter));
        Invalidate(Rectangle.Intersect(ImageEditingService.GetCounterMarkerBounds(center, diameter), _selection));
    }

    private void DrawCounterMarkers(Graphics graphics)
    {
        var state = graphics.Save();
        graphics.SetClip(_selection);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        foreach (var marker in _counterMarkers)
        {
            var text = marker.Value.ToString();
            var bounds = ImageEditingService.GetCounterMarkerBounds(marker.Center, marker.Diameter);
            var fontSize = ImageEditingService.GetCounterMarkerFontSize(marker.Diameter);
            using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Point);
            ImageEditingService.DrawCounterMarker(graphics, bounds, text, font, _borrarColor, _counterFillColor);
        }

        graphics.Restore(state);
    }

    private void DrawLines(Graphics graphics)
    {
        var state = graphics.Save();
        graphics.SetClip(_selection);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Color.Red, FreehandPenWidth)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };

        foreach (var annotation in _lineAnnotations)
        {
            DrawLineOrArrow(graphics, pen, annotation.Start, annotation.End, annotation.IsArrow);
        }

        if (_isDrawingLine)
        {
            DrawLineOrArrow(graphics, pen, _lineStart, _lineEnd, _annotationTool == AnnotationTool.Arrow);
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
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Color.Red, FreehandPenWidth);

        foreach (var rectangle in _rectangles)
        {
            graphics.DrawRectangle(pen, rectangle);
        }

        if (_isDrawingRectangle)
        {
            graphics.DrawRectangle(pen, GetRectangle(_rectangleStart, _rectangleEnd));
        }

        graphics.Restore(state);
    }

    private void DrawCircles(Graphics graphics)
    {
        var state = graphics.Save();
        graphics.SetClip(_selection);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Color.Red, FreehandPenWidth);

        foreach (var circle in _circles)
        {
            graphics.DrawEllipse(pen, circle);
        }

        if (_isDrawingCircle)
        {
            graphics.DrawEllipse(pen, GetRectangle(_circleStart, _circleEnd));
        }

        graphics.Restore(state);
    }

    private static void DrawLineOrArrow(Graphics graphics, Pen pen, Point start, Point end, bool isArrow)
    {
        if (!isArrow)
        {
            graphics.DrawLine(pen, start, end);
            return;
        }

        using var arrowPen = (Pen)pen.Clone();
        using var arrowCap = new AdjustableArrowCap(5, 7, true);
        arrowPen.CustomEndCap = arrowCap;
        graphics.DrawLine(arrowPen, start, end);
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
