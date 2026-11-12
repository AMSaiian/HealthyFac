import { HttpStatusCode } from '@angular/common/http';
import { computed, effect, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, pipe, tap } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import { type Reloadable, withActionResultReload } from '@shared/actions/with-action-result-reload';
import { withConfirm } from '@shared/confirm/with-confirm';
import { FileDownloader } from '@shared/files/file-downloader';
import { FilesLinksApi } from '@shared/files/files-links-api';
import { withGridState } from '@shared/grid/with-grid-state';
import { httpErrorKey, isHttpStatus, skipErrorToast } from '@shared/http/http-error-key';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';
import { CLOCK } from '@shared/time/clock';
import { withToasts } from '@shared/toasts/with-toasts';

import type { FileColumn, FileFilter, FileGridItem, FileSortColumn, FilesScope } from '@files/file';
import { FileActionType } from '@files/file-actions';
import { FilesApi } from '@files/files-api';
import { IngestApi } from '@files/ingest/ingest-api';
import { IngestState } from '@files/ingest/ingest-attempt';
import { ingestConflictKey } from '@files/ingest/ingest-conflict';
import { toSearchBody } from '@files/list/file-query';
import { type FileRowAccess, FileRowAction } from '@files/list/file-grid/file-grid.utils';
import { INITIAL_GRID, isSameScope, type ScopeState } from '@files/list/files-grid.store.utils';
import { withMachineSearch } from '@files/with-machine-search';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

/**
 * The files grid. Its one owned state group is the scope of the visit. The grid, the machine search, and the toasts are
 * shared features with their own state. The list resource and the row actions write the list, not the scope.
 */
