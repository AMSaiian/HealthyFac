using HealthyFac.Insights.Application.Conversion;
using HealthyFac.Insights.Application.Ingest.Processing;

namespace HealthyFac.Insights.Application.Ingest.Screening;

/// <summary>
/// Screens the chunks of a converted file before anything embeds or extracts them. A file with a chunk that tells a
/// reader (a person or a model) to ignore rules, to leak data, or to act outside the task is unsafe.
/// </summary>
public interface IChunkScreener
{
    /// <summary>
    /// Screens the chunks in batches, in order. The first unsafe chunk stops the screening, so no later batch is sent.
    /// The screener stores one <c>screener</c> run for the attempt, with its model, prompt, tokens, and result.
    /// </summary>
    /// <param name="attempt">The attempt that the run belongs to.</param>
    /// <param name="chunks">The chunks in reading order.</param>
    /// <param name="cancellationToken">The token that stops the screening.</param>
    /// <returns>The result: safe, or the location and the reason of the first unsafe chunk.</returns>
    /// <exception cref="ProviderUnavailableException">Thrown when the model stayed unavailable.</exception>
    Task<ScreeningResult> ScreenAsync(
        IngestAttemptSnapshot attempt,
        IReadOnlyList<ConvertedChunk> chunks,
        CancellationToken cancellationToken
    );
}
