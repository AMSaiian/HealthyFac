using System.Collections.Frozen;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.Application.Grids.Keyset;

/// <summary>
/// Reads one page of a keyset grid.
/// </summary>
public static class KeysetQueryExtensions
{
    extension<T>(IQueryable<T> query)
        where T : class, IEntity<Guid>, IKeysetGridItem<T>
    {
        /// <summary>
        /// Counts the filtered rows, cuts the rows after the position, orders them by the key and the ID, and reads
        /// one page. It reads one extra row to learn whether a next page exists. Call it on the filtered query of the
        /// sorted row type.
        /// </summary>
        /// <param name="sort">The sort column and direction of the request.</param>
        /// <param name="sortMap">Maps each sort column to its keyset sort key.</param>
        /// <param name="limit">The number of rows to read.</param>
        /// <param name="after">The position after which the page starts, or <see langword="null"/> for page 1.</param>
        /// <param name="cancellationToken">
        /// The <see cref="CancellationToken"/> to observe for cancellation requests.
        /// </param>
        /// <typeparam name="TSortColumn">The enumeration of the sortable columns.</typeparam>
        /// <returns>The page, the filtered row count, and the position of the next page.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the sort column has no entry in <paramref name="sortMap"/> (see
        /// <see cref="GridErrors.SortColumnNotMapped"/>).
        /// </exception>
        public async Task<KeysetGridResponse<T>> ToKeysetResponseAsync<TSortColumn>(
            GridSort<TSortColumn> sort,
            FrozenDictionary<TSortColumn, IKeysetSortKey<T>> sortMap,
            int limit,
            KeysetPosition? after,
            CancellationToken cancellationToken
        )
            where TSortColumn : notnull
        {
            if (!sortMap.TryGetValue(sort.Column, out var key))
            {
                throw GridErrors.SortColumnNotMapped(sort.Column);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var cut = after is null ? query : key.After(query, sort.Direction, after);
            var rows = await key.Order(cut, sort.Direction).Take(limit + 1).ToListAsync(cancellationToken);

            var isNextPageAvailable = rows.Count > limit;
            var items = rows.Take(limit).Select(row => row.WithPosition(key.PositionOf(row))).ToList();

            return new KeysetGridResponse<T>
            {
                Items = items,
                TotalCount = totalCount,
                NextPosition = isNextPageAvailable ? items[^1].Position : null,
            };
        }
    }
}
