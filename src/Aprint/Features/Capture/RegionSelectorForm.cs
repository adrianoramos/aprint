namespace Aprint.Features.Capture;

/// <summary>Overlay em tela cheia (todas as telas) que permite ao usuário arrastar o mouse para selecionar uma área a capturar.</summary>
public sealed class RegionSelectorForm : Form
{
    private const int SelectionBorderWidth = 2;

    private readonly Bitmap _background;
    private readonly SolidBrush _overlayBrush = new(Color.FromArgb(120, 0, 0, 0));
    private readonly Pen _borderPen = new(Color.DeepSkyBlue, SelectionBorderWidth);
    private Point _startPoint;
    private Rectangle _selection;
    private bool _isSelecting;

    public Rectangle? SelectedRegion { get; private set; }

    public RegionSelectorForm(Bitmap background)
    {
        _background = background;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = SystemInformation.VirtualScreen;
        TopMost = true;
        ShowInTaskbar = false;
        KeyPreview = true;
        DoubleBuffered = true;
        Cursor = Cursors.Cross;
        BackColor = Color.Black;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.DrawImageUnscaled(_background, Point.Empty);
        e.Graphics.FillRectangle(_overlayBrush, ClientRectangle);

        if (_selection is { Width: > 0, Height: > 0 })
        {
            // Revela o conteúdo original (sem escurecer) dentro da área selecionada.
            e.Graphics.DrawImage(_background, _selection, _selection, GraphicsUnit.Pixel);
            e.Graphics.DrawRectangle(_borderPen, _selection);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isSelecting = true;
            _startPoint = e.Location;
            _selection = new Rectangle(e.Location, Size.Empty);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_isSelecting)
        {
            var nextSelection = GetRectangle(_startPoint, e.Location);
            if (nextSelection == _selection)
            {
                return;
            }

            using var invalidRegion = GetInvalidRegion(_selection, nextSelection);
            _selection = nextSelection;
            Invalidate(invalidRegion);
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || !_isSelecting)
        {
            return;
        }

        _isSelecting = false;
        _selection = GetRectangle(_startPoint, e.Location);

        if (_selection.Width > 2 && _selection.Height > 2)
        {
            SelectedRegion = _selection;
            DialogResult = DialogResult.OK;
        }

        Close();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _overlayBrush.Dispose();
            _borderPen.Dispose();
        }

        base.Dispose(disposing);
    }

    private static Region GetInvalidRegion(Rectangle previous, Rectangle current)
    {
        var changedRegion = new Region(previous);
        changedRegion.Xor(current);

        AddBorderToRegion(changedRegion, previous);
        AddBorderToRegion(changedRegion, current);
        return changedRegion;
    }

    private static void AddBorderToRegion(Region region, Rectangle rectangle)
    {
        if (rectangle.Width <= 0 || rectangle.Height <= 0)
        {
            return;
        }

        var borderMargin = SelectionBorderWidth + 1;
        var outerRectangle = rectangle;
        outerRectangle.Inflate(borderMargin, borderMargin);

        using var borderRegion = new Region(outerRectangle);
        var innerRectangle = rectangle;
        innerRectangle.Inflate(-borderMargin, -borderMargin);
        if (innerRectangle.Width > 0 && innerRectangle.Height > 0)
        {
            borderRegion.Exclude(innerRectangle);
        }

        region.Union(borderRegion);
    }

    private static Rectangle GetRectangle(Point p1, Point p2) => new(
        Math.Min(p1.X, p2.X),
        Math.Min(p1.Y, p2.Y),
        Math.Abs(p1.X - p2.X),
        Math.Abs(p1.Y - p2.Y));
}
