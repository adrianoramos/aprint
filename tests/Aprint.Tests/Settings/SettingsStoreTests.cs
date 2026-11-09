using Aprint.Features.Capture;
using Aprint.Features.Editor;
using Aprint.Features.Settings;

namespace Aprint.Tests.Settings;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), "Aprint.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void SaveAndLoadPreserveSettings()
    {
        var filePath = Path.Combine(_testDirectory, "settings.json");
        var store = new SettingsStore(filePath);
        var expected = new AppSettings
        {
            SaveFolder = @"C:\Capturas",
            ShowNotificationAfterCapture = false,
            CaptureAction = CaptureAction.Both,
            DefaultScreenshotFormat = ScreenshotFileFormat.Jpeg,
            NotificationClickAction = NotificationClickAction.OpenEditor,
            CaptureMode = CaptureMode.SelectArea,
            OutsideCaptureClickAction = OutsideCaptureClickAction.ExitCaptureMode,
            OpenEditorAfterCapture = true,
            PlaySoundOnCapture = true,
            MaskMode = MaskMode.Solid,
            LastCaptureTool = EditTool.Circle,
            ShowToolLabels = false,
            SaveEditorImageWithoutPrompt = true,
            DocumentTemplatePath = @"C:\Templates\report.docx",
            DocumentOutputFolder = @"C:\Reports",
            SaveDocumentWithoutPrompt = false,
            CloseEditorAfterDocumentSend = true,
            CloseAllEditorsOnSessionEnd = true,
            StartSessionHotkey = new HotkeySettings { Key = Keys.F7, Ctrl = true, Win = true },
            EndSessionHotkey = new HotkeySettings { Key = Keys.F9, Shift = true, Alt = true },
            AutoCloseEditorWhenInactive = true,
            EditorInactivityTimeoutSeconds = 180,
            LastTextFontFamily = "Arial",
            LastTextFontSize = 18F,
            LastTextFontStyle = FontStyle.Bold | FontStyle.Italic,
            LastHighlightWidth = 32,
            LastHighlightColorArgb = Color.Magenta.ToArgb(),
            LastLineColorArgb = Color.Blue.ToArgb(),
            LastMaskColorArgb = Color.Cyan.ToArgb(),
            LastCounterSize = 64,
            LastCounterTextColorArgb = Color.Green.ToArgb(),
            LastCounterCircleColorArgb = Color.Purple.ToArgb(),
            LastTextColorArgb = Color.Green.ToArgb(),
            LastFillColorArgb = Color.Yellow.ToArgb(),
            LastMaskPixelSize = 24,
            HotkeyKey = Keys.F8,
            HotkeyCtrl = false,
            HotkeyShift = false,
            HotkeyAlt = true,
            HotkeyWin = true,
            ForcePrintScreen = true,
        };

        store.Save(expected);
        var actual = store.Load();

        Assert.Equivalent(expected, actual, strict: true);
    }

    [Fact]
    public void LoadReturnsDefaultsWhenJsonIsInvalid()
    {
        var filePath = Path.Combine(_testDirectory, "settings.json");
        Directory.CreateDirectory(_testDirectory);
        File.WriteAllText(filePath, "{invalid-json");
        var store = new SettingsStore(filePath);

        var settings = store.Load();

        Assert.Equal(CaptureAction.ClipboardOnly, settings.CaptureAction);
        Assert.Equal(NotificationClickAction.OpenEditor, settings.NotificationClickAction);
        Assert.Equal(CaptureMode.SelectArea, settings.CaptureMode);
        Assert.Equal(ScreenshotFileFormat.Png, settings.DefaultScreenshotFormat);
        Assert.False(settings.ShowToolLabels);
        Assert.True(settings.HotkeyCtrl);
        Assert.True(settings.HotkeyShift);
        Assert.Equal(Keys.S, settings.StartSessionHotkey.Key);
        Assert.True(settings.StartSessionHotkey.Ctrl);
        Assert.Equal(Keys.E, settings.EndSessionHotkey.Key);
        Assert.True(settings.EndSessionHotkey.Ctrl);
        Assert.True(settings.EndSessionHotkey.Alt);
        Assert.True(settings.EndSessionHotkey.Ctrl);
    }

    [Fact]
    public void LoadPreservesMaskSettingsFromLegacyJsonNames()
    {
        var filePath = Path.Combine(_testDirectory, "settings.json");
        Directory.CreateDirectory(_testDirectory);
        File.WriteAllText(filePath, "{\"BorrarMode\":0,\"LastBorrarPixelSize\":24}");
        var store = new SettingsStore(filePath);

        var settings = store.Load();

        Assert.Equal(MaskMode.Solid, settings.MaskMode);
        Assert.Equal(24, settings.LastMaskPixelSize);
        Assert.True(settings.CloseEditorAfterDocumentSend);
        Assert.Equal(AppSettings.GetDefaultDocumentOutputFolder(), settings.DocumentOutputFolder);
        Assert.False(settings.SaveDocumentWithoutPrompt);
    }

    [Fact]
    public void ForcePrintScreenNormalizesPersistedHotkey()
    {
        var filePath = Path.Combine(_testDirectory, "settings.json");
        var store = new SettingsStore(filePath);
        var settings = new AppSettings
        {
            ForcePrintScreen = true,
            HotkeyKey = Keys.F8,
            HotkeyCtrl = true,
            HotkeyShift = true,
            HotkeyAlt = true,
            HotkeyWin = true,
        };

        store.Save(settings);
        var loaded = store.Load();

        Assert.True(loaded.ForcePrintScreen);
        Assert.Equal(Keys.PrintScreen, loaded.HotkeyKey);
        Assert.False(loaded.HotkeyCtrl);
        Assert.False(loaded.HotkeyShift);
        Assert.False(loaded.HotkeyAlt);
        Assert.False(loaded.HotkeyWin);
    }

    [Fact]
    public void LoadNormalizesNullOrUnsupportedSessionHotkeys()
    {
        var filePath = Path.Combine(_testDirectory, "settings.json");
        Directory.CreateDirectory(_testDirectory);
        File.WriteAllText(filePath, "{\"StartSessionHotkey\":null,\"EndSessionHotkey\":{\"Key\":\"PrintScreen\"}}");
        var store = new SettingsStore(filePath);

        var settings = store.Load();

        Assert.Equal(Keys.S, settings.StartSessionHotkey.Key);
        Assert.Equal(Keys.E, settings.EndSessionHotkey.Key);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
}
