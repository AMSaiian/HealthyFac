import { computed, effect, inject, untracked } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { concatMap, EMPTY, exhaustMap, forkJoin, from, map, type Observable, pipe, tap, toArray } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';
import { withConfirm } from '@shared/confirm/with-confirm';
import { DEFAULT_PAGE_SIZE } from '@shared/grid/requests/grid-limits';
import { setFilter } from '@shared/grid/requests/grid-filters';
import { SortDirection } from '@shared/grid/requests/grid-request';
import { withGridState } from '@shared/grid/with-grid-state';
import { httpErrorKey, skipErrorToast } from '@shared/http/http-error-key';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';
import { withToasts } from '@shared/toasts/with-toasts';

import {
  DraftColumn,
  DraftKind,
  DraftSortColumn,
  DraftState,
  type MachineDraftFilter,
  type MachineDraftGridItem,
  type MachineDraftsPage,
  type SearchMachineDraftsBody,
} from '@machines/drafts/draft';
import { draftConflictKey } from '@machines/drafts/draft-conflict';
import { toApplyList, withSelected } from '@machines/drafts/list/machine-drafts.store.utils';
import { DraftActionType } from '@machines/drafts/draft-actions';

import { reviewMachineDraft, reviewMetricDrafts, reviewStatusDraft } from '@machines/drafts/draft-review';
import { DraftsApi } from '@machines/drafts/drafts-api';
import { withDraftQuickApply } from '@machines/drafts/with-draft-quick-apply';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

export const INITIAL_DRAFT_FILTER: MachineDraftFilter = { kinds: [], states: [] };

export const DRAFT_COLUMN_ORDER: readonly DraftColumn[] = [
  DraftColumn.Kind,
  DraftColumn.State,
  DraftColumn.Label,
  DraftColumn.SourceLocation,
  DraftColumn.ProposedAt,
];

/**
 * The payload of the dialog: the machine whose drafts it lists (`machines.drafts`), or the ingest attempt whose drafts
 * it lists (`files.ingestSummary`).
 */
interface DraftsScopePayload {
  readonly machineId?: string;
  readonly attemptId?: string;
}

interface DraftsScope {
  readonly kind: 'machine' | 'attempt';
  readonly id: string;
}

const PENDING_ONLY = { states: setFilter([DraftState.Pending]) };

/**
 * The drafts dialog of one machine or one ingest attempt. Both lists have the same grid, the same filters, the same
 * order (pending first), and the same buttons. The store owns the open action with the dismiss flags. The grid, the
 * quick apply, the toasts, and the confirmation are shared features with their own state.
 */
