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
}