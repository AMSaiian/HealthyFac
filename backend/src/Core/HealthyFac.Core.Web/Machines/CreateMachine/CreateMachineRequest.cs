namespace HealthyFac.Core.Web.Machines.CreateMachine;

/// <summary>
/// Represents the request payload for creating a machine.
/// </summary>
public sealed record CreateMachineRequest
{
    public required string Name { get; init; }
    public required string SerialNumber { get; init; }
    public required Guid TenantId { get; init; }
}
