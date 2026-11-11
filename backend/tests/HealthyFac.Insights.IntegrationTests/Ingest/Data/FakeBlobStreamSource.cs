using System.Text;
using HealthyFac.Insights.Application.Ingest.Processing;

namespace HealthyFac.Insights.IntegrationTests.Ingest.Data;

/// <summary>
/// Serves a small fixed content for every link and counts the opens.
/// </summary>
public sealed class FakeBlobStreamSource : IBlobStreamSource
{
    private const string Content = "file-content";

    private int _openCount;

    public int OpenCount => _openCount;

    public void Reset() => Interlocked.Exchange(ref _openCount, value: 0);

    public Task<Stream> OpenAsync(string url, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _openCount);

        return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(Content)));
    }
}
