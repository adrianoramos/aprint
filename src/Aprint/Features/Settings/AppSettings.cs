using System.Drawing;
using System.Text.Json.Serialization;
using Aprint.Features.Capture;
using Aprint.Features.Editor;

namespace Aprint.Features.Settings;

public enum EditorWindowStartState
{
    Default = 0,
    Maximized = 1,
    Minimized = 2,
}

public sealed class HotkeySettings
{
    public Keys Key { get; set; } = Keys.S;

    public bool Ctrl { get; set; }

    public bool Shift { get; set; }

    public bool Alt { get; set; }

    public bool Win { get; set; }

    public HotkeySettings Clone() => new()
    {
        Key = Key,
        Ctrl = Ctrl,
        Shift = Shift,
        Alt = Alt,
        Win = Win,
    };
}

public sealed class AppSettings
{
    public string SaveFolder { get; set; } = GetDefaultSaveFolder();

    public bool ShowNotificationAfterCapture { get; set; } = true;

    public CaptureAction CaptureAction { get; set; } = CaptureAction.ClipboardOnly;

    public ScreenshotFileFormat DefaultScreenshotFormat { get; set; } = ScreenshotFileFormat.Png;

    public NotificationClickAction NotificationClickAction { get; set; } = NotificationClickAction.OpenEditor;

    public CaptureMode CaptureMode { get; set; } = CaptureMode.SelectArea;

    public OutsideCaptureClickAction OutsideCaptureClickAction { get; set; } = OutsideCaptureClickAction.SelectNewArea;

    public bool OpenEditorAfterCapture { get; set; }

    public bool PlaySoundOnCapture { get; set; }

    public EditorWindowStartState EditorStartState { get; set; } = EditorWindowStartState.Default;

    [JsonPropertyName("BorrarMode")]
    public MaskMode MaskMode { get; set; } = MaskMode.Pixelate;

    public EditTool LastCaptureTool { get; set; } = EditTool.Freehand;

    public bool ShowToolLabels { get; set; } = false;

    public bool SaveEditorImageWithoutPrompt { get; set; }

    public bool CloseEditorOnCopyOrSave { get; set; }

    public string DocumentTemplatePath { get; set; } = string.Empty;

    public string DocumentOutputFolder { get; set; } = GetDefaultDocumentOutputFolder();

    public bool SaveDocumentWithoutPrompt { get; set; }

    public bool CloseEditorAfterDocumentSend { get; set; } = true;

    public bool CloseAllEditorsOnSessionEnd { get; set; }

    public HotkeySettings StartSessionHotkey { get; set; } = new() { Key = Keys.S, Ctrl = true, Alt = true };

    public HotkeySettings EndSessionHotkey { get; set; } = new() { Key = Keys.E, Ctrl = true, Alt = true };

    public bool AutoCloseEditorWhenInactive { get; set; }

    public int EditorInactivityTimeoutSeconds { get; set; } = 30;

    public bool RememberLastEditorTool { get; set; }

    public EditTool LastEditorTool { get; set; } = EditTool.Freehand;

    public EditTool DefaultEditorTool { get; set; } = EditTool.Freehand;

    public int LastCounterSize { get; set; } = 40;

    public int LastCounterTextColorArgb { get; set; } = Color.Red.ToArgb();

    public int LastCounterCircleColorArgb { get; set; } = Color.Red.ToArgb();

    public string LastTextFontFamily { get; set; } = "Segoe UI";

    public float LastTextFontSize { get; set; } = 12F;

    public FontStyle LastTextFontStyle { get; set; } = FontStyle.Regular;

    public int LastHighlightWidth { get; set; } = 18;

    public int LastHighlightColorArgb { get; set; } = Color.Yellow.ToArgb();

    public int LastLineColorArgb { get; set; } = Color.Red.ToArgb();

    public int LastLineWidth { get; set; } = ImageEditingService.AnnotationPenWidth;

    public int LastMaskColorArgb { get; set; } = Color.Red.ToArgb();

    public int LastTextColorArgb { get; set; } = Color.Red.ToArgb();

    public int? LastFillColorArgb { get; set; }

    [JsonPropertyName("LastBorrarPixelSize")]
    public int LastMaskPixelSize { get; set; } = 6;

    public Keys HotkeyKey { get; set; } = Keys.P;

    public bool HotkeyCtrl { get; set; } = true;

    public bool HotkeyShift { get; set; } = true;

    public bool HotkeyAlt { get; set; }

    public bool HotkeyWin { get; set; }

    public bool ForcePrintScreen { get; set; }

    public string Language { get; set; } = AppLocalization.SystemLanguageCode;

    private static string GetDefaultSaveFolder() =>
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

    internal static string GetDefaultDocumentOutputFolder()
    {
        var documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return string.IsNullOrWhiteSpace(documentsFolder) ? GetDefaultSaveFolder() : documentsFolder;
    }

}
