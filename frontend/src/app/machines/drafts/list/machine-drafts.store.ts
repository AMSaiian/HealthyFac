import { HttpContext, HttpErrorResponse } from '@angular/common/http';
import { computed, effect, inject, untracked } from '@angular/core';
import { Router } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { EMPTY, exhaustMap, forkJoin, map, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';
import { setFilter } from '@shared/grid/requests/grid-filters';
import { DEFAULT_PAGE_SIZE } from '@shared/grid/requests/grid-limits';
import { SortDirection } from '@shared/grid/requests/grid-request';
import { withGridState } from '@shared/grid/with-grid-state';
import { httpErrorMessageKey } from '@shared/http/http-error-message';
import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';
import { AuthStore } from '@shared/security/auth.store';
import { Permission } from '@shared/security/permissions';

import {
  DraftColumn,
  DraftSortColumn,
  type MachineDraftFilter,
  type MachineDraftGridItem,
  type SearchMachineDraftsBody,
} from '@machines/drafts/draft';
import type { MachineDraftsActionPayload } from '@machines/drafts/draft-action-payloads';
import { draftConflictKey } from '@machines/drafts/draft-conflict';
import { DraftActionType } from '@machines/drafts/draft-actions';
import { DraftsApi } from '@machines/drafts/drafts-api';
import { reviewMachineDraft, reviewMetricDrafts, reviewStatusDraft } from '@machines/drafts/draft-review';
import { DraftKind } from '@machines/drafts/draft';
import { withDraftQuickApply } from '@machines/drafts/quick-apply/with-draft-quick-apply';

interface DraftsState {
  action: AppAction | null;
  /** Set when a draft was dismissed, so the owner of the lists reloads after the dialog closes. */
  hasChanged: boolean;
  /** True while the call that dismisses every draft of the machine runs. */
  isDismissing: boolean;
}

export const INITIAL_DRAFT_FILTER: MachineDraftFilter = { kinds: [] };

export const DRAFT_COLUMN_ORDER: readonly DraftColumn[] = [
  DraftColumn.Kind,
  DraftColumn.Label,
  DraftColumn.SourceLocation,
  DraftColumn.ProposedAt,
];

export const MachineDraftsStore = signalStore(
  withGridState<MachineDraftFilter, DraftSortColumn, DraftColumn>({
    limit: DEFAULT_PAGE_SIZE,
    sort: { column: DraftSortColumn.ProposedAt, direction: SortDirection.Descending },
    filter: INITIAL_DRAFT_FILTER,
    columns: DRAFT_COLUMN_ORDER,
  }),
  withState<DraftsState>({ action: null, hasChanged: false, isDismissing: false }),
  withProps(() => ({
    _api: inject(DraftsApi),
    _actions: inject(ActionStore),
    _auth: inject(AuthStore),
    _router: inject(Router),
    _confirmation: inject(ConfirmationService),
    _messages: inject(MessageService),
    _translate: inject(TranslateService),
  })),
  withComputed((store) => ({
    machineId: computed(() => (store.action()?.payload as MachineDraftsActionPayload | undefined)?.machineId),
    canWrite: computed(() => store._auth.hasPermission(Permission.DraftsWrite)),
  })),
  withProps((store) => ({
    drafts: rxResource({
      params: () => {
        const machineId = store.machineId();
        if (machineId === undefined) {
          return undefined;
        }
        const sort = store.sort();
        const body: SearchMachineDraftsBody = {
          offset: store.offset(),
          limit: store.limit(),
          ...(sort === null ? {} : { sort }),
          filter: { kinds: setFilter(store.filter().kinds) },
        };
        return { machineId, body };
      },
      stream: ({ params }) => store._api.searchForMachine(params.machineId, params.body),
    }),
  })),
  withProps((store) => ({
    /** The drafts of the attempts on the page, to tell which draft applies as it is. */
    attempts: rxResource({
      params: () => {
        const ids = store.drafts.hasValue()
          ? [...new Set(store.drafts.value().items.map((item) => item.attemptId))]
          : [];
        return ids.length === 0 ? undefined : ids.join(',');
      },
      stream: ({ params }) =>
        forkJoin(
          params
            .split(',')
            .map((attemptId) =>
              store._api
                .listForAttempt(attemptId, { context: new HttpContext().set(SKIP_ERROR_TOAST, true) })
                .pipe(map((drafts) => ({ attemptId, drafts }))),
            ),
        ),
    }),
  })),
  withDraftQuickApply(),
  withMethods((store) => {
    const dismissDraft = rxMethod<MachineDraftGridItem>(
      pipe(
        exhaustMap((item) =>
          store._api.dismiss(item.id, { context: new HttpContext().set(SKIP_ERROR_TOAST, true) }).pipe(
            tap(() => {
              patchState(store, { hasChanged: true });
              store._messages.add({
                severity: 'success',
                summary: store._translate.instant('FILES.TOASTS.SUCCESS'),
                detail: store._translate.instant('DRAFTS.TOASTS.DISMISSED'),
              });
              store.drafts.reload();
            }),
            catchError((error: unknown) => {
              store._messages.add({
                severity: 'error',
                summary: store._translate.instant('ERRORS.SUMMARY'),
                detail: store._translate.instant(
                  draftConflictKey(error) ?? httpErrorMessageKey(error instanceof HttpErrorResponse ? error.status : 0),
                ),
              });
              store.drafts.reload();
              return EMPTY;
            }),
          ),
        ),
      ),
    );

    const confirmDismissAll = (machineId: string, count: number): void => {
      store._confirmation.confirm({
        header: store._translate.instant('DRAFTS.DISMISS_ALL_HEADER'),
        message: store._translate.instant('DRAFTS.DISMISS_ALL_MESSAGE', { count }),
        icon: 'pi pi-exclamation-triangle',
        acceptLabel: store._translate.instant('DRAFTS.DISMISS_ALL'),
        rejectLabel: store._translate.instant('ACTIONS.CANCEL'),
        acceptButtonProps: { severity: 'danger' },
        rejectButtonProps: { severity: 'secondary' },
        accept: () => {
          dismissAll(machineId);
        },
      });
    };

    /** Reads the count of all pending drafts of the machine, whatever the grid filter, then asks. */
    const askDismissAll = rxMethod<string>(
      pipe(
        exhaustMap((machineId) =>
          store._api
            .searchForMachine(
              machineId,
              { offset: 0, limit: 1, filter: { kinds: setFilter([]) } },
              { context: new HttpContext().set(SKIP_ERROR_TOAST, true) },
            )
            .pipe(
              tap((page) => {
                if (page.totalCount === 0) {
                  store.drafts.reload();
                } else {
                  confirmDismissAll(machineId, page.totalCount);
                }
              }),
              catchError((error: unknown) => {
                store._messages.add({
                  severity: 'error',
                  summary: store._translate.instant('ERRORS.SUMMARY'),
                  detail: store._translate.instant(
                    httpErrorMessageKey(error instanceof HttpErrorResponse ? error.status : 0),
                  ),
                });
                return EMPTY;
              }),
            ),
        ),
      ),
    );

    const dismissAll = rxMethod<string>(
      pipe(
        tap(() => patchState(store, { isDismissing: true })),
        exhaustMap((machineId) =>
          store._api.dismissForMachine(machineId, { context: new HttpContext().set(SKIP_ERROR_TOAST, true) }).pipe(
            tap((response) => {
              patchState(store, { hasChanged: true, isDismissing: false });
              store._messages.add({
                severity: 'success',
                summary: store._translate.instant('FILES.TOASTS.SUCCESS'),
                detail: store._translate.instant('DRAFTS.TOASTS.DISMISSED_ALL', { count: response.dismissedCount }),
              });
              store.drafts.reload();
              store.attempts.reload();
            }),
            catchError((error: unknown) => {
              patchState(store, { isDismissing: false });
              store._messages.add({
                severity: 'error',
                summary: store._translate.instant('ERRORS.SUMMARY'),
                detail: store._translate.instant(
                  draftConflictKey(error) ?? httpErrorMessageKey(error instanceof HttpErrorResponse ? error.status : 0),
                ),
              });
              store.drafts.reload();
              return EMPTY;
            }),
          ),
        ),
      ),
    );

    return {
      start(action: AppAction): void {
        patchState(store, { action, hasChanged: false, isDismissing: false });
        store.resetGrid();
      },
      /** Reviews one draft of an existing machine: a machine draft here is always an update. */
      requestReview(item: MachineDraftGridItem): void {
        const machineId = store.machineId();
        if (item.kind === DraftKind.Machine) {
          reviewMachineDraft(store._actions, item.id, false);
        } else if (item.kind === DraftKind.Status && machineId !== undefined) {
          reviewStatusDraft(store._actions, { machineId, attemptId: item.attemptId, draftId: item.id });
        } else if (item.kind === DraftKind.Metric && machineId !== undefined) {
          void reviewMetricDrafts(store._actions, store._router, machineId, item.attemptId);
        }
      },
      /** Loads the list again after a failed load. */
      retryLoad(): void {
        store.drafts.reload();
      },
      /** Applies one draft without the review screen. */
      requestApply(item: MachineDraftGridItem): void {
        store.applyDraft(item.attemptId, item.id);
      },
      /** Applies the drafts of the machine in every attempt, on all pages of its pending drafts. */
      requestApplyAll(): void {
        const machineId = store.machineId();
        if (machineId !== undefined) {
          store.applyMachine(machineId);
        }
      },
      /** Asks for a confirmation, then dismisses every pending draft of the machine in one call. */
      requestDismissAll(): void {
        const machineId = store.machineId();
        if (machineId !== undefined) {
          askDismissAll(machineId);
        }
      },
      requestDismiss(item: MachineDraftGridItem): void {
        store._confirmation.confirm({
          header: store._translate.instant('DRAFTS.DISMISS_HEADER'),
          message: store._translate.instant('DRAFTS.DISMISS_MESSAGE', {
            name: item.label ?? store._translate.instant('DRAFTS.UNNAMED'),
          }),
          icon: 'pi pi-exclamation-triangle',
          acceptLabel: store._translate.instant('DRAFTS.DISMISS'),
          rejectLabel: store._translate.instant('ACTIONS.CANCEL'),
          acceptButtonProps: { severity: 'danger' },
          rejectButtonProps: { severity: 'secondary' },
          accept: () => {
            dismissDraft(item);
          },
        });
      },
      /** Closes the dialog. After a dismiss, the close tells the machines grid to reload. */
      close(): void {
        if (store.hasChanged()) {
          store._actions.complete(DraftActionType.Changed);
        } else {
          store._actions.cancel();
        }
      },
    };
  }),
  withComputed((store) => ({
    /** True when the last load of the list failed: the dialog shows an error with a retry, never the empty state. */
    hasLoadError: computed(() => store.drafts.status() === 'error'),
  })),
  withHooks({
    onInit(store) {
      effect(() => {
        store.setQuickDrafts(store.attempts.hasValue() ? store.attempts.value() : []);
      });
      let seen = 0;
      effect(() => {
        const run = store.quickRun();
        if (run.count > seen) {
          seen = run.count;
          untracked(() => {
            if (run.hasApplied) {
              patchState(store, { hasChanged: true });
            }
            store.drafts.reload();
            store.attempts.reload();
          });
        }
      });
    },
  }),
);
