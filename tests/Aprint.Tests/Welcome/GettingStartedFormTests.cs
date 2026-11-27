using System.Globalization;
using Aprint.Features.Settings;
using Aprint.Features.Welcome;

namespace Aprint.Tests.Welcome;

public sealed class GettingStartedFormTests
{
    [Theory]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    [InlineData("es")]
    public void SettingsButtonFitsLocalizedTextAndCloseButtonIsRemoved(string language)
    {
        var previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
            using var form = new GettingStartedForm(new AppSettings(), () => { });
            var settingsButton = Assert.IsType<Button>(form.Controls["btnSettings"]);
            var okButton = Assert.IsType<Button>(form.Controls["btnOk"]);
            var startupCheckBox = Assert.IsType<CheckBox>(form.Controls["chkStartWithWindows"]);

            Assert.Equal(AppLocalization.Translate("Tray.Settings"), settingsButton.Text);
            Assert.True(settingsButton.Width >= settingsButton.GetPreferredSize(Size.Empty).Width);
            Assert.Equal(AppLocalization.Translate("Welcome.Ok"), okButton.Text);
            Assert.Equal(AppLocalization.Translate("Settings.StartWithWindows"), startupCheckBox.Text);
            Assert.True(startupCheckBox.Checked);
            Assert.Equal(
                okButton.Top + (okButton.Height - startupCheckBox.Height) / 2,
                startupCheckBox.Top);
            Assert.Equal(2, form.Controls.OfType<Button>().Count());
            Assert.DoesNotContain(form.Controls.OfType<Button>(), control => control.Text == AppLocalization.Translate("Welcome.Close"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }
}
