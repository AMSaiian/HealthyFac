namespace HealthyFac.Insights.Application.Ingest.Screening;

/// <summary>
/// Keeps the <see cref="ScreeningRunLog"/> of one attempt for the whole run. The pipeline sets it when the screening
/// ends (safe, unsafe, or failed). The ingest handler reads it on every way out of the run, so each outcome after the
/// screening stores the <c>screener</c> run with its tokens in the save of that outcome.
/// </summary>
public sealed class ScreeningRunHolder
{
    /// <summary>The log of the screening, or <see langword="null"/> when the run did not reach the screening.</summary>
    public ScreeningRunLog? Log { get; set; }
}
