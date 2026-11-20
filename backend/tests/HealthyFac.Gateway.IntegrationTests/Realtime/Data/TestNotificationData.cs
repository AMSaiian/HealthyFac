namespace HealthyFac.Gateway.IntegrationTests.Realtime.Data;

public sealed record TestNotificationData
{
    public required Guid MachineId { get; init; }
}
