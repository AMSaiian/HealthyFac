import { computed, effect, inject, type ResourceRef, type Signal, untracked } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import {
  patchState,
  signalStoreFeature,
  type,
  withComputed,
  withHooks,
  withMethods,
  withProps,
  withState,
} from '@ngrx/signals';

import { idSetFilter, setFilter, textFilter } from '@shared/grid/requests/grid-filters';
import { SortDirection } from '@shared/grid/requests/grid-request';
import { axisOf } from '@shared/schedule/schedule-board/schedule-board.utils';
import type { ScheduleRow } from '@shared/schedule/schedule-row';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import type { CompanyDaySchedule } from '@schedule/schedule-api';
import {
  type SearchWorkOrdersBody,
  type WorkOrderFilter,
  WorkOrderSortColumn,
  WorkOrderStatus,
} from '@work-orders/work-order';
import { WorkOrdersApi } from '@work-orders/work-orders-api';
import { WorkOrdersGridStore } from '@work-orders/list/work-orders-grid.store';
import { STAGING_LIMIT } from '@work-orders/work-order-limits';
import {
  applyPending,
  canApply,
  type ChangeTarget,
  conflictsOf,
  type KnownWorkOrder,
  localStartOf,
  type PendingChange,
  stagingOf,
  stagingTotalOf,
  targetOf,
  withTarget,
} from './schedule.store.utils';

interface PendingState {
  pending: readonly PendingChange[];
  known: Readonly<Record<string, KnownWorkOrder>>;
  visited: Readonly<Record<string, readonly ScheduleRow[]>>;
  /** Whether a hint came while changes were pending. The tab keeps the changes and tells the user. */
  isChangedRemotely: boolean;
  /** Whether the backlog drawer is open. It starts closed and keeps its state when changes are discarded. */
  isBacklogOpen: boolean;
}

export interface BarDrop {
  readonly workOrderId: string;
  readonly assigneeId: string;
  readonly startInstant: Temporal.Instant;
}

export interface BarResize {
  readonly workOrderId: string;
  readonly estimateMinutes: number;
}

const EMPTY_PENDING: PendingState = {
  pending: [],
  known: {},
  visited: {},
  isChangedRemotely: false,
  isBacklogOpen: false,
};

function stagingBodyOf(filter: WorkOrderFilter): SearchWorkOrdersBody {
  return {
    offset: 0,
    limit: STAGING_LIMIT,
    sort: { column: WorkOrderSortColumn.Priority, direction: SortDirection.Descending },
    filter: {
      title: textFilter(filter.title),
      statuses: setFilter([WorkOrderStatus.Backlog]),
      priorities: setFilter(filter.priorities),
      categories: setFilter(filter.categories),
      assignee: idSetFilter(filter.assigneeIds),
      machine: idSetFilter(filter.machineIds),
    },
  };
}

