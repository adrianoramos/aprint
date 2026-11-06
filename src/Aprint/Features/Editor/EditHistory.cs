namespace Aprint.Features.Editor;

public sealed class BitmapSnapshotBudget
{
    public const long DefaultMaximumBytes = 256L * 1024 * 1024;

    public static BitmapSnapshotBudget Shared { get; } = new(DefaultMaximumBytes);

    private readonly object _sync = new();
    private readonly LinkedList<EditHistory.HistoryEntry> _entries = new();
    private long _retainedBytes;
    private long _reservedBytes;

    public BitmapSnapshotBudget(long maximumBytes = DefaultMaximumBytes)
    {
        if (maximumBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        }

        MaximumBytes = maximumBytes;
    }

    public long MaximumBytes { get; }

    public long RetainedBytes
    {
        get
        {
            lock (_sync)
            {
                return _retainedBytes;
            }
        }
    }

    internal bool TryReserve(long memoryBytes)
    {
        lock (_sync)
        {
            if (memoryBytes > MaximumBytes)
            {
                return false;
            }

            while (_retainedBytes + memoryBytes > MaximumBytes && _entries.First is { } oldest)
            {
                var entry = oldest.Value;
                _entries.RemoveFirst();
                _retainedBytes -= entry.MemoryBytes;
                entry.BudgetNode = null;
                entry.Owner.RemoveEvictedSnapshot(entry);
            }

            if (_retainedBytes + memoryBytes > MaximumBytes)
            {
                return false;
            }

            _retainedBytes += memoryBytes;
            _reservedBytes += memoryBytes;
            return true;
        }
    }

    internal void Commit(EditHistory.HistoryEntry entry)
    {
        lock (_sync)
        {
            entry.BudgetNode = _entries.AddLast(entry);
            _reservedBytes -= entry.MemoryBytes;
        }
    }

    internal void ReleaseReservation(long memoryBytes)
    {
        lock (_sync)
        {
            _reservedBytes -= memoryBytes;
            _retainedBytes -= memoryBytes;
        }
    }

    internal void Release(EditHistory.HistoryEntry entry)
    {
        lock (_sync)
        {
            if (entry.BudgetNode is null)
            {
                return;
            }

            _entries.Remove(entry.BudgetNode);
            entry.BudgetNode = null;
            _retainedBytes -= entry.MemoryBytes;
        }
    }
}

public sealed class EditHistory : IDisposable
{
    private const int MaxHistoryStates = 20;

    private readonly LinkedList<HistoryEntry> _undoStack = new();
    private readonly LinkedList<HistoryEntry> _redoStack = new();
    private readonly BitmapSnapshotBudget _memoryBudget;
    private readonly Func<Bitmap, Rectangle?, Bitmap> _cloneSnapshot;

    public bool CanUndo => _undoStack.Count > 0;

    public bool CanRedo => _redoStack.Count > 0;

    public EditHistory(Bitmap initialBitmap, int initialNextCounterValue = 1, BitmapSnapshotBudget? memoryBudget = null)
        : this(initialBitmap, initialNextCounterValue, memoryBudget, static (bitmap, region) =>
            region is { } bounds ? bitmap.Clone(bounds, bitmap.PixelFormat) : (Bitmap)bitmap.Clone())
    {
    }

    internal EditHistory(
        Bitmap initialBitmap,
        int initialNextCounterValue,
        BitmapSnapshotBudget? memoryBudget,
        Func<Bitmap, Rectangle?, Bitmap> cloneSnapshot)
    {
        ArgumentNullException.ThrowIfNull(initialBitmap);
        ArgumentNullException.ThrowIfNull(cloneSnapshot);
        _memoryBudget = memoryBudget ?? BitmapSnapshotBudget.Shared;
        _cloneSnapshot = cloneSnapshot;
    }

    public void Capture(Bitmap bitmap, int nextCounterValue)
    {
        var captured = PushSnapshot(_undoStack, bitmap, nextCounterValue);
        DisposeAll(_redoStack);
        if (!captured)
        {
            DisposeAll(_undoStack);
        }

        TrimToHistoryLimit();
    }

    public void CaptureRegion(Bitmap bitmap, Rectangle changedRegion, int nextCounterValue) =>
        CaptureRegions(bitmap, [changedRegion], nextCounterValue);

    public void CaptureRegions(Bitmap bitmap, IEnumerable<Rectangle> changedRegions, int nextCounterValue)
    {
        var regions = NormalizeRegions(bitmap, changedRegions);
        if (regions.Count == 0)
        {
            return;
        }

        var captured = PushRegionSnapshots(_undoStack, bitmap, regions, nextCounterValue);
        DisposeAll(_redoStack);
        if (!captured)
        {
            DisposeAll(_undoStack);
        }

        TrimToHistoryLimit();
    }

