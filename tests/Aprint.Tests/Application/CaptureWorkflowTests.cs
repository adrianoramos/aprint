using System.Reflection;
using Aprint.Application;
using Aprint.Features.Capture;
using Aprint.Features.Settings;

namespace Aprint.Tests.Application;

public sealed class CaptureWorkflowTests
{
    [Theory]
    [InlineData(CaptureAction.ClipboardOnly, false, false, true, false)]
    [InlineData(CaptureAction.SaveToDiskOnly, false, false, false, true)]
    [InlineData(CaptureAction.Both, false, false, true, true)]
    [InlineData(CaptureAction.SaveToDiskOnly, true, true, false, false)]
    [InlineData(CaptureAction.Both, true, true, true, false)]
    public void FloatingCaptureFollowsConfiguredPostCaptureFlow(
        CaptureAction captureAction,
        bool openEditorAfterCapture,
        bool expectedOpenEditor,
        bool expectedCopyToClipboard,
        bool expectedSaveToDisk)
    {
        var settings = new AppSettings
        {
            CaptureAction = captureAction,
            OpenEditorAfterCapture = openEditorAfterCapture,
        };

        var actions = ResolveActions(settings, RegionSelectionAction.Capture);

        Assert.Equal(expectedOpenEditor, actions.OpenEditor);
        Assert.Equal(expectedCopyToClipboard, actions.CopyToClipboard);
        Assert.Equal(expectedSaveToDisk, actions.SaveToDisk);
    }

    [Fact]
    public void ExplicitCopyAndSaveActionsOverrideConfiguredPostCaptureFlow()
    {
        var settings = new AppSettings
        {
            CaptureAction = CaptureAction.Both,
            OpenEditorAfterCapture = true,
        };

        Assert.Equal((false, true, false), ResolveActions(settings, RegionSelectionAction.Copy));
        Assert.Equal((false, false, true), ResolveActions(settings, RegionSelectionAction.Save));
    }

    private static (bool OpenEditor, bool CopyToClipboard, bool SaveToDisk) ResolveActions(
        AppSettings settings,
        RegionSelectionAction? selectionAction)
    {
        var method = typeof(CaptureWorkflow).GetMethod("ResolveActions", BindingFlags.Static | BindingFlags.NonPublic)!;
        return ((bool OpenEditor, bool CopyToClipboard, bool SaveToDisk))method.Invoke(null, [settings, selectionAction])!;
    }
}