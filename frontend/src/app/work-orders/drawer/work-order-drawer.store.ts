import { HttpContext, HttpErrorResponse, HttpEventType, HttpStatusCode } from '@angular/common/http';
import { computed, effect, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { EMPTY, exhaustMap, filter, finalize, map, type Observable, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import { EmployeeStore } from '@shared/employee/employee.store';
import type { AppAction } from '@shared/actions/app-action';
import { httpErrorMessageKey } from '@shared/http/http-error-message';
import { createLookup } from '@shared/lookups/create-lookup';
import { type LookupOption, mergeLookupOptions } from '@shared/lookups/lookup-options';
import { LookupsApi } from '@shared/lookups/lookups-api';
import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';
import { AuthStore } from '@shared/security/auth.store';
import { Permission } from '@shared/security/permissions';

import type { FilesScope } from '@files/file';
import { FilesApi } from '@files/files-api';
import { type WorkOrder, WorkOrderStatus } from '@work-orders/work-order';
import { type SavedDetail, WorkOrderActionType, type WorkOrderRowActionPayload } from '@work-orders/work-order-actions';
import { statusTargets } from '@work-orders/board/board.store.utils';
import type { WorkOrderFormValue } from '@work-orders/work-order-schema';
import { WorkOrdersApi } from '@work-orders/work-orders-api';

import {
  EMPTY_VALUE,
  fileEntries,
  overlay,
  payloadOf,
  toCreateBody,
  toFormValue,
  toUpdateBody,
} from './work-order-drawer.store.utils';

const SUCCESS_SUMMARY_KEY = 'WORK_ORDERS.TOASTS.SUCCESS';
const ERROR_SUMMARY_KEY = 'ERRORS.SUMMARY';

interface DrawerState {
  action: AppAction | null;
  isHistoryActive: boolean;
  isSaving: boolean;
  isPickerOpen: boolean;
  fileIds: readonly string[];
  machineId: string | null;
}

const scopeOf = (machineId: string | null): FilesScope =>
  machineId === null ? { kind: 'company' } : { kind: 'machine', machineId };

/** Holds the work order drawer: the loaded work order, its history, the lookup of its files, and the save. */
export const WorkOrderDrawerStore = signalStore(
  withState<DrawerState>({
    action: null,
    isHistoryActive: false,
    isSaving: false,
    isPickerOpen: false,
    fileIds: [],
    machineId: null,
  }),
  withProps(() => ({
    _api: inject(WorkOrdersApi),
    _lookups: inject(LookupsApi),
    _files: inject(FilesApi),
    _auth: inject(AuthStore),
    _actions: inject(ActionStore),
    _employee: inject(EmployeeStore),
    _messages: inject(MessageService),
    _translate: inject(TranslateService),
  })),
  withComputed((store) => ({
    isUpdate: computed(() => payloadOf(store.action()).workOrderId !== undefined),
    workOrderId: computed(() => payloadOf(store.action()).workOrderId),
    canManage: computed(() => store._auth.hasPermission(Permission.WorkOrdersManage)),
    canWrite: computed(() => store._auth.hasPermission(Permission.WorkOrdersWrite)),
  })),
  withProps((store) => ({
    workOrder: rxResource({
      params: () => store.workOrderId(),
      stream: ({ params }) => store._api.get(params),
    }),
    history: rxResource({
      params: () => (store.isHistoryActive() ? store.workOrderId() : undefined),
      stream: ({ params }) => store._api.history(params),
    }),
    machineLookup: createLookup((search) => store._lookups.machines(search)),
    lookup: rxResource({
      params: () => (store.fileIds().length === 0 ? undefined : [...store.fileIds()]),
      stream: ({ params }) => store._api.lookupFiles(params).pipe(map((response) => response.files)),
    }),
  })),
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
  withComputed((store) => ({
    /** The status change is open to a supervisor, and to a worker for own assigned work orders. */
    canChangeStatus: computed(() => {
      const current = store.workOrder.hasValue() ? store.workOrder.value() : null;
      if (!store.isUpdate() || current === null) {
        return false;
      }
      const isOwn = current.assigneeId !== null && current.assigneeId === store._employee.id();
      return (
        (store.canManage() || (store.canWrite() && isOwn)) &&
        statusTargets(current.status, store.canManage()).length > 0
      );
    }),
    prefilledValue: computed((): WorkOrderFormValue | null => {
      const initial = store.initialValue();
      return initial === null ? null : overlay(initial, payloadOf(store.action()));
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

    const request$ = (value: WorkOrderFormValue): Observable<unknown> => {
      const options = { context: new HttpContext().set(SKIP_ERROR_TOAST, true) };
      if (!store.isUpdate()) {
        return store._api.create(toCreateBody(value), options);
      }
      const current: WorkOrder | undefined = store.workOrder.hasValue() ? store.workOrder.value() : undefined;
      return current === undefined
        ? EMPTY
        : store._api.update(store.workOrderId() ?? '', toUpdateBody(value, current, store.canManage()), options);
    };

    const handleFailure = (error: unknown): void => {
      const status = error instanceof HttpErrorResponse ? error.status : 0;
      if (status === HttpStatusCode.Conflict) {
        showError('WORK_ORDERS.TOASTS.CONFLICT');
        store.workOrder.reload();
        store.history.reload();
        return;
      }
      showError(
        status === HttpStatusCode.UnprocessableEntity ? 'WORK_ORDERS.TOASTS.RULE' : httpErrorMessageKey(status),
      );
    };

    return {
      start(action: AppAction): void {
        patchState(store, { action, isHistoryActive: false });
      },
      /** Starts the loading of the history: the History tab calls it when it opens, so a closed tab sends no request. */
      activateHistory(): void {
        patchState(store, { isHistoryActive: true });
      },
      /** Opens the status dialog. When it ends, the drawer returns and loads the work order and its history again. */
      requestChangeStatus(): void {
        const current = store.workOrder.hasValue() ? store.workOrder.value() : null;
        const action = store.action();
        if (current === null || action === null) {
          return;
        }
        const payload: WorkOrderRowActionPayload = { workOrderId: current.id, rowVersion: current.rowVersion };
        void store._actions.dispatch({ type: WorkOrderActionType.ChangeStatus, payload }, action);
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
      upload(file: File): Observable<string> {
        const options = { context: new HttpContext().set(SKIP_ERROR_TOAST, true) };
        return store._files.upload(scopeOf(store.machineId()), file, null, options).pipe(
          filter((event) => event.type === HttpEventType.Response),
          map((event) => (event as { body: { id: string } }).body.id),
          catchError((error: unknown) => {
            showError(
              error instanceof HttpErrorResponse
                ? httpErrorMessageKey(error.status)
                : 'WORK_ORDERS.TOASTS.UPLOAD_FAILED',
            );
            return EMPTY;
          }),
        );
      },
      save: rxMethod<WorkOrderFormValue>(
        pipe(
          tap(() => patchState(store, { isSaving: true })),
          exhaustMap((value) =>
            request$(value).pipe(
              tap(() => {
                store._messages.add({
                  severity: 'success',
                  summary: store._translate.instant(SUCCESS_SUMMARY_KEY),
                  detail: store._translate.instant(
                    store.isUpdate() ? 'WORK_ORDERS.TOASTS.UPDATED' : 'WORK_ORDERS.TOASTS.CREATED',
                  ),
                });
                store._actions.complete(
                  WorkOrderActionType.Saved,
                  store.isUpdate() ? undefined : ({ status: WorkOrderStatus.Backlog } satisfies SavedDetail),
                );
              }),
              catchError((error: unknown) => {
                handleFailure(error);
                return EMPTY;
              }),
              finalize(() => patchState(store, { isSaving: false })),
            ),
          ),
        ),
      ),
      cancel(): void {
        store._actions.cancel();
      },
    };
  }),
  withHooks({
    onInit(store) {
      effect(() => {
        if (store.workOrder.status() === 'error') {
          store._actions.cancel();
        }
      });
    },
  }),
);
