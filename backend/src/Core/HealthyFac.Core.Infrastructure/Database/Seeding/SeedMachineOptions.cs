namespace HealthyFac.Core.Infrastructure.Database.Seeding;

/// <summary>
/// Describes a development machine with fixed values. Another service must know the machine ID and the gateway ID
/// before Core publishes the machine, so both come from the seed data file. This is the approved seed exception of the
/// ID generation rule.
/// </summary>
public sealed class SeedMachineOptions
{
    public required Guid MachineId { get; init; }

    public required string GatewayId { get; init; }

    /// <summary>The name of the machine, or <see langword="null"/> to use the numbered default name.</summary>
    public string? Name { get; init; }
}
