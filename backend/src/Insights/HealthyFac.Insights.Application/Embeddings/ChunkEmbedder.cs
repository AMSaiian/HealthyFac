using HealthyFac.Insights.Application.Ingest.Processing;
using Microsoft.Extensions.AI;

namespace HealthyFac.Insights.Application.Embeddings;

/// <summary>
/// Embeds the text of chunks. Each call embeds one text, and at most <see cref="ChunkEmbeddingLimits.Parallelism"/>
/// calls run at the same time. Every call is one retry unit of <see cref="IProviderCalls"/>, so one transient error
/// repeats one text, never the whole list. Every vector is scaled to length 1.
/// </summary>
/// <remarks>
/// The task type of the model (retrieval of documents) is a provider option, so the generator that Infrastructure
/// registers sets it. This class sets only the size of the vector.
/// </remarks>
/// <param name="generator">Creates the embeddings.</param>
/// <param name="limits">Holds the vector size and the number of parallel calls.</param>
/// <param name="providers">Runs each call with the retries.</param>
public sealed class ChunkEmbedder(
    IEmbeddingGenerator<string, Embedding<float>> generator,
    ChunkEmbeddingLimits limits,
    IProviderCalls providers
)
{
    private const string WrongSizeFormat = "The embedding has {0} values. The expected size is {1}.";
    private const string NoEmbeddingMessage = "The embedding service returned no embedding.";

    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator = generator;
    private readonly ChunkEmbeddingLimits _limits = limits;
    private readonly IProviderCalls _providers = providers;

    /// <summary>
    /// Embeds the texts.
    /// </summary>
    /// <param name="texts">The texts to embed.</param>
    /// <param name="cancellationToken">The token that cancels the calls.</param>
    /// <returns>One vector of length 1 for each text, in the order of <paramref name="texts"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the service returns no embedding, a vector of another size than <see cref="ChunkEmbeddingLimits"/>,
    /// or a vector with only zero values.
    /// </exception>
    public async Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken
    )
    {
        using var gate = new SemaphoreSlim(_limits.Parallelism);
        var tasks = texts.Select(text => EmbedOneAsync(text, gate, cancellationToken)).ToList();

        return await Task.WhenAll(tasks);
    }

    private async Task<float[]> EmbedOneAsync(string text, SemaphoreSlim gate, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var options = new EmbeddingGenerationOptions { Dimensions = _limits.Dimensions };
            var result = await _providers.ExecuteAsync(
                token => _generator.GenerateAsync([text], options, token),
                cancellationToken);
            var embedding = result.FirstOrDefault() ?? throw new InvalidOperationException(NoEmbeddingMessage);
            var values = embedding.Vector;
            if (values.Length != _limits.Dimensions)
            {
                throw new InvalidOperationException(string.Format(WrongSizeFormat, values.Length, _limits.Dimensions));
            }

            return VectorNormalization.ToUnitLength(values.Span);
        }
        finally
        {
            gate.Release();
        }
    }
}
