using System.Linq.Expressions;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.Application.Grids;

/// <summary>
/// Applies the filter, the sort, and the page of a grid request to a query.
/// </summary>
public static class GridQueryExtensions
{
    extension<T>(IQueryable<T> query)
        where T : class, IEntity<Guid>
    {
        /// <summary>
        /// Adds the predicate of the filter as one <c>Where</c>. A filter without a filled field leaves the query as it
        /// is.
        /// </summary>
        /// <param name="filter">The typed filter of the grid.</param>
        /// <returns>The filtered query.</returns>
        public IQueryable<T> ApplyFilter(IFilter<T> filter) =>
            filter.ToPredicate() is { } predicate ? query.Where(predicate) : query;

        /// <summary>
        /// Orders the query by the mapped key of the sort column, then by the ID. The ID is the last key, so rows with
        /// equal keys keep one order between two page reads and a page never skips or repeats a row (the IDs are
        /// version 7 GUIDs and so differ). Without a sort, the query orders by the ID.
        /// </summary>
        /// <param name="sort">The sort column and direction, or <see langword="null"/> for the ID order.</param>
        /// <param name="sortMap">Maps each sort column to its key expression.</param>
        /// <typeparam name="TSortColumn">The enumeration of the sortable columns.</typeparam>
        /// <returns>The ordered query.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the sort column has no entry in <paramref name="sortMap"/> (see
        /// <see cref="GridErrors.SortColumnNotMapped"/>). The validator prevents it for input.
        /// </exception>
        public IQueryable<T> ApplySort<TSortColumn>(
            GridSort<TSortColumn>? sort,
            IReadOnlyDictionary<TSortColumn, Expression<Func<T, dynamic>>> sortMap
        )
            where TSortColumn : notnull
        {
            if (sort is null)
            {
                return query.OrderBy(entity => entity.Id);
            }

            if (!sortMap.TryGetValue(sort.Column, out var key))
            {
                throw GridErrors.SortColumnNotMapped(sort.Column);
            }

            var ordered = sort.Direction == SortDirection.Descending
                ? query.OrderByDescending(key)
                : query.OrderBy(key);

            return ordered.ThenBy(entity => entity.Id);
        }

        /// <summary>
        /// Counts the rows of the query, then reads one page of them. Call it after the filter and the sort.
        /// </summary>
        /// <param name="offset">The number of rows to skip.</param>
        /// <param name="limit">The number of rows to read.</param>
        /// <param name="projection">Maps an entity to the row type of the response.</param>
        /// <param name="cancellationToken">
        /// The <see cref="CancellationToken"/> to observe for cancellation requests.
        /// </param>
        /// <typeparam name="TItem">The row type of the response.</typeparam>
        /// <returns>The page and the number of rows that match the filter.</returns>
        public async Task<GridResponse<TItem>> ToGridResponseAsync<TItem>(
            int offset,
            int limit,
            Expression<Func<T, TItem>> projection,
            CancellationToken cancellationToken
        )
        {
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query.Skip(offset).Take(limit).Select(projection).ToListAsync(cancellationToken);

            return new GridResponse<TItem> { Items = items, TotalCount = totalCount };
        }
    }
}
