namespace Aprint.Features.Editor;

public sealed class EditHistory : IDisposable
{
    private const int MaxHistoryStates = 20;
    private const long HistoryMemoryBudgetBytes = 256L * 1024 * 1024;

    private readonly LinkedList<HistoryEntry> _undoStack = new();
    private readonly LinkedList<HistoryEntry> _redoStack = new();
    private long _totalMemoryBytes;

    public EditHistory(Bitmap initialBitmap, int initialNextCounterValue = 1)
    {
        PushSnapshot(_undoStack, initialBitmap, initialNextCounterValue);
        TrimToBudget();
    }

    public void Capture(Bitmap bitmap, int nextCounterValue)
    {
        PushSnapshot(_undoStack, bitmap, nextCounterValue);
        DisposeAll(_redoStack);
        TrimToBudget();
    }

    public (Bitmap Bitmap, int NextCounterValue)? Undo(Bitmap currentBitmap, int currentNextCounterValue)
    {
        if (_undoStack.Count <= 1)
        {
            return null;
        }

        PushSnapshot(_redoStack, currentBitmap, currentNextCounterValue);
        var snapshot = TakeSnapshot(_undoStack);
        TrimToBudget();
        return snapshot;
    }

    public (Bitmap Bitmap, int NextCounterValue)? Redo(Bitmap currentBitmap, int currentNextCounterValue)
    {
        if (_redoStack.Count == 0)
        {
            return null;
        }

        PushSnapshot(_undoStack, currentBitmap, currentNextCounterValue);
        var snapshot = TakeSnapshot(_redoStack);
        TrimToBudget();
        return snapshot;
    }

    public void Dispose()
    {
        DisposeAll(_undoStack);
        DisposeAll(_redoStack);
    }

    private void PushSnapshot(LinkedList<HistoryEntry> stack, Bitmap bitmap, int nextCounterValue)
    {
        var snapshot = (Bitmap)bitmap.Clone();
        var entry = new HistoryEntry(snapshot, nextCounterValue, EstimateMemoryBytes(snapshot));
        stack.AddLast(entry);
        _totalMemoryBytes += entry.MemoryBytes;
    }

    private void TrimToBudget()
    {
        while ((GetTotalStateCount() > MaxHistoryStates || GetTotalMemoryBytes() > HistoryMemoryBudgetBytes) &&
            GetTotalStateCount() > 1)
        {
            if (_redoStack.Count > 0)
            {
                TrimOldestEntry(_redoStack);
            }
            else if (_undoStack.Count > 1)
            {
                TrimOldestEntry(_undoStack);
            }
            else
            {
                break;
            }
        }
    }

    private void TrimOldestEntry(LinkedList<HistoryEntry> stack)
    {
        var entry = stack.First!.Value;
        stack.RemoveFirst();
        entry.Bitmap.Dispose();
        _totalMemoryBytes -= entry.MemoryBytes;
    }

    private int GetTotalStateCount() => _undoStack.Count + _redoStack.Count;

    private long GetTotalMemoryBytes() => _totalMemoryBytes;

    private static long EstimateMemoryBytes(Bitmap bitmap)
    {
        var bitsPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat);
        var bytesPerPixel = Math.Max(1, (bitsPerPixel + 7) / 8);
        return (long)bitmap.Width * bitmap.Height * bytesPerPixel;
    }

    private (Bitmap Bitmap, int NextCounterValue) TakeSnapshot(LinkedList<HistoryEntry> stack)
    {
        var node = stack.Last!;
        var snapshot = node.Value;
        stack.RemoveLast();
        _totalMemoryBytes -= snapshot.MemoryBytes;
        return (snapshot.Bitmap, snapshot.NextCounterValue);
    }

    private void DisposeAll(LinkedList<HistoryEntry> stack)
    {
        while (stack.Last is { } node)
        {
            var entry = node.Value;
            stack.RemoveLast();
            entry.Bitmap.Dispose();
            _totalMemoryBytes -= entry.MemoryBytes;
        }
    }

    private sealed record HistoryEntry(Bitmap Bitmap, int NextCounterValue, long MemoryBytes);
}
