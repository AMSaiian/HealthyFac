namespace HealthyFac.Insights.Application.Extraction;

/// <summary>
/// Extracts machines, metrics, and statuses from the chunks of a document with a language model.
/// </summary>
public interface IDraftExtractor
{
    /// <summary>
    /// Asks the model for the structured answer.
    /// </summary>
    /// <param name="input">The chunks and the context.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The machines that the document describes.</returns>
    /// <exception cref="InvalidExtractionException">
    /// Thrown when the model answers twice with a text that does not match the response schema.
    /// </exception>
    Task<ExtractionResult> ExtractAsync(ExtractionInput input, CancellationToken cancellationToken);
}
