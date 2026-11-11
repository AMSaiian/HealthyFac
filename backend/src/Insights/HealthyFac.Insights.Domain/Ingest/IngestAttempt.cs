using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Insights.Domain.Ingest;

/// <summary>
/// One run of the ingest of a file: the snapshot of the file at the start, the state, and the result. The attempt moves
/// from <c>queued</c> to <c>inProcess</c> to a final state (<c>completed</c>, <c>failed</c>, or <c>interrupted</c>).
/// A removal keeps the row (soft delete).
/// </summary>
public class IngestAttempt : IEntity, IAuditable, ISoftDeletable, IVersioned
{
    /// <summary>
    /// Creates a queued attempt.
    /// </summary>
    /// <param name="companyId">The company of the file.</param>
    /// <param name="fileId">The file to ingest.</param>
    /// <param name="machineId">The machine of the file, or <see langword="null"/> for a company file.</param>
    /// <param name="machineChangedAt">The time of the last tie change of the file (UTC).</param>
    /// <param name="fileName">The name of the file without the extension.</param>
    /// <param name="extension">The lower case extension of the file without the dot.</param>
    /// <param name="sizeBytes">The size of the file in bytes.</param>
    /// <param name="queuedAt">The time when the user queued the attempt (UTC).</param>
    /// <param name="traceParent">The trace context of the start request, or <see langword="null"/>.</param>
    public IngestAttempt(
        Guid companyId,
        Guid fileId,
        Guid? machineId,
        DateTime machineChangedAt,
        string fileName,
        string extension,
        long sizeBytes,
        DateTime queuedAt,
        string? traceParent
    )
    {
        CompanyId = companyId;
        FileId = fileId;
        MachineId = machineId;
        MachineChangedAt = machineChangedAt;
        FileName = fileName;
        Extension = extension;
        SizeBytes = sizeBytes;
        State = IngestState.Queued;
        QueuedAt = queuedAt;
        TraceParent = traceParent;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public uint RowVersion { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid FileId { get; private set; }
    public Guid? MachineId { get; private set; }
    public DateTime MachineChangedAt { get; private set; }
    public string FileName { get; private set; }
    public string Extension { get; private set; }
    public long SizeBytes { get; private set; }
    public IngestState State { get; private set; }
    public IngestErrorCode? ErrorCode { get; private set; }
    public IngestInterruptReason? InterruptReason { get; private set; }

    /// <summary>A translation key with values (JSON) that tells the user about a partial result, or null.</summary>
    public string? Warning { get; private set; }

    public DateTime QueuedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }

    /// <summary>The time until which the worker owns the attempt. After it, a sweep interrupts the attempt.</summary>
    public DateTime? LeaseUntil { get; private set; }

    public string? TraceParent { get; private set; }
    public int? PageCount { get; private set; }
    public int? ChunkCount { get; private set; }
    public int? ProposedDraftCount { get; private set; }

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>Tells whether the attempt is queued or in process.</summary>
    public bool IsActive => State == IngestState.Queued || State == IngestState.InProcess;

    /// <summary>Tells whether the attempt ended: completed, failed, or interrupted.</summary>
    public bool IsFinal => !IsActive;

    /// <summary>
    /// Starts the run: <c>queued</c> to <c>inProcess</c>.
    /// </summary>
    /// <param name="now">The start time (UTC).</param>
    /// <param name="leaseUntil">The end of the lease (UTC).</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="IngestInvariantErrors"/> when the attempt is not queued.
    /// </exception>
    public void Claim(DateTime now, DateTime leaseUntil)
    {
        EnsureState(IngestState.Queued, action: "start");
        State = IngestState.InProcess;
        StartedAt = now;
        LeaseUntil = leaseUntil;
    }

    /// <summary>
    /// Ends the run with a result: <c>inProcess</c> to <c>completed</c>.
    /// </summary>
    /// <param name="now">The end time (UTC).</param>
    /// <param name="pageCount">The number of pages of the document.</param>
    /// <param name="chunkCount">The number of stored chunks.</param>
    /// <param name="draftCount">The number of proposed drafts.</param>
    /// <param name="warning">The warning about a partial result, or <see langword="null"/>.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="IngestInvariantErrors"/> when the attempt is not in process.
    /// </exception>
    public void Complete(DateTime now, int pageCount, int chunkCount, int draftCount, string? warning)
    {
        EnsureState(IngestState.InProcess, action: "complete");
        State = IngestState.Completed;
        FinishedAt = now;
        LeaseUntil = null;
        PageCount = pageCount;
        ChunkCount = chunkCount;
        ProposedDraftCount = draftCount;
        Warning = warning;
    }

    /// <summary>
    /// Ends the run with an error: <c>inProcess</c> to <c>failed</c>.
    /// </summary>
    /// <param name="now">The end time (UTC).</param>
    /// <param name="code">Why the run failed.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="IngestInvariantErrors"/> when the attempt is not in process.
    /// </exception>
    public void Fail(DateTime now, IngestErrorCode code)
    {
        EnsureState(IngestState.InProcess, action: "fail");
        State = IngestState.Failed;
        FinishedAt = now;
        LeaseUntil = null;
        ErrorCode = code;
    }

    /// <summary>
    /// Stops the attempt: <c>queued</c> or <c>inProcess</c> to <c>interrupted</c>.
    /// </summary>
    /// <param name="now">The end time (UTC).</param>
    /// <param name="reason">Why the attempt stopped.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="IngestInvariantErrors"/> when the attempt is final.
    /// </exception>
    public void Interrupt(DateTime now, IngestInterruptReason reason)
    {
        if (IsFinal)
        {
            throw IngestInvariantErrors.IllegalTransition(Id, State, action: "be interrupted");
        }

        State = IngestState.Interrupted;
        FinishedAt = now;
        LeaseUntil = null;
        InterruptReason = reason;
    }

    /// <summary>
    /// Stops the attempt because Document deleted the file: <c>queued</c>, <c>inProcess</c>, or <c>completed</c> to
    /// <c>interrupted</c> with the reason <c>fileDeleted</c>. A completed attempt keeps its page, chunk, and draft
    /// counts. The other interrupts (a user cancel, a lease sweep, a service stop) never touch a completed attempt.
    /// </summary>
    /// <param name="now">The end time (UTC).</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="IngestInvariantErrors"/> when the attempt is <c>failed</c> or <c>interrupted</c>.
    /// </exception>
    public void InterruptForFileDelete(DateTime now)
    {
        if (State == IngestState.Failed || State == IngestState.Interrupted)
        {
            throw IngestInvariantErrors.IllegalTransition(Id, State, action: "be interrupted by a file delete");
        }

        State = IngestState.Interrupted;
        FinishedAt = now;
        LeaseUntil = null;
        InterruptReason = IngestInterruptReason.FileDeleted;
    }

    /// <summary>
    /// Stores the machine of the file. A change that is not newer than the stored one changes nothing, because
    /// messages can arrive in another order than Document sent them.
    /// </summary>
    /// <param name="machineId">The machine, or <see langword="null"/> when the file is untied.</param>
    /// <param name="changedAt">The time of the change in Document (UTC).</param>
    public void ChangeMachine(Guid? machineId, DateTime changedAt)
    {
        if (changedAt <= MachineChangedAt)
        {
            return;
        }

        MachineId = machineId;
        MachineChangedAt = changedAt;
    }

    private void EnsureState(IngestState expected, string action)
    {
        if (State != expected)
        {
            throw IngestInvariantErrors.IllegalTransition(Id, State, action);
        }
    }
}
