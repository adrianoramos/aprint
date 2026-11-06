using System.Globalization;
using Aprint.Features.Settings;

namespace Aprint.Tests.Settings;

public sealed class AppLocalizationTests
{
    [Theory]
    [InlineData("es-ES")]
    [InlineData("es-MX")]
    [InlineData("es-AR")]
    public void SystemSpanishCultureSelectsSpanish(string systemCultureName)
    {
        var culture = AppLocalization.ResolveCulture(
            AppLocalization.SystemLanguageCode,
            CultureInfo.GetCultureInfo(systemCultureName));

        Assert.Equal(AppLocalization.SpanishCode, culture.Name);
    }

    [Fact]
    public void SpanishCultureLoadsSpanishResourcesAndLanguageOption()
    {
        var previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = AppLocalization.ResolveCulture(AppLocalization.SpanishCode);

            Assert.Equal("Capturar ahora", AppLocalization.Translate("Tray.CaptureNow"));
            Assert.Contains(
                AppLocalization.GetLanguageOptions(),
                option => option.Code == AppLocalization.SpanishCode && option.DisplayName == "Español");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }
}