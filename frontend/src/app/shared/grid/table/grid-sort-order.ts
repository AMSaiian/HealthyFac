import { SortDirection } from '@shared/grid/requests/grid-request';

/** The PrimeNG table sort order values. */
const ASCENDING_ORDER = 1;
const DESCENDING_ORDER = -1;

export function toSortOrder(direction: SortDirection | undefined): number {
  return direction === SortDirection.Descending ? DESCENDING_ORDER : ASCENDING_ORDER;
}

export function toSortDirection(order: number | null | undefined): SortDirection {
  return order === DESCENDING_ORDER ? SortDirection.Descending : SortDirection.Ascending;
}
