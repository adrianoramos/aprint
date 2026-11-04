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
    public void HistorySnapshotsAreDeferredAndShareOneMemoryBudget()
    {
        using var bitmap = new Bitmap(2, 2, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var budget = new BitmapSnapshotBudget(maximumBytes: 16);
        using var firstHistory = new EditHistory(bitmap, memoryBudget: budget);
        using var secondHistory = new EditHistory(bitmap, memoryBudget: budget);

        Assert.Equal(0, budget.RetainedBytes);

        firstHistory.Capture(bitmap, nextCounterValue: 1);
        Assert.True(firstHistory.CanUndo);
        Assert.Equal(16, budget.RetainedBytes);

        secondHistory.Capture(bitmap, nextCounterValue: 1);

        Assert.False(firstHistory.CanUndo);
        Assert.True(secondHistory.CanUndo);
        Assert.Equal(16, budget.RetainedBytes);
    }

    [Fact]
    public void HistorySkipsSnapshotLargerThanSharedMemoryBudget()
    {
        using var bitmap = new Bitmap(2, 2, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var budget = new BitmapSnapshotBudget(maximumBytes: 8);
        using var history = new EditHistory(bitmap, memoryBudget: budget);

        history.Capture(bitmap, nextCounterValue: 1);

        Assert.False(history.CanUndo);
        Assert.Equal(0, budget.RetainedBytes);
    }

    [Fact]
    public void RegionSnapshotUsesOnlyChangedPixelsAndUndoesInPlace()
    {
        using var bitmap = new Bitmap(100, 100, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        bitmap.SetPixel(20, 30, Color.White);
        var budget = new BitmapSnapshotBudget(maximumBytes: 1024);
        using var history = new EditHistory(bitmap, memoryBudget: budget);
        var changedRegion = new Rectangle(20, 30, 5, 7);

        history.CaptureRegion(bitmap, changedRegion, nextCounterValue: 1);
        bitmap.SetPixel(20, 30, Color.Red);

        Assert.Equal(5 * 7 * 4, budget.RetainedBytes);
        var undone = history.Undo(bitmap, currentNextCounterValue: 2);
        Assert.NotNull(undone);
        Assert.Same(bitmap, undone.Value.Bitmap);
        Assert.Equal(1, undone.Value.NextCounterValue);
        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(20, 30).ToArgb());

        var redone = history.Redo(bitmap, currentNextCounterValue: undone.Value.NextCounterValue);
        Assert.NotNull(redone);
        Assert.Same(bitmap, redone.Value.Bitmap);
        Assert.Equal(2, redone.Value.NextCounterValue);
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(20, 30).ToArgb());
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
