import { DEFAULT_PAGE_SIZE } from '@shared/grid/requests/grid-limits';
import { SortDirection } from '@shared/grid/requests/grid-request';
import type { GridStateInit } from '@shared/grid/with-grid-state';
import { EMPTY_RANGE } from '@shared/time/plain-date-range';

import {
  MachineErrorCodeColumn,
  type MachineErrorCodeFilter,
  MachineErrorCodeSortColumn,
} from '@machines/error-codes/machine-error-code';

export const INITIAL_FILTER: MachineErrorCodeFilter = {
  code: '',
  createdAt: EMPTY_RANGE,
  updatedAt: EMPTY_RANGE,
};

export const COLUMN_ORDER: readonly MachineErrorCodeColumn[] = [
  MachineErrorCodeColumn.Code,
  MachineErrorCodeColumn.CreatedAt,
  MachineErrorCodeColumn.UpdatedAt,
];

export const INITIAL_GRID: GridStateInit<MachineErrorCodeFilter, MachineErrorCodeSortColumn, MachineErrorCodeColumn> = {
  limit: DEFAULT_PAGE_SIZE,
  sort: { column: MachineErrorCodeSortColumn.Code, direction: SortDirection.Ascending },
  filter: INITIAL_FILTER,
  columns: COLUMN_ORDER,
};

/** The dialog state of the page: `errorCodeId` is `null` for a new error code. */
export interface ErrorCodeEditor {
  readonly errorCodeId: string | null;
}
