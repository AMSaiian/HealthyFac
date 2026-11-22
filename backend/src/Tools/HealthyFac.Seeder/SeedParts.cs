using HealthyFac.Seeder.Timeline;

namespace HealthyFac.Seeder;

/// <summary>Lists the parts of the seed in the order of their files.</summary>
public static class SeedParts
{
    /// <summary>Creates the parts.</summary>
    /// <param name="hosts">The service hosts that the parts write through.</param>
    /// <returns>The parts. No part exists yet: each cycle adds its part here.</returns>
    public static IReadOnlyList<ISeedPart> Create(ServiceHosts.ServiceHosts hosts) => [];
}
