using Aprint.Features.Settings;
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
    }

    private const int SelectionBorderWidth = 2;
    private const int HandleSize = 10;
    private const int MinimumSelectionSize = 3;
    private const int FreehandPenWidth = 3;

    private readonly Bitmap _background;
    private readonly OutsideCaptureClickAction _outsideCaptureClickAction;
    private readonly SolidBrush _overlayBrush = new(Color.FromArgb(120, 0, 0, 0));
    private readonly Pen _borderPen = new(Color.White, SelectionBorderWidth);
    private readonly SolidBrush _handleBrush = new(Color.White);
    private readonly List<Image> _actionImages = [];
    private readonly ContextMenuStrip _drawingMenu = new();
    private readonly List<List<Point>> _freehandStrokes = [];
    private readonly List<(Point Start, Point End, bool IsArrow)> _lineAnnotations = [];
    private readonly List<Rectangle> _rectangles = [];
    private readonly List<Rectangle> _circles = [];
    private readonly Panel _actionBar;
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
    private bool _isDrawingLine;
    private bool _isDrawingRectangle;
    private bool _isDrawingCircle;
    private Point _lineStart;
    private Point _lineEnd;
    private Point _rectangleStart;
    private Point _rectangleEnd;
    private Point _circleStart;
    private Point _circleEnd;

    public Rectangle? SelectedRegion { get; private set; }

    public RegionSelectionAction? SelectedAction { get; private set; }

    public RegionSelectorForm(Bitmap background, OutsideCaptureClickAction outsideCaptureClickAction = OutsideCaptureClickAction.SelectNewArea)
    {
        _background = background;
        _outsideCaptureClickAction = outsideCaptureClickAction;

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
            DrawLines(e.Graphics);
            DrawRectangles(e.Graphics);
            DrawCircles(e.Graphics);
            e.Graphics.DrawRectangle(_borderPen, _selection);
            DrawHandles(e.Graphics);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
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

                if (_annotationTool == AnnotationTool.Freehand && _selection.Contains(e.Location))
                {
                    _isDrawingFreehand = true;
                    _freehandStrokes.Add([ClampToSelection(e.Location)]);
                    Capture = true;
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
            _lineAnnotations.Clear();
            _rectangles.Clear();
            _circles.Clear();
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
            Cursor = GetResizeCursor(GetResizeEdges(e.Location));
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        if (_isDrawingFreehand)
        {
            AddFreehandPoint(e.Location);
            _isDrawingFreehand = false;
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _overlayBrush.Dispose();
            _borderPen.Dispose();
            _handleBrush.Dispose();
            _drawingMenu.Dispose();
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
            Size = new Size(680, 94),
            Padding = new Padding(6),
        };

        var captureButton = CreateActionButton(AppLocalization.Translate("Capture.Region.Capture"), true, LoadActionIcon("capture.png", Color.White));
        var saveButton = CreateActionButton(AppLocalization.Translate("Capture.Region.Save"), false, LoadActionIcon("save.png", Color.FromArgb(45, 49, 54)));
        var copyButton = CreateActionButton(AppLocalization.Translate("Capture.Region.Copy"), false, LoadActionIcon("copy.png", Color.FromArgb(45, 49, 54)));
        var drawingLabel = AppLocalization.Translate("Capture.Region.Drawing");
        var drawingButton = CreateActionButton($"{drawingLabel} \u25BE", false, null);
        drawingButton.AccessibleName = drawingLabel;
        drawingButton.ContextMenuStrip = _drawingMenu;
        drawingButton.Click += (_, _) => _drawingMenu.Show(drawingButton, new Point(0, drawingButton.Height));

        captureButton.Click += (_, _) => CompleteSelection(RegionSelectionAction.Capture);
        saveButton.Click += (_, _) => CompleteSelection(RegionSelectionAction.Save);
        copyButton.Click += (_, _) => CompleteSelection(RegionSelectionAction.Copy);

        AddAnnotationMenuItem(drawingButton, AnnotationTool.Freehand, "Capture.Region.Freehand", "freehand.png");
        AddAnnotationMenuItem(drawingButton, AnnotationTool.Line, "Capture.Region.Line", "line.png");
        AddAnnotationMenuItem(drawingButton, AnnotationTool.Arrow, "Capture.Region.Arrow", "arrow.png");
        AddAnnotationMenuItem(drawingButton, AnnotationTool.Rectangle, "Capture.Region.Rectangle", "square.png");
        AddAnnotationMenuItem(drawingButton, AnnotationTool.Circle, "Capture.Region.Circle", "circle.png");

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = panel.BackColor,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        var toolsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = panel.BackColor,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        toolsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        toolsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
        toolsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));

        var actionsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = panel.BackColor,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        for (var column = 0; column < 3; column++)
        {
            actionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        }

        toolsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        toolsLayout.Controls.Add(drawingButton, 1, 0);
        actionsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        actionsLayout.Controls.Add(captureButton, 0, 0);
        actionsLayout.Controls.Add(saveButton, 1, 0);
        actionsLayout.Controls.Add(copyButton, 2, 0);
        layout.Controls.Add(toolsLayout, 0, 0);
        layout.Controls.Add(actionsLayout, 0, 1);
        panel.Controls.Add(layout);
        return panel;
    }

    private void AddAnnotationMenuItem(Button drawingButton, AnnotationTool tool, string localizationKey, string iconFileName)
    {
        var icon = LoadActionIcon(iconFileName, Color.FromArgb(45, 49, 54));
        var menuItem = new ToolStripMenuItem(AppLocalization.Translate(localizationKey), icon);
        menuItem.Click += (_, _) => SelectAnnotationTool(tool, drawingButton, menuItem, icon);
        _drawingMenu.Items.Add(menuItem);
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
            Dock = DockStyle.Fill,
            Height = 34,
            Margin = new Padding(3),
            Padding = Padding.Empty,
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
        if (_freehandStrokes.Count > 0 || _lineAnnotations.Count > 0 || _rectangles.Count > 0 || _circles.Count > 0 || _isDrawingLine || _isDrawingRectangle || _isDrawingCircle)
        {
            using var graphics = Graphics.FromImage(_background);
            DrawFreehandStrokes(graphics);
            DrawLines(graphics);
            DrawRectangles(graphics);
            DrawCircles(graphics);
        }

        SelectedRegion = _selection;
        SelectedAction = action;
        DialogResult = DialogResult.OK;
        Close();
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

    private void SelectAnnotationTool(AnnotationTool tool, Button drawingButton, ToolStripMenuItem selectedMenuItem, Image selectedIcon)
    {
        _annotationTool = _annotationTool == tool ? AnnotationTool.None : tool;
        foreach (ToolStripMenuItem menuItem in _drawingMenu.Items)
        {
            menuItem.Checked = _annotationTool != AnnotationTool.None && menuItem == selectedMenuItem && _annotationTool == tool;
        }

        drawingButton.Image = _annotationTool == AnnotationTool.None ? null : selectedIcon;
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
