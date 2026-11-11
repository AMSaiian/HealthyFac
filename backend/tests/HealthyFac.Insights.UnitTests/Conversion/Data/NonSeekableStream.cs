namespace HealthyFac.Insights.UnitTests.Conversion.Data;

/// <summary>
/// A readable stream that cannot report a length or seek, like a blob download. A copy that needs the length fails.
/// </summary>
/// <param name="content">The bytes to serve.</param>
public sealed class NonSeekableStream(byte[] content) : Stream
{
    private readonly MemoryStream _inner = new(content);

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

    public override int Read(Span<byte> buffer) => _inner.Read(buffer);

    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        _inner.ReadAsync(buffer, cancellationToken);

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
