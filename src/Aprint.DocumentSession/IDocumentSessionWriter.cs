namespace Aprint.DocumentSession;

public interface IDocumentSessionWriter : IDisposable
{
    int ImageCount { get; }

    Task AddImageAsync(ReadOnlyMemory<byte> pngBytes, CancellationToken cancellationToken = default);

    string? Complete();
}