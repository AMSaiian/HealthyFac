import { HttpContext, HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { EMPTY, exhaustMap, finalize, type Observable, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';
import { httpErrorMessageKey } from '@shared/http/http-error-message';
import { createLookup } from '@shared/lookups/create-lookup';
import { type LookupOption, mergeLookupOptions } from '@shared/lookups/lookup-options';
import { LookupsApi } from '@shared/lookups/lookups-api';
import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import { type DaySchedule, type WorkOrder, WorkOrderStatus } from '@work-orders/work-order';
import { WorkOrderActionType, type WorkOrderRowActionPayload } from '@work-orders/work-order-actions';
import { WorkOrdersApi } from '@work-orders/work-orders-api';

import type { AssignFormValue } from './assign-dialog/assign-dialog-schema';
import { firstFreeStart, toPrefillDay } from './slot-prefill';

const SUCCESS_SUMMARY_KEY = 'WORK_ORDERS.TOASTS.SUCCESS';
const ERROR_SUMMARY_KEY = 'ERRORS.SUMMARY';

const CHANGEABLE_STATUSES: readonly WorkOrderStatus[] = [
  WorkOrderStatus.Staged,
  WorkOrderStatus.InProgress,
  WorkOrderStatus.Blocked,
  WorkOrderStatus.Completed,
  WorkOrderStatus.Rejected,
];

interface Selection {
  readonly assigneeId: string;
  readonly date: Temporal.PlainDate;
}

interface ActionState {
  action: AppAction | null;
  isSaving: boolean;
  isConfirmationShown: boolean;
  selection: Selection | null;
}

interface WriteRequest {
  readonly request$: Observable<unknown>;
  readonly isDialogOpen: boolean;
}

const payloadOf = (action: AppAction | null): Partial<WorkOrderRowActionPayload> =>
  (action?.payload ?? {}) as Partial<WorkOrderRowActionPayload>;

/**
 * Runs the row actions of the work orders (assign, move, unassign, change status, delete). A change completes the
 * action with `work-orders.saved`; a `409` completes it with `work-orders.conflict` after a toast, so the grid reloads;
 * a `422` shows the rule toast and keeps a dialog open.
 */
export const ScheduleActionsStore = signalStore(
  withState<ActionState>({ action: null, isSaving: false, isConfirmationShown: false, selection: null }),
  withProps(() => ({
    _api: inject(WorkOrdersApi),
    _lookups: inject(LookupsApi),
    _actions: inject(ActionStore),
    _confirmation: inject(ConfirmationService),
    _messages: inject(MessageService),
    _translate: inject(TranslateService),
    _clock: inject(CLOCK),
  })),
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
  withProps((store) => ({
    workOrder: rxResource({
      params: () => store.workOrderId(),
      stream: ({ params }) => store._api.get(params),
    }),
    employeeLookup: createLookup((search) => store._lookups.employees(search)),
    daySchedule: rxResource({
      params: () => {
        const selection = store.selection();
        return selection === null ? undefined : selection;
      },
      stream: ({ params }): Observable<DaySchedule> =>
        store._api.daySchedule(params.assigneeId, params.date.toString()),
    }),
  })),
  withComputed((store) => ({
    statusOptions: computed((): readonly WorkOrderStatus[] => {
      const current = store.workOrder.hasValue() ? store.workOrder.value().status : null;
      return CHANGEABLE_STATUSES.filter((status) => status !== current);
    }),
    suggestion: computed((): Temporal.PlainDateTime | null => {
      if (!store.daySchedule.hasValue() || !store.workOrder.hasValue()) {
        return null;
      }
      const start = firstFreeStart(
        toPrefillDay(store.daySchedule.value(), store.workOrderId()),
        store.workOrder.value().estimateMinutes,
        store._clock.now(),
      );
      return start === null ? null : start.toZonedDateTimeISO(store._clock.timeZoneId()).toPlainDateTime();
    }),
    initialAssignValue: computed((): { assigneeId: string; start: Temporal.PlainDateTime | null } | null => {
      if (!store.isScheduleDialog() || !store.workOrder.hasValue()) {
        return null;
      }
      const workOrder = store.workOrder.value();
      const zone = store._clock.timeZoneId();
      const start =
        store.type() === WorkOrderActionType.Move && workOrder.plannedStartLocal !== null
          ? Temporal.PlainDateTime.from(workOrder.plannedStartLocal)
          : store._clock.now().toZonedDateTimeISO(zone).toPlainDate().toPlainDateTime();
      return { assigneeId: store.type() === WorkOrderActionType.Move ? (workOrder.assigneeId ?? '') : '', start };
    }),
  })),
  withComputed((store) => ({
    hasNoFreeStart: computed(() => store.daySchedule.hasValue() && store.suggestion() === null),
    employeeOptions: computed((): LookupOption[] => {
      const current = store.workOrder.hasValue() ? store.workOrder.value() : null;
      return mergeLookupOptions(
        store.type() === WorkOrderActionType.Move && current?.assigneeId != null
          ? [{ id: current.assigneeId, displayName: current.assigneeName ?? current.assigneeId }]
          : [],
        store.employeeLookup.options(),
      );
    }),
  })),
  withMethods((store) => {
    const showError = (detailKey: string): void => {
      store._messages.add({
        severity: 'error',
        summary: store._translate.instant(ERROR_SUMMARY_KEY),
        detail: store._translate.instant(detailKey),
      });
    };

    const run = rxMethod<WriteRequest>(
      pipe(
        tap(() => patchState(store, { isSaving: true })),
        exhaustMap(({ request$, isDialogOpen }) =>
          request$.pipe(
            tap(() => {
              store._messages.add({
                severity: 'success',
                summary: store._translate.instant(SUCCESS_SUMMARY_KEY),
                detail: store._translate.instant('WORK_ORDERS.TOASTS.UPDATED'),
              });
              store._actions.complete(WorkOrderActionType.Saved);
            }),
            catchError((error: unknown) => {
              const status = error instanceof HttpErrorResponse ? error.status : 0;
              if (status === HttpStatusCode.Conflict) {
                showError('WORK_ORDERS.TOASTS.CONFLICT');
                store._actions.complete(WorkOrderActionType.Conflict);
                return EMPTY;
              }
              showError(
                status === HttpStatusCode.UnprocessableEntity ? 'WORK_ORDERS.TOASTS.RULE' : httpErrorMessageKey(status),
              );
              if (!isDialogOpen) {
                store._actions.cancel();
              }
              return EMPTY;
            }),
            finalize(() => patchState(store, { isSaving: false })),
          ),
        ),
      ),
    );

    const options = { context: new HttpContext().set(SKIP_ERROR_TOAST, true) };
    const idAndVersion = (): { id: string; rowVersion: number } => ({
      id: store.workOrderId() ?? '',
      rowVersion: store.rowVersion() ?? 0,
    });

    const confirm = (kind: 'unassign' | 'delete', accept: () => void): void => {
      const title = store.workOrder.hasValue() ? (store.workOrder.value() as WorkOrder).title : '';
      const upper = kind.toUpperCase();
      store._confirmation.confirm({
        header: store._translate.instant(`WORK_ORDERS.CONFIRM.${upper}_HEADER`),
        message: store._translate.instant(`WORK_ORDERS.CONFIRM.${upper}_MESSAGE`, { title }),
        icon: 'pi pi-exclamation-triangle',
        acceptLabel: store._translate.instant(`WORK_ORDERS.CONFIRM.${upper}_ACCEPT`),
        rejectLabel: store._translate.instant('ACTIONS.CANCEL'),
        acceptButtonProps: { severity: 'danger' },
        rejectButtonProps: { severity: 'secondary' },
        accept,
        reject: () => store._actions.cancel(),
      });
    };

    return {
      start(action: AppAction): void {
        patchState(store, { action, isConfirmationShown: false });
      },
      select(selection: Selection | null): void {
        patchState(store, { selection });
      },
      /** Opens the confirmation of unassign or delete once, after the work order loaded (its title is in the text). */
      requestConfirmation(): void {
        if (store.isConfirmationShown() || !store.workOrder.hasValue()) {
          return;
        }
        patchState(store, { isConfirmationShown: true });
        if (store.type() === WorkOrderActionType.Unassign) {
          confirm('unassign', () => {
            const { id, rowVersion } = idAndVersion();
            run({ request$: store._api.unassign(id, rowVersion, options), isDialogOpen: false });
          });
        } else if (store.type() === WorkOrderActionType.Delete) {
          confirm('delete', () => {
            const { id, rowVersion } = idAndVersion();
            run({ request$: store._api.delete(id, rowVersion, options), isDialogOpen: false });
          });
        }
      },
      submitSchedule(value: AssignFormValue): void {
        const { id, rowVersion } = idAndVersion();
        const start = value.start?.toString();
        if (start === undefined) {
          return;
        }
        run({
          request$:
            store.type() === WorkOrderActionType.Move
              ? store._api.move(id, { assigneeId: value.assigneeId, plannedStartLocal: start, rowVersion }, options)
              : store._api.assign(id, { assigneeId: value.assigneeId, plannedStartLocal: start, rowVersion }, options),
          isDialogOpen: true,
        });
      },
      submitStatus(status: WorkOrderStatus): void {
        const { id, rowVersion } = idAndVersion();
        run({ request$: store._api.changeStatus(id, { status, rowVersion }, options), isDialogOpen: true });
      },
      cancel(): void {
        store._actions.cancel();
      },
    };
  }),
);
