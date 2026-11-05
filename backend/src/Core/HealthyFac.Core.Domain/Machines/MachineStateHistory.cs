using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.Machines;

/// <summary>
/// Represents one state of a machine and the moment it started. The state of the row with the latest
/// <see cref="StartedAt"/> is the current state of the machine. A row never changes after it is created, and a
/// removal keeps the row (soft delete).
/// </summary>
public class MachineStateHistory : IEntity, IAuditable, ISoftDeletable
{
    /// <summary>
    /// Creates a row of the state history.
    /// </summary>
    /// <param name="machineId">The machine of the row. It never changes.</param>
    /// <param name="state">The state that starts.</param>
    /// <param name="startedAt">The moment the state started, as a UTC instant.</param>
    internal MachineStateHistory(Guid machineId, MachineState state, DateTime startedAt)
    {
        MachineId = machineId;
        State = state;
        StartedAt = startedAt;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid MachineId { get; private set; }
    public MachineState State { get; private set; }
    public DateTime StartedAt { get; private set; }

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
}
