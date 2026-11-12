import { HttpEventType } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, from, mergeMap, pipe, Subject, takeUntil, tap, toArray } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';
import { skipErrorToast } from '@shared/http/http-error-key';
import { withToasts } from '@shared/toasts/with-toasts';

import type { FilesScope } from '@files/file';
import { FileActionType } from '@files/file-actions';
import { FilesApi } from '@files/files-api';
import {
  COMPANY_SCOPE,
  MAX_PARALLEL_UPLOADS,
  progressOf,
  scopeOf,
  toDescriptionHtml,
  toUploadItem,
  uploadErrorKey,
  type UploadItem,
  UploadStatus,
} from '@files/upload/file-upload.store.utils';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

const MAX_PERCENT = 100;

interface UploadState {
  scope: FilesScope;
  items: readonly UploadItem[];
  nextKey: number;
  isClosed: boolean;
}

export const FileUploadStore = signalStore(
  withState<UploadState>({ scope: COMPANY_SCOPE, items: [], nextKey: 0, isClosed: false }),
  withProps(() => ({
    _api: inject(FilesApi),
    _actions: inject(ActionStore),
    _stop: new Subject<void>(),
  })),
  withToasts(),
  withComputed((store) => ({
    readyCount: computed(() => store.items().filter((item) => item.status === UploadStatus.Ready).length),
    isUploading: computed(() => store.items().some((item) => item.status === UploadStatus.Uploading)),
    hasDone: computed(() => store.items().some((item) => item.status === UploadStatus.Done)),
  })),
  withMethods((store) => {
    const update = (key: number, changes: Partial<UploadItem>): void => {
      patchState(store, (state) => ({
        items: state.items.map((item) => (item.key === key ? { ...item, ...changes } : item)),
      }));
    };

    const finish = (): void => {
      if (store.isClosed()) {
        return;
      }
      const hasFailed = store.items().some((item) => item.status === UploadStatus.Failed);
      if (!hasFailed && store.hasDone()) {
        store._toastSuccess('FILES.TOASTS.UPLOADED');
        patchState(store, { isClosed: true });
        store._actions.complete(FileActionType.Saved);
      }
    };

    const upload = rxMethod<void>(
      pipe(
        exhaustMap(() => {
          const pending = store.items().filter((item) => item.status === UploadStatus.Ready);
          if (pending.length === 0) {
            return EMPTY;
          }
          pending.forEach((item) => update(item.key, { status: UploadStatus.Uploading, progress: 0, errorKey: null }));

          return from(pending).pipe(
            mergeMap(
              (item) =>
                store._api.upload(store.scope(), item.file, toDescriptionHtml(item.description), skipErrorToast()).pipe(
                  tap((event) => {
                    const progress = progressOf(event);
                    if (progress !== null) {
                      update(item.key, { progress });
                    } else if (event.type === HttpEventType.Response) {
                      update(item.key, { status: UploadStatus.Done, progress: MAX_PERCENT });
                    }
                  }),
                  catchError((error: unknown) => {
                    update(item.key, { status: UploadStatus.Failed, errorKey: uploadErrorKey(error) });
                    return EMPTY;
                  }),
                ),
              MAX_PARALLEL_UPLOADS,
            ),
            takeUntil(store._stop),
            toArray(),
            tap(() => finish()),
          );
        }),
      ),
    );

    return {
      start(action: AppAction): void {
        patchState(store, { scope: scopeOf(action) });
      },
      addFiles(files: readonly File[]): void {
        patchState(store, (state) => ({
          items: [...state.items, ...files.map((file, index) => toUploadItem(file, state.nextKey + index))],
          nextKey: state.nextKey + files.length,
        }));
      },
      remove(key: number): void {
        patchState(store, (state) => ({ items: state.items.filter((item) => item.key !== key) }));
      },
      setDescription(key: number, description: string): void {
        update(key, { description });
      },
      upload,
      /** Closes the dialog: pending uploads stop, and the grid reloads when a file was stored. */
      close(): void {
        store._stop.next();
        patchState(store, { isClosed: true });
        if (store.hasDone()) {
          store._actions.complete(FileActionType.Saved);
        } else {
          store._actions.cancel();
        }
      },
    };
  }),
  withStoreDevtools('file-upload'),
);
