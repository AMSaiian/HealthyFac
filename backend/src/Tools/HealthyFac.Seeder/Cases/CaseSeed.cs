using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.Cases;

public sealed class CaseSeed
{
    public required Guid Id { get; init; }

    /// <summary>The key of the machine in <c>machines.json</c>.</summary>
    public required string Machine { get; init; }

    public required RelativeMoment OpenedAt { get; init; }
}
