using System.Reflection;
using Aprint.Features.Editor;

namespace Aprint.Tests.Editor;

public sealed class EditHistoryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FirstCloneFailureReleasesReservation(bool regional)
    {
        using var bitmap = new Bitmap(4, 4);
        var budget = new BitmapSnapshotBudget(64);
        var shouldFail = true;
        using var history = CreateHistory(bitmap, budget, (source, bounds) =>
        {
            if (shouldFail)
            {
                throw new InvalidOperationException("Clone failed.");
            }

            return bounds is { } region ? source.Clone(region, source.PixelFormat) : (Bitmap)source.Clone();
        });

        void Capture()
        {
            if (regional)
            {
                history.CaptureRegion(bitmap, new Rectangle(0, 0, 2, 2), 1);
            }
            else
            {
                history.Capture(bitmap, 1);
            }
        }

        Assert.Throws<InvalidOperationException>(Capture);
        Assert.Equal(0, budget.RetainedBytes);
        Assert.False(history.CanUndo);

        shouldFail = false;
        Capture();
        Assert.True(history.CanUndo);
        Assert.Equal(regional ? 16 : 64, budget.RetainedBytes);
    }

    [Fact]
    public void LaterCloneFailureDisposesEarlierPatchesAndReleasesReservation()
    {
        using var bitmap = new Bitmap(4, 4);
        var budget = new BitmapSnapshotBudget(64);
        Bitmap? firstPatch = null;
        using var history = CreateHistory(bitmap, budget, (source, bounds) =>
        {
            if (firstPatch is not null)
            {
                throw new InvalidOperationException("Second patch failed.");
            }

            firstPatch = source.Clone(bounds!.Value, source.PixelFormat);
            return firstPatch;
        });

        Assert.Throws<InvalidOperationException>(() => history.CaptureRegions(
            bitmap, [new Rectangle(0, 0, 1, 1), new Rectangle(3, 3, 1, 1)], 1));

        Assert.Equal(0, budget.RetainedBytes);
        Assert.False(history.CanUndo);
        Assert.NotNull(firstPatch);
        Assert.Throws<ArgumentException>(() => firstPatch.GetPixel(0, 0));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedNewCapturePreservesRedo(bool regional)
    {
        using var bitmap = new Bitmap(4, 4);
        var budget = new BitmapSnapshotBudget(256);
        var shouldFail = false;
        using var history = CreateHistory(bitmap, budget, (source, bounds) =>
        {
            if (shouldFail)
            {
                throw new InvalidOperationException("New capture failed.");
            }

            return bounds is { } region ? source.Clone(region, source.PixelFormat) : (Bitmap)source.Clone();
        });
        history.CaptureRegion(bitmap, new Rectangle(0, 0, 1, 1), 1);
        bitmap.SetPixel(0, 0, Color.Red);
        history.Undo(bitmap, 2);
        var retainedBytes = budget.RetainedBytes;
        shouldFail = true;

        Assert.Throws<InvalidOperationException>(() =>
        {
            if (regional)
            {
                history.CaptureRegion(bitmap, new Rectangle(0, 0, 1, 1), 1);
            }
            else
            {
                history.Capture(bitmap, 1);
            }
        });

        Assert.True(history.CanRedo);
        Assert.Equal(retainedBytes, budget.RetainedBytes);
        shouldFail = false;
        history.Redo(bitmap, 1);
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(0, 0).ToArgb());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void UndoAndRedoCloneFailureRetainsOriginalEntry(bool redo, bool regional)
    {
        using var bitmap = new Bitmap(4, 4);
        bitmap.SetPixel(0, 0, Color.White);
        var budget = new BitmapSnapshotBudget(128);
        var shouldFail = false;
        using var history = CreateHistory(bitmap, budget, (source, bounds) =>
        {
            if (shouldFail)
            {
                throw new InvalidOperationException("Inverse snapshot failed.");
            }

            return bounds is { } region ? source.Clone(region, source.PixelFormat) : (Bitmap)source.Clone();
        });
        if (regional)
        {
            history.CaptureRegion(bitmap, new Rectangle(0, 0, 1, 1), 1);
        }
        else
        {
            history.Capture(bitmap, 1);
        }

        bitmap.SetPixel(0, 0, Color.Red);
        Bitmap? transferredBitmap = null;
        try
        {
            var currentBitmap = bitmap;
            if (redo)
            {
                var undone = history.Undo(bitmap, 2)!.Value;
                currentBitmap = undone.Bitmap;
                if (!ReferenceEquals(bitmap, currentBitmap))
                {
                    transferredBitmap = currentBitmap;
                }
            }

            var retainedBytes = budget.RetainedBytes;
            shouldFail = true;
            Assert.Throws<InvalidOperationException>(() =>
            {
                if (redo)
                {
                    history.Redo(currentBitmap, 1);
                }
                else
                {
                    history.Undo(currentBitmap, 2);
                }
            });

            Assert.Equal(retainedBytes, budget.RetainedBytes);
            Assert.True(redo ? history.CanRedo : history.CanUndo);
            Assert.Equal((redo ? Color.White : Color.Red).ToArgb(), currentBitmap.GetPixel(0, 0).ToArgb());

            shouldFail = false;
            var restored = (redo ? history.Redo(currentBitmap, 1) : history.Undo(currentBitmap, 2))!.Value;
            try
            {
                Assert.Equal((redo ? Color.Red : Color.White).ToArgb(), restored.Bitmap.GetPixel(0, 0).ToArgb());
            }
            finally
            {
                if (!ReferenceEquals(currentBitmap, restored.Bitmap))
                {
                    restored.Bitmap.Dispose();
                }
            }
        }
        finally
        {
            transferredBitmap?.Dispose();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PatchApplicationFailureDisposesDetachedEntry(bool redo)
    {
        using var bitmap = new Bitmap(4, 4);
        var budget = new BitmapSnapshotBudget(128);
        var patches = new List<Bitmap>();
        using var history = CreateHistory(bitmap, budget, (source, bounds) =>
        {
            var patch = source.Clone(bounds!.Value, source.PixelFormat);
            patches.Add(patch);
            return patch;
        });
        history.CaptureRegion(bitmap, new Rectangle(0, 0, 1, 1), 1);
        if (redo)
        {
            history.Undo(bitmap, 2);
        }

        var detachedPatch = patches[^1];
        using var indexedBitmap = new Bitmap(4, 4, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
        Assert.Throws<ArgumentException>(() =>
        {
            if (redo)
            {
                history.Redo(indexedBitmap, 1);
            }
            else
            {
                history.Undo(indexedBitmap, 2);
            }
        });

        Assert.Throws<ArgumentException>(() => detachedPatch.GetPixel(0, 0));
        history.Dispose();
        Assert.Equal(0, budget.RetainedBytes);
        foreach (var patch in patches)
        {
            Assert.Throws<ArgumentException>(() => patch.GetPixel(0, 0));
        }
    }

    private static EditHistory CreateHistory(
        Bitmap bitmap,
        BitmapSnapshotBudget budget,
        Func<Bitmap, Rectangle?, Bitmap> cloneSnapshot) => (EditHistory)typeof(EditHistory)
        .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
            [typeof(Bitmap), typeof(int), typeof(BitmapSnapshotBudget), typeof(Func<Bitmap, Rectangle?, Bitmap>)], null)!
        .Invoke([bitmap, 1, budget, cloneSnapshot]);

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
