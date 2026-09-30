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
            NotificationClickAction = NotificationClickAction.OpenEditor,
            CaptureMode = CaptureMode.SelectArea,
            OpenEditorAfterCapture = true,
            PlaySoundOnCapture = true,
            BorrarMode = BorrarMode.Solid,
            ShowToolLabels = false,
            SaveEditorImageWithoutPrompt = true,
            AutoCloseEditorWhenInactive = true,
            EditorInactivityTimeoutSeconds = 180,
            LastTextFontFamily = "Arial",
            LastTextFontSize = 18F,
            LastTextFontStyle = FontStyle.Bold | FontStyle.Italic,
            LastHighlightWidth = 32,
            LastLineColorArgb = Color.Blue.ToArgb(),
            LastTextColorArgb = Color.Green.ToArgb(),
            LastFillColorArgb = Color.Yellow.ToArgb(),
            LastBorrarPixelSize = 24,
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

        Assert.Equal(CaptureAction.SaveToDiskOnly, settings.CaptureAction);
        Assert.Equal(CaptureMode.AllScreens, settings.CaptureMode);
        Assert.True(settings.HotkeyCtrl);
        Assert.True(settings.HotkeyShift);
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

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
}
