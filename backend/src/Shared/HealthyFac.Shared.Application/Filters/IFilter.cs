using System.Linq.Expressions;

namespace HealthyFac.Shared.Application.Filters;

/// <summary>
/// Turns the typed filter of a grid into one predicate on the entity. A field without a value adds no condition.
/// </summary>
/// <typeparam name="T">The entity type that the grid reads.</typeparam>
public interface IFilter<T>
{
    /// <summary>
    /// Builds the predicate that every filled field must hold.
    /// </summary>
    /// <returns>The predicate for a <c>Where</c> call, or <see langword="null"/> when no field has a value.</returns>
    Expression<Func<T, bool>>? ToPredicate();
}
