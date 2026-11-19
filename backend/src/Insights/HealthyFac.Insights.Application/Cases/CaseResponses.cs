using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;

namespace HealthyFac.Insights.Application.Cases;

/// <summary>
/// One row of the case grid. <see cref="IsAiUnavailable"/> is true when the last run of the case is <c>failed</c> with
/// <c>providerUnavailable</c> or <c>unexpectedError</c>, or <c>interrupted</c>. A <c>caseChanged</c> failure is not
/// "AI unavailable", because a new triage run follows.
/// </summary>
public record CaseListItemResponse
{
    public required Guid Id { get; init; }

    public required Guid MachineId { get; init; }

    public required CaseState State { get; init; }

    public SignalSeverity? HighestOpenSeverity { get; init; }

    public required int SignalCount { get; init; }

    public AgentRunState? LastRunState { get; init; }

    public required bool IsAiUnavailable { get; init; }

    public required DateTime OpenedAt { get; init; }

    public DateTime? ClosedAt { get; init; }

    public string? CloseReason { get; init; }
}

/// <summary>The header of one case with its outcome. The signals and the runs have their own paged reads.</summary>
public record CaseDetailResponse : CaseListItemResponse
{
    public required CaseOutcomeResponse Outcome { get; init; }
}

/// <summary>
/// The result of a case. <see cref="Reason"/> is the final text of the last triage or planner run.
/// </summary>
public record CaseOutcomeResponse
{
    public required CaseOutcomeKind Kind { get; init; }

    public Guid? WorkOrderDraftId { get; init; }

    public Guid? WorkOrderId { get; init; }

    public string? Title { get; init; }

    public string? Reason { get; init; }
}

public record CaseSignalResponse
{
    public required Guid Id { get; init; }

    public required SignalRule Rule { get; init; }

    public required SignalSeverity Severity { get; init; }

    public required SignalState State { get; init; }

    public required SignalCoverage Coverage { get; init; }

    public string? MetricKey { get; init; }

    public string? StatusCode { get; init; }

    public double? Value { get; init; }

    public double? Limit { get; init; }

    public required DateTime OpenedAt { get; init; }

    public DateTime? ResolvedAt { get; init; }

    public Guid? WorkOrderDraftId { get; init; }

    public Guid? WorkOrderId { get; init; }
}

public record CaseRunResponse
{
    public required Guid Id { get; init; }

    public required AgentRole Role { get; init; }

    public required AgentRunState State { get; init; }

    public string? Model { get; init; }

    public string? PromptVersion { get; init; }

    public required int AttemptNumber { get; init; }

    public required int InputTokens { get; init; }

    public required int OutputTokens { get; init; }

    public AgentRunVerdict? Verdict { get; init; }

    public string? VerdictReason { get; init; }

    public AgentRunErrorCode? ErrorCode { get; init; }

    public AgentRunSkipReason? SkipReason { get; init; }

    public DateTime? StartedAt { get; init; }

    public DateTime? FinishedAt { get; init; }
}

/// <summary>The tool calls of one run, in the order of their sequence.</summary>
public record CaseToolCallsResponse
{
    public required IReadOnlyList<CaseToolCallResponse> ToolCalls { get; init; }
}

public record CaseToolCallResponse
{
    public required int Sequence { get; init; }

    public required string ToolName { get; init; }

    public required ToolCallOutcome Outcome { get; init; }

    public required string Arguments { get; init; }

    public required string Result { get; init; }

    public required int DurationMilliseconds { get; init; }
}
