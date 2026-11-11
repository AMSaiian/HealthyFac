using Microsoft.Extensions.AI;

namespace HealthyFac.Insights.IntegrationTests.Ingest.Data;

/// <summary>
/// Returns a vector of the size that the options ask for, without a call to Google. A test can hold the calls with a
/// hook, for example to make a cancel arrive between two stages.
/// </summary>
public sealed class FakeEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private const int DefaultDimensions = 1536;

    private int _callCount;
    private int _isFailureSpent;

    /// <summary>Runs before each call answers. A test uses it to hold the pipeline at this stage.</summary>
    public Func<CancellationToken, Task>? Before { get; set; }

    /// <summary>When set, every call throws it.</summary>
    public Exception? Failure { get; set; }

    public int CallCount => _callCount;

    /// <summary>The text whose first call answers 429, as one transient provider error.</summary>
    public string? TransientFailureText { get; set; }

    public void Reset()
    {
        Before = null;
        Failure = null;
        TransientFailureText = null;
        Interlocked.Exchange(ref _callCount, value: 0);
        Interlocked.Exchange(ref _isFailureSpent, value: 0);
    }

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        if (Before is not null)
        {
            await Before(cancellationToken);
        }

        Interlocked.Increment(ref _callCount);
        if (Failure is not null)
        {
            throw Failure;
        }

        if (TransientFailureText is not null
            && values.Contains(TransientFailureText)
            && Interlocked.Exchange(ref _isFailureSpent, value: 1) == 0)
        {
            throw new HttpRequestException(message: "Quota", inner: null, System.Net.HttpStatusCode.TooManyRequests);
        }

        var size = options?.Dimensions ?? DefaultDimensions;
        var embeddings = values
            .Select(_ => new Embedding<float>(Enumerable.Repeat(element: 1f, size).ToArray()))
            .ToList();

        return new GeneratedEmbeddings<Embedding<float>>(embeddings);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
