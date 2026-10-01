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
}
