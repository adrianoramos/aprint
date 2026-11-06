using System.Globalization;
using System.Resources;

namespace Aprint.Features.Settings;

public static class AppLocalization
{
    public const string SystemLanguageCode = "system";
    public const string PortugueseBrazilCode = "pt-BR";
    public const string EnglishUsCode = "en-US";
    public const string SpanishCode = "es";

    private static readonly CultureInfo PortugueseBrazil = CultureInfo.GetCultureInfo(PortugueseBrazilCode);
    private static readonly CultureInfo EnglishUs = CultureInfo.GetCultureInfo(EnglishUsCode);
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo(SpanishCode);
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
        => ResolveCulture(languageCode, CultureInfo.InstalledUICulture);

    internal static CultureInfo ResolveCulture(string? languageCode, CultureInfo systemCulture)
    {
        if (string.Equals(languageCode, PortugueseBrazilCode, StringComparison.OrdinalIgnoreCase))
        {
            return PortugueseBrazil;
        }

        if (string.Equals(languageCode, EnglishUsCode, StringComparison.OrdinalIgnoreCase))
        {
            return EnglishUs;
        }

        if (string.Equals(languageCode, SpanishCode, StringComparison.OrdinalIgnoreCase))
        {
            return Spanish;
        }

        return ResolveSystemCulture(systemCulture);
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
        (SpanishCode, Translate("Settings.LanguageSpanish")),
    ];

    private static CultureInfo ResolveSystemCulture(CultureInfo systemCulture)
    {
        if (string.Equals(systemCulture.Name, PortugueseBrazilCode, StringComparison.OrdinalIgnoreCase))
        {
            return PortugueseBrazil;
        }

        if (systemCulture.TwoLetterISOLanguageName.Equals("es", StringComparison.OrdinalIgnoreCase))
        {
            return Spanish;
        }

        if (systemCulture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            return EnglishUs;
        }

        return EnglishUs;
    }
}
