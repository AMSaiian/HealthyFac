namespace HealthyFac.Shared.Domain.Entities;

/// <summary>
/// Marks an entity that stays in the table when a use case removes it. The persistence layer sets both values when it
/// saves a removal, and a global query filter hides the entity from every query. All times are UTC.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>The time of the removal, or <see langword="null"/> while the entity exists.</summary>
    DateTime? DeletedAt { get; }

    /// <summary>The actor of the removal, or <see langword="null"/> while the entity exists.</summary>
    Guid? DeletedBy { get; }
}
