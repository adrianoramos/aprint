namespace Aprint.Features.Editor;

public sealed class EditHistory : IDisposable
{
    private const int MaxHistoryStates = 20;

    private readonly Stack<HistoryEntry> _undoStack = new();
    private readonly Stack<HistoryEntry> _redoStack = new();

    public EditHistory(Bitmap initialBitmap, int initialNextCounterValue = 1)
    {
        PushWithLimit(_undoStack, new HistoryEntry((Bitmap)initialBitmap.Clone(), initialNextCounterValue));
    }

    public void Capture(Bitmap bitmap, int nextCounterValue)
    {
        PushWithLimit(_undoStack, new HistoryEntry((Bitmap)bitmap.Clone(), nextCounterValue));
        DisposeAll(_redoStack);
    }

    public (Bitmap Bitmap, int NextCounterValue)? Undo(Bitmap currentBitmap, int currentNextCounterValue)
    {
        if (_undoStack.Count <= 1)
        {
            return null;
        }

        PushWithLimit(_redoStack, new HistoryEntry((Bitmap)currentBitmap.Clone(), currentNextCounterValue));
        return TakeSnapshot(_undoStack);
    }

    public (Bitmap Bitmap, int NextCounterValue)? Redo(Bitmap currentBitmap, int currentNextCounterValue)
    {
        if (_redoStack.Count == 0)
        {
            return null;
        }

        PushWithLimit(_undoStack, new HistoryEntry((Bitmap)currentBitmap.Clone(), currentNextCounterValue));
        return TakeSnapshot(_redoStack);
    }

    public void Dispose()
    {
        DisposeAll(_undoStack);
        DisposeAll(_redoStack);
    }

    private static void PushWithLimit(Stack<HistoryEntry> stack, HistoryEntry entry)
    {
        stack.Push(entry);
        TrimOldestEntries(stack, MaxHistoryStates);
    }

    private static void TrimOldestEntries(Stack<HistoryEntry> stack, int maxStates)
    {
        if (stack.Count <= maxStates)
        {
            return;
        }

        var entries = stack.ToArray();

        for (var i = maxStates; i < entries.Length; i++)
        {
            entries[i].Bitmap.Dispose();
        }

        stack.Clear();
        for (var i = maxStates - 1; i >= 0; i--)
        {
            stack.Push(entries[i]);
        }
    }

    private static (Bitmap Bitmap, int NextCounterValue) TakeSnapshot(Stack<HistoryEntry> stack)
    {
        var snapshot = stack.Pop();
        try
        {
            return ((Bitmap)snapshot.Bitmap.Clone(), snapshot.NextCounterValue);
        }
        finally
        {
            snapshot.Bitmap.Dispose();
        }
    }

    private static void DisposeAll(Stack<HistoryEntry> stack)
    {
        while (stack.TryPop(out var entry))
        {
            entry.Bitmap.Dispose();
        }
    }

    private sealed record HistoryEntry(Bitmap Bitmap, int NextCounterValue);
}
