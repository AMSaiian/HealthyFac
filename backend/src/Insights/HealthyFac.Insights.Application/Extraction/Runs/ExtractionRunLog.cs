using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.ToolCalls;

namespace HealthyFac.Insights.Application.Extraction.Runs;

/// <summary>
/// What the extraction of an attempt did, kept in memory until the attempt ends. The ingest handler stores the run
/// and its tool calls in the same save as the outcome of the attempt, as it does for the screening, so the log and the
/// outcome never differ and a retried extraction adds no row of its own.
/// </summary>
public sealed class ExtractionRunLog
{
    private readonly List<AgentToolCall> _calls = [];

    /// <summary>
    /// Starts the log with a running run.
    /// </summary>
    /// <param name="attemptId">The ingest attempt of the run.</param>
    /// <param name="startedAt">The start time.</param>
    public ExtractionRunLog(Guid attemptId, DateTime startedAt)
    {
        Run = new AgentRun(AgentRole.Extractor, caseId: null, attemptId, startedAt);
        Run.Claim(startedAt, startedAt);
        Recorder = new ToolCallRecorder(
            Run.Id,
            (createRow, _) =>
            {
                _calls.Add(createRow(_calls.Count + 1));

                return Task.CompletedTask;
            });
    }

    /// <summary>The run. It is final after <see cref="Complete"/> or <see cref="Fail"/>.</summary>
    public AgentRun Run { get; }

    /// <summary>Keeps each tool call in memory, in the order of the calls.</summary>
    public ToolCallRecorder Recorder { get; }

    /// <summary>The tool calls so far.</summary>
    public IReadOnlyList<AgentToolCall> Calls => _calls;

    /// <summary>The input tokens of every model call so far.</summary>
    public int InputTokens { get; private set; }

    /// <summary>The output tokens of every model call so far.</summary>
    public int OutputTokens { get; private set; }

    /// <summary>
    /// Adds the tokens of one model run.
    /// </summary>
    /// <param name="inputTokens">The input tokens.</param>
    /// <param name="outputTokens">The output tokens.</param>
    public void AddUsage(int inputTokens, int outputTokens)
    {
        InputTokens += inputTokens;
        OutputTokens += outputTokens;
    }

    /// <summary>
    /// Ends the run as completed.
    /// </summary>
    /// <param name="now">The end time.</param>
    public void Complete(DateTime now) =>
        Run.Complete(now, verdict: null, verdictReason: null, InputTokens, OutputTokens);

    /// <summary>
    /// Ends the run as failed.
    /// </summary>
    /// <param name="now">The end time.</param>
    /// <param name="errorCode">The reason.</param>
    public void Fail(DateTime now, AgentRunErrorCode errorCode) => Run.Fail(now, errorCode, InputTokens, OutputTokens);

    /// <summary>
    /// Adds the run and its tool calls to the context. A run that is still open (the attempt ended during the
    /// extraction) fails with <see cref="AgentRunErrorCode.UnexpectedError"/> first.
    /// </summary>
    /// <param name="dbContext">Receives the rows. The caller saves.</param>
    /// <param name="now">The end time of an open run.</param>
    public void AddTo(IInsightsDbContext dbContext, DateTime now)
    {
        if (!Run.IsFinal)
        {
            Fail(now, AgentRunErrorCode.UnexpectedError);
        }

        dbContext.AgentRuns.Add(Run);
        dbContext.AgentToolCalls.AddRange(_calls);
    }
}
