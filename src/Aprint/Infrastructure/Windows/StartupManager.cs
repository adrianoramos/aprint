using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Windows.ApplicationModel;

namespace Aprint.Infrastructure.Windows;

/// <summary>Registra/remove o Aprint na chave Run do Windows para iniciar junto com o sistema.</summary>
public static class StartupManager
{
    private const string RunRegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Aprint";
    private const string StartupTaskId = "AprintStartup";

    public static async Task<bool> IsEnabledAsync()
    {
        if (HasPackageIdentity())
        {
            var task = await StartupTask.GetAsync(StartupTaskId);
            return IsEnabled(task.State);
        }

        using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKeyPath, writable: false);
        return key?.GetValue(ValueName) is string existingPath
            && string.Equals(existingPath, GetExecutablePath(), StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<bool> SetEnabledAsync(bool enabled)
    {
        if (HasPackageIdentity())
        {
            var task = await StartupTask.GetAsync(StartupTaskId);
            if (IsEnabled(task.State) != enabled)
            {
                if (enabled)
                {
                    return IsEnabled(await task.RequestEnableAsync());
                }

                task.Disable();
            }

            return IsEnabled(task.State) == enabled;
        }

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

        return true;
    }

    private static bool IsEnabled(StartupTaskState state) =>
        state is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;

    private static bool HasPackageIdentity()
    {
        const int errorInsufficientBuffer = 122;
        const int appModelErrorNoPackage = 15700;
        uint length = 0;
        var result = GetCurrentPackageFullName(ref length, IntPtr.Zero);
        return result switch
        {
            errorInsufficientBuffer => true,
            appModelErrorNoPackage => false,
            _ => throw new Win32Exception(result),
        };
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref uint packageFullNameLength, IntPtr packageFullName);

    private static string GetExecutablePath() => Environment.ProcessPath ?? System.Windows.Forms.Application.ExecutablePath;
}
