import { SortDirection } from '@shared/grid/requests/grid-request';
import { dateRangeFilter, idSetFilter, setFilter, textFilter } from '@shared/grid/requests/grid-filters';
import type { PlainDateRange } from '@shared/time/plain-date-range';

import {
  type SearchWorkOrdersBody,
  type WorkOrderFilter,
  type WorkOrderRow,
  WorkOrderSortColumn,
  WorkOrderStatus,
} from '@work-orders/work-order';

export const BOARD_STATUSES: readonly WorkOrderStatus[] = [
  WorkOrderStatus.Backlog,
  WorkOrderStatus.Staged,
  WorkOrderStatus.InProgress,
  WorkOrderStatus.Blocked,
  WorkOrderStatus.Completed,
  WorkOrderStatus.Rejected,
];

export const MAX_WINDOW_LIMIT = 100;
export const TITLE_DEBOUNCE_MS = 300;
export const WINDOW_BUFFER = 10;
export const INITIAL_RANGE: ScrollRange = { first: 0, last: 20 };

export interface ScrollRange {
  readonly first: number;
  readonly last: number;
}

/** The loaded part of one column: the rows from `offset` on and the number of rows that the column holds. */
export interface ColumnWindow {
  readonly offset: number;
  readonly items: readonly WorkOrderRow[];
  readonly totalCount: number;
}

/** The `offset` and `limit` of one search request of a column. */
export interface WindowRequest {
  readonly offset: number;
  readonly limit: number;
}

/**
 * The state of one column: its loaded window, the last requested range and window, and the loading flag.
 */
export interface ColumnState {
  readonly window: ColumnWindow;
  readonly range: ScrollRange;
  readonly requested: WindowRequest | null;
  readonly isLoading: boolean;
}

export const EMPTY_WINDOW: ColumnWindow = { offset: 0, items: [], totalCount: 0 };

export type DropDecision = 'none' | 'status' | 'assign' | 'unassign' | 'refused';

/**
 * Decides what a drop of a card does. A drop inside one column does nothing. Between two scheduled columns it changes
 * the status. From the backlog only `staged` is allowed, through the assign dialog, and only with `work-orders:manage`;
 * to the backlog it unassigns, also only with `work-orders:manage`.
 */
export function dropDecision(source: WorkOrderStatus, target: WorkOrderStatus, canManage: boolean): DropDecision {
  if (source === target) {
    return 'none';
  }
  if (source === WorkOrderStatus.Backlog) {
    return canManage && target === WorkOrderStatus.Staged ? 'assign' : 'refused';
  }
  if (target === WorkOrderStatus.Backlog) {
    return canManage ? 'unassign' : 'refused';
  }
  return 'status';
}

/**
 * Tells whether a column accepts a card. A column that loads accepts nothing: `cdkDropListDisabled` stops only a drag
 * out of a list, so the drop into it is refused here.
 */
export function canEnterColumn(
  source: WorkOrderStatus,
  target: WorkOrderStatus,
  canManage: boolean,
  isLoading: boolean,
): boolean {
  return !isLoading && dropDecision(source, target, canManage) !== 'refused';
}

/** The columns that a finished drop reloads: the source and the target, or every column for a completion. */
export function touchedColumns(source: WorkOrderStatus, target: WorkOrderStatus): readonly WorkOrderStatus[] {
  return target === WorkOrderStatus.Completed ? BOARD_STATUSES : [source, target];
}

/** The targets that the menu "Change status" and the drop offer for a card. */
export function statusTargets(source: WorkOrderStatus, canManage: boolean): readonly WorkOrderStatus[] {
  return BOARD_STATUSES.filter((target) => {
    const decision = dropDecision(source, target, canManage);
    return decision !== 'none' && decision !== 'refused';
  });
}

/** The `offset` and `limit` of the request for a scroller range: the range plus a buffer, at most 100 rows. */
export function windowRequest(range: ScrollRange): WindowRequest {
  return { offset: range.first, limit: Math.min(MAX_WINDOW_LIMIT, range.last - range.first + WINDOW_BUFFER) };
}

/**
 * Tells whether a window is one that the backend accepts: a whole `offset` of at least 0 and a whole `limit` from 1 to
 * 100. A scroller that measures a 0 height can report a range that is not.
 */
export function isValidWindow(window: WindowRequest): boolean {
  return (
    Number.isInteger(window.offset) &&
    Number.isInteger(window.limit) &&
    window.offset >= 0 &&
    window.limit > 0 &&
    window.limit <= MAX_WINDOW_LIMIT
  );
}

/** Tells whether a lazy load event must send a request: its window is valid and differs from the last request. */
export function isNewWindow(range: ScrollRange, requested: WindowRequest | null): boolean {
  const window = windowRequest(range);
  return isValidWindow(window) && (requested?.offset !== window.offset || requested.limit !== window.limit);
}

/**
 * Tells whether the rows of a scroller range are all in the loaded window already. The scroller asks for a range
 * that can reach past the last row of the column, so only the rows that exist count. A scroller that starts again
 * after a reload asks for the range of the new row count, and the window that the reload loaded holds it.
 */
export function isLoadedRange(range: ScrollRange, window: ColumnWindow): boolean {
  return range.first >= window.offset && Math.min(range.last, window.totalCount) <= window.offset + window.items.length;
}

/** The rows of a column as the scroller shows them: one entry per row, `null` outside the loaded window. */
export function slotsOf(window: ColumnWindow): (WorkOrderRow | null)[] {
  return Array.from({ length: window.totalCount }, (_, index) => window.items[index - window.offset] ?? null);
}

/** The sort of a column: planned start, then priority; the backlog sorts by priority, then creation. */
function sortOf(status: WorkOrderStatus): SearchWorkOrdersBody['sort'] {
  return {
    column: status === WorkOrderStatus.Backlog ? WorkOrderSortColumn.Priority : WorkOrderSortColumn.PlannedStart,
    direction: status === WorkOrderStatus.Backlog ? SortDirection.Descending : SortDirection.Ascending,
  };
}

/** Builds the search of one column. The backlog has no planned start, so it ignores the date range. */
export function columnBody(
  status: WorkOrderStatus,
  range: ScrollRange,
  filter: WorkOrderFilter,
  plannedStart: PlainDateRange,
  timeZoneId: string,
): SearchWorkOrdersBody {
  const { offset, limit } = windowRequest(range);
  const isBacklog = status === WorkOrderStatus.Backlog;
  return {
    offset,
    limit,
    sort: sortOf(status),
    filter: {
      title: textFilter(filter.title),
      statuses: setFilter([status]),
      priorities: setFilter(filter.priorities),
      categories: setFilter(filter.categories),
      assignee: idSetFilter(filter.assigneeIds),
      machine: idSetFilter(filter.machineIds),
      ...(isBacklog ? {} : { plannedStart: dateRangeFilter(plannedStart, timeZoneId) }),
    },
  };
}
