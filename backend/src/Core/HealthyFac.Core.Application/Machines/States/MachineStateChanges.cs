using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Machines.States;

/// <summary>
/// Defines the one rule that changes the state of a machine. The update and the state-only use case share it.
/// </summary>
public static class MachineStateChanges
{
    /// <summary>
    /// Starts a new state and queues <see cref="MachineStateChanged"/>, unless it is the current state.
    /// </summary>
    /// <remarks>
    /// The method marks the machine row as modified. A new history row changes no column of the machine, so without
    /// the mark two parallel changes would both pass the <c>xmin</c> check and add two rows. With the mark, the
    /// <c>AuditableEntityInterceptor</c> sets <c>UpdatedAt</c> and <c>UpdatedBy</c>, and the row version check catches
    /// the parallel change. The message joins the same save as the history
    /// row. The caller must load the full state history (<see cref="Machine.ChangeState"/>).
    /// </remarks>
    /// <param name="dbContext">Tracks the machine.</param>
    /// <param name="outbox">Stores the state message with the change.</param>
    /// <param name="machine">The machine with its full state history.</param>
    /// <param name="state">The requested state.</param>
    /// <param name="nowUtc">The start of the new state, as a UTC instant.</param>
    /// <returns><see langword="true"/> when the state changed, <see langword="false"/> for the current state.</returns>
    public static bool Apply(
        ICoreDbContext dbContext,
        IOutbox outbox,
        Machine machine,
        MachineState state,
        DateTime nowUtc
    )
    {
        var current = machine.StateHistory.MaxBy(row => row.StartedAt)?.State;

        if (current == state)
        {
            return false;
        }

        machine.ChangeState(state, nowUtc);
        dbContext.Entry(machine).State = EntityState.Modified;

        outbox.Add(
            new MachineStateChanged
            {
                MachineId = machine.Id,
                CompanyId = machine.CompanyId,
                State = MachineStateValue.FromValue(state.Value),
                StartedAt = nowUtc,
            });

        return true;
    }
}
