namespace Aprint.Infrastructure.Windows;

/// <summary>Carrega o ícone do aplicativo a partir do .ico incorporado, para uso em formulários e na bandeja do sistema.</summary>
public static class IconProvider
{
    private const string AppIconResourceName = "Aprint.Resources.AppIcon.ico";
    private const string SessionIconResourceName = "Aprint.Resources.AppIconSession.ico";

    public static Icon LoadAppIcon() => LoadIcon(AppIconResourceName);

    public static Icon LoadSessionIcon() => LoadIcon(SessionIconResourceName);

    private static Icon LoadIcon(string resourceName)
    {
        using var stream = typeof(IconProvider).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Recurso incorporado '{resourceName}' não encontrado.");
        return new Icon(stream);
    }
}
