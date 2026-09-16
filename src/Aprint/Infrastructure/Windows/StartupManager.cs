using Microsoft.Win32;

namespace Aprint.Infrastructure.Windows;

/// <summary>Registra/remove o Aprint na chave Run do Windows para iniciar junto com o sistema.</summary>
public static class StartupManager
{
    private const string RunRegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Aprint";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKeyPath, writable: false);
        return key?.GetValue(ValueName) is string existingPath
            && string.Equals(existingPath, GetExecutablePath(), StringComparison.OrdinalIgnoreCase);
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunRegistryKeyPath)!;

        if (enabled)
        {
            key.SetValue(ValueName, GetExecutablePath());
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    private static string GetExecutablePath() => Environment.ProcessPath ?? System.Windows.Forms.Application.ExecutablePath;
}
