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
        tabCapture = new TabPage();
        tabHotkey = new TabPage();
        tabEditor = new TabPage();
        tabGeneral = new TabPage();
        chkCtrl = new CheckBox();
        chkShift = new CheckBox();
        chkAlt = new CheckBox();
        chkWin = new CheckBox();
        chkForcePrintScreen = new CheckBox();
        cmbKey = new ComboBox();
        lblCaptureMode = new Label();
        cmbCaptureMode = new ComboBox();
        lblOutsideCaptureClickAction = new Label();
        cmbOutsideCaptureClickAction = new ComboBox();
        lblCaptureAction = new Label();
        cmbCaptureAction = new ComboBox();
        lblSavePath = new Label();
        txtSaveFolder = new TextBox();
        btnBrowse = new Button();
        cmbNotificationClick = new ComboBox();
        lblNotificationClick = new Label();
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
        chkShowNotification = new CheckBox();
        chkPlaySound = new CheckBox();
        chkStartWithWindows = new CheckBox();
        lblLanguage = new Label();
        cmbLanguage = new ComboBox();
        btnSave = new Button();
        btnCancel = new Button();
        tabSettings.SuspendLayout();
        tabHotkey.SuspendLayout();
        tabCapture.SuspendLayout();
        tabEditor.SuspendLayout();
        tabGeneral.SuspendLayout();
        SuspendLayout();
        //
        // tabSettings
        //
        tabSettings.Controls.AddRange([tabGeneral, tabHotkey, tabCapture, tabEditor]);
        tabSettings.Location = new Point(12, 12);
        tabSettings.Name = "tabSettings";
        tabSettings.SelectedIndex = 0;
        tabSettings.Size = new Size(436, 258);
        tabSettings.TabIndex = 0;
        //
        // tabHotkey
        //
        tabHotkey.Controls.AddRange([chkCtrl, chkShift, chkAlt, chkWin, chkForcePrintScreen, cmbKey]);
        tabHotkey.Location = new Point(4, 24);
        tabHotkey.Name = "tabHotkey";
        tabHotkey.Padding = new Padding(3);
        tabHotkey.Size = new Size(428, 230);
        tabHotkey.TabIndex = 1;
        tabHotkey.Text = "Atalho";
        tabHotkey.UseVisualStyleBackColor = true;
        // 
        // chkCtrl
        // 
        chkCtrl.AutoSize = true;
        chkCtrl.Location = new Point(15, 51);
        chkCtrl.Name = "chkCtrl";
        chkCtrl.Size = new Size(45, 19);
        chkCtrl.TabIndex = 1;
        chkCtrl.Text = "Ctrl";
        // 
        // chkShift
        // 
        chkShift.AutoSize = true;
        chkShift.Location = new Point(75, 51);
        chkShift.Name = "chkShift";
        chkShift.Size = new Size(50, 19);
        chkShift.TabIndex = 2;
        chkShift.Text = "Shift";
        // 
        // chkAlt
        // 
        chkAlt.AutoSize = true;
        chkAlt.Location = new Point(140, 51);
        chkAlt.Name = "chkAlt";
        chkAlt.Size = new Size(41, 19);
        chkAlt.TabIndex = 3;
        chkAlt.Text = "Alt";
        // 
        // chkWin
        // 
        chkWin.AutoSize = true;
        chkWin.Location = new Point(195, 51);
        chkWin.Name = "chkWin";
        chkWin.Size = new Size(47, 19);
        chkWin.TabIndex = 4;
        chkWin.Text = "Win";
        // 
        // chkForcePrintScreen
        // 
        chkForcePrintScreen.AutoSize = true;
        chkForcePrintScreen.Location = new Point(15, 24);
        chkForcePrintScreen.Name = "chkForcePrintScreen";
        chkForcePrintScreen.Size = new Size(260, 19);
        chkForcePrintScreen.TabIndex = 0;
        chkForcePrintScreen.Text = "Usar tecla padrão do Windows (Print Screen)";
        chkForcePrintScreen.CheckedChanged += chkForcePrintScreen_CheckedChanged;
        // 
        // cmbKey
        // 
        cmbKey.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbKey.Location = new Point(250, 49);
        cmbKey.Name = "cmbKey";
        cmbKey.Size = new Size(163, 23);
        cmbKey.TabIndex = 5;
        //
        // tabCapture
        //
        tabCapture.Controls.AddRange([
            lblCaptureMode,
            cmbCaptureMode,
            lblCaptureAction,
            cmbCaptureAction,
            lblOutsideCaptureClickAction,
            cmbOutsideCaptureClickAction,
            lblSavePath,
            txtSaveFolder,
            btnBrowse,
        ]);
        tabCapture.Location = new Point(4, 24);
        tabCapture.Name = "tabCapture";
        tabCapture.Padding = new Padding(3);
        tabCapture.Size = new Size(428, 230);
        tabCapture.TabIndex = 2;
        tabCapture.Text = "Captura";
        tabCapture.UseVisualStyleBackColor = true;
        // 
        // lblCaptureMode
        // 
        lblCaptureMode.AutoSize = true;
        lblCaptureMode.Location = new Point(15, 23);
        lblCaptureMode.Name = "lblCaptureMode";
        lblCaptureMode.Size = new Size(101, 15);
        lblCaptureMode.TabIndex = 6;
        lblCaptureMode.Text = "Modo de captura:";
        // 
        // cmbCaptureMode
        // 
        cmbCaptureMode.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbCaptureMode.Location = new Point(15, 42);
        cmbCaptureMode.Name = "cmbCaptureMode";
        cmbCaptureMode.Size = new Size(398, 23);
        cmbCaptureMode.TabIndex = 7;
        // 
        // lblCaptureAction
        // 
        lblCaptureAction.AutoSize = true;
        lblCaptureAction.Location = new Point(15, 73);
        lblCaptureAction.Name = "lblCaptureAction";
        lblCaptureAction.Size = new Size(115, 15);
        lblCaptureAction.TabIndex = 8;
        lblCaptureAction.Text = "Após capturar a tela:";
        // 
        // cmbCaptureAction
        // 
        cmbCaptureAction.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbCaptureAction.Location = new Point(15, 92);
        cmbCaptureAction.Name = "cmbCaptureAction";
        cmbCaptureAction.Size = new Size(398, 23);
        cmbCaptureAction.TabIndex = 9;
        cmbCaptureAction.SelectedIndexChanged += cmbCaptureAction_SelectedIndexChanged;
        // 
        // lblSavePath
        // 
        lblSavePath.AutoSize = true;
        lblSavePath.Location = new Point(15, 123);
        lblSavePath.Name = "lblSavePath";
        lblSavePath.Size = new Size(136, 15);
        lblSavePath.TabIndex = 8;
        lblSavePath.Text = "Salvar arquivos na pasta:";
        // 
        // txtSaveFolder
        // 
        txtSaveFolder.Location = new Point(15, 142);
        txtSaveFolder.Name = "txtSaveFolder";
        txtSaveFolder.Size = new Size(317, 23);
        txtSaveFolder.TabIndex = 9;
        // 
        // btnBrowse
        // 
        btnBrowse.Location = new Point(340, 142);
        btnBrowse.Name = "btnBrowse";
        btnBrowse.Size = new Size(73, 25);
        btnBrowse.TabIndex = 10;
        btnBrowse.Text = "&Procurar...";
        btnBrowse.Click += btnBrowse_Click;
        // 
        // lblOutsideCaptureClickAction
        // 
        lblOutsideCaptureClickAction.Location = new Point(15, 173);
        lblOutsideCaptureClickAction.Name = "lblOutsideCaptureClickAction";
        lblOutsideCaptureClickAction.Size = new Size(178, 19);
        lblOutsideCaptureClickAction.TabIndex = 11;
        lblOutsideCaptureClickAction.Text = "Ao clicar fora da área de captura:";
        lblOutsideCaptureClickAction.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // cmbOutsideCaptureClickAction
        // 
        cmbOutsideCaptureClickAction.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbOutsideCaptureClickAction.Location = new Point(201, 169);
        cmbOutsideCaptureClickAction.Name = "cmbOutsideCaptureClickAction";
        cmbOutsideCaptureClickAction.Size = new Size(212, 23);
        cmbOutsideCaptureClickAction.TabIndex = 12;
        // 
        // cmbNotificationClick
        // 
        cmbNotificationClick.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbNotificationClick.Location = new Point(205, 97);
        cmbNotificationClick.Name = "cmbNotificationClick";
        cmbNotificationClick.Size = new Size(208, 23);
        cmbNotificationClick.TabIndex = 15;
        // 
        // lblNotificationClick
        // 
        lblNotificationClick.AutoSize = true;
        lblNotificationClick.Location = new Point(15, 100);
        lblNotificationClick.Name = "lblNotificationClick";
        lblNotificationClick.Size = new Size(134, 15);
        lblNotificationClick.TabIndex = 14;
        lblNotificationClick.Text = "Ao clicar na notificação:";
        //
        // tabEditor
        //
        tabEditor.Controls.AddRange([
            chkOpenEditor,
            chkCloseEditorOnCopyOrSave,
            chkSaveEditorImageWithoutPrompt,
            chkAutoCloseEditorWhenInactive,
            cmbEditorInactivityTimeout,
            cmbEditorStartState,
            chkRememberLastTool,
            chkShowToolLabels,
            lblDefaultEditorTool,
            cmbDefaultEditorTool,
        ]);
        tabEditor.Location = new Point(4, 24);
        tabEditor.Name = "tabEditor";
        tabEditor.Padding = new Padding(3);
        tabEditor.Size = new Size(428, 230);
        tabEditor.TabIndex = 3;
        tabEditor.Text = "Editor";
        tabEditor.UseVisualStyleBackColor = true;
        // 
        // chkOpenEditor
        // 
        chkOpenEditor.AutoSize = true;
        chkOpenEditor.Location = new Point(15, 22);
        chkOpenEditor.Name = "chkOpenEditor";
        chkOpenEditor.Size = new Size(166, 19);
        chkOpenEditor.TabIndex = 12;
        chkOpenEditor.Text = "Abrir editor após a captura";
        chkOpenEditor.CheckedChanged += chkOpenEditor_CheckedChanged;
        // 
        // chkCloseEditorOnCopyOrSave
        // 
        chkCloseEditorOnCopyOrSave.AutoSize = true;
        chkCloseEditorOnCopyOrSave.Location = new Point(15, 47);
        chkCloseEditorOnCopyOrSave.Name = "chkCloseEditorOnCopyOrSave";
        chkCloseEditorOnCopyOrSave.Size = new Size(248, 19);
        chkCloseEditorOnCopyOrSave.TabIndex = 13;
        chkCloseEditorOnCopyOrSave.Text = "Fechar o editor ao copiar/salvar a imagem";
        // 
        // cmbEditorStartState
        // 
        cmbEditorStartState.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbEditorStartState.Location = new Point(205, 20);
        cmbEditorStartState.Name = "cmbEditorStartState";
        cmbEditorStartState.Size = new Size(208, 23);
        cmbEditorStartState.TabIndex = 16;
        // 
        // chkRememberLastTool
        // 
        chkRememberLastTool.AutoSize = true;
        chkRememberLastTool.Location = new Point(15, 151);
        chkRememberLastTool.Name = "chkRememberLastTool";
        chkRememberLastTool.Size = new Size(219, 19);
        chkRememberLastTool.TabIndex = 14;
        chkRememberLastTool.Text = "Lembrar última ferramenta do editor";
        chkRememberLastTool.CheckedChanged += chkRememberLastTool_CheckedChanged;
        // 
        // chkShowToolLabels
        // 
        chkShowToolLabels.AutoSize = true;
        chkShowToolLabels.Location = new Point(15, 72);
        chkShowToolLabels.Name = "chkShowToolLabels";
        chkShowToolLabels.Size = new Size(339, 19);
        chkShowToolLabels.TabIndex = 15;
        chkShowToolLabels.Text = "Exibir o rótulo dos botões na barra de ferramentas do editor";
        // 
        // chkSaveEditorImageWithoutPrompt
        // 
        chkSaveEditorImageWithoutPrompt.AutoSize = true;
        chkSaveEditorImageWithoutPrompt.Location = new Point(15, 98);
        chkSaveEditorImageWithoutPrompt.Name = "chkSaveEditorImageWithoutPrompt";
        chkSaveEditorImageWithoutPrompt.Size = new Size(152, 19);
        chkSaveEditorImageWithoutPrompt.TabIndex = 16;
        chkSaveEditorImageWithoutPrompt.Text = "Salvar sem perguntar";
        // 
        // chkAutoCloseEditorWhenInactive
        // 
        chkAutoCloseEditorWhenInactive.AutoSize = true;
        chkAutoCloseEditorWhenInactive.Location = new Point(15, 123);
        chkAutoCloseEditorWhenInactive.Name = "chkAutoCloseEditorWhenInactive";
        chkAutoCloseEditorWhenInactive.Size = new Size(208, 19);
        chkAutoCloseEditorWhenInactive.TabIndex = 17;
        chkAutoCloseEditorWhenInactive.Text = "Fechar automaticamente se inativo";
        chkAutoCloseEditorWhenInactive.CheckedChanged += chkAutoCloseEditorWhenInactive_CheckedChanged;
        // 
        // cmbEditorInactivityTimeout
        // 
        cmbEditorInactivityTimeout.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbEditorInactivityTimeout.Location = new Point(250, 121);
        cmbEditorInactivityTimeout.Name = "cmbEditorInactivityTimeout";
        cmbEditorInactivityTimeout.Size = new Size(163, 23);
        cmbEditorInactivityTimeout.TabIndex = 18;
        // 
        // lblDefaultEditorTool
        // 
        lblDefaultEditorTool.AutoSize = true;
        lblDefaultEditorTool.Location = new Point(15, 179);
        lblDefaultEditorTool.Name = "lblDefaultEditorTool";
        lblDefaultEditorTool.Size = new Size(161, 15);
        lblDefaultEditorTool.TabIndex = 19;
        lblDefaultEditorTool.Text = "Ferramenta padrão do editor:";
        // 
        // cmbDefaultEditorTool
        // 
        cmbDefaultEditorTool.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbDefaultEditorTool.Location = new Point(205, 176);
        cmbDefaultEditorTool.Name = "cmbDefaultEditorTool";
        cmbDefaultEditorTool.Size = new Size(208, 23);
        cmbDefaultEditorTool.TabIndex = 20;
        //
        // tabGeneral
        //
        tabGeneral.Controls.AddRange([
            chkShowNotification,
            lblNotificationClick,
            cmbNotificationClick,
            chkPlaySound,
            chkStartWithWindows,
            lblLanguage,
            cmbLanguage,
        ]);
        tabGeneral.Location = new Point(4, 24);
        tabGeneral.Name = "tabGeneral";
        tabGeneral.Padding = new Padding(3);
        tabGeneral.Size = new Size(428, 230);
        tabGeneral.TabIndex = 0;
        tabGeneral.Text = "Geral";
        tabGeneral.UseVisualStyleBackColor = true;
        // 
        // chkShowNotification
        // 
        chkShowNotification.AutoSize = true;
        chkShowNotification.Location = new Point(15, 74);
        chkShowNotification.Name = "chkShowNotification";
        chkShowNotification.Size = new Size(206, 19);
        chkShowNotification.TabIndex = 17;
        chkShowNotification.Text = "Exibir notificação após tirar o print";
        chkShowNotification.CheckedChanged += chkShowNotification_CheckedChanged;
        // 
        // chkPlaySound
        // 
        chkPlaySound.AutoSize = true;
        chkPlaySound.Location = new Point(15, 48);
        chkPlaySound.Name = "chkPlaySound";
        chkPlaySound.Size = new Size(144, 19);
        chkPlaySound.TabIndex = 15;
        chkPlaySound.Text = "Tocar som ao capturar";
        // 
        // chkStartWithWindows
        // 
        chkStartWithWindows.AutoSize = true;
        chkStartWithWindows.Location = new Point(15, 22);
        chkStartWithWindows.Name = "chkStartWithWindows";
        chkStartWithWindows.Size = new Size(147, 19);
        chkStartWithWindows.TabIndex = 13;
        chkStartWithWindows.Text = "Iniciar com o Windows";
        // 
        // lblLanguage
        // 
        lblLanguage.AutoSize = true;
        lblLanguage.Location = new Point(15, 135);
        lblLanguage.Name = "lblLanguage";
        lblLanguage.Size = new Size(47, 15);
        lblLanguage.TabIndex = 21;
        lblLanguage.Text = "Idioma:";
        // 
        // cmbLanguage
        // 
        cmbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbLanguage.FormattingEnabled = true;
        cmbLanguage.Location = new Point(107, 131);
        cmbLanguage.Name = "cmbLanguage";
        cmbLanguage.Size = new Size(306, 23);
        cmbLanguage.TabIndex = 22;
        // 
        // btnSave
        // 
        btnSave.DialogResult = DialogResult.OK;
        btnSave.Location = new Point(280, 282);
        btnSave.Name = "btnSave";
        btnSave.Size = new Size(80, 28);
        btnSave.TabIndex = 23;
        btnSave.Text = "&Salvar";
        btnSave.Click += btnSave_Click;
        // 
        // btnCancel
        // 
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(368, 282);
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
        ClientSize = new Size(460, 320);
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
        tabHotkey.ResumeLayout(false);
        tabHotkey.PerformLayout();
        tabCapture.ResumeLayout(false);
        tabCapture.PerformLayout();
        tabEditor.ResumeLayout(false);
        tabEditor.PerformLayout();
        tabGeneral.ResumeLayout(false);
        tabGeneral.PerformLayout();
        ResumeLayout(false);
    }
}
