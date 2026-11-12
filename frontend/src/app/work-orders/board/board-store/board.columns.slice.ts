import { effect, inject, type Signal, untracked } from '@angular/core';
import { patchState, signalStoreFeature, type, withHooks, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, mergeMap, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { CLOCK } from '@shared/time/clock';
import type { PlainDateRange } from '@shared/time/plain-date-range';

import type { SearchWorkOrdersBody, WorkOrderStatus } from '@work-orders/work-order';
import { WorkOrdersGridStore } from '@work-orders/list/work-orders-grid.store';
import { WorkOrdersApi } from '@work-orders/work-orders-api';
import {
  columnBody,
  type Columns,
  type ColumnState,
  INITIAL_RANGE,
  initialColumn,
  initialColumns,
  isLoadedRange,
  isNewWindow,
  type ScrollRange,
  windowRequest,
} from './board.store.utils';

interface ColumnRange {
  readonly status: WorkOrderStatus;
  readonly range: ScrollRange;
}

interface StampedRange extends ColumnRange {
  readonly sequence: number;
}

interface BatchSend {
  readonly stamped: readonly StampedRange[];
  readonly bodies: readonly SearchWorkOrdersBody[];
}

interface ColumnsState {
  columns: Columns;
}

/** The lazy windows of the columns: the loads, the scroll windows, and the reloads after a change. */
export function withBoardColumns() {
  return signalStoreFeature(
    {
      state: type<{ plannedStart: PlainDateRange }>(),
      props: type<{ visibleStatuses: Signal<readonly WorkOrderStatus[]> }>(),
    },
    withState<ColumnsState>({ columns: initialColumns() }),
    withMethods((store) => {
      const grid = inject(WorkOrdersGridStore);
      const api = inject(WorkOrdersApi);
      const clock = inject(CLOCK);

      const patchColumn = (status: WorkOrderStatus, patch: Partial<ColumnState>): void => {
        patchState(store, (state) => ({
          columns: { ...state.columns, [status]: { ...state.columns[status], ...patch } },
        }));
      };

      /** The number of the latest request of each column. A page of an older request is dropped. */
      const latest: Partial<Record<WorkOrderStatus, number>> = {};

      const sendBatch = rxMethod<BatchSend>(
        pipe(
          mergeMap(({ stamped, bodies }) => {
            const isCurrent = ({ status, sequence }: StampedRange): boolean => latest[status] === sequence;
            return api.searchBatch(bodies).pipe(
              tap((pages) =>
                stamped.forEach((entry, index) => {
                  if (isCurrent(entry)) {
                    patchColumn(entry.status, {
                      isLoading: false,
                      window: {
                        offset: entry.range.first,
                        items: pages[index].items,
                        totalCount: pages[index].totalCount,
                      },
                    });
                  }
                }),
              ),
              catchError(() => {
                for (const entry of stamped.filter(isCurrent)) {
                  patchColumn(entry.status, { isLoading: false });
                }
                return EMPTY;
              }),
            );
          }),
        ),
      );

      /** Loads the ranges of the columns with one batch request: one search for each column. */
      const load = (ranges: readonly ColumnRange[]): void => {
        if (ranges.length === 0) {
          return;
        }
        const stamped = ranges.map(({ status, range }) => {
          const sequence = (latest[status] ?? 0) + 1;
          latest[status] = sequence;
          patchColumn(status, { range, requested: windowRequest(range), isLoading: true });
          return { status, range, sequence };
        });
        const bodies = stamped.map(({ status, range }) =>
          columnBody(status, range, grid.filter(), store.plannedStart(), clock.timeZoneId()),
        );
        sendBatch({ stamped, bodies });
      };

      return {
        /**
         * Loads the window of a lazy scroll event. The same window as the last request, and a range that the loaded rows
         * already hold, send nothing.
         */
        loadRange(status: WorkOrderStatus, range: ScrollRange): void {
          const column = store.columns()[status];
          if (
            store.visibleStatuses().includes(status) &&
            !isLoadedRange(range, column.window) &&
            isNewWindow(range, column.requested)
          ) {
            load([{ status, range }]);
          }
        },
        _loadRanges(ranges: readonly ColumnRange[]): void {
          load(ranges);
        },
        _resetColumn(status: WorkOrderStatus): void {
          patchColumn(status, initialColumn());
        },
        reloadAll(): void {
          patchState(store, { columns: initialColumns() });
          load(store.visibleStatuses().map((status) => ({ status, range: INITIAL_RANGE })));
        },
        reload(statuses: readonly WorkOrderStatus[]): void {
          load(
            statuses
              .filter((candidate) => store.visibleStatuses().includes(candidate))
              .map((status) => ({ status, range: store.columns()[status].range })),
          );
        },
        _markLoading(statuses: readonly WorkOrderStatus[], isLoading: boolean): void {
          for (const status of statuses) {
            patchColumn(status, { isLoading });
          }
        },
      };
    }),
    withHooks({
      onInit(store) {
        const grid = inject(WorkOrdersGridStore);
        effect(() => {
          void grid.filter();
          void store.plannedStart();
          untracked(() => store.reloadAll());
        });
      },
    }),
  );
}
