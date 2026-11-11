import { DEFAULT_PAGE_SIZE } from '@shared/grid/requests/grid-limits';
import { SortDirection } from '@shared/grid/requests/grid-request';
import type { GridStateInit } from '@shared/grid/with-grid-state';
import { EMPTY_RANGE } from '@shared/time/plain-date-range';

import { MachineColumn, type MachineFilter, MachineSortColumn } from '@machines/machine';

export const INITIAL_FILTER: MachineFilter = {
  name: '',
  serialNumber: '',
  states: [],
  createdAt: EMPTY_RANGE,
  updatedAt: EMPTY_RANGE,
  hasPendingDrafts: null,
};

export const COLUMN_ORDER: readonly MachineColumn[] = [
  MachineColumn.Name,
  MachineColumn.SerialNumber,
  MachineColumn.State,
  MachineColumn.CreatedAt,
  MachineColumn.UpdatedAt,
  MachineColumn.Drafts,
];

export const INITIAL_GRID: GridStateInit<MachineFilter, MachineSortColumn, MachineColumn> = {
  limit: DEFAULT_PAGE_SIZE,
  sort: { column: MachineSortColumn.CreatedAt, direction: SortDirection.Descending },
  filter: INITIAL_FILTER,
  columns: COLUMN_ORDER,
};
