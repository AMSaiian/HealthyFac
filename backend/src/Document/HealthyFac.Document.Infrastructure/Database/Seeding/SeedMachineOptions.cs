namespace HealthyFac.Document.Infrastructure.Database.Seeding;

/// <summary>
/// Describes a development machine with fixed values. Core seeds the same machine from the same seed data file, so
/// the registration exists before the <c>MachineCreated</c> message arrives. The seed data file also holds a
/// gateway ID, which Document does not read. This is the approved seed exception of
/// the ID generation rule.
/// </summary>
public sealed class SeedMachineOptions
{
    public required Guid MachineId { get; init; }
}
