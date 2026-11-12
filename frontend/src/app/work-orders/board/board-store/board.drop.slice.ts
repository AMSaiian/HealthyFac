import { HttpStatusCode } from '@angular/common/http';
import { computed, effect, inject, type Signal, untracked } from '@angular/core';
import { patchState, signalStoreFeature, type, withComputed, withHooks, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, mergeMap, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import { reloadOnActionResult } from '@shared/actions/with-action-result-reload';
import { httpErrorKey, isHttpStatus, skipErrorToast } from '@shared/http/http-error-key';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';

import type { WorkOrderRow, WorkOrderStatus } from '@work-orders/work-order';
import { WorkOrderActionType, type WorkOrderRowActionPayload } from '@work-orders/work-order-actions';
import { WorkOrdersApi } from '@work-orders/work-orders-api';
import { BOARD_STATUSES, createdStatusOf, dropDecision, type PendingDrop, touchedColumns } from './board.store.utils';

const RELOAD_RESULT_TYPES: readonly string[] = [WorkOrderActionType.Saved, WorkOrderActionType.Conflict];

interface StatusDrop {
  readonly row: WorkOrderRow;
  readonly target: WorkOrderStatus;
}

interface DropState {
  pendingDrop: PendingDrop | null;
}

/** The drop of a card: a status change at once, an assign or an unassign through an action. */
export function withBoardDrop() {
  return signalStoreFeature(
    {
      props: type<{ visibleStatuses: Signal<readonly WorkOrderStatus[]> }>(),
      methods: type<{
        reload: (statuses: readonly WorkOrderStatus[]) => void;
        _markLoading: (statuses: readonly WorkOrderStatus[], isLoading: boolean) => void;
        _toastSuccess: (key: string) => void;
        _toastError: (key: string) => void;
      }>(),
    },
    withState<DropState>({ pendingDrop: null }),
    withComputed(() => {
      const auth = inject(AuthStore);
      return {
        canManage: computed(() => auth.hasPermission(Permission.WorkOrdersManage)),
      };
    }),
    withMethods((store) => {
      const api = inject(WorkOrdersApi);
      const actions = inject(ActionStore);

      const changeStatus = rxMethod<StatusDrop>(
        pipe(
          mergeMap(({ row, target }) => {
            const touched = touchedColumns(row.status, target);
            store._markLoading(touched, true);
            return api.changeStatus(row.id, { status: target, rowVersion: row.rowVersion }, skipErrorToast()).pipe(
              tap(() => {
                store._toastSuccess('WORK_ORDERS.TOASTS.UPDATED');
                store.reload(touched);
              }),
              catchError((error: unknown) => {
                if (isHttpStatus(error, HttpStatusCode.Conflict)) {
                  store._toastError('WORK_ORDERS.TOASTS.CONFLICT');
                  store.reload(touched);
                } else {
                  store._markLoading(touched, false);
                  store._toastError(
                    httpErrorKey(error, { [HttpStatusCode.UnprocessableEntity]: 'WORK_ORDERS.TOASTS.RULE' }),
                  );
                }
                return EMPTY;
              }),
            );
          }),
        ),
      );

      return {
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
                seenSequence: actions.lastResult()?.sequence ?? 0,
              },
            });
            void actions.dispatch({
              type: decision === 'assign' ? WorkOrderActionType.Assign : WorkOrderActionType.Unassign,
              payload,
            });
          }
        },
        requestOpen(id: string): void {
          void actions.dispatch({ type: WorkOrderActionType.Open, payload: { workOrderId: id } });
        },
        handleActionResult(detail?: unknown): void {
          const pending = store.pendingDrop();
          patchState(store, { pendingDrop: null });
          if (pending === null) {
            const created = createdStatusOf(detail);
            store.reload(created === null ? BOARD_STATUSES : [created]);
          } else {
            store.reload(touchedColumns(pending.source, pending.target));
          }
        },
      };
    }),
    withHooks({
      onInit(store) {
        const actions = inject(ActionStore);
        effect(() => {
          const active = actions.active();
          const pending = store.pendingDrop();
          if (pending === null) {
            return;
          }
          if (active !== null && !pending.isStarted) {
            untracked(() => patchState(store, { pendingDrop: { ...pending, isStarted: true } }));
          } else if (
            active === null &&
            pending.isStarted &&
            (actions.lastResult()?.sequence ?? 0) === pending.seenSequence
          ) {
            untracked(() => patchState(store, { pendingDrop: null }));
          }
        });
        reloadOnActionResult(actions, RELOAD_RESULT_TYPES, (result) => {
          store.handleActionResult(result.detail);
        });
      },
    }),
  );
}
