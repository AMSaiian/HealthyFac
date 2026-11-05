import { HttpContext, HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { EMPTY, exhaustMap, finalize, type Observable, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { httpErrorMessageKey } from '@shared/http/http-error-message';
import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';
import { MachineErrorCodesApi } from '@machines/error-codes/machine-error-codes-api';

import { EMPTY_VALUE, toFormValue } from './machine-error-code-drawer.store.utils';
import type { MachineErrorCodeFormValue } from './machine-error-code-form-schema';

const SUCCESS_SUMMARY_KEY = 'MACHINE_ERROR_CODES.TOASTS.SUCCESS';
const ERROR_SUMMARY_KEY = 'ERRORS.SUMMARY';

/**
 * One save of the dialog. `rxMethod` returns no result to its caller, so the caller passes `onSaved`, and the store
 * calls it once after a successful save.
 */
interface SaveRequest {
  readonly value: MachineErrorCodeFormValue;
  readonly onSaved: () => void;
}

interface DrawerState {
  machineId: string | null;
  errorCodeId: string | null;
  isSaving: boolean;
}

export const MachineErrorCodeDrawerStore = signalStore(
  withState<DrawerState>({ machineId: null, errorCodeId: null, isSaving: false }),
  withProps(() => ({
    _api: inject(MachineErrorCodesApi),
    _messages: inject(MessageService),
    _translate: inject(TranslateService),
  })),
  withComputed((store) => ({
    isUpdate: computed(() => store.errorCodeId() !== null),
  })),
  withProps((store) => ({
    errorCode: rxResource({
      params: () => {
        const machineId = store.machineId();
        const errorCodeId = store.errorCodeId();
        return machineId === null || errorCodeId === null ? undefined : { machineId, errorCodeId };
      },
      stream: ({ params }) => store._api.get(params.machineId, params.errorCodeId),
    }),
  })),
  withComputed((store) => ({
    initialValue: computed((): MachineErrorCodeFormValue | null => {
      if (store.machineId() === null) {
        return null;
      }
      if (!store.isUpdate()) {
        return EMPTY_VALUE;
      }
      return store.errorCode.hasValue() ? toFormValue(store.errorCode.value()) : null;
    }),
  })),
  withMethods((store) => {
    const request$ = (value: MachineErrorCodeFormValue): Observable<unknown> => {
      const machineId = store.machineId() ?? '';
      const errorCodeId = store.errorCodeId();
      const body = { code: value.code.trim(), description: value.description };
      const options = { context: new HttpContext().set(SKIP_ERROR_TOAST, true) };
      return errorCodeId === null
        ? store._api.create(machineId, body, options)
        : store._api.update(machineId, errorCodeId, body, options);
    };

    return {
      start(machineId: string, errorCodeId: string | null): void {
        patchState(store, { machineId, errorCodeId });
      },
      save: rxMethod<SaveRequest>(
        pipe(
          tap(() => patchState(store, { isSaving: true })),
          exhaustMap(({ value, onSaved }) =>
            request$(value).pipe(
              tap(() => {
                store._messages.add({
                  severity: 'success',
                  summary: store._translate.instant(SUCCESS_SUMMARY_KEY),
                  detail: store._translate.instant(
                    store.isUpdate() ? 'MACHINE_ERROR_CODES.TOASTS.UPDATED' : 'MACHINE_ERROR_CODES.TOASTS.CREATED',
                  ),
                });
                onSaved();
              }),
              catchError((error: unknown) => {
                const status = error instanceof HttpErrorResponse ? error.status : 0;
                store._messages.add({
                  severity: 'error',
                  summary: store._translate.instant(ERROR_SUMMARY_KEY),
                  detail: store._translate.instant(
                    status === HttpStatusCode.Conflict
                      ? 'MACHINE_ERROR_CODES.TOASTS.CODE_TAKEN'
                      : httpErrorMessageKey(status),
                  ),
                });
                return EMPTY;
              }),
              finalize(() => patchState(store, { isSaving: false })),
            ),
          ),
        ),
      ),
    };
  }),
);
