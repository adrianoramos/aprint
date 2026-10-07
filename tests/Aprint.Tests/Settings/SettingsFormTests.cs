using Aprint.Features.Settings;

namespace Aprint.Tests.Settings;

public sealed class SettingsFormTests
{
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