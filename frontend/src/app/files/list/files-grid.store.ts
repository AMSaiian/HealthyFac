import { HttpContext, HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, effect, inject, untracked } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { EMPTY, exhaustMap, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

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
import { toSearchBody } from '@files/list/file-query';
import { INITIAL_GRID } from '@files/list/files-grid.store.utils';

interface ScopeState {
  scope: FilesScope | null;
}

export const FilesGridStore = signalStore(
  withGridState<FileFilter, FileSortColumn, FileColumn>(INITIAL_GRID),
  withState<ScopeState>({ scope: null }),
  withProps(() => ({
    _auth: inject(AuthStore),
    _clock: inject(CLOCK),
    _actions: inject(ActionStore),
    _api: inject(FilesApi),
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

    return {
      /** Starts a visit to the files of a scope: sets the scope and resets the grid. */
      setScope(scope: FilesScope): void {
        patchState(store, { scope });
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
