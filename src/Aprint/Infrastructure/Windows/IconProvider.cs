namespace Aprint.Infrastructure.Windows;

/// <summary>Carrega o ícone do aplicativo a partir do .ico incorporado, para uso em formulários e na bandeja do sistema.</summary>
public static class IconProvider
{
    private const string ResourceName = "Aprint.Resources.AppIcon.ico";

    public static Icon LoadAppIcon()
    {
        using var stream = typeof(IconProvider).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Recurso incorporado '{ResourceName}' não encontrado.");
        return new Icon(stream);
    }
}
