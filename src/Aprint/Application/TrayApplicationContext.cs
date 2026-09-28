using System.Diagnostics;
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
    private readonly ToolStripMenuItem _captureNowMenuItem;
    private readonly ToolStripMenuItem _settingsMenuItem;
    private readonly ToolStripMenuItem _aboutMenuItem;
    private readonly ToolStripMenuItem _exitMenuItem;
    private AppSettings _settings;
    private SettingsForm? _settingsForm;
    private string? _lastSavedFilePath;
    private Bitmap? _lastCapturedBitmap;

    public TrayApplicationContext(AppSettings settings, SettingsStore settingsStore)
    {
        _settings = settings;
        _settingsStore = settingsStore;
        AppLocalization.ApplyCulture(_settings.Language);

        _captureNowMenuItem = new ToolStripMenuItem();
        _captureNowMenuItem.Click += (_, _) => CaptureScreenshot();
        _settingsMenuItem = new ToolStripMenuItem();
        _settingsMenuItem.Click += (_, _) => OpenSettings();
        _aboutMenuItem = new ToolStripMenuItem();
        _aboutMenuItem.Click += (_, _) => ShowAbout();
        _exitMenuItem = new ToolStripMenuItem();
        _exitMenuItem.Click += (_, _) => ExitApplication();

        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.Add(_captureNowMenuItem);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(_settingsMenuItem);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(_aboutMenuItem);
        _contextMenu.Items.Add(_exitMenuItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = Program.AppIcon,
            Visible = true,
            ContextMenuStrip = _contextMenu,
        };
        _notifyIcon.DoubleClick += (_, _) => CaptureScreenshot();
        _notifyIcon.BalloonTipClicked += (_, _) => HandleBalloonTipClicked();

        ApplyLocalizedTexts();

        _hotkeyManager.HotkeyPressed += CaptureScreenshot;
        RegisterHotkey();
    }

    private void RegisterHotkey()
    {
        var key = _settings.ForcePrintScreen ? Keys.PrintScreen : _settings.HotkeyKey;
        var ctrl = _settings.ForcePrintScreen ? false : _settings.HotkeyCtrl;
        var shift = _settings.ForcePrintScreen ? false : _settings.HotkeyShift;
        var alt = _settings.ForcePrintScreen ? false : _settings.HotkeyAlt;
        var win = _settings.ForcePrintScreen ? false : _settings.HotkeyWin;

        var ok = _hotkeyManager.Register(key, ctrl, shift, alt, win);
        if (!ok)
        {
            _notifyIcon.ShowBalloonTip(3000, AppLocalization.Translate("App.Name"),
                AppLocalization.Translate("Tray.HotkeyRegisterError"),
                ToolTipIcon.Warning);
        }
    }

    private void CaptureScreenshot()
    {
        try
        {
            using var result = _captureWorkflow.Execute(_settings);
            if (result is null)
            {
                return;
            }

            _lastCapturedBitmap?.Dispose();
            _lastCapturedBitmap = (Bitmap)result.Bitmap.Clone();

            if (_settings.OpenEditorAfterCapture)
            {
                if (_settings.ShowNotificationAfterCapture)
                {
                    _notifyIcon.ShowBalloonTip(3000, AppLocalization.Translate("Tray.ScreenshotCaptured"), AppLocalization.Translate("Tray.EditorOpened"), ToolTipIcon.Info);
                }

                OpenEditor(result.Bitmap);
                return;
            }

            _lastSavedFilePath = result.SavedFilePath;

            if (_settings.ShowNotificationAfterCapture)
            {
                var message = _settings.CaptureAction switch
                {
                    CaptureAction.ClipboardOnly => AppLocalization.Translate("Tray.ClipboardOnly"),
                    CaptureAction.SaveToDiskOnly => AppLocalization.Format("Tray.SavedTo", result.SavedFilePath ?? string.Empty),
                    CaptureAction.Both => AppLocalization.Format("Tray.BothSavedAndClipboard", result.SavedFilePath ?? string.Empty),
                    _ => AppLocalization.Translate("Tray.CapturedFallback"),
                };
                _notifyIcon.ShowBalloonTip(3000, AppLocalization.Translate("Tray.ScreenshotCaptured"), message, ToolTipIcon.Info);
            }
        }
        catch (Exception ex)
        {
            _notifyIcon.ShowBalloonTip(4000, AppLocalization.Translate("Tray.CaptureError"), ex.Message, ToolTipIcon.Error);
        }
    }

    private void OpenEditor(Bitmap bitmap)
    {
        var initialTool = _settings.RememberLastEditorTool ? _settings.LastEditorTool : _settings.DefaultEditorTool;
        using var editor = new EditorForm(bitmap, _settings.SaveFolder, _settings.BorrarMode, _settings.ShowToolLabels, _settings.CloseEditorOnCopyOrSave, initialTool, _settings.SaveEditorImageWithoutPrompt);

        editor.BorrarModeChanged += mode =>
        {
            _settings.BorrarMode = mode;
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

        editor.ShowDialog();

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
            _notifyIcon.ShowBalloonTip(4000, AppLocalization.Translate("Tray.OpenError"), ex.Message, ToolTipIcon.Error);
        }
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            AppLocalization.Translate("Tray.AboutText"),
            AppLocalization.Translate("Tray.AboutTitle"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
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
            AppLocalization.ApplyCulture(_settings.Language);
            _settingsStore.Save(_settings);
            ApplyLocalizedTexts();
            RegisterHotkey();
        };
        _settingsForm.FormClosed += (_, _) => _settingsForm = null;
        _settingsForm.Show();
        _settingsForm.Activate();
    }

    private void ExitApplication()
    {
        _notifyIcon.Visible = false;
        _lastCapturedBitmap?.Dispose();
        _hotkeyManager.Dispose();
        _settingsForm?.Close();
        _contextMenu.Dispose();
        _notifyIcon.Dispose();
        System.Windows.Forms.Application.Exit();
    }

    private void ApplyLocalizedTexts()
    {
        _captureNowMenuItem.Text = AppLocalization.Translate("Tray.CaptureNow");
        _settingsMenuItem.Text = AppLocalization.Translate("Tray.Settings");
        _aboutMenuItem.Text = AppLocalization.Translate("Tray.About");
        _exitMenuItem.Text = AppLocalization.Translate("Tray.Exit");
        _notifyIcon.Text = AppLocalization.Translate("Tray.IconText");
    }
}
