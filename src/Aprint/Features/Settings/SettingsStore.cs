using System.Diagnostics;
using System.Text.Json;

namespace Aprint.Features.Settings;

/// <summary>Persiste as configurações do aplicativo em JSON.</summary>
public sealed class SettingsStore
{
    private readonly string _settingsFilePath;

    public SettingsStore(string? settingsFilePath = null)
    {
        _settingsFilePath = settingsFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Aprint",
            "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings is not null)
                {
                    NormalizeHotkeySettings(settings);
                    NormalizeSessionSettings(settings);
                    return settings;
                }
            }
        }
        catch (Exception ex) when (!Debugger.IsAttached && ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Usa os valores padrão quando o arquivo está corrompido ou inacessível.
        }

        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        NormalizeHotkeySettings(settings);
        NormalizeSessionSettings(settings);
        var directory = Path.GetDirectoryName(_settingsFilePath)!;
        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsFilePath, json);
    }

    private static void NormalizeHotkeySettings(AppSettings settings)
    {
        if (!settings.ForcePrintScreen)
        {
            return;
        }

        settings.HotkeyKey = Keys.PrintScreen;
        settings.HotkeyCtrl = false;
        settings.HotkeyShift = false;
        settings.HotkeyAlt = false;
        settings.HotkeyWin = false;
    }

    private static void NormalizeSessionSettings(AppSettings settings)
    {
        settings.DocumentTemplatePath ??= string.Empty;
        settings.DocumentOutputFolder ??= AppSettings.GetDefaultDocumentOutputFolder();
        settings.StartSessionHotkey ??= new HotkeySettings { Key = Keys.S, Ctrl = true, Alt = true };
        settings.EndSessionHotkey ??= new HotkeySettings { Key = Keys.E, Ctrl = true, Alt = true };
        NormalizeSessionHotkey(settings.StartSessionHotkey, Keys.S);
        NormalizeSessionHotkey(settings.EndSessionHotkey, Keys.E);
    }

    private static void NormalizeSessionHotkey(HotkeySettings hotkey, Keys fallbackKey)
    {
        if (hotkey.Key is < Keys.A or > Keys.Z and not (>= Keys.F1 and <= Keys.F12) and not (>= Keys.D0 and <= Keys.D9))
        {
            hotkey.Key = fallbackKey;
        }

        if (!hotkey.Ctrl && !hotkey.Shift && !hotkey.Alt && !hotkey.Win)
        {
            hotkey.Ctrl = true;
            hotkey.Alt = true;
        }
    }
}
