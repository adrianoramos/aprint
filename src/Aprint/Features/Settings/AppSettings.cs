using System.Drawing;
using Aprint.Features.Capture;
using Aprint.Features.Editor;

namespace Aprint.Features.Settings;

public enum EditorWindowStartState
{
    Default = 0,
    Maximized = 1,
    Minimized = 2,
}

public sealed class AppSettings
{
    public string SaveFolder { get; set; } = GetDefaultSaveFolder();

    public bool ShowNotificationAfterCapture { get; set; } = true;

    public CaptureAction CaptureAction { get; set; } = CaptureAction.SaveToDiskOnly;

    public NotificationClickAction NotificationClickAction { get; set; } = NotificationClickAction.None;

    public CaptureMode CaptureMode { get; set; } = CaptureMode.AllScreens;

    public OutsideCaptureClickAction OutsideCaptureClickAction { get; set; } = OutsideCaptureClickAction.SelectNewArea;

    public bool OpenEditorAfterCapture { get; set; }

    public bool PlaySoundOnCapture { get; set; }

    public EditorWindowStartState EditorStartState { get; set; } = EditorWindowStartState.Default;

    public BorrarMode BorrarMode { get; set; } = BorrarMode.Pixelate;

    public bool ShowToolLabels { get; set; } = true;

    public bool SaveEditorImageWithoutPrompt { get; set; }

    public bool CloseEditorOnCopyOrSave { get; set; }

    public bool AutoCloseEditorWhenInactive { get; set; }

    public int EditorInactivityTimeoutSeconds { get; set; } = 30;

    public bool RememberLastEditorTool { get; set; }

    public EditTool LastEditorTool { get; set; } = EditTool.Freehand;

    public EditTool DefaultEditorTool { get; set; } = EditTool.Freehand;

    public string LastTextFontFamily { get; set; } = "Segoe UI";

    public float LastTextFontSize { get; set; } = 12F;

    public FontStyle LastTextFontStyle { get; set; } = FontStyle.Regular;

    public int LastHighlightWidth { get; set; } = 18;

    public int LastLineColorArgb { get; set; } = Color.Red.ToArgb();

    public int LastTextColorArgb { get; set; } = Color.Red.ToArgb();

    public int? LastFillColorArgb { get; set; }

    public int LastBorrarPixelSize { get; set; } = 6;

    public Keys HotkeyKey { get; set; } = Keys.P;

    public bool HotkeyCtrl { get; set; } = true;

    public bool HotkeyShift { get; set; } = true;

    public bool HotkeyAlt { get; set; }

    public bool HotkeyWin { get; set; }

    public bool ForcePrintScreen { get; set; }

    public string Language { get; set; } = AppLocalization.SystemLanguageCode;

    private static string GetDefaultSaveFolder() =>
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

}
