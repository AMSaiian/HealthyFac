namespace HealthyFac.Document.ApiTests.Files.Data;

/// <summary>
/// A read-only stream of a fixed length that starts with a header and continues with zero bytes. A test sends a body
/// of hundreds of megabytes without holding it in memory.
/// </summary>
/// <param name="header">The first bytes of the stream.</param>
/// <param name="length">The length of the stream in bytes.</param>
public sealed class GeneratedStream(byte[] header, long length) : Stream
{
    private readonly byte[] _header = header;
    private readonly long _length = length;
    private long _position;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        var count = (int)Math.Min(buffer.Length, _length - _position);
        buffer[..count].Clear();

        for (var index = 0; index < count && _position + index < _header.Length; index++)
        {
            buffer[index] = _header[_position + index];
        }

        _position += count;

        return count;
    }

    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        ValueTask.FromResult(Read(buffer.Span));

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
