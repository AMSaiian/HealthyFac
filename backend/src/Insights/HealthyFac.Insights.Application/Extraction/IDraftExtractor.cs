using HealthyFac.Insights.Application.Extraction.Runs;

namespace HealthyFac.Insights.Application.Extraction;

/// <summary>
/// Extracts machines, metrics, statuses, and phases from the chunks of a document with a language model that calls
/// the extractor tools.
/// </summary>
public interface IDraftExtractor
{
    /// <summary>
    /// Runs the model with the extractor tools. The tools fill the buffer of the session, and the extractor logs the
    /// run and each tool call in the session.
    /// </summary>
    /// <param name="input">The chunks and the context.</param>
    /// <param name="session">The buffer of the proposals and the log of the run.</param>
    /// <param name="cancellationToken">The token that cancels the run.</param>
    /// <returns>A task that completes when the model ends the run.</returns>
    /// <exception cref="InvalidExtractionException">
    /// Thrown when the model calls the tools wrongly more often in a row than the limit allows.
    /// </exception>
    Task ExtractAsync(ExtractionInput input, ExtractionSession session, CancellationToken cancellationToken);
}
