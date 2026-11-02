using System.Net;

namespace HealthyFac.Shared.UnitTests.HttpFailures.Data;

/// <summary>
/// A content that fails when anyone reads it, so a test proves that a success reads no body.
/// </summary>
public sealed class ThrowingContent : HttpContent
{
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
        throw new InvalidOperationException("The content must not be read.");

    protected override bool TryComputeLength(out long length)
    {
        length = 0;

        return false;
    }
}
