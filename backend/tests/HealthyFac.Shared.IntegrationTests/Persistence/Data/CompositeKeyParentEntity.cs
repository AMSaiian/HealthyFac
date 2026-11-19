using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Shared.IntegrationTests.Persistence.Data;

public sealed class CompositeKeyParentEntity : IEntity, ISoftDeletable
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public required string Name { get; init; }

    public List<CompositeKeyChildEntity> Children { get; } = [];

    public DateTime? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }
}

public sealed class CompositeKeyChildEntity : ISoftDeletable
{
    public Guid ParentId { get; private set; }

    public int Position { get; init; }

    public DateTime? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }
}
