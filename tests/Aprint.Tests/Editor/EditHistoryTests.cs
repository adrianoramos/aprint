using Aprint.Features.Editor;

namespace Aprint.Tests.Editor;

public sealed class EditHistoryTests
{
    [Fact]
    public void UndoAndRedoRestoreBitmapStates()
    {
        using var bitmap = new Bitmap(2, 2);
        bitmap.SetPixel(0, 0, Color.Red);
        using var history = new EditHistory(bitmap, initialNextCounterValue: 1);
        history.Capture(bitmap, nextCounterValue: 1);
        bitmap.SetPixel(0, 0, Color.Blue);

        var undone = history.Undo(bitmap, currentNextCounterValue: 2);
        Assert.NotNull(undone);

        using var undoneBitmap = undone.Value.Bitmap;
        Assert.Equal(1, undone.Value.NextCounterValue);
        Assert.Equal(Color.Red.ToArgb(), undoneBitmap.GetPixel(0, 0).ToArgb());

        var redone = history.Redo(undoneBitmap, currentNextCounterValue: undone.Value.NextCounterValue);
        Assert.NotNull(redone);

        using var redoneBitmap = redone.Value.Bitmap;
        Assert.Equal(2, redone.Value.NextCounterValue);
        Assert.Equal(Color.Blue.ToArgb(), redoneBitmap.GetPixel(0, 0).ToArgb());
    }
}
