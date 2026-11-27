import { DEFAULT_PAGE_SIZE } from '@shared/grid/requests/grid-limits';
import { idSetFilter, setFilter } from '@shared/grid/requests/grid-filters';
import { SortDirection } from '@shared/grid/requests/grid-request';
import type { GridStateInit } from '@shared/grid/with-grid-state';

import {
  CaseColumn,
  type CaseFilter,
  type CaseGridState,
  CaseSortColumn,
  type SearchCasesBody,
} from '@maintenance/case';

export const INITIAL_FILTER: CaseFilter = {
  machineIds: [],
  states: [],
  severities: [],
  lastRunStates: [],
  isAiUnavailable: null,
};

export const COLUMN_ORDER: readonly CaseColumn[] = [
  CaseColumn.Machine,
  CaseColumn.State,
  CaseColumn.Severity,
  CaseColumn.Signals,
  CaseColumn.LastRun,
  CaseColumn.OpenedAt,
];

export const INITIAL_GRID: GridStateInit<CaseFilter, CaseSortColumn, CaseColumn> = {
  limit: DEFAULT_PAGE_SIZE,
  sort: { column: CaseSortColumn.OpenedAt, direction: SortDirection.Descending },
  filter: INITIAL_FILTER,
  columns: COLUMN_ORDER,
};

export function toSearchBody(state: CaseGridState): SearchCasesBody {
  return {
    offset: state.offset,
    limit: state.limit,
    ...(state.sort === null ? {} : { sort: state.sort }),
    filter: {
      machineIds: idSetFilter(state.filter.machineIds),
      states: setFilter(state.filter.states),
      severities: setFilter(state.filter.severities),
      lastRunStates: setFilter(state.filter.lastRunStates),
      isAiUnavailable: state.filter.isAiUnavailable ?? undefined,
    },
  };
}