export const FilesGridStore = signalStore(
  withGridState<FileFilter, FileSortColumn, FileColumn>(INITIAL_GRID),
  withState<ScopeState>({ scope: null }),
  withMachineSearch(),
  withToasts(),
  withConfirm(),
  withProps(() => ({
    _auth: inject(AuthStore),
    _clock: inject(CLOCK),
    _actions: inject(ActionStore),
    _api: inject(FilesApi),
    _ingestApi: inject(IngestApi),
    _linksApi: inject(FilesLinksApi),
    _downloader: inject(FileDownloader),
  })),
  withComputed((store) => {
    const isCompany = computed(() => store.scope()?.kind === 'company');
    const hasScope = computed(() => store.scope() !== null);
    return {
      canUpload: computed(
        () =>
          hasScope() && store._auth.hasPermission(isCompany() ? Permission.CompanyFilesWrite : Permission.FilesWrite),
      ),
      canEdit: computed(
        () =>
          hasScope() && store._auth.hasPermission(isCompany() ? Permission.CompanyFilesWrite : Permission.FilesWrite),
      ),
      canDelete: computed(
        () =>
          hasScope() && store._auth.hasPermission(isCompany() ? Permission.CompanyFilesDelete : Permission.FilesDelete),
      ),
      /** The ingest and tie actions exist on the company page only. */
      rowAccess: computed((): FileRowAccess => ({
        canIngestWrite: isCompany() && store._auth.hasPermission(Permission.IngestWrite),
        canIngestRead: isCompany() && store._auth.hasPermission(Permission.IngestRead),
        canTie: isCompany() && store._auth.hasPermission(Permission.CompanyFilesWrite),
      })),
      isCompany,
    };
  }),
  withProps((store) => ({
    files: rxResource({
      params: () => {
        const scope = store.scope();
        return scope === null
          ? undefined
          : {
              scope,
              body: toSearchBody(
                { offset: store.offset(), limit: store.limit(), sort: store.sort(), filter: store.filter() },
                store._clock.timeZoneId(),
              ),
            };
      },
      stream: ({ params }) => store._api.search(params.scope, params.body),
    }),
  })),
  withMethods((store) => {
    const showNotFound = (): void => {
      store._toastError('FILES.ERRORS.NOT_FOUND');
      store.files.reload();
    };

    const showHttpError = (error: unknown): void => {
      store._toastError(httpErrorKey(error));
    };

    /** Shows the new ingest state in the row at once, before the grid loads again. */
    const patchRow = (fileId: string, patch: Partial<FileGridItem>): void => {
      store.files.update((page) =>
        page === undefined
          ? page
          : { ...page, items: page.items.map((item) => (item.id === fileId ? { ...item, ...patch } : item)) },
      );
    };

    const deleteFile = rxMethod<FileGridItem>(
      pipe(
        exhaustMap((file) => {
          const scope = store.scope();
          if (scope === null) {
            return EMPTY;
          }
          return store._api.delete(scope, file.id, skipErrorToast()).pipe(
            tap(() => {
              store._toastSuccess('FILES.TOASTS.DELETED');
              store.files.reload();
            }),
            catchError((error: unknown) => {
              if (isHttpStatus(error, HttpStatusCode.NotFound)) {
                showNotFound();
              } else {
                showHttpError(error);
              }
              return EMPTY;
            }),
          );
        }),
      ),
    );

    const untie = rxMethod<FileGridItem>(
      pipe(
        exhaustMap((file) =>
          store._api.untie(file.id, { rowVersion: file.rowVersion }, skipErrorToast()).pipe(
            tap(() => {
              store._toastSuccess('FILES.TOASTS.UNTIED');
              store.files.reload();
            }),
            catchError((error: unknown) => {
              if (isHttpStatus(error, HttpStatusCode.Conflict)) {
                store._toastError('FILES.ERRORS.CONFLICT');
                store.files.reload();
              } else if (isHttpStatus(error, HttpStatusCode.NotFound)) {
                showNotFound();
              } else {
                showHttpError(error);
              }
              return EMPTY;
            }),
          ),
        ),
      ),
    );

    const startIngest = rxMethod<FileGridItem>(
      pipe(
        exhaustMap((file) =>
          store._ingestApi.start(file.id, skipErrorToast()).pipe(
            tap(() => {
              patchRow(file.id, { ingestState: IngestState.Queued, canIngest: false });
              store._toastSuccess('INGEST.TOASTS.STARTED');
            }),
            catchError((error: unknown) => {
              const conflictKey = ingestConflictKey(error);
              if (conflictKey !== null) {
                store._toastError(conflictKey);
                store.files.reload();
              } else if (isHttpStatus(error, HttpStatusCode.NotFound)) {
                showNotFound();
              } else {
                showHttpError(error);
              }
              return EMPTY;
            }),
          ),
        ),
      ),
    );

    const cancelIngest = rxMethod<FileGridItem>(
      pipe(
        exhaustMap((file) =>
          store._ingestApi.listForFile(file.id, skipErrorToast()).pipe(
            map((page) =>
              page.items.find(
                (attempt) => attempt.state === IngestState.Queued || attempt.state === IngestState.InProcess,
              ),
            ),
            exhaustMap((attempt) => {
              if (attempt === undefined) {
                store._toastError('INGEST.CONFLICTS.NOT_ACTIVE');
                store.files.reload();
                return EMPTY;
              }
              return store._ingestApi.cancel(attempt.id, skipErrorToast()).pipe(
                tap(() => {
                  patchRow(file.id, { ingestState: IngestState.Interrupted, canIngest: true });
                  store._toastSuccess('INGEST.TOASTS.CANCELLED');
                }),
              );
            }),
            catchError((error: unknown) => {
              const conflictKey = ingestConflictKey(error);
              if (conflictKey !== null) {
                store._toastError(conflictKey);
                store.files.reload();
              } else {
                showHttpError(error);
              }
              return EMPTY;
            }),
          ),
        ),
      ),
    );

    const download = rxMethod<FileGridItem>(
      pipe(
        exhaustMap((file) =>
          store._linksApi.getLinks([file.id]).pipe(
            tap((response) => {
              const link = response.links.find((candidate) => candidate.id === file.id);
              if (link === undefined) {
                showNotFound();
                return;
              }
              store._downloader.start(link.url);
            }),
            catchError(() => EMPTY),
          ),
        ),
      ),
    );

    return {
      /** Starts a visit to the files of a scope: sets the scope and resets the grid. An equal scope changes nothing. */
      setScope(scope: FilesScope): void {
        if (isSameScope(store.scope(), scope)) {
          return;
        }
        patchState(store, { scope });
        store.enableMachineSearch(scope.kind === 'company' && store._auth.hasPermission(Permission.MachinesRead));
        store.resetGrid();
      },
      download,
      requestUpload(): void {
        const scope = store.scope();
        if (scope !== null) {
          void store._actions.dispatch({ type: FileActionType.Upload, payload: { scope } });
        }
      },
      requestEdit(file: FileGridItem): void {
        const scope = store.scope();
        if (scope !== null) {
          void store._actions.dispatch({ type: FileActionType.Edit, payload: { scope, item: file } });
        }
      },
      /** Runs one ingest or tie action of the row menu. */
      requestRowAction(request: { action: FileRowAction; file: FileGridItem }): void {
        const { action, file } = request;
        switch (action) {
          case FileRowAction.Ingest:
            startIngest(file);
            break;
          case FileRowAction.CancelIngest:
            store._confirmDestructive({
              headerKey: 'INGEST.CANCEL_HEADER',
              messageKey: 'INGEST.CANCEL_MESSAGE',
              params: { name: file.fileName },
              acceptKey: 'FILES.ACTIONS_MENU.CANCEL_INGEST',
              onAccept: () => {
                cancelIngest(file);
              },
            });
            break;
          case FileRowAction.Tie:
            void store._actions.dispatch({
              type: FileActionType.Tie,
              payload: { item: { id: file.id, fileName: file.fileName, rowVersion: file.rowVersion } },
            });
            break;
          case FileRowAction.Untie:
            store._confirmDestructive({
              headerKey: 'FILES.UNTIE_HEADER',
              messageKey: 'FILES.UNTIE_MESSAGE',
              params: { name: file.fileName },
              acceptKey: 'FILES.ACTIONS_MENU.UNTIE',
              onAccept: () => {
                untie(file);
              },
            });
            break;
          case FileRowAction.History:
            void store._actions.dispatch({ type: FileActionType.IngestHistory, payload: { fileId: file.id } });
            break;
        }
      },
      requestDelete(file: FileGridItem): void {
        store._confirmDestructive({
          headerKey: 'FILES.DELETE_HEADER',
          messageKey: 'FILES.DELETE_MESSAGE',
          params: { name: file.fileName },
          acceptKey: 'ACTIONS.DELETE',
          onAccept: () => {
            deleteFile(file);
          },
        });
      },
    };
  }),
  withHooks({
    onInit(store) {
      effect(() => {
        store.setSelectedMachines(store.filter().machine.machineIds);
      });
    },
  }),
  withActionResultReload<{ files: Reloadable }>([FileActionType.Saved], (store) => {
    store.files.reload();
  }),
  withStoreDevtools('files-grid'),
);
