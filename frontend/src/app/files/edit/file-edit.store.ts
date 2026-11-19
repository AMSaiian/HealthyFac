import { HttpStatusCode } from '@angular/common/http';
import { computed, effect, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, finalize, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';
import { withDetailsMode } from '@shared/forms/with-details-mode';
import { httpErrorKey, isHttpStatus, skipErrorToast } from '@shared/http/http-error-key';
import { descriptionLinks } from '@shared/rich-text/file-image/description-links';
import { RichTextImageLinks } from '@shared/rich-text/file-image/rich-text-image-links';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';
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
  withDetailsMode(),
  withProps(() => ({
    _api: inject(FilesApi),
    _actions: inject(ActionStore),
    _auth: inject(AuthStore),
  })),
  withToasts(),
  withComputed((store) => ({
    payload: computed(() => store.action()?.payload as FileEditActionPayload | undefined),
  })),
  withProps((store) => ({
    /** The file that the dialog shows: the grid row can be stale, so the dialog loads the file by its ID. */
    file: rxResource({
      params: () => store.payload(),
      stream: ({ params }) => store._api.get(params.scope, params.id),
    }),
  })),
  withProps((store) => ({
    descriptionLinks: descriptionLinks(
      () => (store.file.hasValue() ? store.file.value().description : null),
      inject(RichTextImageLinks),
    ).links,
  })),
  withComputed((store) => ({
    initialValue: computed((): FileEditFormValue | null => {
      if (!store.file.hasValue()) {
        return null;
      }
      const { fileName, description } = store.file.value();
      return { fileName, description };
    }),
    extension: computed(() => (store.file.hasValue() ? store.file.value().extension : '')),
    canEdit: computed(() =>
      store._auth.hasPermission(
        store.payload()?.scope.kind === 'company' ? Permission.CompanyFilesWrite : Permission.FilesWrite,
      ),
    ),
  })),
  withComputed((store) => ({
    /** Tells whether the pencil shows: the loaded file in view mode, for a user who may change it. */
    isEditShown: computed(() => store.isViewMode() && store.file.hasValue() && store.canEdit()),
  })),
  withMethods((store) => {
    return {
      start(action: AppAction): void {
        patchState(store, { action });
        store._startMode(true);
      },
      cancelEdit(): void {
        store.view();
      },
      save: rxMethod<FileEditFormValue>(
        pipe(
          tap(() => patchState(store, { isSaving: true })),
          exhaustMap((value) => {
            const payload = store.payload();
            if (payload === undefined || !store.file.hasValue()) {
              return EMPTY;
            }
            return store._api
              .update(
                payload.scope,
                payload.id,
                {
                  fileName: value.fileName,
                  description: value.description,
                  rowVersion: store.file.value().rowVersion,
                },
                skipErrorToast(),
              )
              .pipe(
                tap(() => {
                  store._toastSuccess('FILES.TOASTS.UPDATED');
                  store._actions.notify(FileActionType.Saved);
                  store.file.reload();
                  store.view();
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
  withHooks({
    onInit(store) {
      effect(() => {
        if (store.file.status() === 'error') {
          store._actions.cancel();
        }
      });
    },
  }),
  withStoreDevtools('file-edit'),
);
