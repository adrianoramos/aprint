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

    private enum ToolIconKind
    {
        Color,
        Freehand,
        Line,
        Arrow,
        Rectangle,
        Circle,
        Counter,
        Eraser,
        Filled,
        Pixelated,
        Undo,
        Redo,
        Copy,
        Save,
        NewCapture,
    }

    private const int PenWidth = 3;
    private const int ToolPanelHeight = 50;
    private const int ToolIconBaseSize = 24;
    private const int ToolButtonPadding = 4;
    private const int ToolButtonMargin = 2;
    private const int ToolSeparatorMargin = 4;
    private const int ComboBoxWidth = 56;
    private const int ToolButtonSize = 32;

    private static readonly object ToolIconCacheSync = new();
    private static readonly Dictionary<int, ImageList> ToolIconImageLists = [];
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
    private readonly PictureBox _pictureBox;
    private readonly ToolStripButton _btnFreehand;
    private readonly ToolStripButton _btnLine;
    private readonly ToolStripButton _btnArrow;
    private readonly ToolStripButton _btnRectangle;
    private readonly ToolStripButton _btnCircle;
    private readonly ToolStripButton _btnCounter;
    private readonly ToolStripButton _btnBorrarMode;
    private readonly ToolStripDropDownButton _btnBorrarModeDropdown;
    private readonly ToolStripButton _btnColor;
    private readonly ToolStripButton _btnUndo;
    private readonly ToolStripButton _btnRedo;
    private readonly ToolStripComboBox _cmbBorrarSize;
    private readonly EditHistory _editHistory;
    private readonly System.Windows.Forms.Timer? _inactivityTimer;
    private readonly IMessageFilter? _activityMessageFilter;
    private ToolStripMenuItem _blurMenuFilled;
    private ToolStripMenuItem _blurMenuPixelated;

    private Color _selectedColor = Color.Red;
    private EditTool _currentTool = EditTool.Freehand;
    private BorrarMode _borrarMode = BorrarMode.Pixelate;
    private int _borrarPixelSize = 6;
    private int _nextCounterValue = 1;
    private bool _isDrawing;
    private Point _dragStart;
    private Point _lastFreehandPoint;
    private Rectangle _lastPreviewDirtyRect;
    private Point _shapePreviewEnd;
    private bool _shapePreviewVisible;

    public event Action<string>? Saved;
    public event Action? Copied;
    public event Action<BorrarMode>? BorrarModeChanged;
    public event Action? NewCaptureRequested;

    public EditorForm(Bitmap bitmap, string initialFolder, BorrarMode borrarMode = BorrarMode.Pixelate, bool showToolLabels = true, bool closeAfterCopyOrSave = false, EditTool initialTool = EditTool.Freehand, bool saveWithoutPrompt = false, bool autoCloseWhenInactive = false, int inactivityTimeoutSeconds = 30)
    {
        _workingBitmap = (Bitmap)bitmap.Clone();
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
        _toolIconImageList = GetOrCreateToolIconImageList(_toolIconSize);

        Text = AppLocalization.Translate("Editor.FormTitle");
        Icon = Program.AppIcon;
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
            AppLocalization.Translate("Editor.Color"),
            ToolIconKind.Color,
            _selectedColor);
        UpdateColorButtonAppearance();
        _btnColor.Click += (_, _) => PickColor();

        _btnFreehand = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Freehand"),
            ToolIconKind.Freehand,
            Color.Black,
            isCheckable: true,
            isChecked: true);
        _btnLine = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Line"),
            ToolIconKind.Line,
            Color.Black,
            isCheckable: true);
        _btnArrow = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Arrow"),
            ToolIconKind.Arrow,
            Color.Black,
            isCheckable: true);
        _btnRectangle = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Rectangle"),
            ToolIconKind.Rectangle,
            Color.Black,
            isCheckable: true);
        _btnCircle = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Circle"),
            ToolIconKind.Circle,
            Color.Black,
            isCheckable: true);
        _btnCounter = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Counter"),
            ToolIconKind.Counter,
            Color.Black,
            isCheckable: true);

        _btnBorrarMode = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Blur"),
            ToolIconKind.Eraser,
            Color.Black,
            isCheckable: false);
        _btnBorrarMode.Click += (_, _) => SetActiveTool(EditTool.Blur);

        _btnBorrarModeDropdown = new ToolStripDropDownButton()
        {
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            ImageKey = EnsureToolIconKey(ToolIconKind.Circle, Color.Black),
            ImageScaling = ToolStripItemImageScaling.None,
            Margin = new Padding(0, 0, ToolButtonMargin, 0),
            AutoSize = false,
            Width = 14,
            Height = ToolButtonSize,
            AutoToolTip = false,
            ToolTipText = AppLocalization.Translate("Editor.BlurOptions"),
        };

        _btnFreehand.Click += (_, _) => SetActiveTool(EditTool.Freehand);
        _btnLine.Click += (_, _) => SetActiveTool(EditTool.Line);
        _btnArrow.Click += (_, _) => SetActiveTool(EditTool.Arrow);
        _btnRectangle.Click += (_, _) => SetActiveTool(EditTool.Rectangle);
        _btnCircle.Click += (_, _) => SetActiveTool(EditTool.Circle);
        _btnCounter.Click += (_, _) => SetActiveTool(EditTool.Counter);

        _blurMenuFilled = new ToolStripMenuItem(AppLocalization.Translate("Editor.Filled"))
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _blurMenuFilled.Click += (_, _) => SetBorrarMode(BorrarMode.Solid);

        _blurMenuPixelated = new ToolStripMenuItem(AppLocalization.Translate("Editor.Pixelated"))
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _blurMenuPixelated.Click += (_, _) => SetBorrarMode(BorrarMode.Pixelate);

        _btnBorrarModeDropdown.DropDownItems.Add(_blurMenuFilled);
        _btnBorrarModeDropdown.DropDownItems.Add(_blurMenuPixelated);
        SetBorrarMode(_borrarMode);

        _btnUndo = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Undo"),
            ToolIconKind.Undo,
            Color.Black);
        _btnUndo.Click += (_, _) => UndoLastEdit();

        _btnRedo = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Redo"),
            ToolIconKind.Redo,
            Color.Black);
        _btnRedo.Click += (_, _) => RedoLastEdit();

        _cmbBorrarSize = new ToolStripComboBox
        {
            Width = ComboBoxWidth,
            AutoSize = false,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Visible = _borrarMode == BorrarMode.Pixelate,
        };
        _cmbBorrarSize.Items.AddRange(["6 px", "8 px", "12 px", "18 px", "32 px"]);
        _cmbBorrarSize.SelectedIndex = 0;
        _cmbBorrarSize.SelectedIndexChanged += (_, _) =>
        {
            _borrarPixelSize = _cmbBorrarSize.SelectedIndex switch
            {
                1 => 8,
                2 => 12,
                3 => 18,
                4 => 32,
                _ => 6,
            };
        };

        var btnCopy = CreateStandardToolButton(
            AppLocalization.Translate("Editor.Copy"),
            ToolIconKind.Copy,
            Color.Black);
        btnCopy.Click += (_, _) => CopyToClipboard();

        var btnNewCapture = CreateStandardToolButton(
            AppLocalization.Translate("Editor.NewCapture"),
            ToolIconKind.NewCapture,
            Color.Black);
        btnNewCapture.Click += (_, _) =>
        {
            NewCaptureRequested?.Invoke();
            Close();
        };

        var btnSaveAs = CreateStandardToolButton(
            AppLocalization.Translate("Editor.SaveAs"),
            ToolIconKind.Save,
            Color.Black);
        btnSaveAs.Click += async (_, _) => await SaveAsAsync();

        toolStrip.Items.AddRange([
            btnNewCapture,
            CreateToolStripSeparator(),
            _btnColor,
            _btnFreehand,
            _btnLine,
            _btnArrow,
            _btnRectangle,
            _btnCircle,
            _btnCounter,
            _btnBorrarMode,
            _btnBorrarModeDropdown,
            _cmbBorrarSize,
            _btnUndo,
            _btnRedo,
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
        };
        _pictureBox.MouseDown += PictureBox_MouseDown;
        _pictureBox.MouseMove += PictureBox_MouseMove;
        _pictureBox.MouseUp += PictureBox_MouseUp;
        _pictureBox.Paint += PictureBox_Paint;

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
        bool isChecked = false)
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
        };
    }

    private string EnsureToolIconKey(ToolIconKind kind, Color color)
    {
        var key = new ToolIconCacheKey(kind, color.ToArgb());
        var imageKey = $"{_toolIconSize}:{(int)key.Kind}:{key.ColorArgb}";

        lock (ToolIconCacheSync)
        {
            if (!_toolIconImageList.Images.ContainsKey(imageKey))
            {
                _toolIconImageList.Images.Add(imageKey, RenderToolIcon(kind, color, _toolIconSize));
            }

            return imageKey;
        }
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

    private static ImageList GetOrCreateToolIconImageList(int iconSize)
    {
        lock (ToolIconCacheSync)
        {
            if (!ToolIconImageLists.TryGetValue(iconSize, out var imageList))
            {
                imageList = CreateToolIconImageList(iconSize);
                ToolIconImageLists[iconSize] = imageList;
            }

            return imageList;
        }
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
            if (kind == ToolIconKind.Color && color != Color.Black)
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
            ToolIconKind.Color => "Aprint.Resources.Icons.color.png",
            ToolIconKind.Freehand => "Aprint.Resources.Icons.freehand.png",
            ToolIconKind.Line => "Aprint.Resources.Icons.line.png",
            ToolIconKind.Arrow => "Aprint.Resources.Icons.arrow.png",
            ToolIconKind.Rectangle => "Aprint.Resources.Icons.rectangle.png",
            ToolIconKind.Circle => "Aprint.Resources.Icons.circle.png",
            ToolIconKind.Counter => "Aprint.Resources.Icons.counter.png",
            ToolIconKind.Eraser => "Aprint.Resources.Icons.erase.png",
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
        _currentTool = tool;
        _btnFreehand.Checked = tool == EditTool.Freehand;
        _btnLine.Checked = tool == EditTool.Line;
        _btnArrow.Checked = tool == EditTool.Arrow;
        _btnRectangle.Checked = tool == EditTool.Rectangle;
        _btnCircle.Checked = tool == EditTool.Circle;
        _btnCounter.Checked = tool == EditTool.Counter;
        _btnBorrarMode.Checked = tool == EditTool.Blur;
    }

    private void SetBorrarMode(BorrarMode mode)
    {
        _borrarMode = mode;
        _blurMenuFilled.Checked = mode == BorrarMode.Solid;
        _blurMenuPixelated.Checked = mode == BorrarMode.Pixelate;

        // Atualiza o ícone do botão Borrar baseado no modo selecionado
        var iconKind = mode == BorrarMode.Solid ? ToolIconKind.Filled : ToolIconKind.Pixelated;
        _btnBorrarMode.ImageKey = EnsureToolIconKey(iconKind, Color.Black);

        // Exibe o dropdown de tamanho apenas quando o modo é Pixelado
        if (_cmbBorrarSize != null)
        {
            _cmbBorrarSize.Visible = mode == BorrarMode.Pixelate;
        }

        BorrarModeChanged?.Invoke(mode);
    }

    private void PickColor()
    {
        using var dialog = new ColorDialog { Color = _selectedColor, FullOpen = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _selectedColor = dialog.Color;
            UpdateColorButtonAppearance();
        }
    }

    private void UpdateColorButtonAppearance()
    {
        _btnColor.ImageKey = EnsureToolIconKey(ToolIconKind.Color, _selectedColor);
        _btnColor.ToolTipText = $"{AppLocalization.Translate("Editor.Color")}: {_selectedColor.Name}";
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
        _pictureBox.Invalidate();
    }

    private void PictureBox_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _isDrawing = true;
        _dragStart = e.Location;
        _lastFreehandPoint = e.Location;

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
        if (!_isDrawing)
        {
            return;
        }

        using var pen = new Pen(_selectedColor, PenWidth) { StartCap = LineCap.Round, EndCap = LineCap.Round };

        if (_currentTool == EditTool.Freehand)
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
        if (!_isDrawing)
        {
            return;
        }

        _isDrawing = false;

        if (_currentTool == EditTool.Blur)
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
                ImageEditingService.DrawShape(graphics, pen, _dragStart, e.Location, _currentTool);
            }

            _pictureBox.Image = _workingBitmap;
            _shapePreviewVisible = false;
            _lastPreviewDirtyRect = Rectangle.Empty;
            InvalidatePictureBoxRegion(repaintRegion);
        }
    }

    private void PictureBox_Paint(object? sender, PaintEventArgs e)
    {
        if (!_shapePreviewVisible)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        if (_currentTool == EditTool.Blur)
        {
            using var selectionPen = new Pen(Color.Red, 2) { DashStyle = DashStyle.Dash };
            var selection = ImageEditingService.CreateSelectionRectangle(_dragStart, _shapePreviewEnd);
            e.Graphics.DrawRectangle(selectionPen, selection);
            return;
        }

        using var pen = new Pen(_selectedColor, PenWidth) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        ImageEditingService.DrawShape(e.Graphics, pen, _dragStart, _shapePreviewEnd, _currentTool);
    }

    private void ApplyBorrarToSelection(Rectangle area)
    {
        ImageEditingService.ApplyBorrar(_workingBitmap, area, _borrarMode, _selectedColor, _borrarPixelSize);
        _pictureBox.Image = _workingBitmap;
        InvalidatePictureBoxRegion(InflateRectangle(area, 2));
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
        var baseRect = tool == EditTool.Blur
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
            SetActiveTool(EditTool.Blur);
            return true;
        }

        if (keyData == Keys.F8)
        {
            SetActiveTool(EditTool.Counter);
            return true;
        }

        if (keyData == Keys.F9)
        {
            SetBorrarMode(_borrarMode == BorrarMode.Pixelate ? BorrarMode.Solid : BorrarMode.Pixelate);
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
            _editHistory.Dispose();
            _workingBitmap.Dispose();
        }

        base.Dispose(disposing);
    }
}
