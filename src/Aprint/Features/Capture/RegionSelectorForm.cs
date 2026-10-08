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

    private sealed class ScreenSelectionButton(string text) : ToolStripButton(text)
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            if (DisplayStyle != ToolStripItemDisplayStyle.ImageAndText || Image is null || Owner is null)
            {
                base.OnPaint(e);
                return;
            }

            var textFlags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
            var textSize = TextRenderer.MeasureText(e.Graphics, Text, Font, Size.Empty, textFlags);
            var gap = (int)Math.Round(4 * e.Graphics.DpiX / 96F);
            var contentWidth = Image.Width + gap + textSize.Width;
            var contentLeft = (Width - contentWidth) / 2;
            var imageBounds = new Rectangle(contentLeft, (Height - Image.Height) / 2, Image.Width, Image.Height);
            var textBounds = new Rectangle(imageBounds.Right + gap, (Height - textSize.Height) / 2, textSize.Width, textSize.Height);
            var renderer = Owner.Renderer;
            renderer.DrawButtonBackground(new ToolStripItemRenderEventArgs(e.Graphics, this));
            renderer.DrawItemImage(new ToolStripItemImageRenderEventArgs(e.Graphics, this, Image, imageBounds));
            renderer.DrawItemText(new ToolStripItemTextRenderEventArgs(e.Graphics, this, Text, textBounds, ForeColor, Font, textFlags));
        }
    }

    private sealed class ActionBarRenderer() : ToolStripProfessionalRenderer(ToolStripColorPalette.CreateColorTable())
    {
        private ToolStripItem? _hoveredMainButton;
        private ToolStripItem? _hoveredDropdownButton;
        private ToolStripItem? _primaryMainButton;
        private ToolStripItem? _primaryDropdownButton;
        private ToolStripItem? _activeMainButton;
        private ToolStripItem? _activeDropdownButton;
        private bool _hoveredGroupIsPrimary;

        public void SetPrimaryGroup(ToolStripItem mainButton, ToolStripItem dropdownButton)
        {
            _primaryMainButton = mainButton;
            _primaryDropdownButton = dropdownButton;
        }

        public bool SetActiveGroup(ToolStripItem? mainButton, ToolStripItem? dropdownButton)
        {
            if (ReferenceEquals(_activeMainButton, mainButton) &&
                ReferenceEquals(_activeDropdownButton, dropdownButton))
            {
                return false;
            }

            _activeMainButton = mainButton;
            _activeDropdownButton = dropdownButton;
            return true;
        }

        public bool SetHoveredGroup(ToolStripItem? mainButton, ToolStripItem? dropdownButton, bool isPrimary = false)
        {
            if (ReferenceEquals(_hoveredMainButton, mainButton) &&
                ReferenceEquals(_hoveredDropdownButton, dropdownButton) &&
                _hoveredGroupIsPrimary == isPrimary)
            {
                return false;
            }

            _hoveredMainButton = mainButton;
            _hoveredDropdownButton = dropdownButton;
            _hoveredGroupIsPrimary = isPrimary;
            return true;
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (IsPrimaryGroupItem(e.Item))
            {
                var background = IsHoveredGroupItem(e.Item) || e.Item.Selected || e.Item.Pressed
                    ? ToolStripColorPalette.PrimaryButtonHover
                    : ToolStripColorPalette.PrimaryButtonBackground;
                DrawBackground(
                    e,
                    background,
                    ToolStripColorPalette.PrimaryButtonBorder,
                    omitRightBorder: ReferenceEquals(e.Item, _primaryMainButton));
                return;
            }

            if (IsHoveredGroupItem(e.Item))
            {
                DrawBackground(
                    e,
                    ToolStripColorPalette.ActiveBackground,
                    ToolStripColorPalette.ActiveBorder,
                    omitRightBorder: ReferenceEquals(e.Item, _hoveredMainButton));
                return;
            }

            if (IsActiveGroupItem(e.Item))
            {
                DrawBackground(
                    e,
                    ToolStripColorPalette.ActiveBackground,
                    ToolStripColorPalette.ActiveBorder,
                    omitRightBorder: ReferenceEquals(e.Item, _activeMainButton));
                return;
            }

            base.OnRenderButtonBackground(e);
        }

        protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (IsPrimaryGroupItem(e.Item))
            {
                var background = IsHoveredGroupItem(e.Item) || e.Item.Selected || e.Item.Pressed
                    ? ToolStripColorPalette.PrimaryButtonHover
                    : ToolStripColorPalette.PrimaryButtonBackground;
                DrawBackground(e, background, ToolStripColorPalette.PrimaryButtonBorder);
                return;
            }

            if (IsHoveredGroupItem(e.Item))
            {
                DrawBackground(e, ToolStripColorPalette.ActiveBackground, ToolStripColorPalette.ActiveBorder);
                return;
            }

            if (IsActiveGroupItem(e.Item))
            {
                DrawBackground(e, ToolStripColorPalette.ActiveBackground, ToolStripColorPalette.ActiveBorder);
                return;
            }

            base.OnRenderDropDownButtonBackground(e);
        }

        private bool IsHoveredGroupItem(ToolStripItem item) =>
            ReferenceEquals(item, _hoveredMainButton) || ReferenceEquals(item, _hoveredDropdownButton);

        private bool IsPrimaryGroupItem(ToolStripItem item) =>
            ReferenceEquals(item, _primaryMainButton) || ReferenceEquals(item, _primaryDropdownButton);

        private bool IsActiveGroupItem(ToolStripItem item) =>
            ReferenceEquals(item, _activeMainButton) || ReferenceEquals(item, _activeDropdownButton);

        private static void DrawBackground(ToolStripItemRenderEventArgs e, Color background, Color borderColor, bool omitRightBorder = false)
        {
            var bounds = new Rectangle(Point.Empty, e.Item.Size);
            using var fill = new SolidBrush(background);
            e.Graphics.FillRectangle(fill, bounds);
            if (bounds.Width > 0 && bounds.Height > 0)
            {
                using var border = new Pen(borderColor);
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
    }

    private const int SelectionBorderWidth = 2;
    private const int HandleSize = 10;
    private const int MinimumSelectionSize = 3;
    private const int FreehandPenWidth = ImageEditingService.AnnotationPenWidth;
    private const int ActionControlHeight = ToolStripMetrics.DropdownButtonHeight;
    private const int IconOnlyActionWidth = 52;
    private readonly Rectangle _screenUnderCursorAtStart;
    private readonly Bitmap _background;
    private readonly OutsideCaptureClickAction _outsideCaptureClickAction;
    private readonly SolidBrush _overlayBrush = new(Color.FromArgb(120, 0, 0, 0));
    private readonly Pen _borderPen = new(Color.White, SelectionBorderWidth);
    private readonly Pen _sendToEditorFlashPen = new(Color.FromArgb(0, 210, 170), 4);
    private readonly SolidBrush _handleBrush = new(Color.White);
    private readonly List<Image> _actionImages = [];
    private readonly System.Windows.Forms.Timer _textCaretTimer = new() { Interval = 500 };
    private readonly System.Windows.Forms.Timer _sendToEditorFlashTimer = new() { Interval = 100 };
    private readonly List<List<Point>> _freehandStrokes = [];
    private readonly List<List<Point>> _highlightStrokes = [];
    private readonly List<(Point Start, Point End, bool IsArrow)> _lineAnnotations = [];
    private readonly List<ImageEditingService.CounterMarker> _counterMarkers = [];
    private readonly List<Rectangle> _rectangles = [];
    private readonly List<Rectangle> _circles = [];
    private readonly ToolStrip _screenSelectionBar;
    private readonly ToolStrip _actionBar;
    private ActionBarRenderer? _actionBarRenderer;
    private readonly EditHistory _editHistory;
    private ToolStripButton? _undoButton;
    private ToolStripButton? _redoButton;
    private ToolStripButton? _drawingButton;
    private ToolStripDropDownButton? _drawingDropdownButton;
    private ToolStripButton? _highlightButton;
    private Image? _highlightIcon;
    private ToolStripButton? _counterButton;
    private ToolStripButton? _textButton;
    private ToolStripButton? _maskButton;
    private ToolStripDropDownButton? _maskDropdownButton;
    private Image? _pixelatedIcon;
    private Image? _filledIcon;
    private MaskMode _maskMode = MaskMode.Pixelate;
    private Color _lineColor = Color.Red;
    private int _maskPixelSize = 6;
    private int _highlightWidth = 18;
    private Color? _fillColor;
    private bool _showToolLabels = true;
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
    private bool _isMaskModeActive;
    private bool _isDrawingMaskRegion;
    private bool _sendToEditorFlashVisible;
    private int _sendToEditorFlashTicks;
    private Point _lineStart;
    private Point _lineEnd;
    private Point _rectangleStart;
    private Point _rectangleEnd;
    private Point _circleStart;
    private Point _circleEnd;
    private Point _maskStart;
    private Point _maskEnd;
    private Point _textStart;
    private Point _textEnd;

    public Rectangle? SelectedRegion { get; private set; }

    public RegionSelectionAction? SelectedAction { get; private set; }

    public event Action<Bitmap>? SendToEditorRequested;

    public EditTool LastUsedTool { get; private set; } = EditTool.Freehand;

    public RegionSelectorForm(Bitmap background, OutsideCaptureClickAction outsideCaptureClickAction = OutsideCaptureClickAction.SelectNewArea, MaskMode maskMode = MaskMode.Pixelate, int maskPixelSize = 6, int lineColorArgb = -65536, int highlightWidth = 18, int? fillColorArgb = null, string textFontFamily = "Segoe UI", float textFontSize = 12F, FontStyle textFontStyle = FontStyle.Regular, int textColorArgb = -65536, bool showToolLabels = true, EditTool lastCaptureTool = EditTool.Freehand)
    {
        _background = background;
        _screenUnderCursorAtStart = Screen.FromPoint(Cursor.Position).Bounds;
        _editHistory = new EditHistory(_background, _nextCounterValue);
        _outsideCaptureClickAction = outsideCaptureClickAction;
        _maskMode = maskMode;
        _maskPixelSize = Math.Clamp(maskPixelSize, 4, 32);
        _lineColor = Color.FromArgb(lineColorArgb);
        _highlightWidth = Math.Clamp(highlightWidth, 4, 48);
        _fillColor = fillColorArgb is int selectedFillColorArgb ? Color.FromArgb(selectedFillColorArgb) : null;
        _textFont.Dispose();
        _textFont = CreateTextFont(textFontFamily, textFontSize, textFontStyle);
        _textColor = Color.FromArgb(textColorArgb);
        _showToolLabels = showToolLabels;
        LastUsedTool = NormalizeLastCaptureTool(lastCaptureTool);
        _textCaretTimer.Tick += (_, _) =>
        {
            if (_isEditingText)
            {
                _textCaretVisible = !_textCaretVisible;
                Invalidate(_textBounds);
            }
        };
        _sendToEditorFlashTimer.Tick += (_, _) =>
        {
            _sendToEditorFlashTicks--;
            _sendToEditorFlashVisible = _sendToEditorFlashTicks > 0 && _sendToEditorFlashTicks % 2 == 0;
            if (_sendToEditorFlashTicks <= 0)
            {
                _sendToEditorFlashTimer.Stop();
                _sendToEditorFlashVisible = false;
            }

            Invalidate(Rectangle.Inflate(_selection, 4, 4));
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

        _screenSelectionBar = CreateScreenSelectionBar(Screen.AllScreens.Select(screen => screen.Bounds).ToArray());
        Controls.Add(_screenSelectionBar);
        PositionScreenSelectionBar();

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
            if (_isDrawingMaskRegion)
            {
                using var previewPen = new Pen(Color.Red, 2) { DashStyle = DashStyle.Dash };
                e.Graphics.DrawRectangle(previewPen, GetRectangle(_maskStart, _maskEnd));
            }
            e.Graphics.DrawRectangle(_borderPen, _selection);
            if (_sendToEditorFlashVisible)
            {
                e.Graphics.DrawRectangle(_sendToEditorFlashPen, Rectangle.Inflate(_selection, 1, 1));
            }
            DrawHandles(e.Graphics);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            if (_selection.Contains(e.Location))
            {
                if (_isEditingText)
                {
                    CommitTextEdit();
                }

                CommitAnnotationsAndRecordUndo();
                _isMoving = true;
                _actionBar.Visible = false;
                Cursor = Cursors.SizeAll;
                _dragOrigin = e.Location;
                _originalSelection = _selection;
            }

            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            _screenSelectionBar.Visible = false;
            if (_isEditingText)
            {
                CommitTextEdit();
            }

            if (_selection.Width > 0 && _selection.Height > 0)
            {
                _resizeEdges = GetResizeEdges(e.Location);
                if (_resizeEdges != ResizeEdges.None)
                {
                    CommitAnnotationsAndRecordUndo();
                    _isResizing = true;
                    _actionBar.Visible = false;
                    Cursor = GetResizeCursor(_resizeEdges);
                    _dragOrigin = e.Location;
                    _originalSelection = _selection;
                    return;
                }

                if (_annotationTool == AnnotationTool.Counter && _selection.Contains(e.Location))
                {
                    SaveUndoState();
                    AddCounterMarker(ClampToSelection(e.Location));
                    CommitAnnotations();
                    Invalidate(_selection);
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

                if (_isMaskModeActive && _selection.Contains(e.Location))
                {
                    _isDrawingMaskRegion = true;
                    _maskStart = ClampToSelection(e.Location);
                    _maskEnd = _maskStart;
                    Capture = true;
                    return;
                }

                if (_selection.Contains(e.Location))
                {
                    CommitAnnotationsAndRecordUndo();
                    _isMoving = true;
                    _actionBar.Visible = false;
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

            StopSendToEditorFeedback();
            _actionBar.Visible = false;
            CommitAnnotationsAndRecordUndo();
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

        if (_isDrawingMaskRegion)
        {
            var previousEnd = _maskEnd;
            _maskEnd = ClampToSelection(e.Location);
            InvalidateShapePreview(_maskStart, previousEnd, _maskEnd);
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
        if (e.Button != MouseButtons.Left && !(e.Button == MouseButtons.Right && _isMoving))
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
            if (_freehandStrokes[^1].Count > 1)
            {
                CommitAnnotationsAndRecordUndo();
            }
            else
            {
                CommitAnnotations();
            }
            return;
        }

        if (_isDrawingHighlight)
        {
            AddHighlightPoint(e.Location);
            _isDrawingHighlight = false;
            Capture = false;
            CommitAnnotationsAndRecordUndo();
            return;
        }

        if (_isDrawingLine)
        {
            var previousEnd = _lineEnd;
            _lineEnd = ClampToSelection(e.Location);
            _lineAnnotations.Add((_lineStart, _lineEnd, _annotationTool == AnnotationTool.Arrow));
            _isDrawingLine = false;
            Capture = false;
            CommitAnnotationsAndRecordUndo();
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
            CommitAnnotationsAndRecordUndo();
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
            CommitAnnotationsAndRecordUndo();
            InvalidateShapePreview(_circleStart, previousEnd, _circleEnd);
            return;
        }

        if (_isDrawingMaskRegion)
        {
            var previousEnd = _maskEnd;
            _maskEnd = ClampToSelection(e.Location);
            var area = GetRectangle(_maskStart, _maskEnd);
            _isDrawingMaskRegion = false;
            Capture = false;
            if (area.Width > 0 && area.Height > 0)
            {
                SaveUndoState();
                ImageEditingService.ApplyMask(_background, area, _maskMode, _lineColor, _maskPixelSize);
                Invalidate(area);
            }

            InvalidateShapePreview(_maskStart, previousEnd, _maskEnd);
            return;
        }

        if (_isResizing || _isMoving)
        {
            _isResizing = false;
            _isMoving = false;
            UpdateActionBarLocation();
            _actionBar.Visible = _selection.Width >= MinimumSelectionSize && _selection.Height >= MinimumSelectionSize;
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

        if (keyData == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return true;
        }

        if (_actionBar.Visible)
        {
            if (keyData == (Keys.Control | Keys.Enter))
            {
                SendSelectionToEditor();
                return true;
            }

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
            _editHistory.Dispose();
            _textCaretTimer.Stop();
            _textCaretTimer.Dispose();
            _sendToEditorFlashTimer.Stop();
            _sendToEditorFlashTimer.Dispose();
            _sendToEditorFlashPen.Dispose();
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

    private ToolStrip CreateScreenSelectionBar(Rectangle[] screens)
    {
        var toolStrip = new ToolStrip
        {
            BackColor = ToolStripColorPalette.BarBackground,
            AutoSize = true,
            CanOverflow = false,
            Dock = DockStyle.None,
            GripStyle = ToolStripGripStyle.Hidden,
            Padding = new Padding(6),
            Renderer = new ToolStripProfessionalRenderer(ToolStripColorPalette.CreateColorTable()),
            ShowItemToolTips = true,
        };

        var fullScreenLabel = AppLocalization.Translate("Capture.Region.FullScreen");
        var fullScreenButton = CreateScreenSelectionButton(fullScreenLabel);
        fullScreenButton.ToolTipText = AppLocalization.Translate("Capture.Region.FullScreenTooltip");
        fullScreenButton.Image = LoadActionIcon("captureall.png", Color.FromArgb(45, 49, 54));
        fullScreenButton.ImageAlign = ContentAlignment.MiddleCenter;
        fullScreenButton.TextImageRelation = TextImageRelation.ImageBeforeText;
        fullScreenButton.ImageScaling = ToolStripItemImageScaling.None;
        fullScreenButton.Click += (_, _) => CompleteScreenSelection(new Rectangle(Point.Empty, _background.Size));
        toolStrip.Items.Add(fullScreenButton);

        if (screens.Length > 1)
        {
            var currentScreenLabel = AppLocalization.Translate("Capture.Region.CurrentScreen");
            var currentScreenButton = CreateScreenSelectionButton(currentScreenLabel);
            currentScreenButton.ToolTipText = AppLocalization.Translate("Capture.Region.CurrentScreenTooltip");
            currentScreenButton.Image = LoadActionIcon("currentdisplay.png", Color.FromArgb(45, 49, 54));
            currentScreenButton.ImageAlign = ContentAlignment.MiddleCenter;
            currentScreenButton.TextImageRelation = TextImageRelation.ImageBeforeText;
            currentScreenButton.ImageScaling = ToolStripItemImageScaling.None;
            currentScreenButton.Click += (_, _) => CompleteScreenSelection(_screenUnderCursorAtStart);
            toolStrip.Items.Add(currentScreenButton);
            toolStrip.Items.Add(new ToolStripSeparator());

            var screenLabel = AppLocalization.Translate("Capture.Region.Screen");
            for (var index = 0; index < screens.Length; index++)
            {
                var screenBounds = screens[index];
                if (screenBounds == _screenUnderCursorAtStart)
                {
                    continue;
                }

                var label = $"{screenLabel} {index + 1}";
                var screenButton = CreateScreenSelectionButton(label);
                screenButton.ToolTipText = AppLocalization.Format("Capture.Region.ScreenTooltip", index + 1);
                screenButton.Text = _showToolLabels ? label : $"{index + 1}";
                screenButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                screenButton.Padding = new Padding(3, 3, 3, 3);
                screenButton.Image = LoadActionIcon("display.png", Color.FromArgb(45, 49, 54));
                screenButton.ImageAlign = ContentAlignment.MiddleCenter;
                screenButton.TextImageRelation = TextImageRelation.ImageBeforeText;
                screenButton.ImageScaling = ToolStripItemImageScaling.None;
                screenButton.Click += (_, _) => CompleteScreenSelection(screenBounds);
                toolStrip.Items.Add(screenButton);
            }
        }

        var buttonsWidth = toolStrip.Items
            .Cast<ToolStripItem>()
            .Sum(item => item.GetPreferredSize(Size.Empty).Width + item.Margin.Horizontal);
        toolStrip.MinimumSize = new Size(buttonsWidth + toolStrip.Padding.Horizontal, 52);
        return toolStrip;
    }

    private ToolStripButton CreateScreenSelectionButton(string label) => new ScreenSelectionButton(label)
    {
        AutoSize = true,
        DisplayStyle = _showToolLabels ? ToolStripItemDisplayStyle.ImageAndText : ToolStripItemDisplayStyle.Image,
        ImageAlign = ContentAlignment.MiddleCenter,
        TextAlign = ContentAlignment.MiddleCenter,
        Margin = new Padding(2, 1, 2, 1),
        Padding = new Padding(8, 3, 8, 3),
        AccessibleName = label,
        ToolTipText = label,
    };

    private void PositionScreenSelectionBar()
    {
        var virtualBounds = SystemInformation.VirtualScreen;
        var screenLeft = _screenUnderCursorAtStart.Left - virtualBounds.Left;
        var screenTop = _screenUnderCursorAtStart.Top - virtualBounds.Top;
        var centeredLeft = screenLeft + (_screenUnderCursorAtStart.Width - _screenSelectionBar.Width) / 2;
        var maximumLeft = Math.Max(screenLeft, screenLeft + _screenUnderCursorAtStart.Width - _screenSelectionBar.Width);
        var left = Math.Clamp(centeredLeft, screenLeft, maximumLeft);

        _screenSelectionBar.Location = new Point(left, screenTop + 12);
    }

    private void CompleteScreenSelection(Rectangle screenBounds)
    {
        var virtualBounds = SystemInformation.VirtualScreen;
        var bitmapBounds = new Rectangle(
            screenBounds.X - virtualBounds.X,
            screenBounds.Y - virtualBounds.Y,
            screenBounds.Width,
            screenBounds.Height);
        var selectedRegion = Rectangle.Intersect(bitmapBounds, new Rectangle(Point.Empty, _background.Size));
        if (selectedRegion.Width >= MinimumSelectionSize && selectedRegion.Height >= MinimumSelectionSize)
        {
            CompleteSelection(selectedRegion, RegionSelectionAction.Capture);
        }
    }

    private ToolStrip CreateActionBar()
    {
        var renderer = new ActionBarRenderer();
        _actionBarRenderer = renderer;
        var toolStrip = new ToolStrip
        {
            BackColor = ToolStripColorPalette.BarBackground,
            AutoSize = true,
            CanOverflow = false,
            Dock = DockStyle.None,
            GripStyle = ToolStripGripStyle.Hidden,
            ImageScalingSize = new Size(18, 18),
            MinimumSize = new Size(0, 52),
            Padding = new Padding(6),
            Renderer = renderer,
            ShowItemToolTips = true,
        };

        var captureLabel = AppLocalization.Translate("Capture.Region.Capture");
        var captureButton = CreateActionButton(
            GetCaptureToolButtonText(captureLabel),
            LoadActionIcon("capture.png", Color.White));
        captureButton.BackColor = ToolStripColorPalette.PrimaryButtonBackground;
        captureButton.ForeColor = Color.White;
        captureButton.AccessibleName = captureLabel;
        captureButton.ToolTipText = $"{captureLabel} (Enter)";
        captureButton.Click += (_, _) => CompleteSelection(RegionSelectionAction.Capture);

        var captureDropdownButton = CreateDropdownButton();
        captureDropdownButton.BackColor = ToolStripColorPalette.PrimaryButtonBackground;
        captureDropdownButton.ForeColor = Color.White;
        captureDropdownButton.AccessibleName = AppLocalization.Translate("Capture.Region.Actions");
        ApplyCaptureSplitButtonSize(captureButton, captureDropdownButton);
        renderer.SetPrimaryGroup(captureButton, captureDropdownButton);

        var captureMenuItem = new ToolStripMenuItem(captureLabel, LoadActionIcon("capture.png", Color.FromArgb(45, 49, 54)))
        {
            ShortcutKeyDisplayString = "Enter",
        };
        captureMenuItem.Click += (_, _) => CompleteSelection(RegionSelectionAction.Capture);
        var sendToEditorMenuItem = new ToolStripMenuItem(
            AppLocalization.Translate("Capture.Region.SendToEditor"),
            LoadActionIcon("sendtoeditor.png", Color.FromArgb(45, 49, 54)))
        {
            ShortcutKeyDisplayString = "Ctrl+Enter",
        };
        sendToEditorMenuItem.Click += (_, _) => SendSelectionToEditor();
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
        captureDropdownButton.DropDownItems.AddRange([captureMenuItem, sendToEditorMenuItem, copyMenuItem, saveMenuItem]);

        var drawingLabel = AppLocalization.Translate("Capture.Region.Drawing");
        var drawingButton = CreateActionButton(
            GetCaptureToolButtonText(drawingLabel),
            LoadActionIcon("freehand.png", Color.FromArgb(45, 49, 54)));
        _drawingButton = drawingButton;
        drawingButton.AccessibleName = drawingLabel;
        drawingButton.ToolTipText = drawingLabel;
        drawingButton.Click += (_, _) => ToggleDrawingTool();

        var drawingDropdownButton = CreateDropdownButton();
        _drawingDropdownButton = drawingDropdownButton;
        drawingDropdownButton.AccessibleName = drawingLabel;
        ApplyCaptureSplitButtonSize(drawingButton, drawingDropdownButton);

        var highlightLabel = AppLocalization.Translate("Editor.Highlight");
        _highlightIcon = LoadActionIcon("highlight.png", Color.FromArgb(45, 49, 54));
        var highlightButton = CreateActionButton(GetCaptureToolButtonText(highlightLabel), _highlightIcon);
        ApplyCaptureToolButtonSize(highlightButton);
        _highlightButton = highlightButton;
        highlightButton.AccessibleName = highlightLabel;
        highlightButton.ToolTipText = $"{highlightLabel} (F7)";
        highlightButton.Click += (_, _) => ToggleStandaloneTool(AnnotationTool.Highlight);

        var counterLabel = AppLocalization.Translate("Editor.Counter");
        var counterIcon = LoadActionIcon("counter.png", Color.FromArgb(45, 49, 54));
        var counterButton = CreateActionButton(GetCaptureToolButtonText(counterLabel), counterIcon);
        ApplyCaptureToolButtonSize(counterButton);
        _counterButton = counterButton;
        counterButton.AccessibleName = counterLabel;
        counterButton.ToolTipText = $"{counterLabel} (F9)";
        counterButton.Click += (_, _) => ToggleStandaloneTool(AnnotationTool.Counter);

        var textLabel = AppLocalization.Translate("Editor.Text");
        var textButton = CreateActionButton(GetCaptureToolButtonText(textLabel), LoadActionIcon("text.png", Color.FromArgb(45, 49, 54)));
        ApplyCaptureToolButtonSize(textButton);
        _textButton = textButton;
        textButton.AccessibleName = textLabel;
        textButton.ToolTipText = $"{textLabel} (F10)";
        textButton.Click += (_, _) => ToggleStandaloneTool(AnnotationTool.Text);

        var undoLabel = AppLocalization.Translate("Editor.Undo");
        var undoButton = CreateActionButton(GetCaptureToolButtonText(undoLabel), LoadActionIcon("undo.png", Color.FromArgb(45, 49, 54)));
        ApplyCaptureToolButtonSize(undoButton);
        _undoButton = undoButton;
        undoButton.AccessibleName = undoLabel;
        undoButton.ToolTipText = $"{undoLabel} (Ctrl+Z)";
        undoButton.Enabled = false;
        undoButton.Click += (_, _) => UndoLastEdit();

        var redoLabel = AppLocalization.Translate("Editor.Redo");
        var redoButton = CreateActionButton(GetCaptureToolButtonText(redoLabel), LoadActionIcon("redo.png", Color.FromArgb(45, 49, 54)));
        ApplyCaptureToolButtonSize(redoButton);
        _redoButton = redoButton;
        redoButton.AccessibleName = redoLabel;
        redoButton.ToolTipText = $"{redoLabel} (Ctrl+Y)";
        redoButton.Enabled = false;
        redoButton.Click += (_, _) => RedoLastEdit();

        _pixelatedIcon = LoadActionIcon("pixelated.png", Color.FromArgb(45, 49, 54));
        _filledIcon = LoadActionIcon("filled.png", Color.FromArgb(45, 49, 54));
        var maskLabel = AppLocalization.Translate("Editor.Mask");
        var maskModeLabel = GetMaskModeLabel(_maskMode);
        var maskButton = CreateActionButton(GetCaptureToolButtonText(maskLabel), _maskMode == MaskMode.Pixelate ? _pixelatedIcon : _filledIcon);
        _maskButton = maskButton;
        maskButton.AccessibleName = maskLabel;
        maskButton.AccessibleDescription = maskModeLabel;
        maskButton.ToolTipText = $"{maskLabel}: {maskModeLabel} (F8)";
        maskButton.Click += (_, _) => ToggleMaskMode();

        var maskDropdownButton = CreateDropdownButton();
        _maskDropdownButton = maskDropdownButton;
        maskDropdownButton.AccessibleName = AppLocalization.Translate("Editor.MaskOptions");
        ApplyCaptureSplitButtonSize(maskButton, maskDropdownButton);

        AddAnnotationMenuItem(drawingButton, drawingDropdownButton, AnnotationTool.Freehand, "Capture.Region.Freehand", "freehand.png");
        AddAnnotationMenuItem(drawingButton, drawingDropdownButton, AnnotationTool.Line, "Capture.Region.Line", "line.png");
        AddAnnotationMenuItem(drawingButton, drawingDropdownButton, AnnotationTool.Arrow, "Capture.Region.Arrow", "arrow.png");
        AddAnnotationMenuItem(drawingButton, drawingDropdownButton, AnnotationTool.Rectangle, "Capture.Region.Rectangle", "square.png");
        AddAnnotationMenuItem(drawingButton, drawingDropdownButton, AnnotationTool.Circle, "Capture.Region.Circle", "circle.png");
        AddMaskMenuItem(MaskMode.Pixelate, "Editor.Pixelated", _pixelatedIcon);
        AddMaskMenuItem(MaskMode.Solid, "Editor.Filled", _filledIcon);
        RestoreLastCaptureTool(drawingButton);

        toolStrip.Items.AddRange([
            drawingButton,
            drawingDropdownButton,
            highlightButton,
            maskButton,
            maskDropdownButton,
            counterButton,
            textButton,
            undoButton,
            redoButton,
            captureButton,
            captureDropdownButton,
        ]);
        if (_showToolLabels)
        {
            toolStrip.PerformLayout();
            foreach (var splitButton in new[] { drawingButton, maskButton, captureButton })
            {
                var preferredWidth = splitButton.Width;
                splitButton.AutoSize = false;
                splitButton.Size = new Size(preferredWidth, ActionControlHeight);
            }
        }

        void SetHoveredGroup(ToolStripItem? mainButton, ToolStripItem? dropdownButton, bool isPrimary = false)
        {
            if (renderer.SetHoveredGroup(mainButton, dropdownButton, isPrimary))
            {
                toolStrip.Invalidate();
            }
        }

        void TrackHoverGroup(ToolStripItem mainButton, ToolStripItem dropdownButton, bool isPrimary = false)
        {
            EventHandler enter = (_, _) => SetHoveredGroup(mainButton, dropdownButton, isPrimary);
            EventHandler leave = (_, _) =>
            {
                var point = toolStrip.PointToClient(Control.MousePosition);
                var hoveredItem = toolStrip.GetItemAt(point);
                if (!ReferenceEquals(hoveredItem, mainButton) && !ReferenceEquals(hoveredItem, dropdownButton))
                {
                    SetHoveredGroup(null, null);
                }
            };

            mainButton.MouseEnter += enter;
            mainButton.MouseLeave += leave;
            dropdownButton.MouseEnter += enter;
            dropdownButton.MouseLeave += leave;
        }

        TrackHoverGroup(drawingButton, drawingDropdownButton);
        TrackHoverGroup(maskButton, maskDropdownButton);
        TrackHoverGroup(captureButton, captureDropdownButton, isPrimary: true);
        toolStrip.MouseLeave += (_, _) =>
        {
            SetHoveredGroup(null, null);
        };
        toolStrip.Location = Point.Empty;
        UpdateHistoryButtons();
        return toolStrip;
    }

    private void AddAnnotationMenuItem(ToolStripButton drawingButton, ToolStripDropDownButton drawingDropdownButton, AnnotationTool tool, string localizationKey, string iconFileName)
    {
        var icon = LoadActionIcon(iconFileName, Color.FromArgb(45, 49, 54));
        var label = AppLocalization.Translate(localizationKey);
        var menuItem = new ToolStripMenuItem(label, icon)
        {
            Tag = tool,
            ShortcutKeyDisplayString = GetDrawingToolShortcut(tool),
        };
        menuItem.Click += (_, _) => SelectAnnotationTool(tool, drawingButton, menuItem, icon);
        drawingDropdownButton.DropDownItems.Add(menuItem);
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

    private void RestoreLastCaptureTool(ToolStripButton drawingButton)
    {
        var initialAnnotationTool = LastUsedTool switch
        {
            EditTool.Freehand => AnnotationTool.Freehand,
            EditTool.Line => AnnotationTool.Line,
            EditTool.Arrow => AnnotationTool.Arrow,
            EditTool.Rectangle => AnnotationTool.Rectangle,
            EditTool.Circle => AnnotationTool.Circle,
            _ => AnnotationTool.None,
        };

        if (initialAnnotationTool != AnnotationTool.None)
        {
            var menuItem = _drawingDropdownButton!.DropDownItems
                .OfType<ToolStripMenuItem>()
                .FirstOrDefault(item => item.Tag is AnnotationTool tool && tool == initialAnnotationTool);
            if (menuItem?.Image is Image icon)
            {
                SelectAnnotationTool(initialAnnotationTool, drawingButton, menuItem, icon, toggleIfAlreadyActive: false);
            }
            return;
        }

        var standaloneTool = LastUsedTool switch
        {
            EditTool.Highlight => AnnotationTool.Highlight,
            EditTool.Counter => AnnotationTool.Counter,
            EditTool.Text => AnnotationTool.Text,
            _ => AnnotationTool.None,
        };
        if (standaloneTool != AnnotationTool.None)
        {
            SetStandaloneTool(standaloneTool, toggleIfAlreadyActive: false);
            return;
        }

        if (LastUsedTool is EditTool.Blur or EditTool.Pixelated)
        {
            var maskMode = LastUsedTool == EditTool.Blur ? MaskMode.Solid : MaskMode.Pixelate;
            var menuItem = _maskDropdownButton!.DropDownItems
                .OfType<ToolStripMenuItem>()
                .FirstOrDefault(item => item.Tag is MaskMode mode && mode == maskMode);
            if (menuItem?.Image is Image icon)
            {
                SelectMaskMode(maskMode, menuItem, icon);
            }
        }
    }

    private static EditTool NormalizeLastCaptureTool(EditTool tool) => tool is
        EditTool.Freehand or EditTool.Line or EditTool.Arrow or EditTool.Rectangle or EditTool.Circle or
        EditTool.Highlight or EditTool.Counter or EditTool.Text or EditTool.Blur or EditTool.Pixelated
            ? tool
            : EditTool.Freehand;

    private static EditTool GetEditTool(AnnotationTool tool) => tool switch
    {
        AnnotationTool.Freehand => EditTool.Freehand,
        AnnotationTool.Line => EditTool.Line,
        AnnotationTool.Arrow => EditTool.Arrow,
        AnnotationTool.Rectangle => EditTool.Rectangle,
        AnnotationTool.Circle => EditTool.Circle,
        AnnotationTool.Highlight => EditTool.Highlight,
        AnnotationTool.Counter => EditTool.Counter,
        AnnotationTool.Text => EditTool.Text,
        _ => EditTool.Freehand,
    };

    private void AddMaskMenuItem(MaskMode mode, string localizationKey, Image icon)
    {
        var menuItem = new ToolStripMenuItem(AppLocalization.Translate(localizationKey), icon)
        {
            Checked = mode == _maskMode,
            Tag = mode,
        };
        menuItem.Click += (_, _) => SelectMaskMode(mode, menuItem, icon);
        _maskDropdownButton!.DropDownItems.Add(menuItem);
    }

    private ToolStripButton CreateActionButton(string text, Image? image)
    {
        var button = new ToolStripButton(text)
        {
            Image = image,
            ImageAlign = ContentAlignment.MiddleCenter,
            TextAlign = ContentAlignment.MiddleCenter,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            DisplayStyle = string.IsNullOrEmpty(text)
                ? image is null ? ToolStripItemDisplayStyle.Text : ToolStripItemDisplayStyle.Image
                : _showToolLabels ? ToolStripItemDisplayStyle.ImageAndText : ToolStripItemDisplayStyle.Image,
            ImageScaling = ToolStripItemImageScaling.None,
            AutoSize = _showToolLabels,
            CheckOnClick = false,
            Size = new Size(IconOnlyActionWidth, ActionControlHeight),
            AutoToolTip = true,
            ToolTipText = text,
        };

        ApplyActionButtonStyle(button);
        return button;
    }

    private ToolStripDropDownButton CreateDropdownButton()
    {
        var button = new ToolStripDropDownButton
        {
            AutoSize = false,
            Size = new Size(ToolStripMetrics.DropdownButtonWidth, ActionControlHeight),
            ImageScaling = ToolStripItemImageScaling.None,
            AutoToolTip = true,
        };

        ApplyActionButtonStyle(button);
        button.Padding = Padding.Empty;
        return button;
    }

    private static void ApplyActionButtonStyle(ToolStripItem button)
    {
        button.Margin = new Padding(2, 1, 2, 1);
        button.Padding = new Padding(3, 2, 3, 2);
        button.BackColor = ToolStripColorPalette.ButtonBackground;
        button.ForeColor = ToolStripColorPalette.ButtonForeground;
    }

    private string GetCaptureToolButtonText(string label) => _showToolLabels ? label : string.Empty;

    private void ApplyCaptureToolButtonSize(ToolStripButton button)
    {
        if (_showToolLabels)
        {
            return;
        }

        button.AutoSize = false;
        button.Size = new Size(IconOnlyActionWidth, ActionControlHeight);
    }

    private void ApplyCaptureSplitButtonSize(ToolStripButton mainButton, ToolStripDropDownButton dropdownButton)
    {
        mainButton.Margin = new Padding(2, 1, 0, 1);
        dropdownButton.Margin = new Padding(0, 1, 2, 1);

        if (_showToolLabels)
        {
            return;
        }

        mainButton.AutoSize = false;
        mainButton.Size = new Size(IconOnlyActionWidth - ToolStripMetrics.DropdownButtonWidth, ActionControlHeight);
        dropdownButton.Size = new Size(ToolStripMetrics.DropdownButtonWidth, ActionControlHeight);
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

    private void CompleteSelection(RegionSelectionAction action) => CompleteSelection(_selection, action);

    private void CompleteSelection(Rectangle selection, RegionSelectionAction action)
    {
        CommitTextEdit();
        CommitAnnotationsAndRecordUndo();

        SelectedRegion = selection;
        SelectedAction = action;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void SendSelectionToEditor()
    {
        if (_selection.Width < MinimumSelectionSize || _selection.Height < MinimumSelectionSize || SendToEditorRequested is null)
        {
            return;
        }

        CommitTextEdit();
        CommitAnnotationsAndRecordUndo();

        var boundedRegion = Rectangle.Intersect(_selection, new Rectangle(Point.Empty, _background.Size));
        if (boundedRegion.Width < MinimumSelectionSize || boundedRegion.Height < MinimumSelectionSize)
        {
            return;
        }

        using var bitmap = _background.Clone(boundedRegion, _background.PixelFormat);
        SendToEditorRequested.Invoke(bitmap);
        _sendToEditorFlashTicks = 6;
        _sendToEditorFlashVisible = true;
        _sendToEditorFlashTimer.Stop();
        _sendToEditorFlashTimer.Start();
        Invalidate(Rectangle.Inflate(_selection, 4, 4));
        if (IsHandleCreated && !IsDisposed)
        {
            BeginInvoke((Action)RestoreSelectionFocus);
        }
    }

    private void RestoreSelectionFocus()
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        Activate();
        Focus();
    }

    private void StopSendToEditorFeedback()
    {
        _sendToEditorFlashTimer.Stop();
        _sendToEditorFlashTicks = 0;
        _sendToEditorFlashVisible = false;
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

    private void CommitAnnotationsAndRecordUndo()
    {
        var hasAnnotations = _freehandStrokes.Count > 0 || _highlightStrokes.Count > 0 ||
            _lineAnnotations.Count > 0 || _counterMarkers.Count > 0 || _rectangles.Count > 0 || _circles.Count > 0 ||
            _isDrawingLine || _isDrawingRectangle || _isDrawingCircle;
        if (hasAnnotations)
        {
            SaveUndoState();
        }

        CommitAnnotations();
        UpdateHistoryButtons();
        Invalidate(_selection);
    }

    private void SaveUndoState()
    {
        _editHistory.Capture(_background, _nextCounterValue);
        UpdateHistoryButtons();
    }

    private void UndoLastEdit()
    {
        CommitTextEdit();
        var previousState = _editHistory.Undo(_background, _nextCounterValue);
        if (previousState is null)
        {
            return;
        }

        using var bitmap = previousState.Value.Bitmap;
        RestoreBackground(bitmap);
        _nextCounterValue = previousState.Value.NextCounterValue;
        UpdateHistoryButtons();
        Invalidate(_selection);
    }

    private void RedoLastEdit()
    {
        CommitTextEdit();
        var nextState = _editHistory.Redo(_background, _nextCounterValue);
        if (nextState is null)
        {
            return;
        }

        using var bitmap = nextState.Value.Bitmap;
        RestoreBackground(bitmap);
        _nextCounterValue = nextState.Value.NextCounterValue;
        UpdateHistoryButtons();
        Invalidate(_selection);
    }

    private void RestoreBackground(Bitmap source)
    {
        using var graphics = Graphics.FromImage(_background);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.DrawImageUnscaled(source, Point.Empty);
    }

    private void UpdateHistoryButtons()
    {
        if (_undoButton is not null)
        {
            _undoButton.Enabled = _editHistory.CanUndo;
        }

        if (_redoButton is not null)
        {
            _redoButton.Enabled = _editHistory.CanRedo;
        }
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
            SaveUndoState();
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

    private void SelectAnnotationTool(AnnotationTool tool, ToolStripButton drawingButton, ToolStripMenuItem? selectedMenuItem, Image selectedIcon)
        => SelectAnnotationTool(tool, drawingButton, selectedMenuItem, selectedIcon, toggleIfAlreadyActive: true);

    private void SelectAnnotationTool(AnnotationTool tool, ToolStripButton drawingButton, ToolStripMenuItem? selectedMenuItem, Image selectedIcon, bool toggleIfAlreadyActive)
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
        LastUsedTool = GetEditTool(tool);
        _selectedAnnotationTool = tool;
        _selectedDrawingMenuItem = selectedMenuItem;
        _selectedDrawingIcon = selectedIcon;
        _annotationTool = isTogglingOff ? AnnotationTool.None : tool;
        _isMaskModeActive = false;
        Cursor = GetCursorForLocation(PointToClient(Cursor.Position));
        UpdateMaskButtonAppearance();
        foreach (ToolStripMenuItem menuItem in _drawingDropdownButton!.DropDownItems)
        {
            menuItem.Checked = menuItem == _selectedDrawingMenuItem;
        }

        drawingButton.Image = _selectedDrawingIcon;
        drawingButton.DisplayStyle = _showToolLabels
            ? ToolStripItemDisplayStyle.ImageAndText
            : ToolStripItemDisplayStyle.Image;
        drawingButton.ToolTipText = $"{AppLocalization.Translate("Capture.Region.Drawing")}: {selectedMenuItem?.Text ?? tool.ToString()} ({GetDrawingToolShortcut(tool)})";
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
        if (_annotationTool != AnnotationTool.None)
        {
            LastUsedTool = GetEditTool(tool);
        }
        _isMaskModeActive = false;
        Cursor = GetCursorForLocation(PointToClient(Cursor.Position));
        UpdateMaskButtonAppearance();
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
            var menuItem = _drawingDropdownButton!.DropDownItems
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
                var mode = _isMaskModeActive
                    ? (_maskMode == MaskMode.Solid ? MaskMode.Pixelate : MaskMode.Solid)
                    : _maskMode;
                var modeMenuItem = _maskDropdownButton!.DropDownItems
                    .OfType<ToolStripMenuItem>()
                    .FirstOrDefault(item => item.Tag is MaskMode menuMode && menuMode == mode);
                if (modeMenuItem?.Image is Image modeIcon)
                {
                    SelectMaskMode(mode, modeMenuItem, modeIcon);
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

            if (_annotationTool != AnnotationTool.None || _isMaskModeActive)
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
            var freehandMenuItem = _drawingDropdownButton?.DropDownItems
                .OfType<ToolStripMenuItem>()
                .FirstOrDefault(item => item.Tag is AnnotationTool tool && tool == AnnotationTool.Freehand);
            if (_drawingButton is not null && freehandMenuItem?.Image is Image icon)
            {
                SelectAnnotationTool(AnnotationTool.Freehand, _drawingButton, freehandMenuItem, icon, toggleIfAlreadyActive: false);
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
            _isMaskModeActive = false;
            UpdateMaskButtonAppearance();
        }

        Cursor = GetCursorForLocation(PointToClient(Cursor.Position));
        UpdateDrawingButtonAppearance();
    }

    private void UpdateDrawingButtonAppearance()
    {
        var isActive = _annotationTool is AnnotationTool.Freehand or AnnotationTool.Line or AnnotationTool.Arrow or AnnotationTool.Rectangle or AnnotationTool.Circle;
        SetActiveActionGroup(isActive ? _drawingButton : null, isActive ? _drawingDropdownButton : null);
        if (_drawingButton is not null)
        {
            SetToolButtonAppearance(_drawingButton, isActive);
        }

        if (_drawingDropdownButton is not null)
        {
            _drawingDropdownButton.BackColor = isActive ? ToolStripColorPalette.ActiveBackground : ToolStripColorPalette.ButtonBackground;
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

    private static void SetToolButtonAppearance(ToolStripButton button, bool isActive)
    {
        button.Checked = isActive;
        button.BackColor = isActive ? ToolStripColorPalette.ActiveBackground : ToolStripColorPalette.ButtonBackground;
        button.ForeColor = ToolStripColorPalette.ButtonForeground;
    }

    private void SetActiveActionGroup(ToolStripItem? mainButton, ToolStripItem? dropdownButton)
    {
        if (_actionBarRenderer?.SetActiveGroup(mainButton, dropdownButton) == true &&
            _actionBar is not null &&
            IsHandleCreated)
        {
            _actionBar.Invalidate();
        }
    }

    private void SelectMaskMode(MaskMode mode, ToolStripMenuItem selectedMenuItem, Image selectedIcon)
    {
        if (_isEditingText)
        {
            CommitTextEdit();
        }

        var isTogglingOff = _isMaskModeActive && _maskMode == mode;
        _annotationTool = AnnotationTool.None;
        Cursor = GetCursorForLocation(PointToClient(Cursor.Position));

        _maskMode = mode;
        _isMaskModeActive = !isTogglingOff;
        LastUsedTool = mode == MaskMode.Solid ? EditTool.Blur : EditTool.Pixelated;
        foreach (ToolStripMenuItem menuItem in _maskDropdownButton!.DropDownItems)
        {
            menuItem.Checked = _isMaskModeActive && menuItem == selectedMenuItem;
        }

        if (_maskButton is not null)
        {
            _maskButton.Image = selectedIcon;
            var modeLabel = GetMaskModeLabel(mode);
            _maskButton.AccessibleDescription = modeLabel;
        }

        UpdateDrawingButtonAppearance();
        UpdateMaskButtonAppearance();
    }

    private void ToggleMaskMode()
    {
        if (_isEditingText)
        {
            CommitTextEdit();
        }

        if (!_isMaskModeActive)
        {
            _annotationTool = AnnotationTool.None;
        }

        _isMaskModeActive = !_isMaskModeActive;
        if (_isMaskModeActive)
        {
            LastUsedTool = _maskMode == MaskMode.Solid ? EditTool.Blur : EditTool.Pixelated;
        }
        Cursor = GetCursorForLocation(PointToClient(Cursor.Position));
        UpdateDrawingButtonAppearance();
        UpdateMaskButtonAppearance();
    }

    private static string GetMaskModeLabel(MaskMode mode) =>
        AppLocalization.Translate(mode == MaskMode.Pixelate ? "Editor.Pixelated" : "Editor.Filled");

    private void UpdateMaskButtonAppearance()
    {
        SetActiveActionGroup(
            _isMaskModeActive ? _maskButton : null,
            _isMaskModeActive ? _maskDropdownButton : null);

        if (_maskButton is null)
        {
            return;
        }

        var maskLabel = AppLocalization.Translate("Editor.Mask");
        var modeLabel = GetMaskModeLabel(_maskMode);
        _maskButton.ToolTipText = $"{maskLabel}: {modeLabel} (F8)";
        _maskButton.AccessibleDescription = modeLabel;
        SetToolButtonAppearance(_maskButton, _isMaskModeActive);

        if (_maskDropdownButton is null)
        {
            return;
        }

        _maskDropdownButton.BackColor = _isMaskModeActive ? ToolStripColorPalette.ActiveBackground : ToolStripColorPalette.ButtonBackground;
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
            ImageEditingService.DrawCounterMarker(graphics, marker, _lineColor, _fillColor, _textColor);
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
        var screenBounds = RectangleToClient(Screen.FromRectangle(RectangleToScreen(_selection)).Bounds);
        var left = Math.Clamp(
            _selection.Left + (_selection.Width - _actionBar.Width) / 2,
            screenBounds.Left,
            Math.Max(screenBounds.Left, screenBounds.Right - _actionBar.Width));
        var top = _selection.Bottom + 18;
        if (top + _actionBar.Height > screenBounds.Bottom)
        {
            top = _selection.Top - _actionBar.Height - 18;
        }

        top = Math.Clamp(top, screenBounds.Top, Math.Max(screenBounds.Top, screenBounds.Bottom - _actionBar.Height));

        _actionBar.Location = new Point(left, top);
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
