using Aprint.Features.Capture;
using Aprint.Infrastructure.Windows;

namespace Aprint.Features.Settings;

public sealed partial class SettingsForm : Form
{
    private sealed record LanguageItem(string Code, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    private readonly AppSettings _workingSettings;
    private Keys _previousHotkeyKey;
    private bool _previousHotkeyCtrl;
    private bool _previousHotkeyShift;
    private bool _previousHotkeyAlt;
    private bool _previousHotkeyWin;

    public event Action<AppSettings>? SettingsSaved;

    public SettingsForm(AppSettings currentSettings)
    {
        InitializeComponent();
        Icon = Program.AppIcon;

        // Trabalha em uma cópia para permitir cancelar sem afetar as configurações atuais.
        _workingSettings = new AppSettings
        {
            SaveFolder = currentSettings.SaveFolder,
            ShowNotificationAfterCapture = currentSettings.ShowNotificationAfterCapture,
            CaptureAction = currentSettings.CaptureAction,
            NotificationClickAction = currentSettings.NotificationClickAction,
            CaptureMode = currentSettings.CaptureMode,
            OpenEditorAfterCapture = currentSettings.OpenEditorAfterCapture,
            PlaySoundOnCapture = currentSettings.PlaySoundOnCapture,
            BorrarMode = currentSettings.BorrarMode,
            ShowToolLabels = currentSettings.ShowToolLabels,
            CloseEditorOnCopyOrSave = currentSettings.CloseEditorOnCopyOrSave,
            HotkeyKey = currentSettings.HotkeyKey,
            HotkeyCtrl = currentSettings.HotkeyCtrl,
            HotkeyShift = currentSettings.HotkeyShift,
            HotkeyAlt = currentSettings.HotkeyAlt,
            HotkeyWin = currentSettings.HotkeyWin,
            ForcePrintScreen = currentSettings.ForcePrintScreen,
            Language = currentSettings.Language,
        };

        _previousHotkeyKey = currentSettings.HotkeyKey;
        _previousHotkeyCtrl = currentSettings.HotkeyCtrl;
        _previousHotkeyShift = currentSettings.HotkeyShift;
        _previousHotkeyAlt = currentSettings.HotkeyAlt;
        _previousHotkeyWin = currentSettings.HotkeyWin;

        ApplyLocalizedTexts();
        LoadValuesIntoControls();
    }

    private void ApplyHotkeyUiState(Keys key, bool ctrl, bool shift, bool alt, bool win)
    {
        chkCtrl.Checked = ctrl;
        chkShift.Checked = shift;
        chkAlt.Checked = alt;
        chkWin.Checked = win;

        if (cmbKey.Items.Contains(key))
        {
            cmbKey.SelectedItem = key;
            return;
        }

        if (cmbKey.Items.Count > 0)
        {
            cmbKey.SelectedIndex = 0;
        }
    }

    private void UpdateHotkeyControlsEnabled(bool forcePrintScreen)
    {
        chkCtrl.Enabled = !forcePrintScreen;
        chkShift.Enabled = !forcePrintScreen;
        chkAlt.Enabled = !forcePrintScreen;
        chkWin.Enabled = !forcePrintScreen;
        cmbKey.Enabled = !forcePrintScreen;
    }

    private void LoadValuesIntoControls()
    {
        txtSaveFolder.Text = _workingSettings.SaveFolder;
        chkShowNotification.Checked = _workingSettings.ShowNotificationAfterCapture;
        chkOpenEditor.Checked = _workingSettings.OpenEditorAfterCapture;
        chkStartWithWindows.Checked = StartupManager.IsEnabled();
        chkPlaySound.Checked = _workingSettings.PlaySoundOnCapture;
        chkShowToolLabels.Checked = _workingSettings.ShowToolLabels;
        chkCloseEditorOnCopyOrSave.Checked = _workingSettings.CloseEditorOnCopyOrSave;

        PopulateLanguageOptions();

        cmbKey.Items.Clear();
        foreach (var key in Enum.GetValues<Keys>())
        {
            if (key is >= Keys.A and <= Keys.Z or >= Keys.F1 and <= Keys.F12 or >= Keys.D0 and <= Keys.D9)
            {
                cmbKey.Items.Add(key);
            }
        }

        ApplyHotkeyUiState(_workingSettings.HotkeyKey, _workingSettings.HotkeyCtrl, _workingSettings.HotkeyShift, _workingSettings.HotkeyAlt, _workingSettings.HotkeyWin);
        chkForcePrintScreen.Checked = _workingSettings.ForcePrintScreen;
        UpdateHotkeyControlsEnabled(chkForcePrintScreen.Checked);

        switch (_workingSettings.CaptureAction)
        {
            case CaptureAction.ClipboardOnly:
                radClipboardOnly.Checked = true;
                break;
            case CaptureAction.Both:
                radBoth.Checked = true;
                break;
            default:
                radSaveToDiskOnly.Checked = true;
                break;
        }

        switch (_workingSettings.NotificationClickAction)
        {
            case NotificationClickAction.OpenFolder:
                radClickOpenFolder.Checked = true;
                break;
            case NotificationClickAction.OpenFile:
                radClickOpenFile.Checked = true;
                break;
            case NotificationClickAction.OpenEditor:
                radClickOpenEditor.Checked = true;
                break;
            default:
                radClickNone.Checked = true;
                break;
        }

        radCaptureAllScreens.Checked = _workingSettings.CaptureMode == CaptureMode.AllScreens;
        radCaptureCursorScreen.Checked = _workingSettings.CaptureMode == CaptureMode.ScreenWithCursor;
        radCaptureSelectArea.Checked = _workingSettings.CaptureMode == CaptureMode.SelectArea;

        cmbKey.SelectedItem = _workingSettings.HotkeyKey;
        if (cmbKey.SelectedItem is null && cmbKey.Items.Count > 0)
        {
            cmbKey.SelectedIndex = 0;
        }
    }

    private void PopulateLanguageOptions()
    {
        cmbLanguage.Items.Clear();

        foreach (var option in AppLocalization.GetLanguageOptions())
        {
            cmbLanguage.Items.Add(new LanguageItem(option.Code, option.DisplayName));
        }

        var selectedLanguageCode = string.IsNullOrWhiteSpace(_workingSettings.Language)
            ? AppLocalization.SystemLanguageCode
            : _workingSettings.Language;

        var selectedItem = cmbLanguage.Items
            .OfType<LanguageItem>()
            .FirstOrDefault(item => string.Equals(item.Code, selectedLanguageCode, StringComparison.OrdinalIgnoreCase));

        if (selectedItem is not null)
        {
            cmbLanguage.SelectedItem = selectedItem;
        }
        else if (cmbLanguage.Items.Count > 0)
        {
            cmbLanguage.SelectedIndex = 0;
        }
    }

    private void ApplyLocalizedTexts()
    {
        Text = AppLocalization.Translate("Settings.FormTitle");
        grpHotkey.Text = AppLocalization.Translate("Settings.HotkeyGroup");
        chkForcePrintScreen.Text = AppLocalization.Translate("Settings.ForcePrintScreen");
        grpCaptureMode.Text = AppLocalization.Translate("Settings.CaptureModeGroup");
        radCaptureAllScreens.Text = AppLocalization.Translate("Settings.CaptureAllScreens");
        radCaptureCursorScreen.Text = AppLocalization.Translate("Settings.CaptureCursorScreen");
        radCaptureSelectArea.Text = AppLocalization.Translate("Settings.CaptureSelectArea");
        grpAction.Text = AppLocalization.Translate("Settings.ActionGroup");
        lblSavePath.Text = AppLocalization.Translate("Settings.SavePathLabel");
        btnBrowse.Text = AppLocalization.Translate("Settings.Browse");
        radSaveToDiskOnly.Text = AppLocalization.Translate("Settings.SaveToDiskOnly");
        radClipboardOnly.Text = AppLocalization.Translate("Settings.ClipboardOnly");
        radBoth.Text = AppLocalization.Translate("Settings.Both");
        grpNotificationClick.Text = AppLocalization.Translate("Settings.NotificationClickGroup");
        radClickNone.Text = AppLocalization.Translate("Settings.ClickNone");
        radClickOpenFolder.Text = AppLocalization.Translate("Settings.ClickOpenFolder");
        radClickOpenFile.Text = AppLocalization.Translate("Settings.ClickOpenFile");
        radClickOpenEditor.Text = AppLocalization.Translate("Settings.ClickOpenEditor");
        grpGeneral.Text = AppLocalization.Translate("Settings.GeneralGroup");
        chkOpenEditor.Text = AppLocalization.Translate("Settings.OpenEditorAfterCapture");
        chkShowToolLabels.Text = AppLocalization.Translate("Settings.ShowToolLabels");
        chkCloseEditorOnCopyOrSave.Text = AppLocalization.Translate("Settings.CloseEditorOnCopyOrSave");
        chkShowNotification.Text = AppLocalization.Translate("Settings.ShowNotification");
        chkPlaySound.Text = AppLocalization.Translate("Settings.PlaySound");
        chkStartWithWindows.Text = AppLocalization.Translate("Settings.StartWithWindows");
        lblLanguage.Text = AppLocalization.Translate("Settings.LanguageLabel");
        btnSave.Text = AppLocalization.Translate("Settings.Save");
        btnCancel.Text = AppLocalization.Translate("Settings.Cancel");
    }

    private void btnBrowse_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = AppLocalization.Translate("Settings.SelectSaveFolder"),
            SelectedPath = Directory.Exists(txtSaveFolder.Text) ? txtSaveFolder.Text : string.Empty,
            ShowNewFolderButton = true,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtSaveFolder.Text = dialog.SelectedPath;
        }
    }

    private void btnSave_Click(object? sender, EventArgs e)
    {
        var action = radClipboardOnly.Checked ? CaptureAction.ClipboardOnly
            : radBoth.Checked ? CaptureAction.Both
            : CaptureAction.SaveToDiskOnly;

        if (action is CaptureAction.SaveToDiskOnly or CaptureAction.Both && string.IsNullOrWhiteSpace(txtSaveFolder.Text))
        {
            MessageBox.Show(this, AppLocalization.Translate("Settings.InvalidFolder"), AppLocalization.Translate("Settings.WarningTitle"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!chkForcePrintScreen.Checked && !chkCtrl.Checked && !chkShift.Checked && !chkAlt.Checked && !chkWin.Checked)
        {
            MessageBox.Show(this, AppLocalization.Translate("Settings.SelectModifier"), AppLocalization.Translate("Settings.WarningTitle"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _workingSettings.SaveFolder = txtSaveFolder.Text.Trim();
        _workingSettings.ShowNotificationAfterCapture = chkShowNotification.Checked;
        _workingSettings.OpenEditorAfterCapture = chkOpenEditor.Checked;
        StartupManager.SetEnabled(chkStartWithWindows.Checked);
        _workingSettings.PlaySoundOnCapture = chkPlaySound.Checked;
        _workingSettings.ShowToolLabels = chkShowToolLabels.Checked;
        _workingSettings.CloseEditorOnCopyOrSave = chkCloseEditorOnCopyOrSave.Checked;
        _workingSettings.Language = (cmbLanguage.SelectedItem as LanguageItem)?.Code ?? AppLocalization.SystemLanguageCode;
        _workingSettings.CaptureAction = action;
        _workingSettings.NotificationClickAction = radClickOpenFolder.Checked ? NotificationClickAction.OpenFolder
            : radClickOpenFile.Checked ? NotificationClickAction.OpenFile
            : radClickOpenEditor.Checked ? NotificationClickAction.OpenEditor
            : NotificationClickAction.None;
        _workingSettings.HotkeyCtrl = chkCtrl.Checked;
        _workingSettings.HotkeyShift = chkShift.Checked;
        _workingSettings.HotkeyAlt = chkAlt.Checked;
        _workingSettings.HotkeyWin = chkWin.Checked;
        _workingSettings.HotkeyKey = chkForcePrintScreen.Checked ? _previousHotkeyKey : (Keys)(cmbKey.SelectedItem ?? Keys.P);
        _workingSettings.ForcePrintScreen = chkForcePrintScreen.Checked;
        _workingSettings.CaptureMode = radCaptureCursorScreen.Checked ? CaptureMode.ScreenWithCursor
            : radCaptureSelectArea.Checked ? CaptureMode.SelectArea
            : CaptureMode.AllScreens;

        SettingsSaved?.Invoke(_workingSettings);
        Close();
    }

    private void btnCancel_Click(object? sender, EventArgs e) => Close();

    private void chkForcePrintScreen_CheckedChanged(object sender, EventArgs e)
    {
        var forcePrintScreen = chkForcePrintScreen.Checked;

        if (forcePrintScreen)
        {
            _previousHotkeyKey = (Keys)(cmbKey.SelectedItem ?? _workingSettings.HotkeyKey);
            _previousHotkeyCtrl = chkCtrl.Checked;
            _previousHotkeyShift = chkShift.Checked;
            _previousHotkeyAlt = chkAlt.Checked;
            _previousHotkeyWin = chkWin.Checked;

            cmbKey.SelectedItem = Keys.PrintScreen;
        }
        else
        {
            ApplyHotkeyUiState(_previousHotkeyKey, _previousHotkeyCtrl, _previousHotkeyShift, _previousHotkeyAlt, _previousHotkeyWin);
        }

        UpdateHotkeyControlsEnabled(forcePrintScreen);
    }
}
