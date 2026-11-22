using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.People;

public sealed class CompanySeed
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string TimeZoneId { get; init; }

    public required RelativeMoment CreatedAt { get; init; }
}
