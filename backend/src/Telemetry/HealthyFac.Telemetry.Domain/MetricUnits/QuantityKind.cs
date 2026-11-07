using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Telemetry.Domain.MetricUnits;

/// <summary>
/// A physical quantity of the unit catalog, named as UnitsNet names it (for example <c>Temperature</c>). The system
/// seeds the rows, so the entity has no audit fields.
/// </summary>
public class QuantityKind : IEntity, ISoftDeletable
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public required string Name { get; init; }

    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
}
