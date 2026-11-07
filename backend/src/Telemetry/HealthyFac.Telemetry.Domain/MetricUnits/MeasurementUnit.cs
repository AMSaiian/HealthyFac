using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Telemetry.Domain.MetricUnits;

/// <summary>
/// A unit of a quantity kind, named as UnitsNet names it (for example <c>DegreeCelsius</c>, abbreviation
/// <c>°C</c>; empty for a unit without one, such as <c>Scalar.Amount</c>). A unit name alone is not unique across
/// kinds (<c>Percent</c>), so the pair of the kind name and the unit name is the identity that code uses. The system
/// seeds the rows, so the entity has no audit fields.
/// </summary>
public class MeasurementUnit : IEntity, ISoftDeletable
{
    /// <summary>
    /// Creates a unit for an existing quantity kind.
    /// </summary>
    /// <param name="quantityKindId">The ID of the quantity kind.</param>
    /// <param name="name">The unit name, as UnitsNet names it.</param>
    /// <param name="abbreviation">The abbreviation, or an empty text for a unit without one.</param>
    public MeasurementUnit(Guid quantityKindId, string name, string abbreviation)
    {
        QuantityKindId = quantityKindId;
        Name = name;
        Abbreviation = abbreviation;
    }

    /// <summary>
    /// Creates a unit and keeps the quantity kind object, so a rule can read its name.
    /// </summary>
    /// <param name="quantityKind">The quantity kind.</param>
    /// <param name="name">The unit name, as UnitsNet names it.</param>
    /// <param name="abbreviation">The abbreviation, or an empty text for a unit without one.</param>
    public MeasurementUnit(QuantityKind quantityKind, string name, string abbreviation)
        : this(quantityKind.Id, name, abbreviation)
    {
        QuantityKind = quantityKind;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid QuantityKindId { get; private set; }
    public string Name { get; private set; }
    public string Abbreviation { get; private set; }

    public QuantityKind QuantityKind { get; private set; } = null!;

    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
}
