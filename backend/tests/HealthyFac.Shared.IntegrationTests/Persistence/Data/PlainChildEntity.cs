using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Shared.IntegrationTests.Persistence.Data;

public sealed class PlainChildEntity : IEntity
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid ParentId { get; private set; }

    public required string Name { get; init; }
}
