using Aprint.Features.Capture;
using Aprint.Features.Editor;
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
            OutsideCaptureClickAction = currentSettings.OutsideCaptureClickAction,
            OpenEditorAfterCapture = currentSettings.OpenEditorAfterCapture,
            PlaySoundOnCapture = currentSettings.PlaySoundOnCapture,
            EditorStartState = currentSettings.EditorStartState,
            MaskMode = currentSettings.MaskMode,
            LastCaptureTool = currentSettings.LastCaptureTool,
            ShowToolLabels = currentSettings.ShowToolLabels,
            SaveEditorImageWithoutPrompt = currentSettings.SaveEditorImageWithoutPrompt,
            CloseEditorOnCopyOrSave = currentSettings.CloseEditorOnCopyOrSave,
            AutoCloseEditorWhenInactive = currentSettings.AutoCloseEditorWhenInactive,
            EditorInactivityTimeoutSeconds = currentSettings.EditorInactivityTimeoutSeconds,
            RememberLastEditorTool = currentSettings.RememberLastEditorTool,
            LastEditorTool = currentSettings.LastEditorTool,
            DefaultEditorTool = currentSettings.DefaultEditorTool,
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

    private void chkRememberLastTool_CheckedChanged(object? sender, EventArgs e)
    {
        cmbDefaultEditorTool.Enabled = !chkRememberLastTool.Checked;
    }

    private void UpdateHotkeyControlsEnabled(bool forcePrintScreen)
    {
        chkCtrl.Enabled = !forcePrintScreen;
        chkShift.Enabled = !forcePrintScreen;
        chkAlt.Enabled = !forcePrintScreen;
        chkWin.Enabled = !forcePrintScreen;
        cmbKey.Enabled = !forcePrintScreen;
    }

    private void UpdateEditorStartStateEnabled()
    {
        cmbEditorStartState.Enabled = chkOpenEditor.Checked;
    }

    private void UpdateNotificationComboEnabled()
    {
        cmbNotificationClick.Enabled = chkShowNotification.Checked;
    }

    private void LoadValuesIntoControls()
    {
        txtSaveFolder.Text = _workingSettings.SaveFolder;
        chkShowNotification.Checked = _workingSettings.ShowNotificationAfterCapture;
        chkOpenEditor.Checked = _workingSettings.OpenEditorAfterCapture;
        chkStartWithWindows.Checked = StartupManager.IsEnabled();
        chkPlaySound.Checked = _workingSettings.PlaySoundOnCapture;
        chkShowToolLabels.Checked = _workingSettings.ShowToolLabels;
        chkSaveEditorImageWithoutPrompt.Checked = _workingSettings.SaveEditorImageWithoutPrompt;
        chkCloseEditorOnCopyOrSave.Checked = _workingSettings.CloseEditorOnCopyOrSave;
        chkAutoCloseEditorWhenInactive.Checked = _workingSettings.AutoCloseEditorWhenInactive;
        cmbEditorInactivityTimeout.Items.Clear();
        cmbEditorInactivityTimeout.Items.AddRange(new object[]
        {
            AppLocalization.Translate("Settings.Inactivity30Seconds"),
            AppLocalization.Translate("Settings.Inactivity1Minute"),
            AppLocalization.Translate("Settings.Inactivity3Minutes"),
            AppLocalization.Translate("Settings.Inactivity5Minutes"),
        });
        cmbEditorInactivityTimeout.SelectedIndex = _workingSettings.EditorInactivityTimeoutSeconds switch
        {
            60 => 1,
            180 => 2,
            300 => 3,
            _ => 0,
        };
        cmbEditorInactivityTimeout.Enabled = chkAutoCloseEditorWhenInactive.Checked;
        
        cmbEditorStartState.Items.Clear();
        cmbEditorStartState.Items.AddRange(new object[]
        {
            AppLocalization.Translate("Settings.EditorStartDefault"),
            AppLocalization.Translate("Settings.EditorStartMaximized"),
            AppLocalization.Translate("Settings.EditorStartMinimized"),
        });

        cmbEditorStartState.SelectedIndex = (int)_workingSettings.EditorStartState;
        UpdateEditorStartStateEnabled();
        chkRememberLastTool.Checked = _workingSettings.RememberLastEditorTool;

        cmbDefaultEditorTool.Items.Clear();
        cmbDefaultEditorTool.Items.AddRange(new object[]
        {
            AppLocalization.Translate("Editor.Freehand"),
            AppLocalization.Translate("Editor.Line"),
            AppLocalization.Translate("Editor.Arrow"),
            AppLocalization.Translate("Editor.Rectangle"),
            AppLocalization.Translate("Editor.Circle"),
            AppLocalization.Translate("Editor.Counter"),
            AppLocalization.Translate("Editor.Filled"),
            AppLocalization.Translate("Editor.Highlight"),
            AppLocalization.Translate("Editor.Text"),
            AppLocalization.Translate("Editor.Crop"),
            AppLocalization.Translate("Editor.Pixelated"),
        });

        cmbDefaultEditorTool.SelectedIndex = (int)_workingSettings.DefaultEditorTool;
        cmbDefaultEditorTool.Enabled = !_workingSettings.RememberLastEditorTool;

        cmbCaptureMode.Items.Clear();
        cmbCaptureMode.Items.AddRange(new object[]
        {
            AppLocalization.Translate("Settings.CaptureAllScreens"),
            AppLocalization.Translate("Settings.CaptureCursorScreen"),
            AppLocalization.Translate("Settings.CaptureSelectArea"),
        });
        cmbCaptureMode.SelectedIndex = (int)_workingSettings.CaptureMode;

        cmbCaptureAction.Items.Clear();
        cmbCaptureAction.Items.AddRange(new object[]
        {
            AppLocalization.Translate("Settings.ClipboardOnly"),
            AppLocalization.Translate("Settings.SaveToDiskOnly"),
            AppLocalization.Translate("Settings.CopyAndSave"),
        });
        cmbCaptureAction.SelectedIndex = (int)_workingSettings.CaptureAction;

        cmbOutsideCaptureClickAction.Items.Clear();
        cmbOutsideCaptureClickAction.Items.AddRange(new object[]
        {
            AppLocalization.Translate("Settings.OutsideClickSelectNewArea"),
            AppLocalization.Translate("Settings.OutsideClickExit"),
            AppLocalization.Translate("Settings.OutsideClickNone"),
        });
        var outsideClickActionIndex = (int)_workingSettings.OutsideCaptureClickAction;
        cmbOutsideCaptureClickAction.SelectedIndex = Enum.IsDefined(_workingSettings.OutsideCaptureClickAction)
            ? outsideClickActionIndex
            : (int)OutsideCaptureClickAction.SelectNewArea;

        cmbNotificationClick.Items.Clear();
        cmbNotificationClick.Items.AddRange(new object[]
        {
            AppLocalization.Translate("Settings.ClickNone"),
            AppLocalization.Translate("Settings.ClickOpenFolder"),
            AppLocalization.Translate("Settings.ClickOpenFile"),
            AppLocalization.Translate("Settings.ClickOpenEditor"),
        });
        cmbNotificationClick.SelectedIndex = (int)_workingSettings.NotificationClickAction;
        UpdateNotificationComboEnabled();
        UpdateSaveFolderUi();

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
        grpCaptureMode.Text = AppLocalization.Translate("Settings.CaptureGroup");
        lblCaptureMode.Text = AppLocalization.Translate("Settings.CaptureModeLabel");
        lblCaptureAction.Text = AppLocalization.Translate("Settings.ActionGroup");
        lblOutsideCaptureClickAction.Text = AppLocalization.Translate("Settings.OutsideCaptureClickActionLabel");
        lblSavePath.Text = AppLocalization.Translate("Settings.SavePathLabel");
        btnBrowse.Text = AppLocalization.Translate("Settings.Browse");
        lblNotificationClick.Text = AppLocalization.Translate("Settings.NotificationClickLabel");
        grpEditor.Text = AppLocalization.Translate("Settings.EditorGroup");
        grpGeneral.Text = AppLocalization.Translate("Settings.GeneralGroup");
        chkOpenEditor.Text = AppLocalization.Translate("Settings.OpenEditorAfterCapture");
        chkShowToolLabels.Text = AppLocalization.Translate("Settings.ShowToolLabels");
        chkSaveEditorImageWithoutPrompt.Text = AppLocalization.Translate("Settings.SaveEditorImageWithoutPrompt");
        chkCloseEditorOnCopyOrSave.Text = AppLocalization.Translate("Settings.CloseEditorOnCopyOrSave");
        chkAutoCloseEditorWhenInactive.Text = AppLocalization.Translate("Settings.AutoCloseEditorWhenInactive");
        chkRememberLastTool.Text = AppLocalization.Translate("Settings.RememberLastTool");
        lblDefaultEditorTool.Text = AppLocalization.Translate("Settings.DefaultEditorTool");
        chkShowNotification.Text = AppLocalization.Translate("Settings.ShowNotification");
        chkPlaySound.Text = AppLocalization.Translate("Settings.PlaySound");
        chkStartWithWindows.Text = AppLocalization.Translate("Settings.StartWithWindows");
        lblLanguage.Text = AppLocalization.Translate("Settings.LanguageLabel");
        btnSave.Text = AppLocalization.Translate("Settings.Save");
        btnCancel.Text = AppLocalization.Translate("Settings.Cancel");
    }

    private void cmbCaptureAction_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateSaveFolderUi();
    }

    private void UpdateSaveFolderUi()
    {
        var usesDisk = cmbCaptureAction.SelectedIndex is (int)CaptureAction.SaveToDiskOnly or (int)CaptureAction.Both;
        lblSavePath.Enabled = usesDisk;
        txtSaveFolder.Enabled = usesDisk;
        btnBrowse.Enabled = usesDisk;
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
        var action = (CaptureAction)Math.Max(cmbCaptureAction.SelectedIndex, 0);

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
        _workingSettings.SaveEditorImageWithoutPrompt = chkSaveEditorImageWithoutPrompt.Checked;
        _workingSettings.CloseEditorOnCopyOrSave = chkCloseEditorOnCopyOrSave.Checked;
        _workingSettings.AutoCloseEditorWhenInactive = chkAutoCloseEditorWhenInactive.Checked;
        _workingSettings.EditorInactivityTimeoutSeconds = cmbEditorInactivityTimeout.SelectedIndex switch
        {
            1 => 60,
            2 => 180,
            3 => 300,
            _ => 30,
        };
        _workingSettings.EditorStartState = (EditorWindowStartState)Math.Max(cmbEditorStartState.SelectedIndex, 0);
        _workingSettings.RememberLastEditorTool = chkRememberLastTool.Checked;
        _workingSettings.DefaultEditorTool = (EditTool)(cmbDefaultEditorTool.SelectedIndex >= 0 ? cmbDefaultEditorTool.SelectedIndex : 0);
        _workingSettings.Language = (cmbLanguage.SelectedItem as LanguageItem)?.Code ?? AppLocalization.SystemLanguageCode;
        _workingSettings.CaptureAction = action;
        _workingSettings.NotificationClickAction = (NotificationClickAction)Math.Max(cmbNotificationClick.SelectedIndex, 0);
        _workingSettings.HotkeyCtrl = chkCtrl.Checked;
        _workingSettings.HotkeyShift = chkShift.Checked;
        _workingSettings.HotkeyAlt = chkAlt.Checked;
        _workingSettings.HotkeyWin = chkWin.Checked;
        _workingSettings.HotkeyKey = chkForcePrintScreen.Checked ? Keys.PrintScreen : (Keys)(cmbKey.SelectedItem ?? Keys.P);
        _workingSettings.ForcePrintScreen = chkForcePrintScreen.Checked;
        if (chkForcePrintScreen.Checked)
        {
            _workingSettings.HotkeyCtrl = false;
            _workingSettings.HotkeyShift = false;
            _workingSettings.HotkeyAlt = false;
            _workingSettings.HotkeyWin = false;
        }
        _workingSettings.CaptureMode = (CaptureMode)Math.Max(cmbCaptureMode.SelectedIndex, 0);
        _workingSettings.OutsideCaptureClickAction = (OutsideCaptureClickAction)Math.Max(cmbOutsideCaptureClickAction.SelectedIndex, 0);

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

    private void chkOpenEditor_CheckedChanged(object? sender, EventArgs e)
    {
        UpdateEditorStartStateEnabled();
    }

    private void chkShowNotification_CheckedChanged(object? sender, EventArgs e)
    {
        UpdateNotificationComboEnabled();
    }

    private void chkAutoCloseEditorWhenInactive_CheckedChanged(object? sender, EventArgs e)
    {
        cmbEditorInactivityTimeout.Enabled = chkAutoCloseEditorWhenInactive.Checked;
    }
}
