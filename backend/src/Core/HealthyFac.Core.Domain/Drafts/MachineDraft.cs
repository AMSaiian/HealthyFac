using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.Drafts;

/// <summary>
/// A machine that the ingest of a file proposed. A draft without a target machine proposes a new machine (the create
/// path). A draft with a target machine proposes new values for that machine (the update path). The draft keeps the
/// ID that the proposal message gave it, so a repeat delivery finds the same row. A removal keeps the row (soft
/// delete).
/// </summary>
public class MachineDraft : IEntity, IAuditable, ISoftDeletable, IVersioned
{
    /// <summary>
    /// Creates a pending draft.
    /// </summary>
    /// <param name="id">The ID from the proposal message.</param>
    /// <param name="companyId">The company of the file.</param>
    /// <param name="attemptId">The ingest attempt that proposed the draft.</param>
    /// <param name="fileId">The file that the draft came from.</param>
    /// <param name="queuedAt">The time when the user queued the ingest (UTC).</param>
    /// <param name="targetMachineId">The machine to update, or <see langword="null"/> for a new machine.</param>
    /// <param name="name">The proposed name, or <see langword="null"/> for no proposal.</param>
    /// <param name="serialNumber">The proposed serial number, or <see langword="null"/> for no proposal.</param>
    /// <param name="description">The proposed description as HTML, or <see langword="null"/>.</param>
    public MachineDraft(
        Guid id,
        Guid companyId,
        Guid attemptId,
        Guid fileId,
        DateTime queuedAt,
        Guid? targetMachineId,
        string? name,
        string? serialNumber,
        string? description
    )
    {
        Id = id;
        CompanyId = companyId;
        AttemptId = attemptId;
        FileId = fileId;
        QueuedAt = queuedAt;
        TargetMachineId = targetMachineId;
        State = DraftState.Pending;
        Name = name;
        SerialNumber = serialNumber;
        Description = description;
    }

    public Guid Id { get; private set; }
    public uint RowVersion { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid AttemptId { get; private set; }
    public Guid FileId { get; private set; }
    public DateTime QueuedAt { get; private set; }
    public Guid? TargetMachineId { get; private set; }

    /// <summary>The machine that a user created from the draft, or <see langword="null"/> before the apply.</summary>
    public Guid? AppliedMachineId { get; private set; }

    public DraftState State { get; private set; }
    public string? Name { get; private set; }
    public string? SerialNumber { get; private set; }
    public string? Description { get; private set; }

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>Tells whether the draft proposes a new machine.</summary>
    public bool IsCreate => TargetMachineId is null;

    /// <summary>
    /// Marks the draft as applied.
    /// </summary>
    /// <param name="machineId">The machine that the apply created or updated.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="DraftInvariantErrors"/> when the draft is not pending.
    /// </exception>
    public void MarkApplied(Guid machineId)
    {
        EnsurePending();
        AppliedMachineId = machineId;
        State = DraftState.Applied;
    }

    /// <summary>
    /// Marks the draft as dismissed.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="DraftInvariantErrors"/> when the draft is not pending.
    /// </exception>
    public void Dismiss()
    {
        EnsurePending();
        State = DraftState.Dismissed;
    }

    private void EnsurePending()
    {
        if (State != DraftState.Pending)
        {
            throw DraftInvariantErrors.NotPending(Id, State);
        }
    }
}
