namespace HealthyFac.Core.Web.Machines.CreateMachine;

/// <summary>
/// Represents the response payload for a created machine.
/// </summary>
public sealed record CreateMachineResponse
{
    public required Guid Id { get; init; }
}
