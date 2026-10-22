using HealthyFac.Core.Infrastructure.Database.Seeding;

namespace HealthyFac.Core.Infrastructure.Database;

/// <summary>
/// Configures which data sets the initializer seeds on startup.
/// Required data always seeds. Optional data is development and test material.
/// </summary>
public sealed class SeedingOptions
{
    public const string SectionName = "Seeding";

    /// <summary>Set to true to seed development and test data after the required data.</summary>
    public bool SeedOptionalData { get; init; }

    public DevelopmentDataOptions DevelopmentData { get; init; } = new();
}
