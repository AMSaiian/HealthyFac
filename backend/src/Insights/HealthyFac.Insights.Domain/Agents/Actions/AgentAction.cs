using System.Runtime.CompilerServices;
using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Insights.Domain.Agents.Actions;

/// <summary>
/// A write that an agent run buffers during the run. The run commits the buffered actions when it completes, and
/// discards them when it fails, so a failed run changes nothing. The call ID that the model gave the call makes the
/// action unique in its run, so a repeated call adds no second action.
/// </summary>
public class AgentAction : IEntity
{
    /// <summary>
    /// Creates a buffered action.
    /// </summary>
    /// <param name="runId">The run that buffers the action.</param>
    /// <param name="callId">The ID that the model gave the tool call.</param>
    /// <param name="kind">The kind of the write.</param>
    /// <param name="payload">The data of the write as JSON text.</param>
    public AgentAction(Guid runId, string callId, AgentActionKind kind, string payload)
    {
        RunId = runId;
        CallId = callId;
        Kind = kind;
        Payload = payload;
        State = AgentActionState.Buffered;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid RunId { get; private set; }

    public string CallId { get; private set; }

    public AgentActionKind Kind { get; private set; }

    public string Payload { get; private set; }

    public AgentActionState State { get; private set; }

    /// <summary>
    /// What a user did with the draft of a proposal: created the work order or dismissed the draft. It stays
    /// <see langword="null"/> while the draft is pending.
    /// </summary>
    public AgentActionFollowUp? FollowUp { get; private set; }

    /// <summary>The work order that a user created from the draft. Set only when the follow-up is applied.</summary>
    public Guid? FollowUpWorkOrderId { get; private set; }

    /// <summary>
    /// Records that a user created a work order from the draft. A repeat of the message changes nothing.
    /// </summary>
    /// <param name="workOrderId">The work order that the user created.</param>
    public void FollowApplied(Guid workOrderId)
    {
        if (FollowUp is not null)
        {
            return;
        }

        FollowUp = AgentActionFollowUp.Applied;
        FollowUpWorkOrderId = workOrderId;
    }

    /// <summary>
    /// Records that a user dismissed the draft. A repeat of the message changes nothing.
    /// </summary>
    public void FollowDismissed()
    {
        FollowUp ??= AgentActionFollowUp.Dismissed;
    }

    /// <summary>
    /// Marks the action as committed: the run applied it.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="AgentInvariantErrors"/> when the action is not buffered.
    /// </exception>
    public void Commit()
    {
        EnsureBuffered();
        State = AgentActionState.Committed;
    }

    /// <summary>
    /// Marks the action as discarded: the run ended without applying it.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="AgentInvariantErrors"/> when the action is not buffered.
    /// </exception>
    public void Discard()
    {
        EnsureBuffered();
        State = AgentActionState.Discarded;
    }

    private void EnsureBuffered([CallerMemberName] string action = "")
    {
        if (State != AgentActionState.Buffered)
        {
            throw AgentInvariantErrors.IllegalActionTransition(Id, State, action);
        }
    }
}
