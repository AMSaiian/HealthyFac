using HealthyFac.Insights.Application.Agents.Tools.Reads;
using HealthyFac.Insights.Application.Extraction.Proposals;

namespace HealthyFac.Insights.Application.Agents.Tools.Invocation;

/// <summary>
/// The values of a run that every tool call uses. They come from the case, never from the model.
/// </summary>
public sealed record ToolContext
{
    public required Guid CompanyId { get; init; }

    /// <summary>The machine of the case. An extraction run of a company file has none.</summary>
    public Guid? MachineId { get; init; }

    public required Guid RunId { get; init; }

    public required ToolCallRecorder Recorder { get; init; }

    /// <summary>
    /// Tells whether a read tool of the run failed. One instance serves the whole run, so a later tool call sees a
    /// failure of an earlier one.
    /// </summary>
    public ReadFailures ReadFailures { get; init; } = new();

    /// <summary>The case that the run works on. An extraction run has none.</summary>
    public Guid? CaseId { get; init; }

    /// <summary>
    /// Remembers the manual passages that the run already returned, so a later search can point to them.
    /// </summary>
    public ManualPassageLog ManualPassages { get; init; } = new();

    /// <summary>Counts the failed calls in a row. A run without a limit has none.</summary>
    public FailureStreak? Streak { get; init; }

    /// <summary>The proposals of an extraction run. A planner run has none.</summary>
    public ExtractionBuffer? Extraction { get; init; }

    /// <summary>
    /// Reads the machine of the case.
    /// </summary>
    /// <returns>The machine ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the run has no machine.</exception>
    public Guid RequireMachineId() => MachineId ?? throw new InvalidOperationException(AgentMessages.RunHasNoMachine);

    /// <summary>
    /// Reads the case of the run.
    /// </summary>
    /// <returns>The case ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the run has no case.</exception>
    public Guid RequireCaseId() => CaseId ?? throw new InvalidOperationException(AgentMessages.RunHasNoCase);

    /// <summary>
    /// Renews the lease of the run. The tool function calls it after each tool call. The default does nothing.
    /// </summary>
    public Func<CancellationToken, Task> RenewLease { get; init; } = _ => Task.CompletedTask;

    /// <summary>
    /// Returns the ID that the model gave the current call. The default makes a new ID.
    /// </summary>
    public Func<string> CurrentCallId { get; init; } = () => Guid.NewGuid().ToString(format: "N");

    /// <summary>
    /// Ends the function loop after the current call. The default does nothing.
    /// </summary>
    public Action EndLoop { get; init; } = () => { };
}
