using HealthyFac.Shared.Application.Messaging;

namespace HealthyFac.Core.Application.Machines;

/// <summary>
/// Tells the Telemetry service that a machine exists. The machine, its gateway ID, and its department never change,
/// so one message is enough.
/// </summary>
public sealed record MachineCreated : IIntegrationMessage
{
    public static string MessageType => "core.v1:MachineCreated";

    public required Guid MachineId { get; init; }

    public required string GatewayId { get; init; }

    public required Guid CompanyId { get; init; }

    public required Guid DepartmentId { get; init; }
}