export const MachineDraftsStore = signalStore(
  withGridState<MachineDraftFilter, DraftSortColumn, DraftColumn>({
    limit: DEFAULT_PAGE_SIZE,
    sort: { column: DraftSortColumn.State, direction: SortDirection.Ascending },
    filter: INITIAL_DRAFT_FILTER,
    columns: DRAFT_COLUMN_ORDER,
  }),
  withState<{
    action: AppAction | null;
    hasChanged: boolean;
    isDismissing: boolean;
    /** The checked drafts by ID. The checks stay while the user changes the page, the sort, or the filter. */
    selection: Readonly<Record<string, MachineDraftGridItem>>;
  }>({
    action: null,
    hasChanged: false,
    isDismissing: false,
    selection: {},
  }),
  withToasts(),
  withConfirm(),
  withComputed((store) => {
    const auth = inject(AuthStore);
    return {
      machineId: computed(() => (store.action()?.payload as DraftsScopePayload | undefined)?.machineId),
      attemptId: computed(() => (store.action()?.payload as DraftsScopePayload | undefined)?.attemptId),
      canWrite: computed(() => auth.hasPermission(Permission.DraftsWrite)),
    };
  }),
  withComputed((store) => ({
    scope: computed((): DraftsScope | undefined => {
      const attemptId = store.attemptId();
      const machineId = store.machineId();
      if (attemptId !== undefined) {
        return { kind: 'attempt', id: attemptId };
      }
      return machineId === undefined ? undefined : { kind: 'machine', id: machineId };
    }),
  })),
  withProps((store) => {
    const api = inject(DraftsApi);
    const search = (
      scope: DraftsScope,
      body: SearchMachineDraftsBody,
      options?: Parameters<DraftsApi['searchForMachine']>[2],
    ): Observable<MachineDraftsPage> =>
      scope.kind === 'attempt'
        ? api.searchForAttempt(scope.id, body, options)
        : api.searchForMachine(scope.id, body, options);
    const drafts = rxResource({
      params: () => {
        const scope = store.scope();
        if (scope === undefined) {
          return undefined;
        }
        const sort = store.sort();
        const body: SearchMachineDraftsBody = {
          offset: store.offset(),
          limit: store.limit(),
          ...(sort === null ? {} : { sort }),
          filter: { kinds: setFilter(store.filter().kinds), states: setFilter(store.filter().states) },
        };
        return { scope, body };
      },
      stream: ({ params }) => search(params.scope, params.body),
    });
    return {
      drafts,
      _search: search,
      /** The number of pending drafts in the scope, whatever the grid filter. */
      pending: rxResource({
        params: () => store.scope(),
        stream: ({ params }) =>
          search(params, { offset: 0, limit: 1, filter: { kinds: setFilter([]), ...PENDING_ONLY } }, skipErrorToast()),
      }),
      /** The drafts of the attempts on the page, to tell which draft applies as it is. */
      attempts: rxResource({
        params: () => {
          const ids = drafts.hasValue() ? [...new Set(drafts.value().items.map((item) => item.attemptId))] : [];
          return ids.length === 0 ? undefined : ids.join(',');
        },
        stream: ({ params }) =>
          forkJoin(
            params
              .split(',')
              .map((attemptId) =>
                api.listForAttempt(attemptId, skipErrorToast()).pipe(map((list) => ({ attemptId, drafts: list }))),
              ),
          ),
      }),
    };
  }),
  withComputed((store) => ({
    /** True when the last load of the list failed: the dialog shows an error with a retry, never the empty state. */
    hasLoadError: computed(() => store.drafts.status() === 'error'),
    /** The number of pending drafts in the scope. Applied and dismissed drafts are no work left. */
    pendingCount: computed(() => (store.pending.hasValue() ? store.pending.value().totalCount : 0)),
    selectedIds: computed(() => Object.keys(store.selection())),
    selectedCount: computed(() => Object.keys(store.selection()).length),
  })),
  withMethods((store) => ({
    start(action: AppAction): void {
      patchState(store, { action, hasChanged: false, isDismissing: false, selection: {} });
      store.resetGrid();
    },
    /** Loads the list again after a failed load. */
    retryLoad(): void {
      store.drafts.reload();
    },
    /** Loads the list and the pending count again. */
    reloadDrafts(): void {
      store.drafts.reload();
      store.pending.reload();
    },
  })),
  withDraftQuickApply(),
  withMethods((store) => {
    const api = inject(DraftsApi);
    const translate = inject(TranslateService);
    const actions = inject(ActionStore);
    const router = inject(Router);

    const dismissDraft = rxMethod<MachineDraftGridItem>(
      pipe(
        exhaustMap((item) =>
          api.dismiss(item.id, skipErrorToast()).pipe(
            tap(() => {
              patchState(store, (state) => ({
                hasChanged: true,
                selection: withSelected(state.selection, [item], false),
              }));
              store._toastSuccess('DRAFTS.TOASTS.DISMISSED');
              store.reloadDrafts();
            }),
            catchError((error: unknown) => {
              store._toastError(draftConflictKey(error) ?? httpErrorKey(error));
              store.reloadDrafts();
              return EMPTY;
            }),
          ),
        ),
      ),
    );

    const dismissSelected = rxMethod<readonly string[]>(
      pipe(
        tap(() => patchState(store, { isDismissing: true })),
        exhaustMap((ids) => {
          const doneIds: string[] = [];
          const withoutDone = (): Readonly<Record<string, MachineDraftGridItem>> =>
            Object.fromEntries(Object.entries(store.selection()).filter(([id]) => !doneIds.includes(id)));
          return from(ids).pipe(
            concatMap((id) => api.dismiss(id, skipErrorToast()).pipe(tap(() => doneIds.push(id)))),
            toArray(),
            tap(() => {
              patchState(store, { hasChanged: true, isDismissing: false, selection: {} });
              store._toastSuccess('DRAFTS.TOASTS.DISMISSED_ALL', { count: doneIds.length });
              store.reloadDrafts();
              store.attempts.reload();
            }),
            catchError((error: unknown) => {
              patchState(store, (state) => ({
                isDismissing: false,
                hasChanged: state.hasChanged || doneIds.length > 0,
                selection: withoutDone(),
              }));
              store._toastError(draftConflictKey(error) ?? httpErrorKey(error));
              store.reloadDrafts();
              return EMPTY;
            }),
          );
        }),
      ),
    );

    const dismissAll = rxMethod<DraftsScope>(
      pipe(
        tap(() => patchState(store, { isDismissing: true })),
        exhaustMap((scope) =>
          (scope.kind === 'attempt'
            ? api.dismissForAttempt(scope.id, skipErrorToast())
            : api.dismissForMachine(scope.id, skipErrorToast())
          ).pipe(
            tap((response) => {
              patchState(store, { hasChanged: true, isDismissing: false });
              store._toastSuccess('DRAFTS.TOASTS.DISMISSED_ALL', { count: response.dismissedCount });
              store.reloadDrafts();
              store.attempts.reload();
            }),
            catchError((error: unknown) => {
              patchState(store, { isDismissing: false });
              store._toastError(draftConflictKey(error) ?? httpErrorKey(error));
              store.reloadDrafts();
              return EMPTY;
            }),
          ),
        ),
      ),
    );

    const confirmDismissAll = (scope: DraftsScope, count: number): void => {
      store._confirmDestructive({
        headerKey: 'DRAFTS.DISMISS_ALL_HEADER',
        messageKey: 'DRAFTS.DISMISS_ALL_MESSAGE',
        params: { count },
        acceptKey: 'DRAFTS.DISMISS_ALL',
        onAccept: () => {
          dismissAll(scope);
        },
      });
    };

    return {
      /** Asks for a confirmation, then dismisses every pending draft in the scope in one call. */
      requestDismissAll(): void {
        if (store.selectedCount() > 0) {
          const ids = store.selectedIds();
          store._confirmDestructive({
            headerKey: 'DRAFTS.DISMISS_SELECTED_HEADER',
            messageKey: 'DRAFTS.DISMISS_SELECTED_MESSAGE',
            params: { count: ids.length },
            acceptKey: 'DRAFTS.DISMISS',
            onAccept: () => {
              dismissSelected(ids);
            },
          });
          return;
        }
        const scope = store.scope();
        if (scope === undefined) {
          return;
        }
        if (store.pendingCount() === 0) {
          store.reloadDrafts();
        } else {
          confirmDismissAll(scope, store.pendingCount());
        }
      },
      /** Checks or unchecks one pending draft. */
      toggleSelected(item: MachineDraftGridItem, isChecked: boolean): void {
        patchState(store, (state) => ({ selection: withSelected(state.selection, [item], isChecked) }));
      },
      /** Checks or unchecks every given pending draft, the pending drafts of the page. */
      toggleSelectedMany(items: readonly MachineDraftGridItem[], isChecked: boolean): void {
        patchState(store, (state) => ({ selection: withSelected(state.selection, items, isChecked) }));
      },
      /** Drops every check. */
      clearSelection(): void {
        patchState(store, { selection: {} });
      },
      requestDismiss(item: MachineDraftGridItem): void {
        store._confirmDestructive({
          headerKey: 'DRAFTS.DISMISS_HEADER',
          messageKey: 'DRAFTS.DISMISS_MESSAGE',
          params: { name: item.label ?? translate.instant('DRAFTS.UNNAMED') },
          acceptKey: 'DRAFTS.DISMISS',
          onAccept: () => {
            dismissDraft(item);
          },
        });
      },
      /** Opens the review of a draft. A metric draft opens the metrics page with all pending metric drafts of the attempt. */
      requestReview(item: MachineDraftGridItem): void {
        if (item.kind === DraftKind.Machine) {
          reviewMachineDraft(actions, item.id, item.isCreate);
        } else if (item.kind === DraftKind.Status && item.machineId !== null) {
          reviewStatusDraft(actions, { machineId: item.machineId, attemptId: item.attemptId, draftId: item.id });
        } else if (item.kind === DraftKind.Metric && item.machineId !== null) {
          void reviewMetricDrafts(actions, router, item.machineId, item.attemptId);
        }
      },
      /** Applies one draft without the review screen. */
      requestApply(item: MachineDraftGridItem): void {
        store.applyDraft(item.attemptId, item.id);
      },
      /**
       * Applies the pending drafts in the scope: every group of the attempt, or the drafts of the machine in every
       * attempt, on all pages of its pending drafts.
       */
      requestApplyAll(): void {
        if (store.selectedCount() > 0) {
          store.applyDrafts(toApplyList(Object.values(store.selection())));
          return;
        }
        const scope = store.scope();
        if (scope?.kind === 'attempt') {
          store.applyAttempt(scope.id);
        } else if (scope?.kind === 'machine') {
          store.applyMachine(scope.id);
        }
      },
      /** Closes the dialog. After a dismiss, the close tells the machines grid to reload. */
      close(): void {
        if (store.hasChanged()) {
          actions.complete(DraftActionType.Changed);
        } else {
          actions.cancel();
        }
      },
    };
  }),
  withHooks({
    onInit(store) {
      effect(() => {
        store.setQuickDrafts(store.attempts.value() ?? []);
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
            patchState(store, { selection: {} });
            store.reloadDrafts();
            store.attempts.reload();
          });
        }
      });
    },
  }),
  withStoreDevtools('machine-drafts'),
);
