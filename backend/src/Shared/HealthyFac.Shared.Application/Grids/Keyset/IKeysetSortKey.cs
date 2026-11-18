using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Shared.Application.Grids.Keyset;

/// <summary>
/// One sort key of a keyset grid. The key and then the ID define the order, so a position names exactly one place in
/// it.
/// </summary>
/// <typeparam name="T">The sorted row type.</typeparam>
public interface IKeysetSortKey<T>
    where T : IEntity<Guid>
{
    /// <param name="query">The query to order.</param>
    /// <param name="direction">The sort direction of the key. The ID follows it.</param>
    /// <returns>The query ordered by the key, then by the ID.</returns>
    IOrderedQueryable<T> Order(IQueryable<T> query, SortDirection direction);

    /// <param name="query">The query to cut.</param>
    /// <param name="direction">The sort direction of the key.</param>
    /// <param name="after">The position after which the rows start.</param>
    /// <returns>The rows that follow <paramref name="after"/> in the order of <see cref="Order"/>.</returns>
    /// <exception cref="HealthyFac.Shared.Application.ErrorHandling.AppValidationException">
    /// Thrown when the sort value of <paramref name="after"/> does not fit the type of the key.
    /// </exception>
    IQueryable<T> After(IQueryable<T> query, SortDirection direction, KeysetPosition after);

    /// <param name="row">A materialized row.</param>
    /// <returns>The position of the row under this key.</returns>
    KeysetPosition PositionOf(T row);
}