/** The pending changes of the schedule tab, the known server state, the staging list, and the conflicts. */
export function withSchedulePending() {
  return signalStoreFeature(
    {
      state: type<{ date: Temporal.PlainDate | null }>(),
      props: type<{
        canManage: Signal<boolean>;
        rows: Signal<readonly ScheduleRow[]>;
        day: ResourceRef<CompanyDaySchedule | undefined>;
      }>(),
    },
    withState<PendingState>(EMPTY_PENDING),
    withProps((store) => {
      const grid = inject(WorkOrdersGridStore);
      const api = inject(WorkOrdersApi);
      const clock = inject(CLOCK);
      return {
        staging: rxResource({
          params: () => (store.canManage() ? grid.filter() : undefined),
          stream: ({ params }) => api.search(stagingBodyOf(params)),
        }),
        _zone: () => clock.timeZoneId(),
      };
    }),
    withComputed((store) => {
      const knownMap = computed(() => new Map(Object.entries(store.known())));
      const pendingIds = computed(() => new Set(store.pending().map((entry) => entry.workOrderId)));
      const conflictIds = computed(() => {
        const result = new Set<string>();
        for (const [dateKey, rows] of Object.entries(store.visited())) {
          const axis = axisOf(Temporal.PlainDate.from(dateKey), store._zone());
          const drawn = applyPending(rows, store.pending(), knownMap(), axis, store._zone());
          conflictsOf(drawn, pendingIds()).forEach((id) => result.add(id));
        }
        return result;
      });
      const backlog = computed(() =>
        store.staging.hasValue()
          ? store.staging.value().items.map((item) => ({
              id: item.id,
              title: item.title,
              priority: item.priority,
              estimateMinutes: item.estimateMinutes,
            }))
          : [],
      );
      return {
        pendingIds,
        conflictIds,
        pendingCount: computed(() => store.pending().length),
        canApply: computed(() => canApply(store.pending(), conflictIds())),
        displayRows: computed(() => {
          const date = store.date();
          return date === null
            ? store.rows()
            : applyPending(store.rows(), store.pending(), knownMap(), axisOf(date, store._zone()), store._zone());
        }),
        stagingItems: computed(() => stagingOf(backlog(), store.pending(), knownMap())),
        stagingTotal: computed(() =>
          stagingTotalOf(
            store.staging.hasValue() ? store.staging.value().totalCount : 0,
            backlog(),
            store.pending(),
            knownMap(),
          ),
        ),
        isStagingCut: computed(
          () => store.staging.hasValue() && store.staging.value().items.length < store.staging.value().totalCount,
        ),
        _knownMap: knownMap,
      };
    }),
    withMethods((store) => {
      const stage = (workOrderId: string, patch: Partial<ChangeTarget>): void => {
        const base = store._knownMap().get(workOrderId);
        if (base === undefined) {
          return;
        }
        const current = store.pending().find((entry) => entry.workOrderId === workOrderId);
        const target: ChangeTarget = { ...(current ?? targetOf(base)), ...patch };
        patchState(store, { pending: withTarget(store.pending(), base, target) });
      };

      return {
        dropBar(drop: BarDrop): void {
          stage(drop.workOrderId, {
            assigneeId: drop.assigneeId,
            plannedStartLocal: localStartOf(drop.startInstant, store._zone()),
          });
        },
        resizeBar(resize: BarResize): void {
          stage(resize.workOrderId, { estimateMinutes: resize.estimateMinutes });
        },
        unassignBar(workOrderId: string): void {
          stage(workOrderId, { assigneeId: null, plannedStartLocal: null });
        },
        reloadAll(): void {
          store.day.reload();
          store.staging.reload();
        },
        setBacklogOpen(isOpen: boolean): void {
          patchState(store, { isBacklogOpen: isOpen });
        },
        toggleBacklog(): void {
          patchState(store, (state) => ({ isBacklogOpen: !state.isBacklogOpen }));
        },
        clearPending(): void {
          patchState(store, { pending: [], visited: {}, isChangedRemotely: false });
        },
        /** Reloads the day and the staging area, or holds the reload while changes are pending. */
        reloadOrHold(): void {
          if (store.pending().length > 0) {
            patchState(store, { isChangedRemotely: true });
            return;
          }
          store.day.reload();
          store.staging.reload();
        },
      };
    }),
    withHooks({
      onInit(store) {
        effect(() => {
          if (!store.day.hasValue()) {
            return;
          }
          const day = store.day.value();
          untracked(() => {
            const known: Record<string, KnownWorkOrder> = {};
            for (const employee of day.employees) {
              for (const workOrder of employee.day.workOrders) {
                known[workOrder.id] = {
                  id: workOrder.id,
                  title: workOrder.title,
                  status: workOrder.status,
                  priority: workOrder.priority,
                  rowVersion: workOrder.rowVersion,
                  estimateMinutes: workOrder.estimateMinutes,
                  assigneeId: employee.employeeId,
                  plannedStartLocal: workOrder.plannedStartLocal,
                };
              }
            }
            patchState(store, (state) => ({
              known: { ...state.known, ...known },
              visited: { ...state.visited, [day.date]: store.rows() },
            }));
          });
        });
        effect(() => {
          if (!store.staging.hasValue()) {
            return;
          }
          const items = store.staging.value().items;
          untracked(() => {
            const known: Record<string, KnownWorkOrder> = {};
            for (const item of items) {
              known[item.id] = {
                id: item.id,
                title: item.title,
                status: item.status,
                priority: item.priority,
                rowVersion: item.rowVersion,
                estimateMinutes: item.estimateMinutes,
                assigneeId: null,
                plannedStartLocal: null,
              };
            }
            patchState(store, (state) => ({ known: { ...state.known, ...known } }));
          });
        });
      },
    }),
  );
}
