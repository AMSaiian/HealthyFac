using HealthyFac.Shared.Application.CurrentUser.Actors;

namespace HealthyFac.Seeder.Timeline;

/// <summary>A use case request that a seed event sends. The seed check validates it before any write.</summary>
public sealed class SeedRequest
{
    /// <summary>The service that owns the use case.</summary>
    public required ServiceName Service { get; init; }

    public required object Request { get; init; }
}
