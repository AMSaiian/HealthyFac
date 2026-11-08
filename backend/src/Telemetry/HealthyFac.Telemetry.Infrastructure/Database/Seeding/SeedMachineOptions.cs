namespace HealthyFac.Telemetry.Infrastructure.Database.Seeding;

/// <summary>
/// Describes a development machine with fixed values. Core seeds the same machine from the same seed data file, so
/// the registration exists before the <c>MachineCreated</c> message arrives. This is the approved seed exception of
/// the ID generation rule.
/// </summary>
public sealed class SeedMachineOptions
{
    public required Guid MachineId { get; init; }

    public required string GatewayId { get; init; }
}
