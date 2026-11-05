using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Shared.IntegrationTests.Grids.Data;

public sealed class GridTestEntity : IEntity
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public required string Group { get; init; }

    public required string Text { get; init; }

    public required int Number { get; init; }

    public required decimal Amount { get; init; }

    public required DateTime At { get; init; }

    public DateTime? OptionalAt { get; init; }

    public required Guid Key { get; init; }
}
