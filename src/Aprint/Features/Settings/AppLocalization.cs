using System.Globalization;
using System.Resources;

namespace Aprint.Features.Settings;

public static class AppLocalization
{
    public const string SystemLanguageCode = "system";
    public const string PortugueseBrazilCode = "pt-BR";
    public const string EnglishUsCode = "en-US";

    private static readonly CultureInfo PortugueseBrazil = CultureInfo.GetCultureInfo(PortugueseBrazilCode);
    private static readonly CultureInfo EnglishUs = CultureInfo.GetCultureInfo(EnglishUsCode);
    private static readonly ResourceManager ResourceManager = new("Aprint.Resources.Localization", typeof(AppLocalization).Assembly);

    public static void ApplyCulture(string? languageCode)
    {
        var culture = ResolveCulture(languageCode);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    public static CultureInfo ResolveCulture(string? languageCode)
    {
        if (string.Equals(languageCode, PortugueseBrazilCode, StringComparison.OrdinalIgnoreCase))
        {
            return PortugueseBrazil;
        }

        if (string.Equals(languageCode, EnglishUsCode, StringComparison.OrdinalIgnoreCase))
        {
            return EnglishUs;
        }

        return ResolveSystemCulture();
    }

    public static string Translate(string key) =>
        ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public static string Format(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, Translate(key), args);

    public static IReadOnlyList<(string Code, string DisplayName)> GetLanguageOptions() =>
    [
        (SystemLanguageCode, Translate("Settings.LanguageSystem")),
        (PortugueseBrazilCode, Translate("Settings.LanguagePortugueseBrazil")),
        (EnglishUsCode, Translate("Settings.LanguageEnglishUs")),
    ];

    private static CultureInfo ResolveSystemCulture()
    {
        var systemCulture = CultureInfo.InstalledUICulture;

        if (string.Equals(systemCulture.Name, PortugueseBrazilCode, StringComparison.OrdinalIgnoreCase))
        {
            return PortugueseBrazil;
        }

        if (systemCulture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            return EnglishUs;
        }

        return EnglishUs;
    }
}
