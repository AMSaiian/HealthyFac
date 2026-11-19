using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Shared.IntegrationTests.Persistence.Data;

public sealed class SoftDeleteParentEntity : IEntity, ISoftDeletable
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public required string Name { get; init; }

    public List<SoftDeleteChildEntity> Children { get; } = [];

    public List<PlainChildEntity> PlainChildren { get; } = [];

    public DateTime? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }
}

public sealed class SoftDeleteChildEntity : IEntity, ISoftDeletable
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid ParentId { get; private set; }

    public required string Name { get; init; }

    public List<SoftDeleteGrandchildEntity> Grandchildren { get; } = [];

    public DateTime? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }
}
