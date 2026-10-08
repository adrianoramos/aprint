using Aprint.Features.Capture;
using Aprint.Features.Settings;

namespace Aprint.Tests.Settings;

public sealed class SettingsFormTests
{
    [Theory]
    [InlineData("pt-BR", true, "Exibir rótulos nos botões")]
    [InlineData("pt-BR", false, "Exibir rótulos nos botões")]
    [InlineData("en-US", true, "Show labels on buttons")]
    [InlineData("en-US", false, "Show labels on buttons")]
    public void ToolLabelsOptionIsInGeneralTabWithLocalizedText(string language, bool showToolLabels, string expectedText)
    {
        var previousCulture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(language);
            var settings = new AppSettings { ShowToolLabels = showToolLabels };
            using var form = new SettingsForm(settings);
            var tabControl = Assert.Single(form.Controls.OfType<TabControl>());
            var generalTab = tabControl.TabPages[0];
            var editorTab = tabControl.TabPages[3];
            var option = Assert.IsType<CheckBox>(generalTab.Controls["chkShowToolLabels"]);

            Assert.Null(editorTab.Controls["chkShowToolLabels"]);
            Assert.Equal(expectedText, option.Text);
            Assert.Equal(showToolLabels, option.Checked);
            Assert.Equal(28, option.Top - generalTab.Controls["chkPlaySound"]!.Top);
            Assert.Equal(28, generalTab.Controls["chkShowNotification"]!.Top - option.Top);
            var editorCheckboxes = editorTab.Controls.OfType<CheckBox>().OrderBy(control => control.Top).ToArray();
            for (var index = 1; index < editorCheckboxes.Length; index++)
            {
                Assert.Equal(28, editorCheckboxes[index].Top - editorCheckboxes[index - 1].Top);
            }

            option.Checked = !showToolLabels;
            Assert.Equal(showToolLabels, settings.ShowToolLabels);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    [Theory]
    [InlineData(CaptureMode.AllScreens, false)]
    [InlineData(CaptureMode.ScreenWithCursor, false)]
    [InlineData(CaptureMode.SelectArea, true)]
    public void OutsideClickActionIsEnabledOnlyForAreaSelection(CaptureMode initialMode, bool expectedEnabled)
    {
        using var form = new SettingsForm(new AppSettings
        {
            CaptureMode = initialMode,
            OutsideCaptureClickAction = OutsideCaptureClickAction.ExitCaptureMode,
        });
        var tabControl = Assert.Single(form.Controls.OfType<TabControl>());
        var captureTab = tabControl.TabPages[2];
        var captureMode = Assert.IsType<ComboBox>(captureTab.Controls["cmbCaptureMode"]);
        var outsideAction = Assert.IsType<ComboBox>(captureTab.Controls["cmbOutsideCaptureClickAction"]);

        Assert.Equal(expectedEnabled, outsideAction.Enabled);

        foreach (var mode in new[] { CaptureMode.SelectArea, CaptureMode.AllScreens, CaptureMode.ScreenWithCursor, CaptureMode.SelectArea })
        {
            captureMode.SelectedIndex = (int)mode;

            Assert.Equal(mode == CaptureMode.SelectArea, outsideAction.Enabled);
            Assert.Equal((int)OutsideCaptureClickAction.ExitCaptureMode, outsideAction.SelectedIndex);
        }
    }

    [Fact]
    public void SettingsAreOrganizedInTabsInACompactWindow()
    {
        using var form = new SettingsForm(new AppSettings());
        var tabControl = Assert.Single(form.Controls.OfType<TabControl>());

        Assert.Equal(4, tabControl.TabPages.Count);
        Assert.Equal(
            [
                AppLocalization.Translate("Settings.GeneralGroup"),
                AppLocalization.Translate("Settings.HotkeyGroup"),
                AppLocalization.Translate("Settings.CaptureGroup"),
                AppLocalization.Translate("Settings.EditorGroup"),
            ],
            tabControl.TabPages.Cast<TabPage>().Select(page => page.Text));
        Assert.Contains("chkForcePrintScreen", tabControl.TabPages[1].Controls.Cast<Control>().Select(control => control.Name));
        Assert.Contains("lblCaptureMode", tabControl.TabPages[2].Controls.Cast<Control>().Select(control => control.Name));
        var captureTab = tabControl.TabPages[2];
        var captureMode = captureTab.Controls["cmbCaptureMode"]!;
        var captureAction = captureTab.Controls["cmbCaptureAction"]!;
        var outsideAction = captureTab.Controls["cmbOutsideCaptureClickAction"]!;
        var saveFolder = captureTab.Controls["txtSaveFolder"]!;
        var browseButton = captureTab.Controls["btnBrowse"]!;
        Assert.Equal(captureMode.Right, captureAction.Right);
        Assert.Equal(captureMode.Right, outsideAction.Right);
        Assert.Equal(captureMode.Right, browseButton.Right);
        Assert.Equal(8, browseButton.Left - saveFolder.Right);
        var hotkeyTab = tabControl.TabPages[1];
        var editorTab = tabControl.TabPages[3];
        var generalTab = tabControl.TabPages[0];
        Assert.Equal(captureMode.Right, hotkeyTab.Controls["cmbKey"]!.Right);
        Assert.Equal(captureMode.Right, editorTab.Controls["cmbEditorStartState"]!.Right);
        Assert.Equal(captureMode.Right, editorTab.Controls["cmbDefaultEditorTool"]!.Right);
        Assert.Equal(captureMode.Right, editorTab.Controls["cmbEditorInactivityTimeout"]!.Right);
        Assert.Equal(captureMode.Right, generalTab.Controls["cmbNotificationClick"]!.Right);
        Assert.Equal(captureMode.Right, generalTab.Controls["cmbLanguage"]!.Right);
        Assert.Empty(form.Controls.OfType<GroupBox>());
        Assert.Equal(320, form.ClientSize.Height);
    }
}