using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Telemetry.Domain.DefinitionDrafts;

/// <summary>
/// The shared members of a metric and a status definition draft, so one function can serve both kinds.
/// </summary>
public interface IDefinitionDraft : IEntity
{
    Guid CompanyId { get; }

    Guid ProposalEntryId { get; }

    Guid? TargetMachineId { get; }

    Guid? TargetMachineDraftId { get; }

    DefinitionDraftState State { get; }

    /// <summary>
    /// Moves the draft to the target that the state of its proposal entry names. It changes only a draft in the
    /// state <c>awaitingTarget</c> or <c>pending</c>. The one mapping lives in <see cref="DefinitionDraftFollowing"/>.
    /// </summary>
    /// <param name="entry">The proposal entry of the draft.</param>
    /// <param name="isTargetRegistrationDeleted">
    /// Whether the machine of an <c>onMachine</c> entry has a soft-deleted registration.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the caller must remove the draft (the proposal was rejected before it had a
    /// target). The caller removes it with the context, so the soft delete interceptor runs.
    /// </returns>
    bool Follow(ProposalEntry entry, bool isTargetRegistrationDeleted);

    /// <summary>
    /// Marks the draft as applied: a user turned it into a definition.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the draft is not pending.</exception>
    void MarkApplied();
}