    public (Bitmap Bitmap, int NextCounterValue)? Undo(Bitmap currentBitmap, int currentNextCounterValue)
        => Restore(_undoStack, _redoStack, currentBitmap, currentNextCounterValue);

    public (Bitmap Bitmap, int NextCounterValue)? Redo(Bitmap currentBitmap, int currentNextCounterValue)
        => Restore(_redoStack, _undoStack, currentBitmap, currentNextCounterValue);

    private (Bitmap Bitmap, int NextCounterValue)? Restore(
        LinkedList<HistoryEntry> sourceStack,
        LinkedList<HistoryEntry> destinationStack,
        Bitmap currentBitmap,
        int currentNextCounterValue)
    {
        if (sourceStack.Count == 0)
        {
            return null;
        }

        var sourceNode = sourceStack.Last!;
        var snapshot = TakeSnapshot(sourceStack);
        var ownershipTransferred = false;
        var canRestoreEntry = true;
        try
        {
            if (snapshot.FullBitmap is { } fullBitmap)
            {
                PushSnapshot(destinationStack, currentBitmap, currentNextCounterValue);
                canRestoreEntry = false;
                TrimToHistoryLimit();
                ownershipTransferred = true;
                return (fullBitmap, snapshot.NextCounterValue);
            }

            var regions = snapshot.RegionSnapshots!;
            PushRegionSnapshots(destinationStack, currentBitmap, regions.Select(region => region.Bounds), currentNextCounterValue);
            canRestoreEntry = false;
            ApplyRegionSnapshots(currentBitmap, regions);
            TrimToHistoryLimit();
            return (currentBitmap, snapshot.NextCounterValue);
        }
        catch
        {
            if (canRestoreEntry)
            {
                ownershipTransferred = TryRestoreEntry(sourceStack, sourceNode);
            }

            throw;
        }
        finally
        {
            if (!ownershipTransferred)
            {
                DisposeEntryBitmaps(snapshot);
            }
        }
    }

    private bool TryRestoreEntry(LinkedList<HistoryEntry> stack, LinkedListNode<HistoryEntry> node)
    {
        var entry = node.Value;
        if (!_memoryBudget.TryReserve(entry.MemoryBytes))
        {
            return false;
        }

        var committed = false;
        try
        {
            stack.AddLast(node);
            entry.StackNode = node;
            _memoryBudget.Commit(entry);
            committed = true;
            return true;
        }
        finally
        {
            if (!committed)
            {
                if (node.List is not null)
                {
                    stack.Remove(node);
                    entry.StackNode = null;
                }

                _memoryBudget.ReleaseReservation(entry.MemoryBytes);
            }
        }
    }

    public void Dispose()
    {
        DisposeAll(_undoStack);
        DisposeAll(_redoStack);
    }

    private bool PushSnapshot(LinkedList<HistoryEntry> stack, Bitmap bitmap, int nextCounterValue)
    {
        var memoryBytes = EstimateMemoryBytes(bitmap);
        if (!_memoryBudget.TryReserve(memoryBytes))
        {
            return false;
        }

        Bitmap? snapshot = null;
        HistoryEntry? entry = null;
        var committed = false;
        try
        {
            snapshot = _cloneSnapshot(bitmap, null);
            entry = new HistoryEntry(this, stack, snapshot, null, nextCounterValue, memoryBytes);
            entry.StackNode = stack.AddLast(entry);
            _memoryBudget.Commit(entry);
            committed = true;
            return true;
        }
        finally
        {
            if (!committed)
            {
                if (entry?.StackNode is { } node)
                {
                    stack.Remove(node);
                    entry.StackNode = null;
                }

                _memoryBudget.ReleaseReservation(memoryBytes);
                snapshot?.Dispose();
            }
        }
    }

    private bool PushRegionSnapshots(
        LinkedList<HistoryEntry> stack,
        Bitmap bitmap,
        IEnumerable<Rectangle> changedRegions,
        int nextCounterValue)
    {
        var regions = NormalizeRegions(bitmap, changedRegions);
        if (regions.Count == 0)
        {
            return false;
        }

        var memoryBytes = regions.Sum(region => EstimateMemoryBytes(bitmap, region));
        if (!_memoryBudget.TryReserve(memoryBytes))
        {
            return false;
        }

        List<RegionSnapshot>? snapshots = null;
        Bitmap? pendingSnapshot = null;
        HistoryEntry? entry = null;
        var committed = false;
        try
        {
            snapshots = new List<RegionSnapshot>(regions.Count);
            foreach (var region in regions)
            {
                pendingSnapshot = _cloneSnapshot(bitmap, region);
                snapshots.Add(new RegionSnapshot(region, pendingSnapshot));
                pendingSnapshot = null;
            }

            entry = new HistoryEntry(this, stack, null, snapshots, nextCounterValue, memoryBytes);
            entry.StackNode = stack.AddLast(entry);
            _memoryBudget.Commit(entry);
            committed = true;
            return true;
        }
        finally
        {
            if (!committed)
            {
                if (entry?.StackNode is { } node)
                {
                    stack.Remove(node);
                    entry.StackNode = null;
                }

                _memoryBudget.ReleaseReservation(memoryBytes);
                pendingSnapshot?.Dispose();
                if (snapshots is not null)
                {
                    DisposeRegionSnapshots(snapshots);
                }
            }
        }
    }

