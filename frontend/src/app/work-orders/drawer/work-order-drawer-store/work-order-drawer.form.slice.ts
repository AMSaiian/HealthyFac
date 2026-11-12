import { HttpErrorResponse, HttpEventType } from '@angular/common/http';
import { computed, inject, type ResourceRef, type Signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStoreFeature, type, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { EMPTY, filter, map, type Observable } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';
import { EmployeeStore } from '@shared/employee/employee.store';
import { httpErrorKey, skipErrorToast } from '@shared/http/http-error-key';
import { createLookup } from '@shared/lookups/create-lookup';
import { type LookupOption, mergeLookupOptions } from '@shared/lookups/lookup-options';
import { LookupsApi } from '@shared/lookups/lookups-api';

import { FilesApi } from '@files/files-api';
import { type WorkOrder } from '@work-orders/work-order';
import { statusTargets } from '@work-orders/board/board-store/board.store.utils';
import { WorkOrdersApi } from '@work-orders/work-orders-api';
import { WorkOrderActionType, type WorkOrderRowActionPayload } from '@work-orders/work-order-actions';
import type { WorkOrderFormValue } from '@work-orders/work-order-schema';
import { EMPTY_VALUE, fileEntries, overlay, payloadOf, scopeOf, toFormValue } from './work-order-drawer.store.utils';

interface FormState {
  fileIds: readonly string[];
  machineId: string | null;
  isPickerOpen: boolean;
}

/**
 * The form of the drawer: the attached files, the machine, the picker, the lookups, the upload of a file, and the
 * status change.
 */
export function withWorkOrderDrawerForm() {
  return signalStoreFeature(
    {
      state: type<{ action: AppAction | null }>(),
      props: type<{
        isUpdate: Signal<boolean>;
        canManage: Signal<boolean>;
        canWrite: Signal<boolean>;
        workOrder: ResourceRef<WorkOrder | undefined>;
      }>(),
      methods: type<{ _toastError: (key: string) => void }>(),
    },
    withState<FormState>({ fileIds: [], machineId: null, isPickerOpen: false }),
    withProps((store) => {
      const api = inject(WorkOrdersApi);
      const lookups = inject(LookupsApi);
      return {
        machineLookup: createLookup((search) => lookups.machines(search)),
        lookup: rxResource({
          params: () => (store.fileIds().length === 0 ? undefined : [...store.fileIds()]),
          stream: ({ params }) => api.lookupFiles(params).pipe(map((response) => response.files)),
        }),
      };
    }),
    withComputed((store) => ({
      initialValue: computed((): WorkOrderFormValue | null => {
        if (!store.isUpdate()) {
          return store.action() === null ? null : EMPTY_VALUE;
        }
        return store.workOrder.hasValue() ? toFormValue(store.workOrder.value()) : null;
      }),
      machineOptions: computed((): LookupOption[] => {
        const current = store.workOrder.hasValue() ? store.workOrder.value() : null;
        return mergeLookupOptions(
          current?.machineId == null
            ? []
            : [{ id: current.machineId, displayName: current.machineName ?? current.machineId }],
          store.machineLookup.options(),
        );
      }),
      fileEntries: computed(() => fileEntries(store.fileIds(), store.lookup.hasValue() ? store.lookup.value() : [])),
    })),
    withComputed((store) => {
      const employee = inject(EmployeeStore);
      return {
        /** The status change is open to a supervisor, and to a worker for own assigned work orders. */
        canChangeStatus: computed(() => {
          const current = store.workOrder.hasValue() ? store.workOrder.value() : null;
          if (!store.isUpdate() || current === null) {
            return false;
          }
          const isOwn = current.assigneeId !== null && current.assigneeId === employee.id();
          return (
            (store.canManage() || (store.canWrite() && isOwn)) &&
            statusTargets(current.status, store.canManage()).length > 0
          );
        }),
        prefilledValue: computed((): WorkOrderFormValue | null => {
          const initial = store.initialValue();
          return initial === null ? null : overlay(initial, payloadOf(store.action()));
        }),
      };
    }),
    withMethods((store) => {
      const actions = inject(ActionStore);
      const files = inject(FilesApi);
      return {
        /** Uploads a file for the work order. A failed upload shows a toast and returns no id. */
        upload(file: File): Observable<string> {
          const options = skipErrorToast();
          return files.upload(scopeOf(store.machineId()), file, null, options).pipe(
            filter((event) => event.type === HttpEventType.Response),
            map((event) => (event as { body: { id: string } }).body.id),
            catchError((error: unknown) => {
              store._toastError(
                error instanceof HttpErrorResponse ? httpErrorKey(error) : 'WORK_ORDERS.TOASTS.UPLOAD_FAILED',
              );
              return EMPTY;
            }),
          );
        },
        /** Opens the status dialog. When it ends, the drawer returns and loads the work order and its history again. */
        requestChangeStatus(): void {
          const current = store.workOrder.hasValue() ? store.workOrder.value() : null;
          const action = store.action();
          if (current === null || action === null) {
            return;
          }
          const payload: WorkOrderRowActionPayload = { workOrderId: current.id, rowVersion: current.rowVersion };
          void actions.dispatch({ type: WorkOrderActionType.ChangeStatus, payload }, action);
        },
        setFormState(fileIds: readonly string[], machineId: string | null): void {
          patchState(store, { fileIds, machineId });
        },
        openPicker(): void {
          patchState(store, { isPickerOpen: true });
        },
        closePicker(): void {
          patchState(store, { isPickerOpen: false });
        },
      };
    }),
  );
}
