import { HttpContext, HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, effect, inject, untracked } from '@angular/core';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { debounceTime, EMPTY, mergeMap, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import { httpErrorMessageKey } from '@shared/http/http-error-message';
import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';
import { AuthStore } from '@shared/security/auth.store';
import { Permission } from '@shared/security/permissions';
import { CLOCK } from '@shared/time/clock';
import { type GridColumnState, reorderColumns, toggleColumn, visibleColumns } from '@shared/grid/table/grid-columns';
import { EMPTY_RANGE, type PlainDateRange } from '@shared/time/plain-date-range';

import type { WorkOrderStatus } from '@work-orders/work-order';
import { type SearchWorkOrdersBody, type WorkOrderRow } from '@work-orders/work-order';
import { type SavedDetail, WorkOrderActionType, type WorkOrderRowActionPayload } from '@work-orders/work-order-actions';
import { WorkOrdersGridStore } from '@work-orders/list/work-orders-grid.store';
import { WorkOrdersApi } from '@work-orders/work-orders-api';

import {
  BOARD_STATUSES,
  columnBody,
  type ColumnState,
  dropDecision,
  EMPTY_WINDOW,
  INITIAL_RANGE,
  isLoadedRange,
  isNewWindow,
  type ScrollRange,
  TITLE_DEBOUNCE_MS,
  touchedColumns,
  windowRequest,
} from './board.store.utils';

/** The status of a work order that was just created, from the detail of a `Saved` result, or `null` for another result. */
function createdStatusOf(detail: unknown): WorkOrderStatus | null {
  const status = (detail as Partial<SavedDetail> | undefined)?.status;
  return status !== undefined && BOARD_STATUSES.includes(status) ? status : null;
}

const SUCCESS_SUMMARY_KEY = 'WORK_ORDERS.TOASTS.SUCCESS';
const ERROR_SUMMARY_KEY = 'ERRORS.SUMMARY';
const RELOAD_RESULT_TYPES: readonly string[] = [WorkOrderActionType.Saved, WorkOrderActionType.Conflict];

type Columns = Readonly<Record<WorkOrderStatus, ColumnState>>;

interface PendingDrop {
  readonly source: WorkOrderStatus;
  readonly target: WorkOrderStatus;
  /** Becomes `true` when the action host of the drop is open; a close without a result then ends the drop. */
  readonly isStarted: boolean;
  readonly seenSequence: number;
}

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

interface StatusDrop {
  readonly row: WorkOrderRow;
  readonly target: WorkOrderStatus;
}

interface BoardState {
  titleDraft: string;
  plannedStart: PlainDateRange;
  columns: Columns;
  layout: readonly GridColumnState<WorkOrderStatus>[];
  pendingDrop: PendingDrop | null;
}

const initialColumn = (): ColumnState => ({
  window: EMPTY_WINDOW,
  range: INITIAL_RANGE,
  requested: null,
  isLoading: true,
});

const initialColumns = (): Columns =>
  Object.fromEntries(BOARD_STATUSES.map((status) => [status, initialColumn()])) as unknown as Columns;

/**
 * The status board: one lazy window per status column. The filter (title, priority, category, assignee, machine) comes
 * from the shared `WorkOrdersGridStore`; the planned start range is the own range of the board and defaults to today.
 * A drop moves the card only after the server accepted the change.
 */
export const BoardStore = signalStore(
  withState<BoardState>({
    titleDraft: '',
    plannedStart: EMPTY_RANGE,
    columns: initialColumns(),
    layout: BOARD_STATUSES.map((column) => ({ column, isVisible: true })),
    pendingDrop: null,
  }),
  withProps(() => ({
    _grid: inject(WorkOrdersGridStore),
    _api: inject(WorkOrdersApi),
    _auth: inject(AuthStore),
    _clock: inject(CLOCK),
    _actions: inject(ActionStore),
    _messages: inject(MessageService),
    _translate: inject(TranslateService),
  })),
  withComputed((store) => ({
    canManage: computed(() => store._auth.hasPermission(Permission.WorkOrdersManage)),
    canWrite: computed(() => store._auth.hasPermission(Permission.WorkOrdersWrite)),
    visibleStatuses: computed(() => visibleColumns(store.layout())),
  })),
  withMethods((store) => {
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
          return store._api.searchBatch(bodies).pipe(
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
        columnBody(status, range, store._grid.filter(), store.plannedStart(), store._clock.timeZoneId()),
      );
      sendBatch({ stamped, bodies });
    };

    const showError = (detailKey: string): void => {
      store._messages.add({
        severity: 'error',
        summary: store._translate.instant(ERROR_SUMMARY_KEY),
        detail: store._translate.instant(detailKey),
      });
    };

    const reload = (statuses: readonly WorkOrderStatus[]): void => {
      load(
        statuses
          .filter((candidate) => store.visibleStatuses().includes(candidate))
          .map((status) => ({ status, range: store.columns()[status].range })),
      );
    };

    const markLoading = (statuses: readonly WorkOrderStatus[]): void => {
      for (const status of statuses) {
        patchColumn(status, { isLoading: true });
      }
    };

    const reloadAll = (): void => {
      patchState(store, {
        columns: Object.fromEntries(BOARD_STATUSES.map((status) => [status, initialColumn()])) as unknown as Columns,
      });
      load(store.visibleStatuses().map((status) => ({ status, range: INITIAL_RANGE })));
    };

    const markLoadingDone = (statuses: readonly WorkOrderStatus[]): void => {
      for (const status of statuses) {
        patchColumn(status, { isLoading: false });
      }
    };

    const changeStatus = rxMethod<StatusDrop>(
      pipe(
        mergeMap(({ row, target }) => {
          const touched = touchedColumns(row.status, target);
          markLoading(touched);
          return store._api
            .changeStatus(
              row.id,
              { status: target, rowVersion: row.rowVersion },
              { context: new HttpContext().set(SKIP_ERROR_TOAST, true) },
            )
            .pipe(
              tap(() => {
                store._messages.add({
                  severity: 'success',
                  summary: store._translate.instant(SUCCESS_SUMMARY_KEY),
                  detail: store._translate.instant('WORK_ORDERS.TOASTS.UPDATED'),
                });
                reload(touched);
              }),
              catchError((error: unknown) => {
                const status = error instanceof HttpErrorResponse ? error.status : 0;
                if (status === HttpStatusCode.Conflict) {
                  showError('WORK_ORDERS.TOASTS.CONFLICT');
                  reload(touched);
                } else {
                  markLoadingDone(touched);
                  showError(
                    status === HttpStatusCode.UnprocessableEntity
                      ? 'WORK_ORDERS.TOASTS.RULE'
                      : httpErrorMessageKey(status),
                  );
                }
                return EMPTY;
              }),
            );
        }),
      ),
    );

    const pushTitle = rxMethod<string>(
      pipe(
        debounceTime(TITLE_DEBOUNCE_MS),
        tap((title) => store._grid.setFilter({ title })),
      ),
    );

    return {
      /** Keeps the text of the title box and writes it to the shared filter after a pause, so a key press reloads nothing. */
      setTitle(title: string): void {
        patchState(store, { titleDraft: title });
        pushTitle(title);
      },
      setPlannedStart(plannedStart: PlainDateRange): void {
        patchState(store, { plannedStart });
      },
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
      /** Shows or hides a column. The last visible column stays. A hidden column sends no search. */
      setColumnVisible(status: WorkOrderStatus, isVisible: boolean): void {
        const visible = store.visibleStatuses();
        if (visible.includes(status) === isVisible || (!isVisible && visible.length <= 1)) {
          return;
        }
        const flagged = store.layout().map((entry) => (entry.column === status ? { ...entry, isVisible } : entry));
        patchState(store, { layout: reorderColumns(flagged, toggleColumn(visible, status, isVisible)) });
        if (isVisible) {
          patchColumn(status, initialColumn());
          load([{ status, range: INITIAL_RANGE }]);
        }
      },
      /** Puts the visible columns in a new order. A hidden column keeps its slot. */
      reorder(order: readonly WorkOrderStatus[]): void {
        patchState(store, { layout: reorderColumns(store.layout(), order) });
      },
      reloadAll,
      reload,
      /** Starts the action of a drop. The card stays in its column until the server accepts the change. */
      drop(row: WorkOrderRow, target: WorkOrderStatus): void {
        const decision = dropDecision(row.status, target, store.canManage());
        const payload: WorkOrderRowActionPayload = { workOrderId: row.id, rowVersion: row.rowVersion };
        if (decision === 'status') {
          changeStatus({ row, target });
        } else if (decision === 'assign' || decision === 'unassign') {
          patchState(store, {
            pendingDrop: {
              source: row.status,
              target,
              isStarted: false,
              seenSequence: store._actions.lastResult()?.sequence ?? 0,
            },
          });
          void store._actions.dispatch({
            type: decision === 'assign' ? WorkOrderActionType.Assign : WorkOrderActionType.Unassign,
            payload,
          });
        }
      },
      requestOpen(id: string): void {
        void store._actions.dispatch({ type: WorkOrderActionType.Open, payload: { workOrderId: id } });
      },
      handleActionResult(detail?: unknown): void {
        const pending = store.pendingDrop();
        patchState(store, { pendingDrop: null });
        if (pending === null) {
          const created = createdStatusOf(detail);
          reload(created === null ? BOARD_STATUSES : [created]);
        } else {
          reload(touchedColumns(pending.source, pending.target));
        }
      },
    };
  }),
  withHooks({
    onInit(store) {
      const today = store._clock.now().toZonedDateTimeISO(store._clock.timeZoneId()).toPlainDate();
      store.setPlannedStart({ from: today, to: today });
      effect(() => {
        void store._grid.filter();
        void store.plannedStart();
        untracked(() => store.reloadAll());
      });
      effect(() => {
        const active = store._actions.active();
        const pending = store.pendingDrop();
        if (pending === null) {
          return;
        }
        if (active !== null && !pending.isStarted) {
          untracked(() => patchState(store, { pendingDrop: { ...pending, isStarted: true } }));
        } else if (
          active === null &&
          pending.isStarted &&
          (store._actions.lastResult()?.sequence ?? 0) === pending.seenSequence
        ) {
          untracked(() => patchState(store, { pendingDrop: null }));
        }
      });
      let seen = store._actions.lastResult()?.sequence ?? 0;
      effect(() => {
        const result = store._actions.lastResult();
        if (result !== null && RELOAD_RESULT_TYPES.includes(result.type) && result.sequence > seen) {
          seen = result.sequence;
          untracked(() => store.handleActionResult(result.detail));
        }
      });
    },
  }),
);
