namespace Aprint.Features.Settings;

partial class SettingsForm
{
    private System.ComponentModel.IContainer components = null!;
    private GroupBox grpHotkey = null!;
    private CheckBox chkCtrl = null!;
    private CheckBox chkShift = null!;
    private CheckBox chkAlt = null!;
    private CheckBox chkWin = null!;
    private CheckBox chkForcePrintScreen = null!;
    private ComboBox cmbKey = null!;
    private GroupBox grpCaptureMode = null!;
    private Label lblCaptureMode = null!;
    private ComboBox cmbCaptureMode = null!;
    private Label lblCaptureAction = null!;
    private ComboBox cmbCaptureAction = null!;
    private ComboBox cmbNotificationClick = null!;
    private Label lblNotificationClick = null!;
    private GroupBox grpEditor = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;
    private GroupBox grpGeneral = null!;
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
        grpHotkey = new GroupBox();
        chkCtrl = new CheckBox();
        chkShift = new CheckBox();
        chkAlt = new CheckBox();
        chkWin = new CheckBox();
        chkForcePrintScreen = new CheckBox();
        cmbKey = new ComboBox();
        grpCaptureMode = new GroupBox();
        lblCaptureMode = new Label();
        cmbCaptureMode = new ComboBox();
        lblCaptureAction = new Label();
        cmbCaptureAction = new ComboBox();
        lblSavePath = new Label();
        txtSaveFolder = new TextBox();
        btnBrowse = new Button();
        cmbNotificationClick = new ComboBox();
        lblNotificationClick = new Label();
        grpEditor = new GroupBox();
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
        grpGeneral = new GroupBox();
        chkShowNotification = new CheckBox();
        chkPlaySound = new CheckBox();
        chkStartWithWindows = new CheckBox();
        lblLanguage = new Label();
        cmbLanguage = new ComboBox();
        btnSave = new Button();
        btnCancel = new Button();
        grpHotkey.SuspendLayout();
        grpCaptureMode.SuspendLayout();
        grpEditor.SuspendLayout();
        grpGeneral.SuspendLayout();
        SuspendLayout();
        // 
        // grpHotkey
        // 
        grpHotkey.Controls.Add(chkCtrl);
        grpHotkey.Controls.Add(chkShift);
        grpHotkey.Controls.Add(chkAlt);
        grpHotkey.Controls.Add(chkWin);
        grpHotkey.Controls.Add(chkForcePrintScreen);
        grpHotkey.Controls.Add(cmbKey);
        grpHotkey.Location = new Point(12, 12);
        grpHotkey.Name = "grpHotkey";
        grpHotkey.Size = new Size(436, 76);
        grpHotkey.TabIndex = 0;
        grpHotkey.TabStop = false;
        grpHotkey.Text = "Atalho para captura";
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
        cmbKey.Location = new Point(258, 49);
        cmbKey.Name = "cmbKey";
        cmbKey.Size = new Size(163, 23);
        cmbKey.TabIndex = 5;
        // 
        // grpCaptureMode
        // 
        grpCaptureMode.Controls.Add(lblCaptureMode);
        grpCaptureMode.Controls.Add(cmbCaptureMode);
        grpCaptureMode.Controls.Add(lblCaptureAction);
        grpCaptureMode.Controls.Add(cmbCaptureAction);
        grpCaptureMode.Controls.Add(lblSavePath);
        grpCaptureMode.Controls.Add(txtSaveFolder);
        grpCaptureMode.Controls.Add(btnBrowse);
        grpCaptureMode.Location = new Point(12, 92);
        grpCaptureMode.Name = "grpCaptureMode";
        grpCaptureMode.Size = new Size(436, 187);
        grpCaptureMode.TabIndex = 1;
        grpCaptureMode.TabStop = false;
        grpCaptureMode.Text = "Captura";
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
        cmbCaptureMode.Size = new Size(406, 23);
        cmbCaptureMode.TabIndex = 7;
        // 
        // lblCaptureAction
        // 
        lblCaptureAction.AutoSize = true;
        lblCaptureAction.Location = new Point(15, 77);
        lblCaptureAction.Name = "lblCaptureAction";
        lblCaptureAction.Size = new Size(115, 15);
        lblCaptureAction.TabIndex = 8;
        lblCaptureAction.Text = "Após capturar a tela:";
        // 
        // cmbCaptureAction
        // 
        cmbCaptureAction.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbCaptureAction.Location = new Point(15, 96);
        cmbCaptureAction.Name = "cmbCaptureAction";
        cmbCaptureAction.Size = new Size(406, 23);
        cmbCaptureAction.TabIndex = 9;
        cmbCaptureAction.SelectedIndexChanged += cmbCaptureAction_SelectedIndexChanged;
        // 
        // lblSavePath
        // 
        lblSavePath.AutoSize = true;
        lblSavePath.Location = new Point(15, 131);
        lblSavePath.Name = "lblSavePath";
        lblSavePath.Size = new Size(136, 15);
        lblSavePath.TabIndex = 8;
        lblSavePath.Text = "Salvar arquivos na pasta:";
        // 
        // txtSaveFolder
        // 
        txtSaveFolder.Location = new Point(15, 150);
        txtSaveFolder.Name = "txtSaveFolder";
        txtSaveFolder.Size = new Size(325, 23);
        txtSaveFolder.TabIndex = 9;
        // 
        // btnBrowse
        // 
        btnBrowse.Location = new Point(348, 150);
        btnBrowse.Name = "btnBrowse";
        btnBrowse.Size = new Size(73, 25);
        btnBrowse.TabIndex = 10;
        btnBrowse.Text = "&Procurar...";
        btnBrowse.Click += btnBrowse_Click;
        // 
        // cmbNotificationClick
        // 
        cmbNotificationClick.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbNotificationClick.Location = new Point(215, 97);
        cmbNotificationClick.Name = "cmbNotificationClick";
        cmbNotificationClick.Size = new Size(206, 23);
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
        // grpEditor
        // 
        grpEditor.Controls.Add(chkOpenEditor);
        grpEditor.Controls.Add(chkCloseEditorOnCopyOrSave);
        grpEditor.Controls.Add(chkSaveEditorImageWithoutPrompt);
        grpEditor.Controls.Add(chkAutoCloseEditorWhenInactive);
        grpEditor.Controls.Add(cmbEditorInactivityTimeout);
        grpEditor.Controls.Add(cmbEditorStartState);
        grpEditor.Controls.Add(chkRememberLastTool);
        grpEditor.Controls.Add(chkShowToolLabels);
        grpEditor.Controls.Add(lblDefaultEditorTool);
        grpEditor.Controls.Add(cmbDefaultEditorTool);
        grpEditor.Location = new Point(12, 283);
        grpEditor.Name = "grpEditor";
        grpEditor.Size = new Size(436, 215);
        grpEditor.TabIndex = 2;
        grpEditor.TabStop = false;
        grpEditor.Text = "Editor";
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
        cmbEditorStartState.Location = new Point(215, 20);
        cmbEditorStartState.Name = "cmbEditorStartState";
        cmbEditorStartState.Size = new Size(206, 23);
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
        cmbEditorInactivityTimeout.Location = new Point(258, 121);
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
        cmbDefaultEditorTool.Location = new Point(215, 176);
        cmbDefaultEditorTool.Name = "cmbDefaultEditorTool";
        cmbDefaultEditorTool.Size = new Size(206, 23);
        cmbDefaultEditorTool.TabIndex = 20;
        // 
        // grpGeneral
        // 
        grpGeneral.Controls.Add(chkShowNotification);
        grpGeneral.Controls.Add(lblNotificationClick);
        grpGeneral.Controls.Add(cmbNotificationClick);
        grpGeneral.Controls.Add(chkPlaySound);
        grpGeneral.Controls.Add(chkStartWithWindows);
        grpGeneral.Controls.Add(lblLanguage);
        grpGeneral.Controls.Add(cmbLanguage);
        grpGeneral.Location = new Point(12, 502);
        grpGeneral.Name = "grpGeneral";
        grpGeneral.Size = new Size(436, 167);
        grpGeneral.TabIndex = 3;
        grpGeneral.TabStop = false;
        grpGeneral.Text = "Geral";
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
        cmbLanguage.Size = new Size(314, 23);
        cmbLanguage.TabIndex = 22;
        // 
        // btnSave
        // 
        btnSave.DialogResult = DialogResult.OK;
        btnSave.Location = new Point(282, 675);
        btnSave.Name = "btnSave";
        btnSave.Size = new Size(80, 28);
        btnSave.TabIndex = 23;
        btnSave.Text = "&Salvar";
        btnSave.Click += btnSave_Click;
        // 
        // btnCancel
        // 
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(368, 675);
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
        ClientSize = new Size(460, 714);
        Controls.Add(grpHotkey);
        Controls.Add(grpCaptureMode);
        Controls.Add(grpEditor);
        Controls.Add(grpGeneral);
        Controls.Add(btnSave);
        Controls.Add(btnCancel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "SettingsForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Aprint - Configurações";
        grpHotkey.ResumeLayout(false);
        grpHotkey.PerformLayout();
        grpCaptureMode.ResumeLayout(false);
        grpCaptureMode.PerformLayout();
        grpEditor.ResumeLayout(false);
        grpEditor.PerformLayout();
        grpGeneral.ResumeLayout(false);
        grpGeneral.PerformLayout();
        ResumeLayout(false);
    }
}
