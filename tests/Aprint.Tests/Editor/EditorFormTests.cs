using System.Reflection;
using Aprint.Features.Capture;
using Aprint.Features.Editor;

namespace Aprint.Tests.Editor;

public sealed class EditorFormTests
{
    [Fact]
    public void InitialToolDefaultsToFreehandWhenNotProvided()
    {
        using var bitmap = new Bitmap(2, 2);
        using var editor = new EditorForm(bitmap, string.Empty);

        Assert.Equal(EditTool.Freehand, editor.SelectedTool);
    }

    [Theory]
    [InlineData((EditTool)int.MaxValue, EditTool.Freehand)]
    [InlineData(EditTool.Line, EditTool.Line)]
    public void InitialToolFallsBackToFreehandOnlyWhenUndefined(EditTool initialTool, EditTool expectedTool)
    {
        using var bitmap = new Bitmap(2, 2);
        using var editor = new EditorForm(bitmap, string.Empty, initialTool: initialTool);

        Assert.Equal(expectedTool, editor.SelectedTool);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EditorToolbarButtonsMatchCaptureDropdownHeight(bool showLabels)
    {
        using var editorBitmap = new Bitmap(2, 2);
        using var captureBitmap = new Bitmap(2, 2);
        using var editor = new EditorForm(editorBitmap, string.Empty, showToolLabels: showLabels);
        using var capture = new RegionSelectorForm(captureBitmap);
        var editorDropdowns = typeof(EditorForm)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(field => typeof(ToolStripDropDownButton).IsAssignableFrom(field.FieldType))
            .Select(field => (ToolStripDropDownButton)field.GetValue(editor)!)
            .ToArray();
        var captureActionBar = (ToolStrip)typeof(RegionSelectorForm)
            .GetField("_actionBar", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(capture)!;
        var captureDropdowns = captureActionBar.Items.OfType<ToolStripDropDownButton>().ToArray();

        Assert.Equal(4, editorDropdowns.Length);
        Assert.Equal(3, captureDropdowns.Length);
        Assert.All(editorDropdowns, button => Assert.Equal(14, button.Width));
        Assert.All(editorDropdowns, button => Assert.Equal(captureDropdowns[0].Width, button.Width));
        Assert.All(captureDropdowns, button => Assert.Equal(34, button.Height));
        Assert.All(editorDropdowns, button => Assert.Equal(captureDropdowns[0].Height, button.Height));
        var editorToolStrip = editor.Controls.OfType<ToolStrip>().Single();
        Assert.All(
            editorToolStrip.Items.OfType<ToolStripButton>(),
            button => Assert.Equal(captureDropdowns[0].Height, button.Height));
        if (showLabels)
        {
            Assert.All(
                editorToolStrip.Items.OfType<ToolStripButton>()
                    .Where(button => button.DisplayStyle == ToolStripItemDisplayStyle.ImageAndText),
                button => Assert.True(
                    button.Width >= TextRenderer.MeasureText(button.Text, editorToolStrip.Font).Width,
                    $"The '{button.Text}' label must fit its button."));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShowWithoutActivationIsEnabledOnlyWhenRequested(bool showWithoutActivation)
    {
        using var bitmap = new Bitmap(2, 2);
        using var editor = new EditorForm(bitmap, string.Empty, showWithoutActivation: showWithoutActivation);
        var property = typeof(EditorForm).GetProperty("ShowWithoutActivation", BindingFlags.Instance | BindingFlags.NonPublic)!;

        Assert.Equal(showWithoutActivation, property.GetValue(editor));
    }
}