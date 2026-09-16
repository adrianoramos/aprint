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
    private RadioButton radCaptureAllScreens = null!;
    private RadioButton radCaptureCursorScreen = null!;
    private RadioButton radCaptureSelectArea = null!;
    private GroupBox grpAction = null!;
    private RadioButton radSaveToDiskOnly = null!;
    private RadioButton radClipboardOnly = null!;
    private RadioButton radBoth = null!;
    private GroupBox grpNotificationClick = null!;
    private RadioButton radClickNone = null!;
    private RadioButton radClickOpenFolder = null!;
    private RadioButton radClickOpenFile = null!;
    private RadioButton radClickOpenEditor = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;

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
        radCaptureAllScreens = new RadioButton();
        radCaptureCursorScreen = new RadioButton();
        radCaptureSelectArea = new RadioButton();
        grpAction = new GroupBox();
        lblSavePath = new Label();
        txtSaveFolder = new TextBox();
        btnBrowse = new Button();
        radSaveToDiskOnly = new RadioButton();
        radClipboardOnly = new RadioButton();
        radBoth = new RadioButton();
        grpNotificationClick = new GroupBox();
        radClickNone = new RadioButton();
        radClickOpenFolder = new RadioButton();
        radClickOpenFile = new RadioButton();
        radClickOpenEditor = new RadioButton();
        btnSave = new Button();
        btnCancel = new Button();
        grpGeneral = new GroupBox();
        chkShowNotification = new CheckBox();
        chkOpenEditor = new CheckBox();
        chkStartWithWindows = new CheckBox();
        chkPlaySound = new CheckBox();
        chkShowToolLabels = new CheckBox();
        chkCloseEditorOnCopyOrSave = new CheckBox();
        lblLanguage = new Label();
        cmbLanguage = new ComboBox();
        grpHotkey.SuspendLayout();
        grpCaptureMode.SuspendLayout();
        grpAction.SuspendLayout();
        grpNotificationClick.SuspendLayout();
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
        grpHotkey.Size = new Size(436, 96);
        grpHotkey.TabIndex = 3;
        grpHotkey.TabStop = false;
        grpHotkey.Text = "Atalho para captura";
        // 
        // chkCtrl
        // 
        chkCtrl.AutoSize = true;
        chkCtrl.Location = new Point(15, 60);
        chkCtrl.Name = "chkCtrl";
        chkCtrl.Size = new Size(45, 19);
        chkCtrl.TabIndex = 0;
        chkCtrl.Text = "Ctrl";
        // 
        // chkShift
        // 
        chkShift.AutoSize = true;
        chkShift.Location = new Point(75, 60);
        chkShift.Name = "chkShift";
        chkShift.Size = new Size(50, 19);
        chkShift.TabIndex = 1;
        chkShift.Text = "Shift";
        // 
        // chkAlt
        // 
        chkAlt.AutoSize = true;
        chkAlt.Location = new Point(140, 60);
        chkAlt.Name = "chkAlt";
        chkAlt.Size = new Size(41, 19);
        chkAlt.TabIndex = 2;
        chkAlt.Text = "Alt";
        // 
        // chkWin
        // 
        chkWin.AutoSize = true;
        chkWin.Location = new Point(195, 60);
        chkWin.Name = "chkWin";
        chkWin.Size = new Size(47, 19);
        chkWin.TabIndex = 3;
        chkWin.Text = "Win";
        // 
        // chkForcePrintScreen
        // 
        chkForcePrintScreen.AutoSize = true;
        chkForcePrintScreen.Location = new Point(15, 30);
        chkForcePrintScreen.Name = "chkForcePrintScreen";
        chkForcePrintScreen.Size = new Size(260, 19);
        chkForcePrintScreen.TabIndex = 4;
        chkForcePrintScreen.Text = "Usar tecla padrão do Windows (Print Screen)";
        chkForcePrintScreen.CheckedChanged += chkForcePrintScreen_CheckedChanged;
        // 
        // cmbKey
        // 
        cmbKey.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbKey.Location = new Point(258, 58);
        cmbKey.Name = "cmbKey";
        cmbKey.Size = new Size(163, 23);
        cmbKey.TabIndex = 5;
        // 
        // grpCaptureMode
        // 
        grpCaptureMode.Controls.Add(radCaptureAllScreens);
        grpCaptureMode.Controls.Add(radCaptureCursorScreen);
        grpCaptureMode.Controls.Add(radCaptureSelectArea);
        grpCaptureMode.Location = new Point(12, 114);
        grpCaptureMode.Name = "grpCaptureMode";
        grpCaptureMode.Size = new Size(436, 60);
        grpCaptureMode.TabIndex = 4;
        grpCaptureMode.TabStop = false;
        grpCaptureMode.Text = "Modo de captura";
        // 
        // radCaptureAllScreens
        // 
        radCaptureAllScreens.AutoSize = true;
        radCaptureAllScreens.Location = new Point(15, 25);
        radCaptureAllScreens.Name = "radCaptureAllScreens";
        radCaptureAllScreens.Size = new Size(128, 19);
        radCaptureAllScreens.TabIndex = 0;
        radCaptureAllScreens.Text = "Padrão (tela inteira)";
        // 
        // radCaptureCursorScreen
        // 
        radCaptureCursorScreen.AutoSize = true;
        radCaptureCursorScreen.Location = new Point(157, 25);
        radCaptureCursorScreen.Name = "radCaptureCursorScreen";
        radCaptureCursorScreen.Size = new Size(146, 19);
        radCaptureCursorScreen.TabIndex = 1;
        radCaptureCursorScreen.Text = "Tela onde está o cursor";
        // 
        // radCaptureSelectArea
        // 
        radCaptureSelectArea.AutoSize = true;
        radCaptureSelectArea.Location = new Point(317, 25);
        radCaptureSelectArea.Name = "radCaptureSelectArea";
        radCaptureSelectArea.Size = new Size(104, 19);
        radCaptureSelectArea.TabIndex = 2;
        radCaptureSelectArea.Text = "Selecionar área";
        // 
        // grpAction
        // 
        grpAction.Controls.Add(lblSavePath);
        grpAction.Controls.Add(txtSaveFolder);
        grpAction.Controls.Add(btnBrowse);
        grpAction.Controls.Add(radSaveToDiskOnly);
        grpAction.Controls.Add(radClipboardOnly);
        grpAction.Controls.Add(radBoth);
        grpAction.Location = new Point(12, 180);
        grpAction.Name = "grpAction";
        grpAction.Size = new Size(436, 117);
        grpAction.TabIndex = 5;
        grpAction.TabStop = false;
        grpAction.Text = "Após capturar a tela";
        // 
        // lblSavePath
        // 
        lblSavePath.AutoSize = true;
        lblSavePath.Location = new Point(15, 63);
        lblSavePath.Name = "lblSavePath";
        lblSavePath.Size = new Size(136, 15);
        lblSavePath.TabIndex = 7;
        lblSavePath.Text = "Salvar arquivos na pasta:";
        // 
        // txtSaveFolder
        // 
        txtSaveFolder.Location = new Point(15, 81);
        txtSaveFolder.Name = "txtSaveFolder";
        txtSaveFolder.Size = new Size(325, 23);
        txtSaveFolder.TabIndex = 5;
        // 
        // btnBrowse
        // 
        btnBrowse.Location = new Point(348, 81);
        btnBrowse.Name = "btnBrowse";
        btnBrowse.Size = new Size(73, 25);
        btnBrowse.TabIndex = 6;
        btnBrowse.Text = "&Procurar...";
        // 
        // radSaveToDiskOnly
        // 
        radSaveToDiskOnly.AutoSize = true;
        radSaveToDiskOnly.Location = new Point(238, 27);
        radSaveToDiskOnly.Name = "radSaveToDiskOnly";
        radSaveToDiskOnly.Size = new Size(107, 19);
        radSaveToDiskOnly.TabIndex = 0;
        radSaveToDiskOnly.Text = "Salvar em disco";
        // 
        // radClipboardOnly
        // 
        radClipboardOnly.AutoSize = true;
        radClipboardOnly.Location = new Point(15, 27);
        radClipboardOnly.Name = "radClipboardOnly";
        radClipboardOnly.Size = new Size(207, 19);
        radClipboardOnly.TabIndex = 1;
        radClipboardOnly.Text = "Copiar para a área de transferência";
        // 
        // radBoth
        // 
        radBoth.AutoSize = true;
        radBoth.Location = new Point(358, 27);
        radBoth.Name = "radBoth";
        radBoth.Size = new Size(63, 19);
        radBoth.TabIndex = 2;
        radBoth.Text = "Ambos";
        // 
        // grpNotificationClick
        // 
        grpNotificationClick.Controls.Add(radClickNone);
        grpNotificationClick.Controls.Add(radClickOpenFolder);
        grpNotificationClick.Controls.Add(radClickOpenFile);
        grpNotificationClick.Controls.Add(radClickOpenEditor);
        grpNotificationClick.Location = new Point(12, 303);
        grpNotificationClick.Name = "grpNotificationClick";
        grpNotificationClick.Size = new Size(436, 83);
        grpNotificationClick.TabIndex = 6;
        grpNotificationClick.TabStop = false;
        grpNotificationClick.Text = "Ao clicar no balão de notificação";
        // 
        // radClickNone
        // 
        radClickNone.AutoSize = true;
        radClickNone.Location = new Point(15, 50);
        radClickNone.Name = "radClickNone";
        radClickNone.Size = new Size(106, 19);
        radClickNone.TabIndex = 0;
        radClickNone.Text = "Nenhuma ação";
        // 
        // radClickOpenFolder
        // 
        radClickOpenFolder.AutoSize = true;
        radClickOpenFolder.Location = new Point(15, 27);
        radClickOpenFolder.Name = "radClickOpenFolder";
        radClickOpenFolder.Size = new Size(91, 19);
        radClickOpenFolder.TabIndex = 1;
        radClickOpenFolder.Text = "Abrir a pasta";
        // 
        // radClickOpenFile
        // 
        radClickOpenFile.AutoSize = true;
        radClickOpenFile.Location = new Point(171, 27);
        radClickOpenFile.Name = "radClickOpenFile";
        radClickOpenFile.Size = new Size(104, 19);
        radClickOpenFile.TabIndex = 2;
        radClickOpenFile.Text = "Abrir o arquivo";
        // 
        // radClickOpenEditor
        // 
        radClickOpenEditor.AutoSize = true;
        radClickOpenEditor.Location = new Point(326, 27);
        radClickOpenEditor.Name = "radClickOpenEditor";
        radClickOpenEditor.Size = new Size(95, 19);
        radClickOpenEditor.TabIndex = 3;
        radClickOpenEditor.Text = "Abrir o editor";
        // 
        // btnSave
        // 
        btnSave.DialogResult = DialogResult.OK;
        btnSave.Location = new Point(282, 611);
        btnSave.Name = "btnSave";
        btnSave.Size = new Size(80, 28);
        btnSave.TabIndex = 12;
        btnSave.Text = "&Salvar";
        btnSave.Click += btnSave_Click;
        // 
        // btnCancel
        // 
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(368, 611);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(80, 28);
        btnCancel.TabIndex = 13;
        btnCancel.Text = "&Cancelar";
        btnCancel.Click += btnCancel_Click;
        // 
        // grpGeneral
        // 
        grpGeneral.Controls.Add(chkShowNotification);
        grpGeneral.Controls.Add(chkOpenEditor);
        grpGeneral.Controls.Add(chkStartWithWindows);
        grpGeneral.Controls.Add(chkPlaySound);
        grpGeneral.Controls.Add(chkShowToolLabels);
        grpGeneral.Controls.Add(chkCloseEditorOnCopyOrSave);
        grpGeneral.Controls.Add(lblLanguage);
        grpGeneral.Controls.Add(cmbLanguage);
        grpGeneral.Location = new Point(12, 392);
        grpGeneral.Name = "grpGeneral";
        grpGeneral.Size = new Size(436, 207);
        grpGeneral.TabIndex = 14;
        grpGeneral.TabStop = false;
        grpGeneral.Text = "Geral";
        // 
        // chkShowNotification
        // 
        chkShowNotification.AutoSize = true;
        chkShowNotification.Location = new Point(15, 91);
        chkShowNotification.Name = "chkShowNotification";
        chkShowNotification.Size = new Size(206, 19);
        chkShowNotification.TabIndex = 12;
        chkShowNotification.Text = "Exibir notificação após tirar o print";
        // 
        // chkOpenEditor
        // 
        chkOpenEditor.AutoSize = true;
        chkOpenEditor.Location = new Point(15, 22);
        chkOpenEditor.Name = "chkOpenEditor";
        chkOpenEditor.Size = new Size(166, 19);
        chkOpenEditor.TabIndex = 13;
        chkOpenEditor.Text = "Abrir editor após a captura";
        // 
        // chkStartWithWindows
        // 
        chkStartWithWindows.AutoSize = true;
        chkStartWithWindows.Location = new Point(15, 137);
        chkStartWithWindows.Name = "chkStartWithWindows";
        chkStartWithWindows.Size = new Size(244, 19);
        chkStartWithWindows.TabIndex = 14;
        chkStartWithWindows.Text = "Iniciar automaticamente com o Windows";
        // 
        // chkPlaySound
        // 
        chkPlaySound.AutoSize = true;
        chkPlaySound.Location = new Point(15, 114);
        chkPlaySound.Name = "chkPlaySound";
        chkPlaySound.Size = new Size(291, 19);
        chkPlaySound.TabIndex = 15;
        chkPlaySound.Text = "Tocar som de notificação do Windows ao capturar";
        // 
        // chkShowToolLabels
        // 
        chkShowToolLabels.AutoSize = true;
        chkShowToolLabels.Location = new Point(15, 68);
        chkShowToolLabels.Name = "chkShowToolLabels";
        chkShowToolLabels.Size = new Size(339, 19);
        chkShowToolLabels.TabIndex = 16;
        chkShowToolLabels.Text = "Exibir o rótulo dos botões na barra de ferramentas do editor";
        // 
        // chkCloseEditorOnCopyOrSave
        // 
        chkCloseEditorOnCopyOrSave.AutoSize = true;
        chkCloseEditorOnCopyOrSave.Location = new Point(15, 45);
        chkCloseEditorOnCopyOrSave.Name = "chkCloseEditorOnCopyOrSave";
        chkCloseEditorOnCopyOrSave.Size = new Size(248, 19);
        chkCloseEditorOnCopyOrSave.TabIndex = 17;
        chkCloseEditorOnCopyOrSave.Text = "Fechar o editor ao copiar/salvar a imagem";
        // 
        // lblLanguage
        // 
        lblLanguage.AutoSize = true;
        lblLanguage.Location = new Point(15, 169);
        lblLanguage.Name = "lblLanguage";
        lblLanguage.Size = new Size(47, 15);
        lblLanguage.TabIndex = 17;
        lblLanguage.Text = "Idioma:";
        // 
        // cmbLanguage
        // 
        cmbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbLanguage.FormattingEnabled = true;
        cmbLanguage.Location = new Point(93, 165);
        cmbLanguage.Name = "cmbLanguage";
        cmbLanguage.Size = new Size(328, 23);
        cmbLanguage.TabIndex = 18;
        // 
        // SettingsForm
        // 
        AcceptButton = btnSave;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new Size(460, 651);
        Controls.Add(grpGeneral);
        Controls.Add(grpHotkey);
        Controls.Add(grpCaptureMode);
        Controls.Add(grpAction);
        Controls.Add(grpNotificationClick);
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
        grpAction.ResumeLayout(false);
        grpAction.PerformLayout();
        grpNotificationClick.ResumeLayout(false);
        grpNotificationClick.PerformLayout();
        grpGeneral.ResumeLayout(false);
        grpGeneral.PerformLayout();
        ResumeLayout(false);
    }

    private GroupBox grpGeneral;
    private CheckBox chkShowNotification;
    private CheckBox chkOpenEditor;
    private CheckBox chkStartWithWindows;
    private CheckBox chkPlaySound;
    private CheckBox chkShowToolLabels;
    private CheckBox chkCloseEditorOnCopyOrSave;
    private Label lblLanguage;
    private ComboBox cmbLanguage;
    private Label lblSavePath;
    private TextBox txtSaveFolder;
    private Button btnBrowse;
}
