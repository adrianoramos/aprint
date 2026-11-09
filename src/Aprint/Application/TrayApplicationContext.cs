using System.Drawing;
using System.Diagnostics;
using Aprint.DocumentSession;
using Aprint.Features.About;
using Aprint.Features.Capture;
using Aprint.Features.Editor;
using Aprint.Features.Settings;
using Aprint.Infrastructure.Windows;

namespace Aprint.Application;

/// <summary>Contexto de aplicação sem janela principal — controla o ícone da bandeja do sistema.</summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly GlobalHotkeyManager _hotkeyManager = new();
    private readonly SettingsStore _settingsStore = new();
    private readonly CaptureWorkflow _captureWorkflow = new();
    private readonly Image _captureNowIcon;
    private readonly ToolStripMenuItem _captureNowMenuItem;
    private readonly ToolStripMenuItem _toggleCaptureModeMenuItem;
    private readonly ToolStripMenuItem _sessionMenuItem;
    private readonly ToolStripMenuItem _settingsMenuItem;
    private readonly ToolStripMenuItem _aboutMenuItem;
    private readonly ToolStripMenuItem _exitMenuItem;
    private readonly HashSet<EditorForm> _openEditors = [];
    private AppSettings _settings;
    private SettingsForm? _settingsForm;
    private AboutForm? _aboutForm;
    private string? _lastSavedFilePath;
    private Bitmap? _lastCapturedBitmap;
    private IDocumentSessionWriter? _documentSession;
    private bool _isCaptureInProgress;
    private bool _isDocumentOperationInProgress;

    public TrayApplicationContext(AppSettings settings, SettingsStore settingsStore)
    {
        _settings = settings;
        _settingsStore = settingsStore;
        AppLocalization.ApplyCulture(_settings.Language);

        _captureNowIcon = LoadMenuIcon("capture.png");
        _captureNowMenuItem = new ToolStripMenuItem { Image = _captureNowIcon };
        _captureNowMenuItem.Click += (_, _) => CaptureScreenshot();
        _toggleCaptureModeMenuItem = new ToolStripMenuItem();
        _toggleCaptureModeMenuItem.Click += (_, _) => CaptureScreenshot(
            _settings.CaptureMode == CaptureMode.SelectArea ? CaptureMode.AllScreens : CaptureMode.SelectArea);
        _sessionMenuItem = new ToolStripMenuItem();
        _sessionMenuItem.Click += (_, _) =>
        {
            if (_documentSession is null)
            {
                StartSession();
            }
            else
            {
                EndSession();
            }
        };
        _settingsMenuItem = new ToolStripMenuItem();
        _settingsMenuItem.Click += (_, _) => OpenSettings();
        _aboutMenuItem = new ToolStripMenuItem();
        _aboutMenuItem.Click += (_, _) => ShowAbout();
        _exitMenuItem = new ToolStripMenuItem();
        _exitMenuItem.Click += (_, _) => ExitApplication();

        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.Add(_captureNowMenuItem);
        _contextMenu.Items.Add(_toggleCaptureModeMenuItem);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(_sessionMenuItem);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(_settingsMenuItem);
        _contextMenu.Items.Add(_aboutMenuItem);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(_exitMenuItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = Program.AppIcon,
            Visible = true,
            ContextMenuStrip = _contextMenu,
        };
        _notifyIcon.DoubleClick += (_, _) => CaptureScreenshot();
        _notifyIcon.BalloonTipClicked += (_, _) => HandleBalloonTipClicked();
        _notifyIcon.BalloonTipClosed += (_, _) =>
        {
            _lastCapturedBitmap?.Dispose();
            _lastCapturedBitmap = null;
        };

        ApplyLocalizedTexts();
        UpdateSessionUi();

        _hotkeyManager.CommandPressed += HandleHotkeyCommand;
        RegisterHotkeys();
    }

    private void RegisterHotkeys()
    {
        var key = _settings.ForcePrintScreen ? Keys.PrintScreen : _settings.HotkeyKey;
        var ctrl = _settings.ForcePrintScreen ? false : _settings.HotkeyCtrl;
        var shift = _settings.ForcePrintScreen ? false : _settings.HotkeyShift;
        var alt = _settings.ForcePrintScreen ? false : _settings.HotkeyAlt;
        var win = _settings.ForcePrintScreen ? false : _settings.HotkeyWin;

        var failedCommands = _hotkeyManager.RegisterMany([
            new GlobalHotkeyBinding(GlobalHotkeyCommand.Capture, key, ctrl, shift, alt, win),
            new GlobalHotkeyBinding(
                GlobalHotkeyCommand.StartSession,
                _settings.StartSessionHotkey.Key,
                _settings.StartSessionHotkey.Ctrl,
                _settings.StartSessionHotkey.Shift,
                _settings.StartSessionHotkey.Alt,
                _settings.StartSessionHotkey.Win),
            new GlobalHotkeyBinding(
                GlobalHotkeyCommand.EndSession,
                _settings.EndSessionHotkey.Key,
                _settings.EndSessionHotkey.Ctrl,
                _settings.EndSessionHotkey.Shift,
                _settings.EndSessionHotkey.Alt,
                _settings.EndSessionHotkey.Win),
        ]);
        if (failedCommands.Count > 0)
        {
            _notifyIcon.ShowBalloonTip(3000, AppLocalization.Translate("App.Name"),
                AppLocalization.Translate("Tray.HotkeyRegisterError"),
                ToolTipIcon.Warning);
        }
    }

    private void HandleHotkeyCommand(GlobalHotkeyCommand command)
    {
        switch (command)
        {
            case GlobalHotkeyCommand.Capture:
                CaptureScreenshot();
                break;
            case GlobalHotkeyCommand.StartSession:
                StartSession();
                break;
            case GlobalHotkeyCommand.EndSession:
                EndSession();
                break;
        }
    }

    private void StartSession()
    {
        if (_documentSession is not null || _isCaptureInProgress)
        {
            return;
        }

        var templatePath = _settings.DocumentTemplatePath;
        var hasTemplate = !string.IsNullOrWhiteSpace(templatePath) &&
            string.Equals(Path.GetExtension(templatePath), ".docx", StringComparison.OrdinalIgnoreCase) &&
            File.Exists(templatePath);
        if (!hasTemplate && MessageBox.Show(
                AppLocalization.Translate("Tray.SessionUseBlankTemplatePrompt"),
                AppLocalization.Translate("App.Name"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            var outputFolder = string.IsNullOrWhiteSpace(_settings.DocumentOutputFolder)
                ? AppSettings.GetDefaultDocumentOutputFolder()
                : _settings.DocumentOutputFolder;
            Directory.CreateDirectory(outputFolder);
            var fileName = $"Aprint_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.docx";
            var outputPath = Path.Combine(outputFolder, $"Aprint_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{Guid.NewGuid():N}.docx");
            if (!_settings.SaveDocumentWithoutPrompt)
            {
                using var dialog = new SaveFileDialog
                {
                    AddExtension = true,
                    DefaultExt = "docx",
                    FileName = fileName,
                    Filter = AppLocalization.Translate("Settings.DocumentTemplateFilter"),
                    InitialDirectory = outputFolder,
                    OverwritePrompt = true,
                };
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                outputPath = dialog.FileName;
            }

            _documentSession = hasTemplate
                ? DocxSessionWriter.Create(templatePath, outputPath)
                : DocxSessionWriter.CreateBlank(outputPath);
            foreach (var editor in _openEditors)
            {
                editor.SetDocumentSessionState(true, _settings.CloseEditorAfterDocumentSend);
            }
            UpdateSessionUi();
            _notifyIcon.ShowBalloonTip(3000, AppLocalization.Translate("Tray.SessionStarted"),
                AppLocalization.Translate("Tray.SessionReady"), ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            _notifyIcon.ShowBalloonTip(4000, AppLocalization.Translate("Tray.SessionStartError"), ex.Message, ToolTipIcon.Error);
        }
    }

    private bool EndSession()
    {
        var session = _documentSession;
        if (session is null)
        {
            return session is null;
        }

        if (_isCaptureInProgress || _isDocumentOperationInProgress)
        {
            _notifyIcon.ShowBalloonTip(3000, AppLocalization.Translate("Tray.SessionEndError"),
                AppLocalization.Translate("Tray.DocumentOperationInProgress"), ToolTipIcon.Warning);
            return false;
        }

        try
        {
            var documentPath = session.Complete();
            session.Dispose();
            _documentSession = null;
            foreach (var editor in _openEditors.ToArray())
            {
                editor.SetDocumentSessionState(false, closeAfterSend: false);
            }
            UpdateSessionUi();
            if (_settings.CloseAllEditorsOnSessionEnd)
            {
                foreach (var editor in _openEditors.ToArray())
                {
                    editor.Close();
                }
            }

            _notifyIcon.ShowBalloonTip(3000,
                documentPath is null ? AppLocalization.Translate("Tray.SessionEnded") : AppLocalization.Translate("Tray.DocumentSaved"),
                documentPath is null ? AppLocalization.Translate("Tray.SessionEmpty") : AppLocalization.Format("Tray.SavedTo", documentPath),
                ToolTipIcon.Info);
            return true;
        }
        catch (Exception ex)
        {
            _notifyIcon.ShowBalloonTip(4000, AppLocalization.Translate("Tray.SessionEndError"), ex.Message, ToolTipIcon.Error);
            return false;
        }
    }

    private void UpdateSessionUi()
    {
        var active = _documentSession is not null;
        _notifyIcon.Icon = active ? Program.SessionIcon : Program.AppIcon;
        _notifyIcon.Text = AppLocalization.Translate(active ? "Tray.SessionIconText" : "Tray.IconText");
        _sessionMenuItem.Text = AppLocalization.Translate(active ? "Tray.EndSession" : "Tray.StartSession");
    }

    private async Task AddImageToDocumentAsync(Bitmap image)
    {
        var session = _documentSession ?? throw new InvalidOperationException(AppLocalization.Translate("Tray.SessionNotActive"));
        if (_isDocumentOperationInProgress)
        {
            throw new InvalidOperationException(AppLocalization.Translate("Tray.DocumentOperationInProgress"));
        }

        _isDocumentOperationInProgress = true;
        try
        {
            using var encoded = new MemoryStream();
            image.Save(encoded, System.Drawing.Imaging.ImageFormat.Png);
            await session.AddImageAsync(encoded.ToArray());
            _notifyIcon.ShowBalloonTip(2000, AppLocalization.Translate("Tray.ImageAddedToDocument"),
                AppLocalization.Format("Tray.DocumentImageCount", session.ImageCount), ToolTipIcon.Info);
        }
        finally
        {
            _isDocumentOperationInProgress = false;
        }
    }

    private void CaptureScreenshot()
    {
        CaptureScreenshot(null);
    }

    private async void CaptureScreenshot(CaptureMode? captureModeOverride)
    {
        if (_isCaptureInProgress)
        {
            return;
        }

        _isCaptureInProgress = true;
        try
        {
            var previousCaptureTool = _settings.LastCaptureTool;
            using var result = await _captureWorkflow.ExecuteAsync(
                _settings,
                captureModeOverride,
                settingsChanged: () => _settingsStore.Save(_settings),
                sendToEditorWithOwnership: bitmap =>
                {
                    OpenEditor(bitmap, showWithoutActivation: true, takeBitmapOwnership: true);
                    return true;
                },
                documentSessionActive: _documentSession is not null);
            if (_settings.LastCaptureTool != previousCaptureTool)
            {
                _settingsStore.Save(_settings);
            }

            if (result is null)
            {
                return;
            }

            _lastCapturedBitmap?.Dispose();
            _lastCapturedBitmap = _settings.ShowNotificationAfterCapture &&
                _settings.NotificationClickAction == NotificationClickAction.OpenEditor
                    ? (Bitmap)result.Bitmap.Clone()
                    : null;

            if (result.SendToDocument)
            {
                await AddImageToDocumentAsync(result.Bitmap);
                return;
            }

            if (result.OpenEditor)
            {
                if (_settings.ShowNotificationAfterCapture)
                {
                    _notifyIcon.ShowBalloonTip(3000, AppLocalization.Translate("Tray.ScreenshotCaptured"), AppLocalization.Translate("Tray.EditorOpened"), ToolTipIcon.Info);
                }

                _isCaptureInProgress = false;
                OpenEditor(result.TakeBitmap(), takeBitmapOwnership: true);
                return;
            }

            _lastSavedFilePath = result.SavedFilePath;

            if (_settings.ShowNotificationAfterCapture)
            {
                var message = result.SelectionAction switch
                {
                    RegionSelectionAction.Copy => AppLocalization.Translate("Tray.ClipboardOnly"),
                    RegionSelectionAction.Save => AppLocalization.Format("Tray.SavedTo", result.SavedFilePath ?? string.Empty),
                    _ => _settings.CaptureAction switch
                    {
                        CaptureAction.ClipboardOnly => AppLocalization.Translate("Tray.ClipboardOnly"),
                        CaptureAction.SaveToDiskOnly => AppLocalization.Format("Tray.SavedTo", result.SavedFilePath ?? string.Empty),
                        CaptureAction.Both => AppLocalization.Format("Tray.BothSavedAndClipboard", result.SavedFilePath ?? string.Empty),
                        _ => AppLocalization.Translate("Tray.CapturedFallback"),
                    },
                };
                _notifyIcon.ShowBalloonTip(3000, AppLocalization.Translate("Tray.ScreenshotCaptured"), message, ToolTipIcon.Info);
            }
        }
        catch (Exception ex)
        {
            if (Debugger.IsAttached)
            {
                throw;
            }

            _notifyIcon.ShowBalloonTip(4000, AppLocalization.Translate("Tray.CaptureError"), ex.Message, ToolTipIcon.Error);
        }
        finally
        {
            _isCaptureInProgress = false;
        }
    }

    private void OpenEditor(Bitmap bitmap, bool topMost = false, bool showWithoutActivation = false, bool takeBitmapOwnership = false)
    {
        var initialTool = _settings.RememberLastEditorTool ? _settings.LastEditorTool : _settings.DefaultEditorTool;
        var editor = new EditorForm(
            bitmap,
            _settings.SaveFolder,
            _settings.MaskMode,
            _settings.ShowToolLabels,
            _settings.CloseEditorOnCopyOrSave,
            initialTool,
            _settings.SaveEditorImageWithoutPrompt,
            _settings.AutoCloseEditorWhenInactive,
            _settings.EditorInactivityTimeoutSeconds,
            _settings.LastTextFontFamily,
            _settings.LastTextFontSize,
            _settings.LastTextFontStyle,
            _settings.LastHighlightWidth,
            _settings.LastLineColorArgb,
            _settings.LastTextColorArgb,
            _settings.LastFillColorArgb,
            _settings.LastMaskPixelSize,
            showWithoutActivation,
            _settings.LastHighlightColorArgb,
            _settings.LastMaskColorArgb,
            _settings.LastCounterSize,
            _settings.LastCounterTextColorArgb,
            _settings.LastCounterCircleColorArgb,
            _settings.LastLineWidth,
            takeBitmapOwnership,
            _settings.DefaultScreenshotFormat,
            _documentSession is not null,
            _settings.CloseEditorAfterDocumentSend);
        var captureAgain = false;
        editor.NewCaptureRequested += () => captureAgain = true;
        editor.SendToDocumentRequested += AddImageToDocumentAsync;

        editor.TextFormattingChanged += (fontFamily, fontSize, fontStyle) =>
        {
            _settings.LastTextFontFamily = fontFamily;
            _settings.LastTextFontSize = fontSize;
            _settings.LastTextFontStyle = fontStyle;
            _settingsStore.Save(_settings);
        };

        editor.HighlightWidthChanged += width =>
        {
            _settings.LastHighlightWidth = width;
            _settingsStore.Save(_settings);
        };

        editor.LineColorChanged += color =>
        {
            _settings.LastLineColorArgb = color.ToArgb();
            _settingsStore.Save(_settings);
        };

        editor.LineWidthChanged += width =>
        {
            _settings.LastLineWidth = width;
            _settingsStore.Save(_settings);
        };

        editor.HighlightColorChanged += color =>
        {
            _settings.LastHighlightColorArgb = color.ToArgb();
            _settingsStore.Save(_settings);
        };

        editor.MaskColorChanged += color =>
        {
            _settings.LastMaskColorArgb = color.ToArgb();
            _settingsStore.Save(_settings);
        };

        editor.CounterSizeChanged += size =>
        {
            _settings.LastCounterSize = size;
            _settingsStore.Save(_settings);
        };

        editor.CounterTextColorChanged += color =>
        {
            _settings.LastCounterTextColorArgb = color.ToArgb();
            _settingsStore.Save(_settings);
        };

        editor.CounterCircleColorChanged += color =>
        {
            _settings.LastCounterCircleColorArgb = color.ToArgb();
            _settingsStore.Save(_settings);
        };

        editor.TextColorChanged += color =>
        {
            _settings.LastTextColorArgb = color.ToArgb();
            _settingsStore.Save(_settings);
        };

        editor.FillColorChanged += color =>
        {
            _settings.LastFillColorArgb = color?.ToArgb();
            _settingsStore.Save(_settings);
        };

        editor.MaskPixelSizeChanged += size =>
        {
            _settings.LastMaskPixelSize = size;
            _settingsStore.Save(_settings);
        };

        editor.MaskModeChanged += mode =>
        {
            _settings.MaskMode = mode;
            _settingsStore.Save(_settings);
        };

        editor.Saved += filePath =>
        {
            _lastSavedFilePath = filePath;

            var savedDirectory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(savedDirectory) && Directory.Exists(savedDirectory) && !string.Equals(_settings.SaveFolder, savedDirectory, StringComparison.OrdinalIgnoreCase))
            {
                _settings.SaveFolder = savedDirectory;
                _settingsStore.Save(_settings);
            }

            if (_settings.ShowNotificationAfterCapture)
            {
                _notifyIcon.ShowBalloonTip(3000, AppLocalization.Translate("Tray.ScreenshotSaved"), AppLocalization.Format("Tray.SavedTo", filePath), ToolTipIcon.Info);
            }
        };

        editor.Copied += () =>
        {
            if (_settings.ShowNotificationAfterCapture)
            {
                _notifyIcon.ShowBalloonTip(3000, AppLocalization.Translate("Tray.ScreenshotCopied"), AppLocalization.Translate("Tray.ClipboardOnly"), ToolTipIcon.Info);
            }
        };

        switch (_settings.EditorStartState)
        {
            case EditorWindowStartState.Maximized:
                editor.WindowState = FormWindowState.Maximized;
                break;
            case EditorWindowStartState.Minimized:
                editor.WindowState = FormWindowState.Minimized;
                break;
            default:
                break;
        }

        _openEditors.Add(editor);
        editor.FormClosed += (_, _) =>
        {
            _openEditors.Remove(editor);
            SaveLastEditorTool(editor);
            if (captureAgain)
            {
                CaptureScreenshot();
            }
        };

        if (showWithoutActivation)
        {
            editor.TopMost = topMost;
            editor.Show();
            return;
        }

        editor.TopMost = topMost;
        using (editor)
        {
            editor.ShowDialog();
        }

    }

    private void SaveLastEditorTool(EditorForm editor)
    {
        if (_settings.RememberLastEditorTool)
        {
            _settings.LastEditorTool = editor.SelectedTool;
            _settingsStore.Save(_settings);
        }
    }

    private void HandleBalloonTipClicked()
    {
        if (_settings.NotificationClickAction == NotificationClickAction.None)
        {
            return;
        }

        try
        {
            if (_settings.NotificationClickAction == NotificationClickAction.OpenEditor)
            {
                if (_lastCapturedBitmap is not null)
                {
                    OpenEditor(_lastCapturedBitmap);
                }
            }
            else if (_lastSavedFilePath is null)
            {
                return;
            }
            else if (_settings.NotificationClickAction == NotificationClickAction.OpenFolder)
            {
                // Abre o Explorer na pasta com o arquivo do print já selecionado.
                using var process = Process.Start("explorer.exe", $"/select,\"{_lastSavedFilePath}\"");
            }
            else
            {
                using var process = Process.Start(new ProcessStartInfo(_lastSavedFilePath) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            if (Debugger.IsAttached)
            {
                throw;
            }

            _notifyIcon.ShowBalloonTip(4000, AppLocalization.Translate("Tray.OpenError"), ex.Message, ToolTipIcon.Error);
        }
    }

    private void ShowAbout()
    {
        if (_aboutForm is { IsDisposed: false })
        {
            _aboutForm.Activate();
            return;
        }

        var aboutForm = new AboutForm();
        _aboutForm = aboutForm;
        try
        {
            aboutForm.ShowDialog();
        }
        finally
        {
            _aboutForm = null;
            aboutForm.Dispose();
        }
    }

    private void OpenSettings()
    {
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.Activate();
            return;
        }

        _settingsForm = new SettingsForm(_settings);
        _settingsForm.SettingsSaved += settings =>
        {
            _settings = settings;
            if (!_settings.ShowNotificationAfterCapture || _settings.NotificationClickAction != NotificationClickAction.OpenEditor)
            {
                _lastCapturedBitmap?.Dispose();
                _lastCapturedBitmap = null;
            }

            AppLocalization.ApplyCulture(_settings.Language);
            _settingsStore.Save(_settings);
            ApplyLocalizedTexts();
            foreach (var editor in _openEditors)
            {
                editor.SetDocumentSessionState(_documentSession is not null, _settings.CloseEditorAfterDocumentSend);
            }
            RegisterHotkeys();
        };
        _settingsForm.FormClosed += (_, _) => _settingsForm = null;
        _settingsForm.Show();
        _settingsForm.Activate();
    }

    private void ExitApplication()
    {
        if (_documentSession is not null && !EndSession())
        {
            return;
        }

        _notifyIcon.Visible = false;
        _lastCapturedBitmap?.Dispose();
        _hotkeyManager.Dispose();
        _settingsForm?.Close();
        ImageEditingService.ClearCounterMarkerFontCache();
        _contextMenu.Dispose();
        _captureNowIcon.Dispose();
        _notifyIcon.Dispose();
        System.Windows.Forms.Application.Exit();
    }

    private static Image LoadMenuIcon(string fileName)
    {
        var resourceName = $"Aprint.Resources.Icons.{fileName}";
        using var stream = typeof(TrayApplicationContext).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Recurso incorporado '{resourceName}' não encontrado.");
        using var resourceImage = Image.FromStream(stream);
        return new Bitmap(resourceImage, new Size(16, 16));
    }

    private void ApplyLocalizedTexts()
    {
        _captureNowMenuItem.Text = AppLocalization.Translate("Tray.CaptureNow");
        _toggleCaptureModeMenuItem.Text = AppLocalization.Translate(
            _settings.CaptureMode == CaptureMode.SelectArea ? "Tray.CaptureFullScreen" : "Tray.SelectArea");
        _settingsMenuItem.Text = AppLocalization.Translate("Tray.Settings");
        _aboutMenuItem.Text = AppLocalization.Translate("Tray.About");
        _exitMenuItem.Text = AppLocalization.Translate("Tray.Exit");
        _sessionMenuItem.Text = AppLocalization.Translate(_documentSession is null ? "Tray.StartSession" : "Tray.EndSession");
        _notifyIcon.Text = AppLocalization.Translate(_documentSession is null ? "Tray.IconText" : "Tray.SessionIconText");
    }
}
