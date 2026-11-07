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
import { StatusDefinitionsApi } from '@machines/statuses/status-definitions-api';

import { EMPTY_VALUE, toFormValue } from './status-definition-drawer.store.utils';
import type { StatusDefinitionFormValue } from '@machines/statuses/drawer/status-definition-form/status-definition-form-schema';

const SUCCESS_SUMMARY_KEY = 'STATUS_DEFINITIONS.TOASTS.SUCCESS';
const ERROR_SUMMARY_KEY = 'ERRORS.SUMMARY';

/**
 * One save of the dialog. `rxMethod` returns no result to its caller, so the caller passes `onSaved`, and the store
 * calls it once after a successful save.
 */
interface SaveRequest {
  readonly value: StatusDefinitionFormValue;
  readonly onSaved: () => void;
}

interface DrawerState {
  machineId: string | null;
  statusDefinitionId: string | null;
  isSaving: boolean;
}

export const StatusDefinitionDrawerStore = signalStore(
  withState<DrawerState>({ machineId: null, statusDefinitionId: null, isSaving: false }),
  withProps(() => ({
    _api: inject(StatusDefinitionsApi),
    _messages: inject(MessageService),
    _translate: inject(TranslateService),
  })),
  withComputed((store) => ({
    isUpdate: computed(() => store.statusDefinitionId() !== null),
  })),
  withProps((store) => ({
    statusDefinition: rxResource({
      params: () => {
        const machineId = store.machineId();
        const statusDefinitionId = store.statusDefinitionId();
        return machineId === null || statusDefinitionId === null ? undefined : { machineId, statusDefinitionId };
      },
      stream: ({ params }) => store._api.get(params.machineId, params.statusDefinitionId),
    }),
  })),
  withComputed((store) => ({
    initialValue: computed((): StatusDefinitionFormValue | null => {
      if (store.machineId() === null) {
        return null;
      }
      if (!store.isUpdate()) {
        return EMPTY_VALUE;
      }
      return store.statusDefinition.hasValue() ? toFormValue(store.statusDefinition.value()) : null;
    }),
  })),
  withMethods((store) => {
    const request$ = (value: StatusDefinitionFormValue): Observable<unknown> => {
      const machineId = store.machineId() ?? '';
      const statusDefinitionId = store.statusDefinitionId();
      const body = { code: value.code.trim(), severity: value.severity, description: value.description };
      const options = { context: new HttpContext().set(SKIP_ERROR_TOAST, true) };
      return statusDefinitionId === null
        ? store._api.create(machineId, body, options)
        : store._api.update(machineId, statusDefinitionId, body, options);
    };

    return {
      start(machineId: string, statusDefinitionId: string | null): void {
        patchState(store, { machineId, statusDefinitionId });
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
                    store.isUpdate() ? 'STATUS_DEFINITIONS.TOASTS.UPDATED' : 'STATUS_DEFINITIONS.TOASTS.CREATED',
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
                      ? 'STATUS_DEFINITIONS.TOASTS.CODE_TAKEN'
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
