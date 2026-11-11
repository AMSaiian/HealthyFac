import { HttpContext, HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { EMPTY, exhaustMap, finalize, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';
import { httpErrorMessageKey } from '@shared/http/http-error-message';
import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';

import type { FileTieActionPayload } from '@files/file-action-payloads';
import { FileActionType } from '@files/file-actions';
import { FilesApi } from '@files/files-api';
import { withMachineSearch } from '@files/with-machine-search';

interface TieState {
  action: AppAction | null;
  machineId: string | null;
  isSaving: boolean;
}

export const FileTieStore = signalStore(
  withState<TieState>({ action: null, machineId: null, isSaving: false }),
  withMachineSearch(),
  withProps(() => ({
    _api: inject(FilesApi),
    _actions: inject(ActionStore),
    _messages: inject(MessageService),
    _translate: inject(TranslateService),
  })),
  withComputed((store) => ({
    payload: computed(() => store.action()?.payload as FileTieActionPayload | undefined),
    isLoading: computed(() => store.machines.isLoading()),
    saveDisabledReason: computed((): string | null =>
      store.machineId() === null ? 'FILES.TIE_DIALOG.NO_MACHINE' : null,
    ),
  })),
  withMethods((store) => {
    const showError = (detailKey: string): void => {
      store._messages.add({
        severity: 'error',
        summary: store._translate.instant('ERRORS.SUMMARY'),
        detail: store._translate.instant(detailKey),
      });
    };

    return {
      start(action: AppAction): void {
        patchState(store, { action });
        store.enableMachineSearch(true);
      },
      pick(machineId: string | null): void {
        patchState(store, { machineId });
        store.setSelectedMachines(machineId === null ? [] : [machineId]);
      },
      save: rxMethod<void>(
        pipe(
          tap(() => patchState(store, { isSaving: true })),
          exhaustMap(() => {
            const payload = store.payload();
            const machineId = store.machineId();
            if (payload === undefined || machineId === null) {
              patchState(store, { isSaving: false });
              return EMPTY;
            }
            return store._api
              .tie(
                payload.item.id,
                { machineId, rowVersion: payload.item.rowVersion },
                { context: new HttpContext().set(SKIP_ERROR_TOAST, true) },
              )
              .pipe(
                tap(() => {
                  store._messages.add({
                    severity: 'success',
                    summary: store._translate.instant('FILES.TOASTS.SUCCESS'),
                    detail: store._translate.instant('FILES.TOASTS.TIED'),
                  });
                  store._actions.complete(FileActionType.Saved);
                }),
                catchError((error: unknown) => {
                  const status = error instanceof HttpErrorResponse ? error.status : 0;
                  if (status === HttpStatusCode.Conflict) {
                    showError('FILES.ERRORS.CONFLICT');
                    store._actions.complete(FileActionType.Saved);
                  } else if (status === HttpStatusCode.NotFound) {
                    showError('FILES.ERRORS.NOT_FOUND');
                    store._actions.complete(FileActionType.Saved);
                  } else {
                    showError(httpErrorMessageKey(status));
                  }
                  return EMPTY;
                }),
                finalize(() => patchState(store, { isSaving: false })),
              );
          }),
        ),
      ),
      cancel(): void {
        store._actions.cancel();
      },
    };
  }),
);
