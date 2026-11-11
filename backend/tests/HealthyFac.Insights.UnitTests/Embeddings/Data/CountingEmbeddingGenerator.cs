using Microsoft.Extensions.AI;

namespace HealthyFac.Insights.UnitTests.Embeddings.Data;

/// <summary>
/// A fake embedding generator that counts the calls that run at the same time and records the options of each call.
/// Each vector holds the position of the text in the first value and 1 in all others, so a test can check the order.
/// </summary>
public sealed class CountingEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private int _inFlight;
    private int _maxInFlight;
    private int _callCount;
    private int _isFailureSpent;

    public int VectorSize { get; set; } = EmbeddingTestData.Dimensions;

    public TimeSpan Delay { get; set; } = TimeSpan.FromMilliseconds(value: 30);

    public int MaxInFlight => _maxInFlight;

    public int CallCount => _callCount;

    /// <summary>The text whose first call answers 429, as one transient provider error.</summary>
    public string? TransientFailureText { get; set; }

    public List<EmbeddingGenerationOptions?> Options { get; } = [];

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        lock (Options)
        {
            Options.Add(options);
        }

        Interlocked.Increment(ref _callCount);
        if (TransientFailureText is not null
            && values.Contains(TransientFailureText)
            && Interlocked.Exchange(ref _isFailureSpent, value: 1) == 0)
        {
            throw new HttpRequestException(message: "Quota", inner: null, System.Net.HttpStatusCode.TooManyRequests);
        }

        var current = Interlocked.Increment(ref _inFlight);
        InterlockedMax(ref _maxInFlight, current);
        try
        {
            await Task.Delay(Delay, cancellationToken);

            return new GeneratedEmbeddings<Embedding<float>>(
                values.Select(value => new Embedding<float>(CreateVector(value))).ToList());
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int seen;
        do
        {
            seen = target;
        }
        while (value > seen && Interlocked.CompareExchange(ref target, value, seen) != seen);
    }

    private float[] CreateVector(string text)
    {
        var vector = Enumerable.Repeat(element: 1f, VectorSize).ToArray();
        vector[0] = int.Parse(text) + 2f;

        return vector;
    }
}
