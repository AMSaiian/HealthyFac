import { setFilter, textFilter } from '@shared/grid/requests/grid-filters';
import { DEFAULT_PAGE_SIZE } from '@shared/grid/requests/grid-limits';
import { SortDirection } from '@shared/grid/requests/grid-request';
import type { GridStateInit } from '@shared/grid/with-grid-state';

import {
  EmployeeColumn,
  type EmployeeFilter,
  type EmployeeGridState,
  EmployeeSortColumn,
  EmployeeStatus,
  type SearchEmployeesBody,
} from '@employees/employee';

export const INITIAL_FILTER: EmployeeFilter = { name: '', email: '', roles: [], statuses: [] };

export const COLUMN_ORDER: readonly EmployeeColumn[] = [
  EmployeeColumn.Name,
  EmployeeColumn.Email,
  EmployeeColumn.Role,
  EmployeeColumn.Status,
  EmployeeColumn.CreatedAt,
];

export const INITIAL_GRID: GridStateInit<EmployeeFilter, EmployeeSortColumn, EmployeeColumn> = {
  limit: DEFAULT_PAGE_SIZE,
  sort: { column: EmployeeSortColumn.Name, direction: SortDirection.Ascending },
  filter: INITIAL_FILTER,
  columns: COLUMN_ORDER,
};

/** The statuses for which a supervisor may start the provisioning again. */
export const RETRYABLE_STATUSES: readonly EmployeeStatus[] = [
  EmployeeStatus.Provisioning,
  EmployeeStatus.ProvisioningFailed,
];

export function isRetryable(status: EmployeeStatus): boolean {
  return RETRYABLE_STATUSES.includes(status);
}

export function toSearchBody(state: EmployeeGridState): SearchEmployeesBody {
  const { filter } = state;
  return {
    offset: state.offset,
    limit: state.limit,
    ...(state.sort === null ? {} : { sort: state.sort }),
    filter: {
      name: textFilter(filter.name),
      email: textFilter(filter.email),
      roles: setFilter(filter.roles),
      statuses: setFilter(filter.statuses),
    },
  };
}
