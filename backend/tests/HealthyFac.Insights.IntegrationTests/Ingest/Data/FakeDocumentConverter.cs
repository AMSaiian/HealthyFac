using HealthyFac.Insights.Application.Conversion;

namespace HealthyFac.Insights.IntegrationTests.Ingest.Data;

/// <summary>
/// Converts nothing: it returns a small fixed document, or the result of the behavior that a test sets. It counts the
/// calls, so a test sees the retries.
/// </summary>
public sealed class FakeDocumentConverter : IDocumentConverter
{
    private int _callCount;

    public int CallCount => _callCount;

    /// <summary>Replaces the answer. The behavior gets the token of the run.</summary>
    public Func<CancellationToken, Task<ConvertedDocument>>? Behavior { get; set; }

    public static ConvertedDocument DefaultDocument { get; } = new()
    {
        PageCount = 2,
        Chunks =
        [
            new ConvertedChunk { Location = "page 1", Text = "Pump manual. Maximum pressure is 6 bar." },
            new ConvertedChunk { Location = "page 2", Text = "Alarm E12 means overheating." },
        ],
    };

    public void Reset()
    {
        Behavior = null;
        Interlocked.Exchange(ref _callCount, value: 0);
    }

    public Task<ConvertedDocument> ConvertAsync(
        Func<CancellationToken, Task<Stream>> openContent,
        string fileName,
        string extension,
        Func<CancellationToken, Task<bool>> isCancelled,
        CancellationToken cancellationToken
    )
    {
        Interlocked.Increment(ref _callCount);

        return Behavior is null ? Task.FromResult(DefaultDocument) : Behavior(cancellationToken);
    }
}