    private void TrimToHistoryLimit()
    {
        while (GetTotalStateCount() > MaxHistoryStates)
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
        RemoveSnapshot(stack.First!.Value);
    }

    private int GetTotalStateCount() => _undoStack.Count + _redoStack.Count;

    private static long EstimateMemoryBytes(Bitmap bitmap, Rectangle? region = null)
    {
        var bitsPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat);
        var bytesPerPixel = Math.Max(1, (bitsPerPixel + 7) / 8);
        var size = region?.Size ?? bitmap.Size;
        return (long)size.Width * size.Height * bytesPerPixel;
    }

    private static List<Rectangle> NormalizeRegions(Bitmap bitmap, IEnumerable<Rectangle> changedRegions)
    {
        var bitmapBounds = new Rectangle(Point.Empty, bitmap.Size);
        var regions = new List<Rectangle>();
        foreach (var changedRegion in changedRegions)
        {
            var region = Rectangle.Intersect(bitmapBounds, changedRegion);
            if (region.IsEmpty)
            {
                continue;
            }

            for (var index = 0; index < regions.Count;)
            {
                if (regions[index].IntersectsWith(region))
                {
                    region = Rectangle.Union(region, regions[index]);
                    regions.RemoveAt(index);
                    index = 0;
                }
                else
                {
                    index++;
                }
            }

            regions.Add(region);
        }

        return regions;
    }

    private static void ApplyRegionSnapshots(Bitmap bitmap, IReadOnlyList<RegionSnapshot> snapshots)
    {
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
        foreach (var snapshot in snapshots)
        {
            graphics.DrawImageUnscaled(snapshot.Bitmap, snapshot.Bounds.Location);
        }
    }

    private static void DisposeRegionSnapshots(IEnumerable<RegionSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            snapshot.Bitmap.Dispose();
        }
    }

    private HistoryEntry TakeSnapshot(LinkedList<HistoryEntry> stack)
    {
        var node = stack.Last!;
        var snapshot = node.Value;
        stack.RemoveLast();
        snapshot.StackNode = null;
        _memoryBudget.Release(snapshot);
        return snapshot;
    }

    private void DisposeAll(LinkedList<HistoryEntry> stack)
    {
        while (stack.Last is { } node)
        {
            RemoveSnapshot(node.Value);
        }
    }

    private void RemoveSnapshot(HistoryEntry entry)
    {
        if (entry.StackNode is not null)
        {
            entry.Stack.Remove(entry.StackNode);
            entry.StackNode = null;
        }

        _memoryBudget.Release(entry);
        DisposeEntryBitmaps(entry);
    }

    internal void RemoveEvictedSnapshot(HistoryEntry entry)
    {
        if (entry.StackNode is not null)
        {
            entry.Stack.Remove(entry.StackNode);
            entry.StackNode = null;
        }

        DisposeEntryBitmaps(entry);
    }

    private static void DisposeEntryBitmaps(HistoryEntry entry)
    {
        entry.FullBitmap?.Dispose();
        if (entry.RegionSnapshots is not null)
        {
            DisposeRegionSnapshots(entry.RegionSnapshots);
        }
    }

    internal sealed class HistoryEntry(
        EditHistory owner,
        LinkedList<HistoryEntry> stack,
        Bitmap? fullBitmap,
        List<RegionSnapshot>? regionSnapshots,
        int nextCounterValue,
        long memoryBytes)
    {
        public EditHistory Owner { get; } = owner;

        public LinkedList<HistoryEntry> Stack { get; } = stack;

        public Bitmap? FullBitmap { get; } = fullBitmap;

        public List<RegionSnapshot>? RegionSnapshots { get; } = regionSnapshots;

        public int NextCounterValue { get; } = nextCounterValue;

        public long MemoryBytes { get; } = memoryBytes;

        public LinkedListNode<HistoryEntry>? StackNode { get; set; }

        public LinkedListNode<HistoryEntry>? BudgetNode { get; set; }
    }

    internal sealed record RegionSnapshot(Rectangle Bounds, Bitmap Bitmap);
}
