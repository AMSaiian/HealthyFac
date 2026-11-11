namespace HealthyFac.Insights.Infrastructure.Ingest;

/// <summary>
/// Wraps the body stream of an HTTP response and disposes the response with it, so the connection ends when the
/// reader ends.
/// </summary>
/// <param name="inner">The body stream.</param>
/// <param name="response">The response to dispose with the stream.</param>
public sealed class ResponseBodyStream(Stream inner, HttpResponseMessage response) : Stream
{
    private readonly Stream _inner = inner;
    private readonly HttpResponseMessage _response = response;

    public override bool CanRead => _inner.CanRead;

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

    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            _inner.Dispose();
            _response.Dispose();
        }

        base.Dispose(isDisposing);
    }
}
