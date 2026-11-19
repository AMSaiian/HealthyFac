using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Shared.IntegrationTests.Persistence.Data;

public sealed class SoftDeleteReferenceEntity : IEntity, ISoftDeletable
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid ParentId { get; private set; }

    public required string Name { get; init; }

    public DateTime? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }
}
