namespace HealthyFac.Shared.Domain.Entities;

/// <summary>
/// Marks a domain entity and gives it an identifier.
/// </summary>
/// <typeparam name="TId">The type of the identifier.</typeparam>
public interface IEntity<out TId> where TId : IEquatable<TId>
{
    /// <summary>The database generates the identifier, so it stays at its default until the first save.</summary>
    TId Id { get; }

    bool IsNew => Id.Equals(default(TId?));
}

/// <summary>
/// Marks a domain entity that uses a <see cref="Guid"/> identifier.
/// </summary>
public interface IEntity : IEntity<Guid>
{
}
