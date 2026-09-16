using Aprint.Application;
using Aprint.Features.Settings;
using Aprint.Infrastructure.Windows;

namespace Aprint;

internal static class Program
{
    // Carregado uma única vez e compartilhado entre a bandeja e os formulários.
    public static Icon AppIcon { get; } = IconProvider.LoadAppIcon();

    [STAThread]
    private static void Main()
    {
        var settingsStore = new SettingsStore();
        var settings = settingsStore.Load();
        AppLocalization.ApplyCulture(settings.Language);

        // Garante uma única instância do aplicativo em execução por vez.
        using var singleInstanceMutex = new Mutex(true, "Aprint.SingleInstance.Mutex", out var isNewInstance);
        if (!isNewInstance)
        {
            MessageBox.Show(AppLocalization.Translate("App.AlreadyRunning"), AppLocalization.Translate("App.Name"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        System.Windows.Forms.Application.Run(new TrayApplicationContext(settings, settingsStore));
    }
}
