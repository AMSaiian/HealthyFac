using System.Net;

namespace HealthyFac.Gateway.IntegrationTests.Proxy.Data;

/// <summary>
/// An HTTP body of zero bytes with a declared length. It streams the bytes, so a test sends a body of hundreds of
/// megabytes without holding it in memory.
/// </summary>
/// <param name="length">The number of bytes to send.</param>
public sealed class ZeroBodyContent(long length) : HttpContent
{
    private const int ChunkBytes = 81920;

    private readonly long _length = length;

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
        SerializeToStreamAsync(stream, context, CancellationToken.None);

    protected override async Task SerializeToStreamAsync(
        Stream stream,
        TransportContext? context,
        CancellationToken cancellationToken
    )
    {
        var chunk = new byte[ChunkBytes];
        var remaining = _length;
        while (remaining > 0)
        {
            var size = (int)Math.Min(ChunkBytes, remaining);
            await stream.WriteAsync(chunk.AsMemory(start: 0, size), cancellationToken);
            remaining -= size;
        }
    }

    protected override bool TryComputeLength(out long computedLength)
    {
        computedLength = _length;

        return true;
    }
}
