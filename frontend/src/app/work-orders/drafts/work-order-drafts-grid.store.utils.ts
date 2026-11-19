import { setFilter } from '@shared/grid/requests/grid-filters';
import { DEFAULT_PAGE_SIZE } from '@shared/grid/requests/grid-limits';
import { SortDirection } from '@shared/grid/requests/grid-request';
import type { GridStateInit } from '@shared/grid/with-grid-state';

import {
  DraftState,
  type SearchWorkOrderDraftsBody,
  WorkOrderDraftColumn,
  type WorkOrderDraftFilter,
  type WorkOrderDraftGridState,
  WorkOrderDraftSortColumn,
} from '@work-orders/drafts/work-order-draft';

export const INITIAL_FILTER: WorkOrderDraftFilter = { states: [DraftState.Pending] };

export const COLUMN_ORDER: readonly WorkOrderDraftColumn[] = [
  WorkOrderDraftColumn.Title,
  WorkOrderDraftColumn.Machine,
  WorkOrderDraftColumn.Priority,
  WorkOrderDraftColumn.Category,
  WorkOrderDraftColumn.Estimate,
  WorkOrderDraftColumn.Case,
  WorkOrderDraftColumn.ProposedAt,
];

export const INITIAL_GRID: GridStateInit<WorkOrderDraftFilter, WorkOrderDraftSortColumn, WorkOrderDraftColumn> = {
  limit: DEFAULT_PAGE_SIZE,
  sort: { column: WorkOrderDraftSortColumn.ProposedAt, direction: SortDirection.Descending },
  filter: INITIAL_FILTER,
  columns: COLUMN_ORDER,
};

export function toSearchBody(state: WorkOrderDraftGridState): SearchWorkOrderDraftsBody {
  return {
    offset: state.offset,
    limit: state.limit,
    ...(state.sort === null ? {} : { sort: state.sort }),
    filter: { states: setFilter(state.filter.states) },
  };
}
