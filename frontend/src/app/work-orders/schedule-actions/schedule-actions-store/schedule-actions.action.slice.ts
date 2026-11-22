import { HttpStatusCode } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStoreFeature, type, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, finalize, type Observable, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';
import type { ConfirmRequest } from '@shared/confirm/with-confirm';
import { httpErrorKey, isHttpStatus, skipErrorToast } from '@shared/http/http-error-key';

import type { AssignmentValue } from '@work-orders/assignment-picker/assignment-picker.utils';
import type { WorkOrderStatus } from '@work-orders/work-order';
import { WorkOrderActionType } from '@work-orders/work-order-actions';
import { WorkOrdersApi } from '@work-orders/work-orders-api';

import { CHANGEABLE_STATUSES, payloadOf } from './schedule-actions.store.utils';

interface ActionState {
  action: AppAction | null;
  isConfirmationShown: boolean;
  isSaving: boolean;
}

interface WriteRequest {
  readonly request$: Observable<unknown>;
  readonly isDialogOpen: boolean;
}

/** The confirmation keys of each destructive kind. Each key is a literal, so the translation check finds it. */
const CONFIRM_KEYS = {
  unassign: {
    headerKey: 'WORK_ORDERS.CONFIRM.UNASSIGN_HEADER',
    messageKey: 'WORK_ORDERS.CONFIRM.UNASSIGN_MESSAGE',
    acceptKey: 'WORK_ORDERS.CONFIRM.UNASSIGN_ACCEPT',
  },
  delete: {
    headerKey: 'WORK_ORDERS.CONFIRM.DELETE_HEADER',
    messageKey: 'WORK_ORDERS.CONFIRM.DELETE_MESSAGE',
    acceptKey: 'ACTIONS.DELETE',
  },
} as const;

/**
 * The open action, its day selection, the reads that the dialogs show (the work order, the employees, and the day),
 * the save in progress, and the writes of the dialogs.
 */
export function withScheduleActionsAction() {
  return signalStoreFeature(
    {
      methods: type<{
        _toastSuccess: (key: string) => void;
        _toastError: (key: string) => void;
        _confirmDestructive: (request: ConfirmRequest) => void;
      }>(),
    },
    withState<ActionState>({ action: null, isConfirmationShown: false, isSaving: false }),
    withComputed((store) => ({
      type: computed(() => store.action()?.type ?? null),
      workOrderId: computed(() => payloadOf(store.action()).workOrderId),
      rowVersion: computed(() => payloadOf(store.action()).rowVersion),
    })),
    withComputed((store) => ({
      isScheduleDialog: computed(
        () => store.type() === WorkOrderActionType.Assign || store.type() === WorkOrderActionType.Move,
      ),
      isStatusDialog: computed(() => store.type() === WorkOrderActionType.ChangeStatus),
    })),
    withProps((store) => {
      const api = inject(WorkOrdersApi);
      return {
        workOrder: rxResource({
          params: () => store.workOrderId(),
          stream: ({ params }) => api.get(params),
        }),
      };
    }),
    withComputed((store) => {
      return {
        statusOptions: computed((): readonly WorkOrderStatus[] => {
          const current = store.workOrder.hasValue() ? store.workOrder.value().status : null;
          return CHANGEABLE_STATUSES.filter((status) => status !== current);
        }),
      };
    }),
    withMethods((store) => ({
      start(action: AppAction): void {
        patchState(store, { action, isConfirmationShown: false });
      },
      _showConfirmation(): void {
        patchState(store, { isConfirmationShown: true });
      },
      _setSaving(isSaving: boolean): void {
        patchState(store, { isSaving });
      },
    })),
    withMethods((store) => {
      const api = inject(WorkOrdersApi);
      const actions = inject(ActionStore);

      const run = rxMethod<WriteRequest>(
        pipe(
          tap(() => store._setSaving(true)),
          exhaustMap(({ request$, isDialogOpen }) =>
            request$.pipe(
              tap(() => {
                store._toastSuccess('WORK_ORDERS.TOASTS.UPDATED');
                actions.complete(WorkOrderActionType.Saved);
              }),
              catchError((error: unknown) => {
                if (isHttpStatus(error, HttpStatusCode.Conflict)) {
                  store._toastError('WORK_ORDERS.TOASTS.CONFLICT');
                  actions.complete(WorkOrderActionType.Conflict);
                  return EMPTY;
                }
                store._toastError(
                  httpErrorKey(error, { [HttpStatusCode.UnprocessableEntity]: 'WORK_ORDERS.TOASTS.RULE' }),
                );
                if (!isDialogOpen) {
                  actions.cancel();
                }
                return EMPTY;
              }),
              finalize(() => store._setSaving(false)),
            ),
          ),
        ),
      );

      const options = skipErrorToast();
      const idAndVersion = (): { id: string; rowVersion: number } => ({
        id: store.workOrderId() ?? '',
        rowVersion: store.rowVersion() ?? 0,
      });

      const confirm = (kind: keyof typeof CONFIRM_KEYS, accept: () => void): void => {
        const title = store.workOrder.hasValue() ? store.workOrder.value().title : '';
        store._confirmDestructive({
          ...CONFIRM_KEYS[kind],
          params: { title },
          onAccept: accept,
          onReject: () => actions.cancel(),
        });
      };

      return {
        /** Opens the confirmation of unassign or delete once, after the work order loaded (its title is in the text). */
        requestConfirmation(): void {
          if (store.isConfirmationShown() || !store.workOrder.hasValue()) {
            return;
          }
          store._showConfirmation();
          if (store.type() === WorkOrderActionType.Unassign) {
            confirm('unassign', () => {
              const { id, rowVersion } = idAndVersion();
              run({ request$: api.unassign(id, rowVersion, options), isDialogOpen: false });
            });
          } else if (store.type() === WorkOrderActionType.Delete) {
            confirm('delete', () => {
              const { id, rowVersion } = idAndVersion();
              run({ request$: api.delete(id, rowVersion, options), isDialogOpen: false });
            });
          }
        },
        submitSchedule(value: AssignmentValue): void {
          const { id, rowVersion } = idAndVersion();
          if (value.assigneeId === null || value.start === null) {
            return;
          }
          const body = { assigneeId: value.assigneeId, plannedStartLocal: value.start.toString(), rowVersion };
          run({
            request$:
              store.type() === WorkOrderActionType.Move ? api.move(id, body, options) : api.assign(id, body, options),
            isDialogOpen: true,
          });
        },
        submitStatus(status: WorkOrderStatus): void {
          const { id, rowVersion } = idAndVersion();
          run({ request$: api.changeStatus(id, { status, rowVersion }, options), isDialogOpen: true });
        },
        cancel(): void {
          actions.cancel();
        },
      };
    }),
  );
}
