using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Shared.IntegrationTests.Persistence.Data;

public sealed class SoftDeleteTestEntity : IEntity, ISoftDeletable
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public required string Name { get; init; }

    public DateTime? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }
}

public sealed class PlainTestEntity : IEntity
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public required string Name { get; init; }
}
