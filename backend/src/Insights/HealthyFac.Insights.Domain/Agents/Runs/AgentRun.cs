using System.Runtime.CompilerServices;
using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Insights.Domain.Agents.Runs;

/// <summary>
/// One run of an agent role: the triage of a case, the plan of a work order, the extraction of a document, or the
/// screening of a file. The run is the row of the AI log: it keeps the model, the prompt version and hash, the token
/// counts, the verdict, and the error. A run moves from <c>queued</c> to <c>running</c> to a final state, and a
/// provider failure moves it back to <c>queued</c> for the next attempt.
/// </summary>
public class AgentRun : IEntity, IAuditable, IVersioned
{
    /// <summary>
    /// Creates a queued run.
    /// </summary>
    /// <param name="role">The role of the agent.</param>
    /// <param name="caseId">The case of a triage or planner run, otherwise <see langword="null"/>.</param>
    /// <param name="ingestAttemptId">The ingest attempt of an extractor or screener run, otherwise null.</param>
    /// <param name="dueAt">The time from which a worker may claim the run (UTC).</param>
    /// <param name="attemptNumber">
    /// The claims of the earlier runs of the same job. A run that replaces an interrupted run starts with the
    /// attempt number of the interrupted run, so its first claim makes the next attempt number. A first run
    /// starts with 0.
    /// </param>
    public AgentRun(
        AgentRole role,
        Guid? caseId,
        Guid? ingestAttemptId,
        DateTime dueAt,
        int attemptNumber = 0
    )
    {
        AttemptNumber = attemptNumber;
        Role = role;
        CaseId = caseId;
        IngestAttemptId = ingestAttemptId;
        DueAt = dueAt;
        State = AgentRunState.Queued;
    }

