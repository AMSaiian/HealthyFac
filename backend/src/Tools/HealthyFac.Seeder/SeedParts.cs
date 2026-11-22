using HealthyFac.Seeder.People;
using HealthyFac.Seeder.Timeline;

namespace HealthyFac.Seeder;

/// <summary>Lists the parts of the seed in the order of their files.</summary>
public static class SeedParts
{
    /// <summary>Lists the parts.</summary>
    /// <param name="people">The people part. The company zone comes from its file.</param>
    /// <returns>The parts. Each cycle adds its part here.</returns>
    public static IReadOnlyList<ISeedPart> Create(PeoplePart people) => [people];
}
