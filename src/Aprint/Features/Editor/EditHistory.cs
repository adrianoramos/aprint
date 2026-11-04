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
            _reservedBytes -= entry.MemoryBytes;
            entry.BudgetNode = _entries.AddLast(entry);
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

    public bool CanUndo => _undoStack.Count > 0;

    public bool CanRedo => _redoStack.Count > 0;

    public EditHistory(Bitmap initialBitmap, int initialNextCounterValue = 1, BitmapSnapshotBudget? memoryBudget = null)
    {
        ArgumentNullException.ThrowIfNull(initialBitmap);
        _memoryBudget = memoryBudget ?? BitmapSnapshotBudget.Shared;
    }

    public void Capture(Bitmap bitmap, int nextCounterValue)
    {
        DisposeAll(_redoStack);
        if (!PushSnapshot(_undoStack, bitmap, nextCounterValue))
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

        DisposeAll(_redoStack);
        if (!PushRegionSnapshots(_undoStack, bitmap, regions, nextCounterValue))
        {
            DisposeAll(_undoStack);
        }

        TrimToHistoryLimit();
    }

    public (Bitmap Bitmap, int NextCounterValue)? Undo(Bitmap currentBitmap, int currentNextCounterValue)
    {
        if (_undoStack.Count == 0)
        {
            return null;
        }

        var snapshot = TakeSnapshot(_undoStack);
        if (snapshot.FullBitmap is { } fullBitmap)
        {
            PushSnapshot(_redoStack, currentBitmap, currentNextCounterValue);
            TrimToHistoryLimit();
            return (fullBitmap, snapshot.NextCounterValue);
        }

        var regions = snapshot.RegionSnapshots!;
        PushRegionSnapshots(_redoStack, currentBitmap, regions.Select(region => region.Bounds), currentNextCounterValue);
        ApplyRegionSnapshots(currentBitmap, regions);
        DisposeRegionSnapshots(regions);
        TrimToHistoryLimit();
        return (currentBitmap, snapshot.NextCounterValue);
    }

    public (Bitmap Bitmap, int NextCounterValue)? Redo(Bitmap currentBitmap, int currentNextCounterValue)
    {
        if (_redoStack.Count == 0)
        {
            return null;
        }

        var snapshot = TakeSnapshot(_redoStack);
        if (snapshot.FullBitmap is { } fullBitmap)
        {
            PushSnapshot(_undoStack, currentBitmap, currentNextCounterValue);
            TrimToHistoryLimit();
            return (fullBitmap, snapshot.NextCounterValue);
        }

        var regions = snapshot.RegionSnapshots!;
        PushRegionSnapshots(_undoStack, currentBitmap, regions.Select(region => region.Bounds), currentNextCounterValue);
        ApplyRegionSnapshots(currentBitmap, regions);
        DisposeRegionSnapshots(regions);
        TrimToHistoryLimit();
        return (currentBitmap, snapshot.NextCounterValue);
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
        try
        {
            snapshot = (Bitmap)bitmap.Clone();
            var entry = new HistoryEntry(this, stack, snapshot, null, nextCounterValue, memoryBytes);
            entry.StackNode = stack.AddLast(entry);
            _memoryBudget.Commit(entry);
            snapshot = null;
            return true;
        }
        finally
        {
            if (snapshot is not null)
            {
                snapshot.Dispose();
                _memoryBudget.ReleaseReservation(memoryBytes);
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

        var snapshots = new List<RegionSnapshot>(regions.Count);
        try
        {
            foreach (var region in regions)
            {
                snapshots.Add(new RegionSnapshot(region, (Bitmap)bitmap.Clone(region, bitmap.PixelFormat)));
            }

            var entry = new HistoryEntry(this, stack, null, snapshots, nextCounterValue, memoryBytes);
            entry.StackNode = stack.AddLast(entry);
            _memoryBudget.Commit(entry);
            snapshots = [];
            return true;
        }
        finally
        {
            if (snapshots.Count > 0)
            {
                DisposeRegionSnapshots(snapshots);
                _memoryBudget.ReleaseReservation(memoryBytes);
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
