import { DEFAULT_PAGE_SIZE } from '@shared/grid/requests/grid-limits';
import { SortDirection } from '@shared/grid/requests/grid-request';
import type { GridStateInit } from '@shared/grid/with-grid-state';
import { EMPTY_RANGE } from '@shared/time/plain-date-range';

import { WorkOrderColumn, type WorkOrderFilter, WorkOrderSortColumn } from '@work-orders/work-order';

export const INITIAL_FILTER: WorkOrderFilter = {
  title: '',
  statuses: [],
  priorities: [],
  categories: [],
  assigneeIds: [],
  machineIds: [],
  plannedStart: EMPTY_RANGE,
};

export const COLUMN_ORDER: readonly WorkOrderColumn[] = [
  WorkOrderColumn.Title,
  WorkOrderColumn.Status,
  WorkOrderColumn.Priority,
  WorkOrderColumn.Category,
  WorkOrderColumn.Assignee,
  WorkOrderColumn.Machine,
  WorkOrderColumn.PlannedStart,
  WorkOrderColumn.Estimate,
  WorkOrderColumn.CreatedAt,
];

export const INITIAL_GRID: GridStateInit<WorkOrderFilter, WorkOrderSortColumn, WorkOrderColumn> = {
  limit: DEFAULT_PAGE_SIZE,
  sort: { column: WorkOrderSortColumn.CreatedAt, direction: SortDirection.Descending },
  filter: INITIAL_FILTER,
  columns: COLUMN_ORDER,
};
