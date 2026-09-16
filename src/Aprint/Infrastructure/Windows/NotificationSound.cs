using System.Runtime.InteropServices;

namespace Aprint.Infrastructure.Windows;

/// <summary>Reproduz o som padrão de notificação do Windows (o mesmo usado pelos toasts do sistema).</summary>
public static class NotificationSound
{
    private const uint SndAlias = 0x00010000;
    private const uint SndAsync = 0x0001;
    private const uint SndNoDefault = 0x0002;

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern bool PlaySound(string? pszSound, IntPtr hmod, uint fdwSound);

    public static void Play() => PlaySound("Notification.Default", IntPtr.Zero, SndAlias | SndAsync | SndNoDefault);
}
