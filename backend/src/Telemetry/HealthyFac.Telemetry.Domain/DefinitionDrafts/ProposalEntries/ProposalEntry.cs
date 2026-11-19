using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Telemetry.Domain.DefinitionDrafts;

/// <summary>
/// One proposed machine of a proposal. Its ID is the ID of the proposed machine draft in Core, so messages that
/// arrive in any order find the same row. Every move is forward-only: a call that asks for a move that the state
/// does not allow returns <see langword="false"/> and changes nothing, so a repeated message is harmless. A removal
/// keeps the row (soft delete).
/// </summary>
public class ProposalEntry : IEntity, IAuditable, ISoftDeletable, IVersioned
{
    /// <summary>
    /// Creates an entry that waits for the outcome of the machine check.
    /// </summary>
    /// <param name="id">The ID of the entry from the message. It is never generated.</param>
    /// <param name="companyId">The company of the proposal.</param>
    public ProposalEntry(Guid id, Guid companyId)
    {
        Id = id;
        CompanyId = companyId;
        State = ProposalEntryState.Awaiting;
    }

    public Guid Id { get; private set; }
    public uint RowVersion { get; private set; }
    public Guid CompanyId { get; private set; }
    public ProposalEntryState State { get; private set; }

    /// <summary>The machine of the entry. It is set only in the state <c>onMachine</c>.</summary>
    public Guid? MachineId { get; private set; }

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// Records that Core rejected the proposal.
    /// </summary>
    /// <returns><see langword="true"/> when the state changed.</returns>
    public bool Reject() => MoveFromAwaiting(ProposalEntryState.Rejected);

    /// <summary>
    /// Records that the entry belongs to an existing machine.
    /// </summary>
    /// <param name="machineId">The machine of the entry.</param>
    /// <returns><see langword="true"/> when the state changed.</returns>
    public bool AcceptOnMachine(Guid machineId)
    {
        var isMoved = MoveFromAwaiting(ProposalEntryState.OnMachine);
        MachineId = isMoved ? machineId : MachineId;

        return isMoved;
    }

    /// <summary>
    /// Records that Core stored a create machine draft for the entry.
    /// </summary>
    /// <returns><see langword="true"/> when the state changed.</returns>
    public bool AcceptOnMachineDraft() => MoveFromAwaiting(ProposalEntryState.OnMachineDraft);

    /// <summary>
    /// Records that a user applied the create machine draft of the entry. The message can arrive before the
    /// outcome of the machine check.
    /// </summary>
    /// <param name="machineId">The machine that the apply created.</param>
    /// <returns><see langword="true"/> when the state changed.</returns>
    public bool ApplyMachineDraft(Guid machineId)
    {
        var isMoved = MoveFromOpen(ProposalEntryState.OnMachine);
        MachineId = isMoved ? machineId : MachineId;

        return isMoved;
    }

    /// <summary>
    /// Records that a user dismissed the create machine draft of the entry. The message can arrive before the
    /// outcome of the machine check.
    /// </summary>
    /// <returns><see langword="true"/> when the state changed.</returns>
    public bool DismissMachineDraft() => MoveFromOpen(ProposalEntryState.MachineDraftDismissed);

    private bool MoveFromAwaiting(ProposalEntryState target)
    {
        if (State != ProposalEntryState.Awaiting)
        {
            return false;
        }

        State = target;

        return true;
    }

    private bool MoveFromOpen(ProposalEntryState target)
    {
        if (State != ProposalEntryState.Awaiting && State != ProposalEntryState.OnMachineDraft)
        {
            return false;
        }

        State = target;

        return true;
    }
}
