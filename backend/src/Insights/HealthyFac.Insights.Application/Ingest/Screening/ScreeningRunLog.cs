using HealthyFac.Insights.Domain.Agents.Runs;

namespace HealthyFac.Insights.Application.Ingest.Screening;

/// <summary>
/// What the screening of an attempt did, kept in memory until the attempt ends. The ingest handler stores it as one
/// <c>screener</c> run in the same save as the outcome of the attempt, so the run and the outcome never differ and the
/// screening adds no save of its own.
/// </summary>
public record ScreeningRunLog
{
    public required string Model { get; init; }

    public required string PromptVersion { get; init; }

    public required string PromptHash { get; init; }

    public required DateTime StartedAt { get; init; }

    public required DateTime FinishedAt { get; init; }

    public AgentRunVerdict? Verdict { get; init; }

    public string? VerdictReason { get; init; }

    public AgentRunErrorCode? ErrorCode { get; init; }

    public required int InputTokens { get; init; }

    public required int OutputTokens { get; init; }

    /// <summary>
    /// Builds the run in its final state: <c>completed</c> with the verdict, or <c>failed</c> with the error code.
    /// </summary>
    /// <param name="attemptId">The ingest attempt that the run belongs to.</param>
    /// <returns>The new run, not yet stored.</returns>
    public AgentRun ToRun(Guid attemptId)
    {
        var run = new AgentRun(AgentRole.Screener, caseId: null, attemptId, StartedAt);
        run.Claim(StartedAt, StartedAt);
        run.UseModel(Model, PromptVersion, PromptHash);

        if (ErrorCode is { } errorCode)
        {
            run.Fail(FinishedAt, errorCode, InputTokens, OutputTokens);
        }
        else
        {
            run.Complete(FinishedAt, Verdict, VerdictReason, InputTokens, OutputTokens);
        }

        return run;
    }
}
