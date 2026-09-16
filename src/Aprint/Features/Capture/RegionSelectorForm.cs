namespace Aprint.Features.Capture;

/// <summary>Overlay em tela cheia (todas as telas) que permite ao usuário arrastar o mouse para selecionar uma área a capturar.</summary>
public sealed class RegionSelectorForm : Form
{
    private readonly Bitmap _background;
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

        using (var overlayBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
        {
            e.Graphics.FillRectangle(overlayBrush, ClientRectangle);
        }

        if (_selection is { Width: > 0, Height: > 0 })
        {
            // Revela o conteúdo original (sem escurecer) dentro da área selecionada.
            e.Graphics.DrawImage(_background, _selection, _selection, GraphicsUnit.Pixel);
            using var borderPen = new Pen(Color.DeepSkyBlue, 2);
            e.Graphics.DrawRectangle(borderPen, _selection);
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
            _selection = GetRectangle(_startPoint, e.Location);
            Invalidate();
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

    private static Rectangle GetRectangle(Point p1, Point p2) => new(
        Math.Min(p1.X, p2.X),
        Math.Min(p1.Y, p2.Y),
        Math.Abs(p1.X - p2.X),
        Math.Abs(p1.Y - p2.Y));
}
