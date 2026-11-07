using HealthyFac.Shared.Application.Messaging;

namespace HealthyFac.Core.Application.Machines;

/// <summary>
/// Tells the Telemetry service that a machine is deleted, so it removes the registration, the metric definitions,
/// and the status definitions of the machine.
/// </summary>
public sealed record MachineDeleted : IIntegrationMessage
{
    public static string MessageType => "core.v1:MachineDeleted";

    public required Guid MachineId { get; init; }

    public required Guid CompanyId { get; init; }
}
