using HealthyFac.Insights.Application.Extraction.Proposals;

namespace HealthyFac.Insights.Application.Extraction.Runs;

/// <summary>
/// The state of one extraction run: the proposals that the tools collect and the log of the run.
/// </summary>
public sealed record ExtractionSession
{
    /// <summary>Collects the proposals.</summary>
    public required ExtractionBuffer Buffer { get; init; }

    /// <summary>Keeps the run and its tool calls until the attempt ends.</summary>
    public required ExtractionRunLog Log { get; init; }
}
