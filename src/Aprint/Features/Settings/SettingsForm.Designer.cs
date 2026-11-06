namespace Aprint.Features.Settings;

partial class SettingsForm
{
    private System.ComponentModel.IContainer components = null!;
    private TabControl tabSettings = null!;
    private TabPage tabHotkey = null!;
    private TabPage tabCapture = null!;
    private TabPage tabEditor = null!;
    private TabPage tabGeneral = null!;
    private CheckBox chkCtrl = null!;
    private CheckBox chkShift = null!;
    private CheckBox chkAlt = null!;
    private CheckBox chkWin = null!;
    private CheckBox chkForcePrintScreen = null!;
    private ComboBox cmbKey = null!;
    private Label lblCaptureMode = null!;
    private ComboBox cmbCaptureMode = null!;
    private Label lblOutsideCaptureClickAction = null!;
    private ComboBox cmbOutsideCaptureClickAction = null!;
    private Label lblCaptureAction = null!;
    private ComboBox cmbCaptureAction = null!;
    private ComboBox cmbNotificationClick = null!;
    private Label lblNotificationClick = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;
    private CheckBox chkShowNotification = null!;
    private CheckBox chkOpenEditor = null!;
    private CheckBox chkRememberLastTool = null!;
    private CheckBox chkStartWithWindows = null!;
    private CheckBox chkPlaySound = null!;
    private CheckBox chkShowToolLabels = null!;
    private CheckBox chkCloseEditorOnCopyOrSave = null!;
    private CheckBox chkSaveEditorImageWithoutPrompt = null!;
    private CheckBox chkAutoCloseEditorWhenInactive = null!;
    private ComboBox cmbEditorInactivityTimeout = null!;
    private ComboBox cmbEditorStartState = null!;
    private ComboBox cmbDefaultEditorTool = null!;
    private Label lblLanguage = null!;
    private ComboBox cmbLanguage = null!;
    private Label lblSavePath = null!;
    private TextBox txtSaveFolder = null!;
    private Button btnBrowse = null!;
    private Label lblDefaultScreenshotFormat = null!;
    private ComboBox cmbDefaultScreenshotFormat = null!;
    private Label lblDefaultEditorTool = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components is not null)
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        tabSettings = new TabControl();
        tabGeneral = new TabPage();
        chkShowNotification = new CheckBox();
        lblNotificationClick = new Label();
        cmbNotificationClick = new ComboBox();
        chkPlaySound = new CheckBox();
        chkStartWithWindows = new CheckBox();
        lblLanguage = new Label();
        cmbLanguage = new ComboBox();
        tabHotkey = new TabPage();
        chkCtrl = new CheckBox();
        chkShift = new CheckBox();
        chkAlt = new CheckBox();
        chkWin = new CheckBox();
        chkForcePrintScreen = new CheckBox();
        cmbKey = new ComboBox();
        tabCapture = new TabPage();
        lblCaptureMode = new Label();
        cmbCaptureMode = new ComboBox();
        lblCaptureAction = new Label();
        cmbCaptureAction = new ComboBox();
        lblOutsideCaptureClickAction = new Label();
        cmbOutsideCaptureClickAction = new ComboBox();
        lblSavePath = new Label();
        txtSaveFolder = new TextBox();
        btnBrowse = new Button();
        lblDefaultScreenshotFormat = new Label();
        cmbDefaultScreenshotFormat = new ComboBox();
        tabEditor = new TabPage();
        chkOpenEditor = new CheckBox();
        chkCloseEditorOnCopyOrSave = new CheckBox();
        chkSaveEditorImageWithoutPrompt = new CheckBox();
        chkAutoCloseEditorWhenInactive = new CheckBox();
        cmbEditorInactivityTimeout = new ComboBox();
        cmbEditorStartState = new ComboBox();
        chkRememberLastTool = new CheckBox();
        chkShowToolLabels = new CheckBox();
        lblDefaultEditorTool = new Label();
        cmbDefaultEditorTool = new ComboBox();
        btnSave = new Button();
        btnCancel = new Button();
        tabSettings.SuspendLayout();
        tabGeneral.SuspendLayout();
        tabHotkey.SuspendLayout();
        tabCapture.SuspendLayout();
        tabEditor.SuspendLayout();
        SuspendLayout();
        // 
        // tabSettings
        // 
        tabSettings.Controls.Add(tabGeneral);
        tabSettings.Controls.Add(tabHotkey);
        tabSettings.Controls.Add(tabCapture);
        tabSettings.Controls.Add(tabEditor);
        tabSettings.Location = new Point(12, 12);
        tabSettings.Name = "tabSettings";
        tabSettings.SelectedIndex = 0;
        tabSettings.Size = new Size(436, 332);
        tabSettings.TabIndex = 0;
        // 
        // tabGeneral
        // 
        tabGeneral.Controls.Add(chkShowNotification);
        tabGeneral.Controls.Add(lblNotificationClick);
        tabGeneral.Controls.Add(cmbNotificationClick);
        tabGeneral.Controls.Add(chkPlaySound);
        tabGeneral.Controls.Add(chkStartWithWindows);
        tabGeneral.Controls.Add(chkShowToolLabels);
        tabGeneral.Controls.Add(lblLanguage);
        tabGeneral.Controls.Add(cmbLanguage);
        tabGeneral.Location = new Point(4, 24);
        tabGeneral.Name = "tabGeneral";
        tabGeneral.Padding = new Padding(3);
        tabGeneral.Size = new Size(428, 304);
        tabGeneral.TabIndex = 0;
        tabGeneral.Text = "Geral";
        tabGeneral.UseVisualStyleBackColor = true;
        // 
        // chkShowNotification
        // 
        chkShowNotification.AutoSize = true;
        chkShowNotification.Location = new Point(15, 102);
        chkShowNotification.Name = "chkShowNotification";
        chkShowNotification.Size = new Size(206, 19);
        chkShowNotification.TabIndex = 3;
        chkShowNotification.Text = "Exibir notificação após tirar o print";
        chkShowNotification.CheckedChanged += chkShowNotification_CheckedChanged;
        // 
        // lblNotificationClick
        // 
        lblNotificationClick.AutoSize = true;
        lblNotificationClick.Location = new Point(15, 134);
        lblNotificationClick.Name = "lblNotificationClick";
        lblNotificationClick.Size = new Size(134, 15);
        lblNotificationClick.TabIndex = 14;
        lblNotificationClick.Text = "Ao clicar na notificação:";
        // 
        // cmbNotificationClick
        // 
        cmbNotificationClick.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbNotificationClick.Location = new Point(205, 130);
        cmbNotificationClick.Name = "cmbNotificationClick";
        cmbNotificationClick.Size = new Size(208, 23);
        cmbNotificationClick.TabIndex = 4;
        // 
        // chkPlaySound
        // 
        chkPlaySound.AutoSize = true;
        chkPlaySound.Location = new Point(15, 46);
        chkPlaySound.Name = "chkPlaySound";
        chkPlaySound.Size = new Size(144, 19);
        chkPlaySound.TabIndex = 1;
        chkPlaySound.Text = "Tocar som ao capturar";
        // 
        // chkStartWithWindows
        // 
        chkStartWithWindows.AutoSize = true;
        chkStartWithWindows.Location = new Point(15, 18);
        chkStartWithWindows.Name = "chkStartWithWindows";
        chkStartWithWindows.Size = new Size(147, 19);
        chkStartWithWindows.TabIndex = 0;
        chkStartWithWindows.Text = "Iniciar com o Windows";
        // 
        // lblLanguage
        // 
        lblLanguage.AutoSize = true;
        lblLanguage.Location = new Point(15, 162);
        lblLanguage.Name = "lblLanguage";
        lblLanguage.Size = new Size(47, 15);
        lblLanguage.TabIndex = 21;
        lblLanguage.Text = "Idioma:";
        // 
        // cmbLanguage
        // 
        cmbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbLanguage.FormattingEnabled = true;
        cmbLanguage.Location = new Point(205, 158);
        cmbLanguage.Name = "cmbLanguage";
        cmbLanguage.Size = new Size(208, 23);
        cmbLanguage.TabIndex = 5;
        // 
        // tabHotkey
        // 
        tabHotkey.Controls.Add(chkCtrl);
        tabHotkey.Controls.Add(chkShift);
        tabHotkey.Controls.Add(chkAlt);
        tabHotkey.Controls.Add(chkWin);
        tabHotkey.Controls.Add(chkForcePrintScreen);
        tabHotkey.Controls.Add(cmbKey);
        tabHotkey.Location = new Point(4, 24);
        tabHotkey.Name = "tabHotkey";
        tabHotkey.Padding = new Padding(3);
        tabHotkey.Size = new Size(428, 304);
        tabHotkey.TabIndex = 1;
        tabHotkey.Text = "Atalho";
        tabHotkey.UseVisualStyleBackColor = true;
        // 
        // chkCtrl
        // 
        chkCtrl.AutoSize = true;
        chkCtrl.Location = new Point(15, 46);
        chkCtrl.Name = "chkCtrl";
        chkCtrl.Size = new Size(45, 19);
        chkCtrl.TabIndex = 1;
        chkCtrl.Text = "Ctrl";
        // 
        // chkShift
        // 
        chkShift.AutoSize = true;
        chkShift.Location = new Point(76, 46);
        chkShift.Name = "chkShift";
        chkShift.Size = new Size(50, 19);
        chkShift.TabIndex = 2;
        chkShift.Text = "Shift";
        // 
        // chkAlt
        // 
        chkAlt.AutoSize = true;
        chkAlt.Location = new Point(142, 46);
        chkAlt.Name = "chkAlt";
        chkAlt.Size = new Size(41, 19);
        chkAlt.TabIndex = 3;
        chkAlt.Text = "Alt";
        // 
        // chkWin
        // 
        chkWin.AutoSize = true;
        chkWin.Location = new Point(199, 46);
        chkWin.Name = "chkWin";
        chkWin.Size = new Size(47, 19);
        chkWin.TabIndex = 4;
        chkWin.Text = "Win";
        // 
        // chkForcePrintScreen
        // 
        chkForcePrintScreen.AutoSize = true;
        chkForcePrintScreen.Location = new Point(15, 18);
        chkForcePrintScreen.Name = "chkForcePrintScreen";
        chkForcePrintScreen.Size = new Size(260, 19);
        chkForcePrintScreen.TabIndex = 0;
        chkForcePrintScreen.Text = "Usar tecla padrão do Windows (Print Screen)";
        chkForcePrintScreen.CheckedChanged += chkForcePrintScreen_CheckedChanged;
        // 
        // cmbKey
        // 
        cmbKey.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbKey.Location = new Point(262, 44);
        cmbKey.Name = "cmbKey";
        cmbKey.Size = new Size(151, 23);
        cmbKey.TabIndex = 5;
        // 
        // tabCapture
        // 
        tabCapture.Controls.Add(lblCaptureMode);
        tabCapture.Controls.Add(cmbCaptureMode);
        tabCapture.Controls.Add(lblCaptureAction);
        tabCapture.Controls.Add(cmbCaptureAction);
        tabCapture.Controls.Add(lblOutsideCaptureClickAction);
        tabCapture.Controls.Add(cmbOutsideCaptureClickAction);
        tabCapture.Controls.Add(lblSavePath);
        tabCapture.Controls.Add(txtSaveFolder);
        tabCapture.Controls.Add(btnBrowse);
        tabCapture.Controls.Add(lblDefaultScreenshotFormat);
        tabCapture.Controls.Add(cmbDefaultScreenshotFormat);
        tabCapture.Location = new Point(4, 24);
        tabCapture.Name = "tabCapture";
        tabCapture.Padding = new Padding(3);
        tabCapture.Size = new Size(428, 304);
        tabCapture.TabIndex = 2;
        tabCapture.Text = "Captura";
        tabCapture.UseVisualStyleBackColor = true;
        // 
        // lblCaptureMode
        // 
        lblCaptureMode.AutoSize = true;
        lblCaptureMode.Location = new Point(15, 16);
        lblCaptureMode.Name = "lblCaptureMode";
        lblCaptureMode.Size = new Size(101, 15);
        lblCaptureMode.TabIndex = 6;
        lblCaptureMode.Text = "Modo de captura:";
        // 
        // cmbCaptureMode
        // 
        cmbCaptureMode.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbCaptureMode.Location = new Point(15, 35);
        cmbCaptureMode.Name = "cmbCaptureMode";
        cmbCaptureMode.Size = new Size(398, 23);
        cmbCaptureMode.TabIndex = 7;
        cmbCaptureMode.SelectedIndexChanged += cmbCaptureMode_SelectedIndexChanged;
        // 
        // lblCaptureAction
        // 
        lblCaptureAction.AutoSize = true;
        lblCaptureAction.Location = new Point(15, 124);
        lblCaptureAction.Name = "lblCaptureAction";
        lblCaptureAction.Size = new Size(115, 15);
        lblCaptureAction.TabIndex = 8;
        lblCaptureAction.Text = "Após capturar a tela:";
        // 
        // cmbCaptureAction
        // 
        cmbCaptureAction.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbCaptureAction.Location = new Point(15, 143);
        cmbCaptureAction.Name = "cmbCaptureAction";
        cmbCaptureAction.Size = new Size(398, 23);
        cmbCaptureAction.TabIndex = 11;
        cmbCaptureAction.SelectedIndexChanged += cmbCaptureAction_SelectedIndexChanged;
        // 
        // lblOutsideCaptureClickAction
        // 
        lblOutsideCaptureClickAction.AutoSize = true;
        lblOutsideCaptureClickAction.Location = new Point(15, 70);
        lblOutsideCaptureClickAction.Name = "lblOutsideCaptureClickAction";
        lblOutsideCaptureClickAction.Size = new Size(178, 15);
        lblOutsideCaptureClickAction.TabIndex = 8;
        lblOutsideCaptureClickAction.Text = "Ao clicar fora da área de captura:";
        lblOutsideCaptureClickAction.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // cmbOutsideCaptureClickAction
        // 
        cmbOutsideCaptureClickAction.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbOutsideCaptureClickAction.Location = new Point(15, 89);
        cmbOutsideCaptureClickAction.Name = "cmbOutsideCaptureClickAction";
        cmbOutsideCaptureClickAction.Size = new Size(398, 23);
        cmbOutsideCaptureClickAction.TabIndex = 9;
        // 
        // lblSavePath
        // 
        lblSavePath.AutoSize = true;
        lblSavePath.Location = new Point(15, 178);
        lblSavePath.Name = "lblSavePath";
        lblSavePath.Size = new Size(136, 15);
        lblSavePath.TabIndex = 12;
        lblSavePath.Text = "Salvar arquivos na pasta:";
        // 
        // txtSaveFolder
        // 
        txtSaveFolder.Location = new Point(15, 197);
        txtSaveFolder.Name = "txtSaveFolder";
        txtSaveFolder.Size = new Size(317, 23);
        txtSaveFolder.TabIndex = 13;
        // 
        // btnBrowse
        // 
        btnBrowse.Location = new Point(340, 196);
        btnBrowse.Name = "btnBrowse";
        btnBrowse.Size = new Size(73, 25);
        btnBrowse.TabIndex = 14;
        btnBrowse.Text = "&Procurar...";
        btnBrowse.Click += btnBrowse_Click;
        //
        // lblDefaultScreenshotFormat
        //
        lblDefaultScreenshotFormat.AutoSize = true;
        lblDefaultScreenshotFormat.Location = new Point(15, 232);
        lblDefaultScreenshotFormat.Name = "lblDefaultScreenshotFormat";
        lblDefaultScreenshotFormat.Size = new Size(152, 15);
        lblDefaultScreenshotFormat.TabIndex = 15;
        lblDefaultScreenshotFormat.Text = "Formato padrão da imagem:";
        //
        // cmbDefaultScreenshotFormat
        //
        cmbDefaultScreenshotFormat.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbDefaultScreenshotFormat.FormattingEnabled = true;
        cmbDefaultScreenshotFormat.Location = new Point(15, 251);
        cmbDefaultScreenshotFormat.Name = "cmbDefaultScreenshotFormat";
        cmbDefaultScreenshotFormat.Size = new Size(398, 23);
        cmbDefaultScreenshotFormat.TabIndex = 16;
        // 
        // tabEditor
        // 
        tabEditor.Controls.Add(chkOpenEditor);
        tabEditor.Controls.Add(chkCloseEditorOnCopyOrSave);
        tabEditor.Controls.Add(chkSaveEditorImageWithoutPrompt);
        tabEditor.Controls.Add(chkAutoCloseEditorWhenInactive);
        tabEditor.Controls.Add(cmbEditorInactivityTimeout);
        tabEditor.Controls.Add(cmbEditorStartState);
        tabEditor.Controls.Add(chkRememberLastTool);
        tabEditor.Controls.Add(lblDefaultEditorTool);
        tabEditor.Controls.Add(cmbDefaultEditorTool);
        tabEditor.Location = new Point(4, 24);
        tabEditor.Name = "tabEditor";
        tabEditor.Padding = new Padding(3);
        tabEditor.Size = new Size(428, 304);
        tabEditor.TabIndex = 3;
        tabEditor.Text = "Editor";
        tabEditor.UseVisualStyleBackColor = true;
        // 
        // chkOpenEditor
        // 
        chkOpenEditor.AutoSize = true;
        chkOpenEditor.Location = new Point(15, 16);
        chkOpenEditor.Name = "chkOpenEditor";
        chkOpenEditor.Size = new Size(166, 19);
        chkOpenEditor.TabIndex = 0;
        chkOpenEditor.Text = "Abrir editor após a captura";
        chkOpenEditor.CheckedChanged += chkOpenEditor_CheckedChanged;
        // 
        // chkCloseEditorOnCopyOrSave
        // 
        chkCloseEditorOnCopyOrSave.AutoSize = true;
        chkCloseEditorOnCopyOrSave.Location = new Point(15, 74);
        chkCloseEditorOnCopyOrSave.Name = "chkCloseEditorOnCopyOrSave";
        chkCloseEditorOnCopyOrSave.Size = new Size(248, 19);
        chkCloseEditorOnCopyOrSave.TabIndex = 2;
        chkCloseEditorOnCopyOrSave.Text = "Fechar o editor ao copiar/salvar a imagem";
        // 
        // chkSaveEditorImageWithoutPrompt
        // 
        chkSaveEditorImageWithoutPrompt.AutoSize = true;
        chkSaveEditorImageWithoutPrompt.Location = new Point(15, 102);
        chkSaveEditorImageWithoutPrompt.Name = "chkSaveEditorImageWithoutPrompt";
        chkSaveEditorImageWithoutPrompt.Size = new Size(137, 19);
        chkSaveEditorImageWithoutPrompt.TabIndex = 3;
        chkSaveEditorImageWithoutPrompt.Text = "Salvar sem perguntar";
        // 
        // chkAutoCloseEditorWhenInactive
        // 
        chkAutoCloseEditorWhenInactive.AutoSize = true;
        chkAutoCloseEditorWhenInactive.Location = new Point(15, 130);
        chkAutoCloseEditorWhenInactive.Name = "chkAutoCloseEditorWhenInactive";
        chkAutoCloseEditorWhenInactive.Size = new Size(211, 19);
        chkAutoCloseEditorWhenInactive.TabIndex = 4;
        chkAutoCloseEditorWhenInactive.Text = "Fechar automaticamente se inativo";
        chkAutoCloseEditorWhenInactive.CheckedChanged += chkAutoCloseEditorWhenInactive_CheckedChanged;
        // 
        // cmbEditorInactivityTimeout
        // 
        cmbEditorInactivityTimeout.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbEditorInactivityTimeout.Location = new Point(15, 153);
        cmbEditorInactivityTimeout.Name = "cmbEditorInactivityTimeout";
        cmbEditorInactivityTimeout.Size = new Size(398, 23);
        cmbEditorInactivityTimeout.TabIndex = 5;
        // 
        // cmbEditorStartState
        // 
        cmbEditorStartState.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbEditorStartState.Location = new Point(15, 39);
        cmbEditorStartState.Name = "cmbEditorStartState";
        cmbEditorStartState.Size = new Size(398, 23);
        cmbEditorStartState.TabIndex = 1;
        // 
        // chkRememberLastTool
        // 
        chkRememberLastTool.AutoSize = true;
        chkRememberLastTool.Location = new Point(15, 188);
        chkRememberLastTool.Name = "chkRememberLastTool";
        chkRememberLastTool.Size = new Size(219, 19);
        chkRememberLastTool.TabIndex = 6;
        chkRememberLastTool.Text = "Lembrar última ferramenta do editor";
        chkRememberLastTool.CheckedChanged += chkRememberLastTool_CheckedChanged;
        // 
        // chkShowToolLabels
        // 
        chkShowToolLabels.AutoSize = true;
        chkShowToolLabels.Location = new Point(15, 74);
        chkShowToolLabels.Name = "chkShowToolLabels";
        chkShowToolLabels.Size = new Size(339, 19);
        chkShowToolLabels.TabIndex = 2;
        chkShowToolLabels.Text = "Exibir rótulos nos botões";
        // 
        // lblDefaultEditorTool
        // 
        lblDefaultEditorTool.AutoSize = true;
        lblDefaultEditorTool.Location = new Point(15, 219);
        lblDefaultEditorTool.Name = "lblDefaultEditorTool";
        lblDefaultEditorTool.Size = new Size(161, 15);
        lblDefaultEditorTool.TabIndex = 19;
        lblDefaultEditorTool.Text = "Ferramenta padrão do editor:";
        // 
        // cmbDefaultEditorTool
        // 
        cmbDefaultEditorTool.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbDefaultEditorTool.Location = new Point(15, 238);
        cmbDefaultEditorTool.Name = "cmbDefaultEditorTool";
        cmbDefaultEditorTool.Size = new Size(398, 23);
        cmbDefaultEditorTool.TabIndex = 7;
        // 
        // btnSave
        // 
        btnSave.DialogResult = DialogResult.OK;
        btnSave.Location = new Point(280, 356);
        btnSave.Name = "btnSave";
        btnSave.Size = new Size(80, 28);
        btnSave.TabIndex = 23;
        btnSave.Text = "&Salvar";
        btnSave.Click += btnSave_Click;
        // 
        // btnCancel
        // 
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(368, 356);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(80, 28);
        btnCancel.TabIndex = 24;
        btnCancel.Text = "&Cancelar";
        btnCancel.Click += btnCancel_Click;
        // 
        // SettingsForm
        // 
        AcceptButton = btnSave;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new Size(460, 396);
        Controls.Add(tabSettings);
        Controls.Add(btnSave);
        Controls.Add(btnCancel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "SettingsForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Aprint - Configurações";
        tabSettings.ResumeLayout(false);
        tabGeneral.ResumeLayout(false);
        tabGeneral.PerformLayout();
        tabHotkey.ResumeLayout(false);
        tabHotkey.PerformLayout();
        tabCapture.ResumeLayout(false);
        tabCapture.PerformLayout();
        tabEditor.ResumeLayout(false);
        tabEditor.PerformLayout();
        ResumeLayout(false);
    }
}
