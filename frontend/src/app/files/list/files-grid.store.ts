import { HttpContext, HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, effect, inject, untracked } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { EMPTY, exhaustMap, pipe, tap } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import { httpErrorMessageKey } from '@shared/http/http-error-message';
import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';
import { FileDownloader } from '@shared/files/file-downloader';
import { FilesLinksApi } from '@shared/files/files-links-api';
import { withGridState } from '@shared/grid/with-grid-state';
import { AuthStore } from '@shared/security/auth.store';
import { Permission } from '@shared/security/permissions';
import { CLOCK } from '@shared/time/clock';

import type { FileColumn, FileFilter, FileGridItem, FileSortColumn, FilesScope } from '@files/file';
import { FileActionType } from '@files/file-actions';
import { FilesApi } from '@files/files-api';
import { IngestApi } from '@files/ingest/ingest-api';
import { ingestConflictKey } from '@files/ingest/ingest-conflict';
import { IngestState } from '@files/ingest/ingest-attempt';
import { toSearchBody } from '@files/list/file-query';
import { type FileRowAccess, FileRowAction } from '@files/list/file-grid/file-grid.utils';
import { INITIAL_GRID, isSameScope } from '@files/list/files-grid.store.utils';
import { withMachineSearch } from '@files/with-machine-search';

interface ScopeState {
  scope: FilesScope | null;
}

