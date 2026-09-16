using Aprint.Features.Capture;
using Aprint.Features.Editor;

namespace Aprint.Features.Settings;

/// <summary>Configurações persistidas do aplicativo (formato JSON em %AppData%).</summary>
public sealed class AppSettings
{
    public string SaveFolder { get; set; } = GetDefaultSaveFolder();

    public bool ShowNotificationAfterCapture { get; set; } = true;

    public CaptureAction CaptureAction { get; set; } = CaptureAction.SaveToDiskOnly;

    public NotificationClickAction NotificationClickAction { get; set; } = NotificationClickAction.None;

    public CaptureMode CaptureMode { get; set; } = CaptureMode.AllScreens;

    public bool OpenEditorAfterCapture { get; set; }

    public bool PlaySoundOnCapture { get; set; }

    public BorrarMode BorrarMode { get; set; } = BorrarMode.Pixelate;

    public bool ShowToolLabels { get; set; } = true;

    public bool CloseEditorOnCopyOrSave { get; set; }

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
