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
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        history.Capture(bitmap, nextCounterValue: 1);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
        bitmap.SetPixel(0, 0, Color.Blue);

        var undone = history.Undo(bitmap, currentNextCounterValue: 2);
        Assert.NotNull(undone);
        Assert.False(history.CanUndo);
        Assert.True(history.CanRedo);

        using var undoneBitmap = undone.Value.Bitmap;
        Assert.Equal(1, undone.Value.NextCounterValue);
        Assert.Equal(Color.Red.ToArgb(), undoneBitmap.GetPixel(0, 0).ToArgb());

        var redone = history.Redo(undoneBitmap, currentNextCounterValue: undone.Value.NextCounterValue);
        Assert.NotNull(redone);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);

        using var redoneBitmap = redone.Value.Bitmap;
        Assert.Equal(2, redone.Value.NextCounterValue);
        Assert.Equal(Color.Blue.ToArgb(), redoneBitmap.GetPixel(0, 0).ToArgb());
    }

    [Fact]
    public void UndoAndRedoRestoreDimensionsAfterCrop()
    {
        using var original = new Bitmap(4, 3);
        using var history = new EditHistory(original);
        history.Capture(original, nextCounterValue: 1);
        using var cropped = ImageEditingService.Crop(original, new Rectangle(1, 1, 2, 2));

        var undone = history.Undo(cropped, currentNextCounterValue: 1);
        Assert.NotNull(undone);
        using var restored = undone.Value.Bitmap;
        Assert.Equal(new Size(4, 3), restored.Size);

        var redone = history.Redo(restored, currentNextCounterValue: 1);
        Assert.NotNull(redone);
        using var croppedAgain = redone.Value.Bitmap;
        Assert.Equal(new Size(2, 2), croppedAgain.Size);
    }

    [Fact]
    public void UndoAfterSecondCropRestoresFirstCrop()
    {
        using var original = new Bitmap(6, 5);
        using var history = new EditHistory(original);
        history.Capture(original, nextCounterValue: 1);
        using var firstCrop = ImageEditingService.Crop(original, new Rectangle(1, 1, 4, 3));
        history.Capture(firstCrop, nextCounterValue: 1);
        using var secondCrop = ImageEditingService.Crop(firstCrop, new Rectangle(1, 1, 2, 2));

        var undone = history.Undo(secondCrop, currentNextCounterValue: 1);
        Assert.NotNull(undone);
        using var restored = undone.Value.Bitmap;
        Assert.Equal(new Size(4, 3), restored.Size);

        var redone = history.Redo(restored, currentNextCounterValue: 1);
        Assert.NotNull(redone);
        using var croppedAgain = redone.Value.Bitmap;
        Assert.Equal(new Size(2, 2), croppedAgain.Size);
    }
}
