import { computed, effect, inject, untracked } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';

import { ActionStore } from '@shared/actions/action.store';
import { withGridState } from '@shared/grid/with-grid-state';
import { createLookup } from '@shared/lookups/create-lookup';
import { LookupsApi } from '@shared/lookups/lookups-api';
import { AuthStore } from '@shared/security/auth.store';
import { Permission } from '@shared/security/permissions';
import { CLOCK } from '@shared/time/clock';

import type { WorkOrderColumn, WorkOrderRow, WorkOrderSortColumn } from '@work-orders/work-order';
import { type WorkOrderFilter } from '@work-orders/work-order';
import { WorkOrderActionType, type WorkOrderRowActionPayload } from '@work-orders/work-order-actions';
import { toSearchBody } from '@work-orders/list/work-order-query';
import { INITIAL_GRID } from '@work-orders/list/work-orders-grid.store.utils';
import { WorkOrdersApi } from '@work-orders/work-orders-api';

type RowActionType =
  | typeof WorkOrderActionType.Assign
  | typeof WorkOrderActionType.Move
  | typeof WorkOrderActionType.Unassign
  | typeof WorkOrderActionType.ChangeStatus
  | typeof WorkOrderActionType.Delete;

const RELOAD_RESULT_TYPES: readonly string[] = [WorkOrderActionType.Saved, WorkOrderActionType.Conflict];

/**
 * The grid state and the rows of the work orders page. The route provides it, so the filters, the sort, and the page
 * stay while the user moves inside the app. A row action opens its action host, and the grid reloads when the host
 * reports a saved change or a conflict (`409`). A rule failure (`422`) keeps the dialog open and the rows as they are.
 */
export const WorkOrdersGridStore = signalStore(
  withGridState<WorkOrderFilter, WorkOrderSortColumn, WorkOrderColumn>(INITIAL_GRID),
  withState({ isListShown: false }),
  withProps(() => ({
    _auth: inject(AuthStore),
    _clock: inject(CLOCK),
    _actions: inject(ActionStore),
    _api: inject(WorkOrdersApi),
    _lookups: inject(LookupsApi),
  })),
  withComputed((store) => ({
    canWrite: computed(() => store._auth.hasPermission(Permission.WorkOrdersWrite)),
    canManage: computed(() => store._auth.hasPermission(Permission.WorkOrdersManage)),
  })),
  withProps((store) => ({
    workOrders: rxResource({
      params: () =>
        store.isListShown()
          ? toSearchBody(
              { offset: store.offset(), limit: store.limit(), sort: store.sort(), filter: store.filter() },
              store._clock.timeZoneId(),
            )
          : undefined,
      stream: ({ params }) => store._api.search(params),
    }),
    employeeLookup: createLookup(
      (search) => store._lookups.employees(search),
      () => store.filter().assigneeIds,
    ),
    machineLookup: createLookup(
      (search) => store._lookups.machines(search),
      () => store.filter().machineIds,
    ),
  })),
  withMethods((store) => ({
    /** Tells the store that the list page is open. The board shares this store and reads its rows through batches. */
    setListShown(isShown: boolean): void {
      patchState(store, { isListShown: isShown });
    },
    requestCreate(): void {
      void store._actions.dispatch({ type: WorkOrderActionType.Open, payload: {} });
    },
    requestOpen(id: string): void {
      void store._actions.dispatch({ type: WorkOrderActionType.Open, payload: { workOrderId: id } });
    },
    requestRowAction(type: RowActionType, row: WorkOrderRow): void {
      const payload: WorkOrderRowActionPayload = { workOrderId: row.id, rowVersion: row.rowVersion };
      void store._actions.dispatch({ type, payload });
    },
  })),
  withHooks({
    onInit(store) {
      let seen = store._actions.lastResult()?.sequence ?? 0;
      effect(() => {
        const result = store._actions.lastResult();
        if (result !== null && RELOAD_RESULT_TYPES.includes(result.type) && result.sequence > seen) {
          seen = result.sequence;
          untracked(() => store.workOrders.reload());
        }
      });
    },
  }),
);
