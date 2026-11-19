import { HttpStatusCode } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, finalize, type Observable, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';
import { httpErrorKey, skipErrorToast } from '@shared/http/http-error-key';
import type { MachineLabel } from '@shared/lookups/lookup-options';
import { LookupsApi } from '@shared/lookups/lookups-api';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';
import { withToasts } from '@shared/toasts/with-toasts';

import { withRefreshCooldown } from '@shared/refresh/with-refresh-cooldown';
import { DEFAULT_PAGE_SIZE } from '@shared/grid/requests/grid-limits';

import { type CaseDetail, CaseState, type CaseToolCall } from '@maintenance/case';
import { CasesApi } from '@maintenance/cases-api';

interface ScopeState {
  caseId: string | null;
  detail: CaseDetail | null;
  isCloseOpen: boolean;
  isBusy: boolean;
  signalsOffset: number;
  signalsLimit: number;
  runsOffset: number;
  runsLimit: number;
  /** The tool calls of the runs that the user opened, by run ID. A run is final, so the calls load once. */
  toolCallsByRun: Readonly<Record<string, readonly CaseToolCall[]>>;
  /** Counts the refresh clicks. A read that depends on it loads again. */
  refreshCount: number;
}

export const CasePageStore = signalStore(
  withState<ScopeState>({
    caseId: null,
    detail: null,
    isCloseOpen: false,
    isBusy: false,
    signalsOffset: 0,
    signalsLimit: DEFAULT_PAGE_SIZE,
    runsOffset: 0,
    runsLimit: DEFAULT_PAGE_SIZE,
    toolCallsByRun: {},
    refreshCount: 0,
  }),
  withRefreshCooldown(),
  withProps(() => ({
    _auth: inject(AuthStore),
    _api: inject(CasesApi),
    _lookups: inject(LookupsApi),
  })),
  withToasts(),
  withProps((store) => ({
    signals: rxResource({
      params: () => {
        const caseId = store.caseId();
        store.refreshCount();
        return caseId === null ? undefined : { caseId, offset: store.signalsOffset(), limit: store.signalsLimit() };
      },
      stream: ({ params }) => store._api.searchSignals(params.caseId, { offset: params.offset, limit: params.limit }),
    }),
    runs: rxResource({
      params: () => {
        const caseId = store.caseId();
        store.refreshCount();
        return caseId === null ? undefined : { caseId, offset: store.runsOffset(), limit: store.runsLimit() };
      },
      stream: ({ params }) => store._api.searchRuns(params.caseId, { offset: params.offset, limit: params.limit }),
    }),
    machines: rxResource({
      params: () => store.detail()?.machineId,
      stream: ({ params }) => store._lookups.machinesByIds([params]),
      defaultValue: [],
    }),
  })),
  withComputed((store) => ({
    canWrite: computed(() => store._auth.hasPermission(Permission.CasesWrite)),
    isOpen: computed(() => store.detail()?.state === CaseState.Open),
    machineLabel: computed((): MachineLabel => {
      const option = (store.machines.hasValue() ? store.machines.value() : []).find(
        (candidate) => candidate.id === store.detail()?.machineId,
      );
      return { name: option?.displayName ?? '', isDeleted: option?.isDeleted ?? false };
    }),
  })),
  withComputed((store) => ({
    canAct: computed(() => store.canWrite() && store.isOpen()),
  })),
  withMethods((store) => {
    const fetchCase = (): Observable<CaseDetail> => store._api.get(store.caseId() ?? '', skipErrorToast());
    const reload = rxMethod<void>(
      pipe(
        exhaustMap(() =>
          fetchCase().pipe(
            tap((detail) => patchState(store, { detail })),
            catchError(() => EMPTY),
          ),
        ),
      ),
    );
    const loadToolCalls = rxMethod<string>(
      pipe(
        exhaustMap((runId) =>
          store._api.getToolCalls(store.caseId() ?? '', runId).pipe(
            tap((response) =>
              patchState(store, { toolCallsByRun: { ...store.toolCallsByRun(), [runId]: response.toolCalls } }),
            ),
            catchError(() => EMPTY),
          ),
        ),
      ),
    );
    const act = (request$: Observable<unknown>, successKey: string): Observable<unknown> => {
      patchState(store, { isBusy: true });
      return request$.pipe(
        tap(() => {
          store._toastSuccess(successKey);
          patchState(store, { isCloseOpen: false });
          reload();
        }),
        catchError((error: unknown) => {
          store._toastError(
            httpErrorKey(error, { [HttpStatusCode.UnprocessableEntity]: 'MAINTENANCE.TOASTS.NOT_OPEN' }),
          );
          reload();
          return EMPTY;
        }),
        finalize(() => patchState(store, { isBusy: false })),
      );
    };
    const runAgain = rxMethod<void>(
      pipe(
        exhaustMap(() =>
          act(store._api.runAgain(store.caseId() ?? '', skipErrorToast()), 'MAINTENANCE.TOASTS.RUN_QUEUED'),
        ),
      ),
    );
    const close = rxMethod<string>(
      pipe(
        exhaustMap((reason) =>
          act(store._api.close(store.caseId() ?? '', { reason }, skipErrorToast()), 'MAINTENANCE.TOASTS.CLOSED'),
        ),
      ),
    );

    return {
      /**
       * Opens the case of the route before the page activates. The state changes only after a successful load. The
       * load skips the error toast, so the caller decides what a failure means.
       */
      openCase(caseId: string): Observable<CaseDetail> {
        return store._api.get(caseId, skipErrorToast()).pipe(
          tap((detail) =>
            patchState(store, {
              caseId,
              detail,
              isCloseOpen: false,
              signalsOffset: 0,
              signalsLimit: DEFAULT_PAGE_SIZE,
              runsOffset: 0,
              runsLimit: DEFAULT_PAGE_SIZE,
              toolCallsByRun: {},
            }),
          ),
        );
      },
      setSignalsPage(offset: number, limit: number): void {
        patchState(store, { signalsOffset: offset, signalsLimit: limit });
      },
      setRunsPage(offset: number, limit: number): void {
        patchState(store, { runsOffset: offset, runsLimit: limit });
      },
      /** Loads the tool calls of a run when the user opens it, once per run. */
      openRun(runId: string): void {
        if (!(runId in store.toolCallsByRun())) {
          loadToolCalls(runId);
        }
      },
      /** Reloads the header, the outcome, and the current page of both tabs, once per cooldown. */
      refresh(): void {
        if (store.isRefreshCoolingDown()) {
          return;
        }
        patchState(store, { refreshCount: store.refreshCount() + 1 });
        reload();
        store.startRefreshCooldown();
      },
      runAgain: () => runAgain(),
      requestClose(): void {
        patchState(store, { isCloseOpen: true });
      },
      cancelClose(): void {
        patchState(store, { isCloseOpen: false });
      },
      close,
    };
  }),
  withStoreDevtools('case-page'),
);
