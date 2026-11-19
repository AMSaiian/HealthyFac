namespace HealthyFac.Insights.Application.Extraction.Runs;

/// <summary>
/// Keeps the <see cref="ExtractionRunLog"/> of one attempt for the whole run. The pipeline sets it when the extraction
/// starts. The ingest handler reads it on every way out of the run, so each outcome after the start stores the
/// <c>extractor</c> run with its tool calls in the save of that outcome.
/// </summary>
public sealed class ExtractionRunHolder
{
    /// <summary>The log of the extraction, or <see langword="null"/> when the run did not reach it.</summary>
    public ExtractionRunLog? Log { get; set; }
}
