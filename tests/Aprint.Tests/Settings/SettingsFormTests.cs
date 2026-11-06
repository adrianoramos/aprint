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
                Assert.True(editorCheckboxes[index].Top >= editorCheckboxes[index - 1].Bottom);
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
        Assert.Equal(396, form.ClientSize.Height);
    }

    [Theory]
    [InlineData("pt-BR", "Formato padrão da imagem:", "Imagem JPEG")]
    [InlineData("en-US", "Default image format:", "JPEG image")]
    [InlineData("es", "Formato de imagen predeterminado:", "Imagen JPEG")]
    public void DefaultScreenshotFormatIsLocalizedAndSelectedOnCaptureTab(
        string language,
        string expectedLabel,
        string expectedSelection)
    {
        var previousCulture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(language);
            using var form = new SettingsForm(new AppSettings { DefaultScreenshotFormat = ScreenshotFileFormat.Jpeg });
            var tabControl = Assert.Single(form.Controls.OfType<TabControl>());
            var captureTab = tabControl.TabPages[2];
            var label = Assert.IsType<Label>(captureTab.Controls["lblDefaultScreenshotFormat"]);
            var format = Assert.IsType<ComboBox>(captureTab.Controls["cmbDefaultScreenshotFormat"]);

            Assert.Equal(expectedLabel, label.Text);
            Assert.Equal(3, format.Items.Count);
            Assert.Equal(1, format.SelectedIndex);
            Assert.Equal(expectedSelection, format.SelectedItem);
            Assert.Equal(format.Right, captureTab.Controls["cmbCaptureMode"]!.Right);
            Assert.True(format.Bottom <= captureTab.ClientSize.Height - 15);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    [Theory]
    [InlineData("es")]
    [InlineData("en-US")]
    public void EditorOptionsDoNotOverlapLocalizedLabels(string language)
    {
        var previousCulture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(language);
            using var form = new SettingsForm(new AppSettings());
            var tabControl = Assert.Single(form.Controls.OfType<TabControl>());
            var editorTab = tabControl.TabPages[3];
            var openEditor = Assert.IsType<CheckBox>(editorTab.Controls["chkOpenEditor"]);
            var closeEditor = Assert.IsType<CheckBox>(editorTab.Controls["chkCloseEditorOnCopyOrSave"]);
            var editorStartState = Assert.IsType<ComboBox>(editorTab.Controls["cmbEditorStartState"]);
            var autoClose = Assert.IsType<CheckBox>(editorTab.Controls["chkAutoCloseEditorWhenInactive"]);
            var inactivityTimeout = Assert.IsType<ComboBox>(editorTab.Controls["cmbEditorInactivityTimeout"]);
            var rememberLastTool = Assert.IsType<CheckBox>(editorTab.Controls["chkRememberLastTool"]);
            var defaultToolLabel = Assert.IsType<Label>(editorTab.Controls["lblDefaultEditorTool"]);
            var defaultTool = Assert.IsType<ComboBox>(editorTab.Controls["cmbDefaultEditorTool"]);
            var captureTab = tabControl.TabPages[2];
            var captureModeLabel = Assert.IsType<Label>(captureTab.Controls["lblCaptureMode"]);
            var captureMode = Assert.IsType<ComboBox>(captureTab.Controls["cmbCaptureMode"]);
            var nextCaptureLabel = Assert.IsType<Label>(captureTab.Controls["lblOutsideCaptureClickAction"]);

            Assert.True(editorStartState.Top >= openEditor.Bottom);
            Assert.True(inactivityTimeout.Top >= autoClose.Bottom);
            Assert.Equal(captureMode.Top - captureModeLabel.Top, defaultTool.Top - defaultToolLabel.Top);
            Assert.Equal(nextCaptureLabel.Top - captureMode.Bottom, closeEditor.Top - editorStartState.Bottom);
            Assert.Equal(nextCaptureLabel.Top - captureMode.Bottom, rememberLastTool.Top - inactivityTimeout.Bottom);
            Assert.All(
                editorTab.Controls.OfType<CheckBox>(),
                checkBox => Assert.True(checkBox.Right <= editorTab.ClientSize.Width - 15));
            Assert.All(
                new[] { editorStartState, inactivityTimeout, defaultTool },
                comboBox => Assert.Equal(editorTab.ClientSize.Width - 15, comboBox.Right));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = previousCulture;
        }
    }
}