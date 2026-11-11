import { textFilter } from '@shared/grid/requests/grid-filters';
import { DEFAULT_PAGE_SIZE } from '@shared/grid/requests/grid-limits';
import { SortDirection } from '@shared/grid/requests/grid-request';
import type { GridStateInit } from '@shared/grid/with-grid-state';

import {
  InviteColumn,
  type InviteFilter,
  type InviteGridState,
  InviteSortColumn,
  type SearchInvitesBody,
} from '@employees/employee';

export const INITIAL_FILTER: InviteFilter = { email: '' };

export const COLUMN_ORDER: readonly InviteColumn[] = [
  InviteColumn.Email,
  InviteColumn.Role,
  InviteColumn.ExpiresAt,
  InviteColumn.IsExpired,
];

export const INITIAL_GRID: GridStateInit<InviteFilter, InviteSortColumn, InviteColumn> = {
  limit: DEFAULT_PAGE_SIZE,
  sort: { column: InviteSortColumn.ExpiresAt, direction: SortDirection.Ascending },
  filter: INITIAL_FILTER,
  columns: COLUMN_ORDER,
};

export function toSearchBody(state: InviteGridState): SearchInvitesBody {
  return {
    offset: state.offset,
    limit: state.limit,
    ...(state.sort === null ? {} : { sort: state.sort }),
    filter: { email: textFilter(state.filter.email) },
  };
}
