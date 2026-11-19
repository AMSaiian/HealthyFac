using HealthyFac.Insights.Application.Ingest.Processing.Failures;

namespace HealthyFac.Insights.Application.Agents.Tools.Invocation;

/// <summary>
/// Remembers what went wrong with the data of a run. A read tool that failed or was rejected means that the run lacks
/// evidence, so a write that needs complete evidence (the close of a case) refuses. A downstream outage means that the
/// run cannot finish: the function-calling loop of the model library turns a thrown exception into a tool result for
/// the model, so the runner reads <see cref="Outage"/> after the loop and ends the run as unavailable.
/// </summary>
public sealed class ReadFailures
{
    public bool HasFailed { get; private set; }

    /// <summary>The first outage of a tool call, or <see langword="null"/> when no tool call met one.</summary>
    public ProviderUnavailableException? Outage { get; private set; }

    public void Record() => HasFailed = true;

    public void RecordOutage(ProviderUnavailableException exception)
    {
        HasFailed = true;
        Outage ??= exception;
    }
}
