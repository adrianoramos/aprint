using System.Globalization;
using Aprint.Features.Settings;

namespace Aprint.Tests.Settings;

[Collection("Localization")]
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
            AppLocalization.ApplyCulture(AppLocalization.SpanishCode);

            Assert.Equal("Capturar ahora", AppLocalization.Translate("Tray.CaptureNow"));
            Assert.Contains(
                AppLocalization.GetLanguageOptions(),
                option => option.Code == AppLocalization.SpanishCode && option.DisplayName == "Español");
        }
        finally
        {
            AppLocalization.ApplyCulture(previousCulture.Name);
        }
    }

    [Fact]
    public void ExistingThreadUsesTheNewApplicationCulture()
    {
        var previousCulture = CultureInfo.CurrentUICulture;
        using var cultureApplied = new ManualResetEventSlim();
        string? translatedText = null;
        var thread = new Thread(() =>
        {
            cultureApplied.Wait();
            translatedText = AppLocalization.Translate("Tray.CaptureNow");
        });

        try
        {
            thread.Start();
            AppLocalization.ApplyCulture(AppLocalization.PortugueseBrazilCode);
            cultureApplied.Set();
            Assert.True(thread.Join(TimeSpan.FromSeconds(5)));
            Assert.Equal("Capturar agora", translatedText);
        }
        finally
        {
            cultureApplied.Set();
            if (thread.IsAlive)
            {
                thread.Join();
            }

            AppLocalization.ApplyCulture(previousCulture.Name);
        }
    }
}