using HealthyFac.Seeder.Moments;
using HealthyFac.Shared.Application.CurrentUser.Actors;

namespace HealthyFac.Seeder.UnitTests.SeedFiles.Data;

public sealed class SeedFolderTestFile
{
    public required string Name { get; init; }

    public required ActorKind Kind { get; init; }

    public required RelativeMoment At { get; init; }
}
