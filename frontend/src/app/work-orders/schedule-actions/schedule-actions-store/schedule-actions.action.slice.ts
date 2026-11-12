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
import { createLookup } from '@shared/lookups/create-lookup';
import { type LookupOption, mergeLookupOptions } from '@shared/lookups/lookup-options';
import { LookupsApi } from '@shared/lookups/lookups-api';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import type { DaySchedule, WorkOrderStatus } from '@work-orders/work-order';
import { WorkOrderActionType } from '@work-orders/work-order-actions';
import { WorkOrdersApi } from '@work-orders/work-orders-api';

import type { AssignFormValue } from '@work-orders/schedule-actions/assign-dialog/assign-dialog-schema';
import { firstFreeStart, toPrefillDay } from '@work-orders/schedule-actions/slot-prefill';
import { CHANGEABLE_STATUSES, payloadOf, type Selection } from './schedule-actions.store.utils';

interface ActionState {
  action: AppAction | null;
  selection: Selection | null;
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
    withState<ActionState>({ action: null, selection: null, isConfirmationShown: false, isSaving: false }),
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
      const lookups = inject(LookupsApi);
      return {
        workOrder: rxResource({
          params: () => store.workOrderId(),
          stream: ({ params }) => api.get(params),
        }),
        employeeLookup: createLookup((search) => lookups.employees(search)),
        daySchedule: rxResource({
          params: () => {
            const selection = store.selection();
            return selection === null ? undefined : selection;
          },
          stream: ({ params }): Observable<DaySchedule> => api.daySchedule(params.assigneeId, params.date.toString()),
        }),
      };
    }),
    withComputed((store) => {
      const clock = inject(CLOCK);
      return {
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
            clock.now(),
          );
          return start === null ? null : start.toZonedDateTimeISO(clock.timeZoneId()).toPlainDateTime();
        }),
        initialAssignValue: computed((): { assigneeId: string; start: Temporal.PlainDateTime | null } | null => {
          if (!store.isScheduleDialog() || !store.workOrder.hasValue()) {
            return null;
          }
          const workOrder = store.workOrder.value();
          const zone = clock.timeZoneId();
          const start =
            store.type() === WorkOrderActionType.Move && workOrder.plannedStartLocal !== null
              ? Temporal.PlainDateTime.from(workOrder.plannedStartLocal)
              : clock.now().toZonedDateTimeISO(zone).toPlainDate().toPlainDateTime();
          return { assigneeId: store.type() === WorkOrderActionType.Move ? (workOrder.assigneeId ?? '') : '', start };
        }),
      };
    }),
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
    withMethods((store) => ({
      start(action: AppAction): void {
        patchState(store, { action, isConfirmationShown: false });
      },
      select(selection: Selection | null): void {
        patchState(store, { selection });
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
        submitSchedule(value: AssignFormValue): void {
          const { id, rowVersion } = idAndVersion();
          const start = value.start?.toString();
          if (start === undefined) {
            return;
          }
          run({
            request$:
              store.type() === WorkOrderActionType.Move
                ? api.move(id, { assigneeId: value.assigneeId, plannedStartLocal: start, rowVersion }, options)
                : api.assign(id, { assigneeId: value.assigneeId, plannedStartLocal: start, rowVersion }, options),
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
