namespace HealthyFac.Shared.Domain.Entities;

/// <summary>
/// Marks a domain entity and gives it an identifier.
/// </summary>
/// <typeparam name="TId">The type of the identifier.</typeparam>
public interface IEntity<out TId> where TId : IEquatable<TId>
{
    /// <summary>
    /// The application generates the identifier when it creates the entity, so the identifier exists before the
    /// first save and a single save can reference it (e.g., in an outbox message).
    /// </summary>
    TId Id { get; }
}

/// <summary>
/// Marks a domain entity that uses a <see cref="Guid"/> identifier.
/// </summary>
public interface IEntity : IEntity<Guid>
{
}
