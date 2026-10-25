using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.Machines;

/// <summary>
/// Represents a machine domain entity.
/// </summary>
public class Machine : IEntity, IAuditable
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public required string Name { get; init; }
    public required string SerialNumber { get; init; }
    public required Guid CompanyId { get; init; }
    public required Guid DepartmentId { get; init; }
    public MachineState State { get; private set; } = MachineState.Operational;

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
}
