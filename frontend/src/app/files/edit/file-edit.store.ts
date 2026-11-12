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

import type { FileEditActionPayload } from '@files/file-action-payloads';
import { FileActionType } from '@files/file-actions';
import { FilesApi } from '@files/files-api';
import type { FileEditFormValue } from '@files/edit/file-edit-form/file-edit-form-schema';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

interface EditState {
  action: AppAction | null;
  isSaving: boolean;
}

export const FileEditStore = signalStore(
  withState<EditState>({ action: null, isSaving: false }),
  withProps(() => ({
    _api: inject(FilesApi),
    _actions: inject(ActionStore),
  })),
  withToasts(),
  withComputed((store) => ({
    payload: computed(() => store.action()?.payload as FileEditActionPayload | undefined),
  })),
  withComputed((store) => ({
    initialValue: computed((): FileEditFormValue | null => {
      const item = store.payload()?.item;
      return item === undefined ? null : { fileName: item.fileName, description: item.description };
    }),
    extension: computed(() => store.payload()?.item.extension ?? ''),
  })),
  withMethods((store) => {
    return {
      start(action: AppAction): void {
        patchState(store, { action });
      },
      save: rxMethod<FileEditFormValue>(
        pipe(
          tap(() => patchState(store, { isSaving: true })),
          exhaustMap((value) => {
            const payload = store.payload();
            if (payload === undefined) {
              return EMPTY;
            }
            return store._api
              .update(
                payload.scope,
                payload.item.id,
                {
                  fileName: value.fileName,
                  description: value.description,
                  rowVersion: payload.item.rowVersion,
                },
                skipErrorToast(),
              )
              .pipe(
                tap(() => {
                  store._toastSuccess('FILES.TOASTS.UPDATED');
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
  withStoreDevtools('file-edit'),
);
