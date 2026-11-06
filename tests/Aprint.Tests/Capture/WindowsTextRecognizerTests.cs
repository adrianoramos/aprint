using System.Reflection;
using Aprint.Features.Capture;

namespace Aprint.Tests.Capture;

public sealed class WindowsTextRecognizerTests
{
    [Fact]
    public async Task DetectRegionsAsyncHonorsCancellationBeforeStartingOcr()
    {
        using var bitmap = new Bitmap(2, 2);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => WindowsTextRegionDetector.DetectRegionsAsync(bitmap, cancellation.Token));
    }

    [Fact]
    public async Task CancelledQueuedDetectionDoesNotCreateSource()
    {
        var gate = GetDetectionSemaphore();
        await gate.WaitAsync();
        using var cancellation = new CancellationTokenSource();
        var sourceCount = 0;
        try
        {
            var detection = WindowsTextRegionDetector.DetectRegionsAsync(() =>
            {
                sourceCount++;
                return new Bitmap(2, 2);
            }, cancellation.Token);

            Assert.False(detection.IsCompleted);
            Assert.Equal(0, sourceCount);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => detection);
            Assert.Equal(0, sourceCount);
        }
        finally
        {
            gate.Release();
        }
    }

    [Fact]
    public async Task SourceCreationFailureReleasesCapacity()
    {
        var failure = new InvalidOperationException("Source creation failed.");
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => WindowsTextRegionDetector.DetectRegionsAsync(() => throw failure));
        Assert.Same(failure, error);

        var gate = GetDetectionSemaphore();
        Assert.True(await gate.WaitAsync(TimeSpan.FromSeconds(5)));
        gate.Release();
    }

    [Fact]
    public async Task CancellationAfterSourceCreationDisposesOwnedBitmap()
    {
        using var cancellation = new CancellationTokenSource();
        using var source = new Bitmap(2, 2);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => WindowsTextRegionDetector.DetectRegionsAsync(() =>
            {
                cancellation.Cancel();
                return source;
            }, cancellation.Token));

        Assert.Throws<ArgumentException>(() => source.GetPixel(0, 0));
        var gate = GetDetectionSemaphore();
        Assert.True(await gate.WaitAsync(TimeSpan.FromSeconds(5)));
        gate.Release();
    }

    private static SemaphoreSlim GetDetectionSemaphore() => (SemaphoreSlim)typeof(WindowsTextRegionDetector)
        .GetField("DetectionSemaphore", BindingFlags.Static | BindingFlags.NonPublic)!
        .GetValue(null)!;
}