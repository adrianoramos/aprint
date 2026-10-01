namespace Aprint.Features.Editor;

public sealed class EditHistory : IDisposable
{
    private const int MaxHistoryStates = 20;
    private const long HistoryMemoryBudgetBytes = 256L * 1024 * 1024;

    private readonly Stack<HistoryEntry> _undoStack = new();
    private readonly Stack<HistoryEntry> _redoStack = new();

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

    private static void PushSnapshot(Stack<HistoryEntry> stack, Bitmap bitmap, int nextCounterValue)
    {
        var snapshot = (Bitmap)bitmap.Clone();
        stack.Push(new HistoryEntry(snapshot, nextCounterValue, EstimateMemoryBytes(snapshot)));
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

    private static void TrimOldestEntry(Stack<HistoryEntry> stack)
    {
        var entries = stack.ToArray();
        entries[^1].Bitmap.Dispose();
        stack.Clear();
        for (var i = entries.Length - 2; i >= 0; i--)
        {
            stack.Push(entries[i]);
        }
    }

    private int GetTotalStateCount() => _undoStack.Count + _redoStack.Count;

    private long GetTotalMemoryBytes() => _undoStack.Sum(entry => entry.MemoryBytes) +
        _redoStack.Sum(entry => entry.MemoryBytes);

    private static long EstimateMemoryBytes(Bitmap bitmap)
    {
        var bitsPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat);
        var bytesPerPixel = Math.Max(1, (bitsPerPixel + 7) / 8);
        return (long)bitmap.Width * bitmap.Height * bytesPerPixel;
    }

    private static (Bitmap Bitmap, int NextCounterValue) TakeSnapshot(Stack<HistoryEntry> stack)
    {
        var snapshot = stack.Pop();
        return (snapshot.Bitmap, snapshot.NextCounterValue);
    }

    private static void DisposeAll(Stack<HistoryEntry> stack)
    {
        while (stack.TryPop(out var entry))
        {
            entry.Bitmap.Dispose();
        }
    }

    private sealed record HistoryEntry(Bitmap Bitmap, int NextCounterValue, long MemoryBytes);
}
