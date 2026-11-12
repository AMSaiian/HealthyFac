import { HttpStatusCode } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, finalize, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';
import { httpErrorKey, isHttpStatus, skipErrorToast } from '@shared/http/http-error-key';
import { withToasts } from '@shared/toasts/with-toasts';

import type { FileTieActionPayload } from '@files/file-action-payloads';
import { FileActionType } from '@files/file-actions';
import { FilesApi } from '@files/files-api';
import { withMachineSearch } from '@files/with-machine-search';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

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
  })),
  withToasts(),
  withComputed((store) => ({
    payload: computed(() => store.action()?.payload as FileTieActionPayload | undefined),
    isLoading: computed(() => store.machines.isLoading()),
    saveDisabledReason: computed((): string | null =>
      store.machineId() === null ? 'FILES.TIE_DIALOG.NO_MACHINE' : null,
    ),
  })),
  withMethods((store) => {
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
              .tie(payload.item.id, { machineId, rowVersion: payload.item.rowVersion }, skipErrorToast())
              .pipe(
                tap(() => {
                  store._toastSuccess('FILES.TOASTS.TIED');
                  store._actions.complete(FileActionType.Saved);
                }),
                catchError((error: unknown) => {
                  store._toastError(
                    httpErrorKey(error, {
                      [HttpStatusCode.Conflict]: 'FILES.ERRORS.CONFLICT',
                      [HttpStatusCode.NotFound]: 'FILES.ERRORS.NOT_FOUND',
                    }),
                  );
                  if (isHttpStatus(error, HttpStatusCode.Conflict, HttpStatusCode.NotFound)) {
                    store._actions.complete(FileActionType.Saved);
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
  withStoreDevtools('file-tie'),
);
