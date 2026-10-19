using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.Machines;

/// <summary>
/// Represents a machine domain entity.
/// </summary>
public class Machine : IEntity, IAuditable
{
    public Guid Id { get; private set; } = Guid.Empty;
    public required string Name { get; init; }
    public required string SerialNumber { get; init; }
    public required Guid TenantId { get; init; }
    public MachineState State { get; private set; } = MachineState.Operational;

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
}
