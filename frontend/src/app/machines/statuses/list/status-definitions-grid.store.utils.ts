import { DEFAULT_PAGE_SIZE } from '@shared/grid/requests/grid-limits';
import { SortDirection } from '@shared/grid/requests/grid-request';
import type { GridStateInit } from '@shared/grid/with-grid-state';
import { EMPTY_RANGE } from '@shared/time/plain-date-range';

import {
  StatusDefinitionColumn,
  type StatusDefinitionFilter,
  StatusDefinitionSortColumn,
} from '@machines/statuses/status-definition';

export const INITIAL_FILTER: StatusDefinitionFilter = {
  code: '',
  severities: [],
  createdAt: EMPTY_RANGE,
  updatedAt: EMPTY_RANGE,
};

export const COLUMN_ORDER: readonly StatusDefinitionColumn[] = [
  StatusDefinitionColumn.Code,
  StatusDefinitionColumn.Severity,
  StatusDefinitionColumn.CreatedAt,
  StatusDefinitionColumn.UpdatedAt,
];

export const INITIAL_GRID: GridStateInit<StatusDefinitionFilter, StatusDefinitionSortColumn, StatusDefinitionColumn> = {
  limit: DEFAULT_PAGE_SIZE,
  sort: { column: StatusDefinitionSortColumn.Code, direction: SortDirection.Ascending },
  filter: INITIAL_FILTER,
  columns: COLUMN_ORDER,
};

/** The dialog state of the page: `statusDefinitionId` is `null` for a new error code. */
export interface ErrorCodeEditor {
  readonly statusDefinitionId: string | null;
}