    private AgentRun()
    {
        Role = null!;
        State = null!;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public uint RowVersion { get; private set; }

    public AgentRole Role { get; private set; }

    public Guid? CaseId { get; private set; }

    public Guid? IngestAttemptId { get; private set; }

    public string? Model { get; private set; }

    public string? PromptVersion { get; private set; }

    public string? PromptHash { get; private set; }

    public AgentRunState State { get; private set; }

    /// <summary>The number of claims of the run. A provider retry raises it.</summary>
    public int AttemptNumber { get; private set; }

    /// <summary>The time from which a worker may claim the run (UTC).</summary>
    public DateTime DueAt { get; private set; }

    /// <summary>The time until which the worker owns the run. After it, a sweep interrupts the run.</summary>
    public DateTime? LeaseUntil { get; private set; }

    public int InputTokens { get; private set; }

    public int OutputTokens { get; private set; }

    public AgentRunVerdict? Verdict { get; private set; }

    /// <summary>The longest text that <see cref="VerdictReason"/> holds.</summary>
    public const int VerdictReasonMaxLength = 1000;

    /// <summary>The reason of the verdict, or the last text of the planner.</summary>
    public string? VerdictReason { get; private set; }

    public AgentRunErrorCode? ErrorCode { get; private set; }

    /// <summary>Why the run did nothing. Set only when the state is <c>skipped</c>.</summary>
    public AgentRunSkipReason? SkipReason { get; private set; }

    public DateTime? StartedAt { get; private set; }

    public DateTime? FinishedAt { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime? UpdatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    /// <summary>Tells whether the run ended: completed, failed, interrupted, or skipped.</summary>
    public bool IsFinal => State != AgentRunState.Queued && State != AgentRunState.Running;

    /// <summary>
    /// Starts an attempt: <c>queued</c> to <c>running</c>.
    /// </summary>
    /// <param name="now">The start time (UTC).</param>
    /// <param name="leaseUntil">The end of the lease (UTC).</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="AgentInvariantErrors"/> when the run is not queued.
    /// </exception>
    public void Claim(DateTime now, DateTime leaseUntil)
    {
        EnsureState(AgentRunState.Queued);
        State = AgentRunState.Running;
        StartedAt ??= now;
        LeaseUntil = leaseUntil;
        AttemptNumber++;
    }

    /// <summary>
    /// Stores the model and the prompt of the role that the run uses. The prompt text is never stored, only its
    /// version and its hash.
    /// </summary>
    /// <param name="model">The model that serves the run.</param>
    /// <param name="promptVersion">The version of the prompt of the role.</param>
    /// <param name="promptHash">The hash of the prompt text.</param>
    public void UseModel(string model, string promptVersion, string promptHash)
    {
        Model = model;
        PromptVersion = promptVersion;
        PromptHash = promptHash;
    }

    /// <summary>
    /// Ends the run with a result: <c>running</c> to <c>completed</c>.
    /// </summary>
    /// <param name="now">The end time (UTC).</param>
    /// <param name="verdict">The verdict of the run, or <see langword="null"/> for a role without one.</param>
    /// <param name="verdictReason">The reason of the verdict, or <see langword="null"/>.</param>
    /// <param name="inputTokens">The input tokens that the attempts used.</param>
    /// <param name="outputTokens">The output tokens that the attempts used.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="AgentInvariantErrors"/> when the run is not running.
    /// </exception>
    public void Complete(
        DateTime now,
        AgentRunVerdict? verdict,
        string? verdictReason,
        int inputTokens,
        int outputTokens
    )
    {
        EnsureState(AgentRunState.Running);
        State = AgentRunState.Completed;
        FinishedAt = now;
        LeaseUntil = null;
        Verdict = verdict;
        VerdictReason = verdictReason;
        InputTokens += inputTokens;
        OutputTokens += outputTokens;
    }

    /// <summary>
    /// Ends the run with an error: <c>running</c> to <c>failed</c>.
    /// </summary>
    /// <param name="now">The end time (UTC).</param>
    /// <param name="errorCode">Why the run failed.</param>
    /// <param name="inputTokens">The input tokens of the run.</param>
    /// <param name="outputTokens">The output tokens of the run.</param>
    /// <param name="verdictReason">The last text of the model, or <see langword="null"/>.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="AgentInvariantErrors"/> when the run is not running.
    /// </exception>
    public void Fail(
        DateTime now,
        AgentRunErrorCode errorCode,
        int inputTokens = 0,
        int outputTokens = 0,
        string? verdictReason = null
    )
    {
        EnsureState(AgentRunState.Running);
        State = AgentRunState.Failed;
        FinishedAt = now;
        LeaseUntil = null;
        ErrorCode = errorCode;
        VerdictReason = verdictReason;
        InputTokens += inputTokens;
        OutputTokens += outputTokens;
    }

    /// <summary>
    /// Moves the run back to <c>queued</c> after a provider failure, so a worker claims it again at
    /// <paramref name="dueAt"/>.
    /// </summary>
    /// <param name="dueAt">The time of the next attempt (UTC).</param>
    /// <param name="errorCode">The code of the failed attempt.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="AgentInvariantErrors"/> when the run is not running.
    /// </exception>
    public void Retry(DateTime dueAt, AgentRunErrorCode errorCode)
    {
        EnsureState(AgentRunState.Running);
        State = AgentRunState.Queued;
        LeaseUntil = null;
        DueAt = dueAt;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Ends a run that found no work: <c>running</c> to <c>skipped</c>. The run used no model and no token.
    /// </summary>
    /// <param name="now">The end time (UTC).</param>
    /// <param name="reason">Why the run did nothing.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="AgentInvariantErrors"/> when the run is not running.
    /// </exception>
    public void Skip(DateTime now, AgentRunSkipReason reason)
    {
        EnsureState(AgentRunState.Running);
        State = AgentRunState.Skipped;
        FinishedAt = now;
        LeaseUntil = null;
        SkipReason = reason;
    }

    /// <summary>
    /// Ends a run whose lease expired: <c>running</c> to <c>interrupted</c>.
    /// </summary>
    /// <param name="now">The end time (UTC).</param>
    /// <param name="inputTokens">The input tokens that the run used before it ended.</param>
    /// <param name="outputTokens">The output tokens that the run used before it ended.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="AgentInvariantErrors"/> when the run is not running.
    /// </exception>
    public void Interrupt(DateTime now, int inputTokens = 0, int outputTokens = 0)
    {
        EnsureState(AgentRunState.Running);
        State = AgentRunState.Interrupted;
        FinishedAt = now;
        LeaseUntil = null;
        InputTokens += inputTokens;
        OutputTokens += outputTokens;
    }

    private void EnsureState(AgentRunState expected, [CallerMemberName] string action = "")
    {
        if (State != expected)
        {
            throw AgentInvariantErrors.IllegalTransition(Id, State, action);
        }
    }
}