export const FilesGridStore = signalStore(
  withGridState<FileFilter, FileSortColumn, FileColumn>(INITIAL_GRID),
  withState<ScopeState>({ scope: null }),
  withMachineSearch(),
  withProps(() => ({
    _auth: inject(AuthStore),
    _clock: inject(CLOCK),
    _actions: inject(ActionStore),
    _api: inject(FilesApi),
    _ingestApi: inject(IngestApi),
    _linksApi: inject(FilesLinksApi),
    _downloader: inject(FileDownloader),
    _confirmation: inject(ConfirmationService),
    _messages: inject(MessageService),
    _translate: inject(TranslateService),
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
      store._messages.add({
        severity: 'error',
        summary: store._translate.instant('ERRORS.SUMMARY'),
        detail: store._translate.instant('FILES.ERRORS.NOT_FOUND'),
      });
      store.files.reload();
    };

    const deleteFile = rxMethod<FileGridItem>(
      pipe(
        exhaustMap((file) => {
          const scope = store.scope();
          if (scope === null) {
            return EMPTY;
          }
          return store._api.delete(scope, file.id, { context: new HttpContext().set(SKIP_ERROR_TOAST, true) }).pipe(
            tap(() => {
              store._messages.add({
                severity: 'success',
                summary: store._translate.instant('FILES.TOASTS.SUCCESS'),
                detail: store._translate.instant('FILES.TOASTS.DELETED'),
              });
              store.files.reload();
            }),
            catchError((error: unknown) => {
              if (error instanceof HttpErrorResponse && error.status === HttpStatusCode.NotFound) {
                showNotFound();
              } else {
                store._messages.add({
                  severity: 'error',
                  summary: store._translate.instant('ERRORS.SUMMARY'),
                  detail: store._translate.instant(
                    httpErrorMessageKey(error instanceof HttpErrorResponse ? error.status : 0),
                  ),
                });
              }
              return EMPTY;
            }),
          );
        }),
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

    const showError = (detailKey: string): void => {
      store._messages.add({
        severity: 'error',
        summary: store._translate.instant('ERRORS.SUMMARY'),
        detail: store._translate.instant(detailKey),
      });
    };

    const showHttpError = (error: unknown): void => {
      showError(httpErrorMessageKey(error instanceof HttpErrorResponse ? error.status : 0));
    };

    const showSuccess = (detailKey: string): void => {
      store._messages.add({
        severity: 'success',
        summary: store._translate.instant('FILES.TOASTS.SUCCESS'),
        detail: store._translate.instant(detailKey),
      });
    };

    /** Shows the new ingest state in the row at once, before the grid loads again. */
    const patchRow = (fileId: string, patch: Partial<FileGridItem>): void => {
      store.files.update((page) =>
        page === undefined
          ? page
          : { ...page, items: page.items.map((item) => (item.id === fileId ? { ...item, ...patch } : item)) },
      );
    };

    const quiet = (): { context: HttpContext } => ({ context: new HttpContext().set(SKIP_ERROR_TOAST, true) });

    const startIngest = rxMethod<FileGridItem>(
      pipe(
        exhaustMap((file) =>
          store._ingestApi.start(file.id, quiet()).pipe(
            tap(() => {
              patchRow(file.id, { ingestState: IngestState.Queued, canIngest: false });
              showSuccess('INGEST.TOASTS.STARTED');
            }),
            catchError((error: unknown) => {
              const conflictKey = ingestConflictKey(error);
              if (conflictKey !== null) {
                showError(conflictKey);
                store.files.reload();
              } else if (error instanceof HttpErrorResponse && error.status === HttpStatusCode.NotFound) {
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
          store._ingestApi.listForFile(file.id, quiet()).pipe(
            map((page) =>
              page.items.find(
                (attempt) => attempt.state === IngestState.Queued || attempt.state === IngestState.InProcess,
              ),
            ),
            exhaustMap((attempt) => {
              if (attempt === undefined) {
                showError('INGEST.CONFLICTS.NOT_ACTIVE');
                store.files.reload();
                return EMPTY;
              }
              return store._ingestApi.cancel(attempt.id, quiet()).pipe(
                tap(() => {
                  patchRow(file.id, { ingestState: IngestState.Interrupted, canIngest: true });
                  showSuccess('INGEST.TOASTS.CANCELLED');
                }),
              );
            }),
            catchError((error: unknown) => {
              const conflictKey = ingestConflictKey(error);
              if (conflictKey !== null) {
                showError(conflictKey);
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

    const untie = rxMethod<FileGridItem>(
      pipe(
        exhaustMap((file) =>
          store._api.untie(file.id, { rowVersion: file.rowVersion }, quiet()).pipe(
            tap(() => {
              showSuccess('FILES.TOASTS.UNTIED');
              store.files.reload();
            }),
            catchError((error: unknown) => {
              const status = error instanceof HttpErrorResponse ? error.status : 0;
              if (status === HttpStatusCode.Conflict) {
                showError('FILES.ERRORS.CONFLICT');
                store.files.reload();
              } else if (status === HttpStatusCode.NotFound) {
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

    const confirm = (
      headerKey: string,
      messageKey: string,
      acceptKey: string,
      file: FileGridItem,
      run: () => void,
    ): void => {
      store._confirmation.confirm({
        header: store._translate.instant(headerKey),
        message: store._translate.instant(messageKey, { name: file.fileName }),
        icon: 'pi pi-exclamation-triangle',
        acceptLabel: store._translate.instant(acceptKey),
        rejectLabel: store._translate.instant('ACTIONS.CANCEL'),
        acceptButtonProps: { severity: 'danger' },
        rejectButtonProps: { severity: 'secondary' },
        accept: run,
      });
    };

    return {
      /** Runs one ingest or tie action of the row menu. */
      requestRowAction(request: { action: FileRowAction; file: FileGridItem }): void {
        const { action, file } = request;
        switch (action) {
          case FileRowAction.Ingest:
            startIngest(file);
            break;
          case FileRowAction.CancelIngest:
            confirm('INGEST.CANCEL_HEADER', 'INGEST.CANCEL_MESSAGE', 'FILES.ACTIONS_MENU.CANCEL_INGEST', file, () => {
              cancelIngest(file);
            });
            break;
          case FileRowAction.Tie:
            void store._actions.dispatch({
              type: FileActionType.Tie,
              payload: { item: { id: file.id, fileName: file.fileName, rowVersion: file.rowVersion } },
            });
            break;
          case FileRowAction.Untie:
            confirm('FILES.UNTIE_HEADER', 'FILES.UNTIE_MESSAGE', 'FILES.ACTIONS_MENU.UNTIE', file, () => {
              untie(file);
            });
            break;
          case FileRowAction.History:
            void store._actions.dispatch({ type: FileActionType.IngestHistory, payload: { fileId: file.id } });
            break;
        }
      },
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
      requestDelete(file: FileGridItem): void {
        store._confirmation.confirm({
          header: store._translate.instant('FILES.DELETE_HEADER'),
          message: store._translate.instant('FILES.DELETE_MESSAGE', { name: file.fileName }),
          icon: 'pi pi-exclamation-triangle',
          acceptLabel: store._translate.instant('FILES.ACTIONS_MENU.DELETE'),
          rejectLabel: store._translate.instant('ACTIONS.CANCEL'),
          acceptButtonProps: { severity: 'danger' },
          rejectButtonProps: { severity: 'secondary' },
          accept: () => {
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
      let seen = store._actions.lastResult()?.sequence ?? 0;
      effect(() => {
        const result = store._actions.lastResult();
        if (result !== null && result.type === FileActionType.Saved && result.sequence > seen) {
          seen = result.sequence;
          untracked(() => store.files.reload());
        }
      });
    },
  }),
);
