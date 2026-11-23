import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { inject } from '@angular/core';
import { patchState, signalStoreFeature, type, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import type { ConfirmRequest } from '@shared/confirm/with-confirm';
import { httpErrorKey, isHttpStatus, skipErrorToast } from '@shared/http/http-error-key';

import { ScheduleChangeRejection } from '@work-orders/work-order';
import { WorkOrdersApi } from '@work-orders/work-orders-api';
import type { PendingChange } from './schedule.store.utils';

export const REJECTION_KEYS: Readonly<Record<ScheduleChangeRejection, string>> = {
  [ScheduleChangeRejection.NotEditable]: 'WORK_ORDERS.SCHEDULE_TAB.REJECTIONS.NOT_EDITABLE',
  [ScheduleChangeRejection.AssigneeNotActive]: 'WORK_ORDERS.SCHEDULE_TAB.REJECTIONS.ASSIGNEE_NOT_ACTIVE',
  [ScheduleChangeRejection.StartOutOfRange]: 'WORK_ORDERS.SCHEDULE_TAB.REJECTIONS.START_OUT_OF_RANGE',
  [ScheduleChangeRejection.OutsideShift]: 'WORK_ORDERS.SCHEDULE_TAB.REJECTIONS.OUTSIDE_SHIFT',
  [ScheduleChangeRejection.SlotTaken]: 'WORK_ORDERS.SCHEDULE_TAB.REJECTIONS.SLOT_TAKEN',
};

interface ApplyState {
  isApplying: boolean;
  failures: Readonly<Record<string, string>>;
}

interface RejectedChange {
  readonly workOrderId: string;
  readonly reason: ScheduleChangeRejection;
}

/** Maps the `changes` extension of a `422` to the translation key of each failed work order. */
export function failuresOf(error: unknown): Record<string, string> {
  const body: unknown = error instanceof HttpErrorResponse ? error.error : null;
  const changes = (body as { changes?: readonly RejectedChange[] } | null)?.changes;
  return Object.fromEntries((changes ?? []).map((change) => [change.workOrderId, REJECTION_KEYS[change.reason]]));
}

/** Apply and Discard of the pending changes. A failed apply keeps the changes. */
export function withScheduleApply() {
  return signalStoreFeature(
    {
      state: type<{ pending: readonly PendingChange[] }>(),
      methods: type<{
        clearPending: () => void;
        reloadAll: () => void;
        _toastSuccess: (key: string) => void;
        _toastError: (key: string) => void;
        _confirmDestructive: (request: ConfirmRequest) => void;
      }>(),
    },
    withState<ApplyState>({ isApplying: false, failures: {} }),
    withMethods((store) => {
      const api = inject(WorkOrdersApi);

      const send = rxMethod<readonly PendingChange[]>(
        pipe(
          tap(() => patchState(store, { isApplying: true, failures: {} })),
          exhaustMap((changes) =>
            api.applySchedule({ changes }, skipErrorToast()).pipe(
              tap(() => {
                patchState(store, { isApplying: false });
                store.clearPending();
                store._toastSuccess('WORK_ORDERS.SCHEDULE_TAB.APPLIED');
                store.reloadAll();
              }),
              catchError((error: unknown) => {
                patchState(store, { isApplying: false });
                if (isHttpStatus(error, HttpStatusCode.Conflict)) {
                  store._toastError('WORK_ORDERS.TOASTS.CONFLICT');
                } else if (isHttpStatus(error, HttpStatusCode.UnprocessableEntity)) {
                  patchState(store, { failures: failuresOf(error) });
                  store._toastError('WORK_ORDERS.SCHEDULE_TAB.REJECTED');
                } else {
                  store._toastError(httpErrorKey(error));
                }
                return EMPTY;
              }),
            ),
          ),
        ),
      );

      return {
        apply(): void {
          send(store.pending());
        },
        discard(): void {
          store._confirmDestructive({
            headerKey: 'WORK_ORDERS.SCHEDULE_TAB.CONFIRM.DISCARD_HEADER',
            messageKey: 'WORK_ORDERS.SCHEDULE_TAB.CONFIRM.DISCARD_MESSAGE',
            acceptKey: 'WORK_ORDERS.SCHEDULE_TAB.CONFIRM.DISCARD_ACCEPT',
            onAccept: () => {
              patchState(store, { failures: {} });
              store.clearPending();
              store.reloadAll();
            },
          });
        },
      };
    }),
  );
}
