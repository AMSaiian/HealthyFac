import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, inject, type ResourceRef, type Signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import {
  type EmptyFeatureResult,
  patchState,
  type SignalStoreFeature,
  signalStoreFeature,
  withComputed,
  withMethods,
  withProps,
  withState,
} from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import {
  concat,
  concatMap,
  concatWith,
  defer,
  EMPTY,
  exhaustMap,
  finalize,
  from,
  ignoreElements,
  map,
  type MonoTypeOperatorFunction,
  type Observable,
  of,
  pipe,
  retry,
  switchMap,
  tap,
  throwError,
  timer,
  toArray,
} from 'rxjs';
import { catchError } from 'rxjs/operators';

import { setFilter } from '@shared/grid/requests/grid-filters';
import { SortDirection } from '@shared/grid/requests/grid-request';
import { httpErrorKey, skipErrorToast } from '@shared/http/http-error-key';

import {
  type AttemptDraftGroup,
  type AttemptDrafts,
  DraftSortColumn,
  DraftState,
  type MachineDraftDetail,
  type MachineDraftsPage,
  type MetricDraftDetail,
  type StatusDraftDetail,
} from '@machines/drafts/draft';
import { draftConflictKey } from '@machines/drafts/draft-conflict';
import { DraftsApi } from '@machines/drafts/drafts-api';
import {
  applicableMetrics,
  applicableStatuses,
  chunk,
  groupReason,
  isTooManyRequests,
  MACHINE_SYNC_RETRY_COUNT,
  machineSyncDelayMs,
  needsReviewReasons,
  retryAfterSeconds,
  STATUS_BATCH_MAX_ITEMS,
} from '@machines/drafts/with-draft-quick-apply.utils';
import { toBatchChange } from '@machines/drafts/status/status-draft-review-store/status-draft-review.store.utils';
import { MetricDefinitionsApi } from '@machines/metrics/metric-definitions-api';
import type { MetricUnitsResponse } from '@machines/metrics/metric-definition';
import { MetricUnitsApi } from '@machines/metrics/metric-units-api';
import {
  itemsFromDefinitions,
  netChanges,
  stageMetricDrafts,
  toApplyBody,
} from '@machines/metrics/metrics-page-store/metrics-page.store.utils';
import { StatusDefinitionsApi } from '@machines/statuses/status-definitions-api';

/** The loaded drafts of one attempt. */
export interface AttemptDraftsEntry {
  readonly attemptId: string;
  readonly drafts: AttemptDrafts;
}

/** Points to one group of an attempt: by its machine draft, else by its machine. */
export interface GroupRef {
  readonly machineDraftId: string | null;
  readonly machineId: string | null;
}

export interface QuickApplyState {
  applyingIds: readonly string[];
  quickDrafts: readonly AttemptDraftsEntry[];
  /** Counts the finished runs. The host reloads its lists when the count changes. */
  quickRun: { readonly count: number; readonly hasApplied: boolean };
}

/**
 * Telemetry learns of a new machine through a message from Core, so its endpoints answer `404` for a short time
 * after the machine create. The first Telemetry call of a step asks again with a doubling wait (about 15 seconds in
 * all), then the step fails with `MachineNotReadyError`. The drafts stay pending.
 */
class MachineNotReadyError extends Error {}

/** The error of a call that got `429` again after the wait. The run stops and the rest of the drafts stay pending. */
class RateLimitedError extends Error {}

function isNotFoundError(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === HttpStatusCode.NotFound;
}

/** Asks again on `404` (the new machine may not have reached Telemetry yet), then fails with `MachineNotReadyError`. */
function retryWhileMachineUnknown<T>(): MonoTypeOperatorFunction<T> {
  return pipe(
    retry({
      count: MACHINE_SYNC_RETRY_COUNT,
      delay: (error: unknown, retryCount: number) =>
        isNotFoundError(error) ? timer(machineSyncDelayMs(retryCount)) : throwError(() => error),
    }),
    catchError((error: unknown) => throwError(() => (isNotFoundError(error) ? new MachineNotReadyError() : error))),
  );
}

const TOAST_SUMMARY_KEYS = {
  success: 'TOASTS.SUCCESS',
  error: 'TOASTS.ERROR',
  info: 'DRAFTS.QUICK.WAITING_TITLE',
} as const;

/** The largest page that the draft search accepts. */
const SEARCH_PAGE_SIZE = 100;

/** One draft to apply: the attempt that holds it and its ID. */
export interface DraftRef {
  readonly attemptId: string;
  readonly draftId: string;
}

type RunRequest =
  | { readonly kind: 'draft'; readonly attemptId: string; readonly draftId: string }
  | { readonly kind: 'group'; readonly attemptId: string; readonly group: GroupRef }
  | { readonly kind: 'drafts'; readonly items: readonly DraftRef[] }
  | { readonly kind: 'attempt'; readonly attemptId: string }
  | { readonly kind: 'machine'; readonly machineId: string };

interface Progress {
  applied: number;
  skipped: number;
  /** The metric and status drafts that a call applied. One `mark-applied` call at the end of the run marks them. */
  readonly appliedIds: string[];
}

function notFound(): HttpErrorResponse {
  return new HttpErrorResponse({ status: HttpStatusCode.NotFound });
}

function pendingIds(group: AttemptDraftGroup): string[] {
  const drafts = [...(group.machineDraft === null ? [] : [group.machineDraft]), ...group.metrics, ...group.statuses];
  return drafts.filter((draft) => draft.state === DraftState.Pending).map((draft) => draft.id);
}

function failureKey(error: unknown): string {
  if (error instanceof MachineNotReadyError) {
    return 'DRAFTS.QUICK.MACHINE_NOT_READY';
  }
  if (error instanceof RateLimitedError) {
    return 'DRAFTS.QUICK.RATE_LIMITED';
  }
  return httpErrorKey(error, {
    [HttpStatusCode.Conflict]: draftConflictKey(error) ?? 'DRAFTS.QUICK.CONFLICT',
    [HttpStatusCode.BadRequest]: 'DRAFTS.QUICK.INVALID',
  });
}

/**
 * Applies pending drafts without the review screen, with the calls of the review flows. The host store gives the
 * loaded drafts (`setQuickDrafts`) for the reasons and reloads its lists when `quickRun` changes. A group run applies
 * the machine draft first, then all metric drafts of the machine in one apply call (one schema revision), then each
 * status draft, and stops at the first failure. A machine run reads every page of the pending drafts of the machine
 * and runs one group run per attempt, oldest attempt first. A draft that needs review stays pending and counts as
 * skipped. One toast reports each run.
 */
export function withDraftQuickApply(): SignalStoreFeature<
  EmptyFeatureResult,
  {
    state: QuickApplyState;
    props: {
      _quickDraftsApi: DraftsApi;
      _quickMetricsApi: MetricDefinitionsApi;
      _quickStatusesApi: StatusDefinitionsApi;
      _quickMessages: MessageService;
      _quickTranslate: TranslateService;
      _quickUnitsApi: MetricUnitsApi;
      units: ResourceRef<MetricUnitsResponse | undefined>;
      isQuickBusy: Signal<boolean>;
      quickReasons: Signal<Readonly<Record<string, string | null>>>;
      quickGroupReasons: Signal<Readonly<Record<string, string | null>>>;
    };
    methods: {
      setQuickDrafts: (quickDrafts: readonly AttemptDraftsEntry[]) => void;
      applyDraft: (attemptId: string, draftId: string) => void;
      applyGroup: (attemptId: string, group: GroupRef) => void;
      applyAttempt: (attemptId: string) => void;
      applyDrafts: (items: readonly DraftRef[]) => void;
      applyMachine: (machineId: string) => void;
    };
  }
> {
  return signalStoreFeature(
    withState<QuickApplyState>({ applyingIds: [], quickDrafts: [], quickRun: { count: 0, hasApplied: false } }),
    withProps(() => ({
      _quickDraftsApi: inject(DraftsApi),
      _quickMetricsApi: inject(MetricDefinitionsApi),
      _quickStatusesApi: inject(StatusDefinitionsApi),
      _quickMessages: inject(MessageService),
      _quickTranslate: inject(TranslateService),
      _quickUnitsApi: inject(MetricUnitsApi),
    })),
    withProps((store) => ({
      units: rxResource({ stream: () => store._quickUnitsApi.getUnits() }),
    })),
    withComputed((store) => {
      const catalog = computed(() => (store.units.hasValue() ? store.units.value() : undefined));
      return {
        isQuickBusy: computed(() => store.applyingIds().length > 0),
        quickReasons: computed(() => {
          const reasons: Record<string, string | null> = {};
          for (const entry of store.quickDrafts()) {
            Object.assign(reasons, needsReviewReasons(entry.drafts, catalog()));
          }
          return reasons as Readonly<Record<string, string | null>>;
        }),
        /** The reason that **Apply all** is off for each group, keyed by the machine draft ID, else the machine ID. */
        quickGroupReasons: computed(() => {
          const reasons: Record<string, string | null> = {};
          for (const entry of store.quickDrafts()) {
            for (const group of entry.drafts.groups) {
              const key = group.machineDraft?.id ?? group.machineId;
              if (key !== null) {
                reasons[key] = groupReason(group, catalog());
              }
            }
          }
          return reasons as Readonly<Record<string, string | null>>;
        }),
      };
    }),
    withMethods((store) => {
      const options = skipErrorToast();
      const catalog = (): ReturnType<typeof store.units.value> | undefined =>
        store.units.hasValue() ? store.units.value() : undefined;

      /**
       * Sends a call. On `429` it shows how long it waits, waits for `Retry-After`, and sends the same call once more;
       * a second `429` ends the call with `RateLimitedError`.
       */
      const rateLimited$ = <T>(call: () => Observable<T>): Observable<T> =>
        defer(call).pipe(
          catchError((error: unknown) => {
            if (!isTooManyRequests(error)) {
              return throwError(() => error);
            }
            const seconds = retryAfterSeconds(error);
            toast('info', store._quickTranslate.instant('DRAFTS.QUICK.WAITING', { seconds }));
            return timer(seconds * 1000).pipe(
              switchMap(() => defer(call)),
              catchError((second: unknown) =>
                throwError(() => (isTooManyRequests(second) ? new RateLimitedError() : second)),
              ),
            );
          }),
        );

      const fresh$ = (attemptId: string): Observable<AttemptDrafts> =>
        rateLimited$(() => store._quickDraftsApi.listForAttempt(attemptId, options));

      const mark$ = (ids: readonly string[]): Observable<void> =>
        ids.length === 0 ? of(undefined) : rateLimited$(() => store._quickDraftsApi.markApplied(ids, options));

      /**
       * Runs the steps, then sends one `mark-applied` call with the drafts that the steps applied. A failed step still
       * marks the drafts that an earlier step applied, so no applied draft stays pending.
       */
      const withMark$ = (run$: Observable<void>, progress: Progress): Observable<void> => {
        const markApplied$ = defer(() => mark$(progress.appliedIds.splice(0)));

        return concat(
          run$.pipe(
            ignoreElements(),
            catchError((error: unknown) =>
              markApplied$.pipe(
                catchError(() => EMPTY),
                ignoreElements(),
                concatWith(throwError(() => error)),
              ),
            ),
          ),
          markApplied$,
        );
      };

      /** Applies a machine draft. The result is the ID of the machine that the draft made or changed. */
      const machineStep$ = (draft: MachineDraftDetail, progress: Progress): Observable<string> => {
        const apply$ = draft.isCreate
          ? rateLimited$(() =>
              store._quickDraftsApi.applyMachineDraft(
                draft.id,
                { name: draft.name ?? '', serialNumber: draft.serialNumber ?? '', description: draft.description },
                options,
              ),
            )
          : rateLimited$(() => store._quickDraftsApi.getMachineDraft(draft.id, options)).pipe(
              switchMap(({ current }) =>
                current === null
                  ? throwError(notFound)
                  : rateLimited$(() =>
                      store._quickDraftsApi.applyMachineDraft(
                        draft.id,
                        {
                          name: draft.name ?? current.name,
                          serialNumber: draft.serialNumber ?? current.serialNumber,
                          description: draft.description ?? current.description,
                          rowVersion: current.rowVersion,
                        },
                        options,
                      ),
                    ),
              ),
            );
        return apply$.pipe(
          tap(() => {
            progress.applied += 1;
          }),
          map((response) => response.machineId),
        );
      };

      /** Stages the drafts on the saved definitions and sends one apply call. The run marks the drafts at its end. */
      const metricsStep$ = (
        machineId: string,
        drafts: readonly MetricDraftDetail[],
        progress: Progress,
      ): Observable<void> =>
        rateLimited$(() => store._quickMetricsApi.getDefinitions(machineId, options)).pipe(
          retryWhileMachineUnknown(),
          switchMap((response) => {
            const staged = stageMetricDrafts(itemsFromDefinitions(response.definitions), 0, drafts, catalog());
            const changes = netChanges(staged.items);
            const hasChanges = changes.created.length + changes.updated.length > 0;
            const apply$: Observable<unknown> = hasChanges
              ? rateLimited$(() =>
                  store._quickMetricsApi.apply(machineId, toApplyBody(response.revision, changes), options),
                )
              : of(undefined);
            return apply$.pipe(
              tap(() => {
                progress.applied += drafts.length;
                progress.appliedIds.push(...drafts.map((draft) => draft.id));
              }),
              map(() => undefined),
            );
          }),
        );

      /** Sends the statuses in batches of at most 200: each batch creates or changes the statuses by code. */
      const statusesStep$ = (
        machineId: string,
        drafts: readonly StatusDraftDetail[],
        progress: Progress,
      ): Observable<void> =>
        from(chunk(drafts, STATUS_BATCH_MAX_ITEMS)).pipe(
          concatMap((part) =>
            rateLimited$(() =>
              store._quickStatusesApi.apply(machineId, { items: part.map(toBatchChange) }, options),
            ).pipe(
              retryWhileMachineUnknown(),
              tap(() => {
                progress.applied += part.length;
                progress.appliedIds.push(...part.map((draft) => draft.id));
              }),
            ),
          ),
          toArray(),
          map(() => undefined),
        );

      const draftRun$ = (attemptId: string, draftId: string, progress: Progress): Observable<void> =>
        withMark$(
          fresh$(attemptId).pipe(
            switchMap((drafts) => {
              for (const group of drafts.groups) {
                const machine = group.machineDraft?.id === draftId ? group.machineDraft : null;
                const metric = group.metrics.find((candidate) => candidate.id === draftId);
                const status = group.statuses.find((candidate) => candidate.id === draftId);
                if (machine !== null) {
                  return machineStep$(machine, progress).pipe(map(() => undefined));
                }
                if (group.machineId === null) {
                  continue;
                }
                if (metric !== undefined) {
                  return metricsStep$(group.machineId, [metric], progress);
                }
                if (status !== undefined) {
                  return statusesStep$(group.machineId, [status], progress);
                }
              }
              return throwError(notFound);
            }),
          ),
          progress,
        );

      const findGroup = (drafts: AttemptDrafts, ref: GroupRef): AttemptDraftGroup | null =>
        drafts.groups.find((group) =>
          ref.machineDraftId === null
            ? group.machineId === ref.machineId
            : group.machineDraft?.id === ref.machineDraftId,
        ) ?? null;

      const machineOfGroup$ = (
        attemptId: string,
        group: AttemptDraftGroup,
        progress: Progress,
      ): Observable<AttemptDraftGroup> => {
        const machine = group.machineDraft;
        if (machine === null || machine.state !== DraftState.Pending) {
          return of(group);
        }
        return machineStep$(machine, progress).pipe(
          switchMap((machineId) =>
            machine.isCreate
              ? fresh$(attemptId).pipe(
                  map((drafts) => drafts.groups.find((candidate) => candidate.machineId === machineId) ?? group),
                  map((moved) => (moved.machineId === null ? { ...moved, machineId } : moved)),
                )
              : of(group),
          ),
        );
      };

      const childrenOfGroup$ = (group: AttemptDraftGroup, progress: Progress): Observable<void> => {
        const machineId = group.machineId;
        if (machineId === null) {
          return of(undefined);
        }
        const metrics = applicableMetrics(group, catalog());
        const statuses = applicableStatuses(group);
        const pendingCount =
          group.metrics.filter((draft) => draft.state === DraftState.Pending).length +
          group.statuses.filter((draft) => draft.state === DraftState.Pending).length;
        progress.skipped += pendingCount - metrics.length - statuses.length;
        const metrics$ = metrics.length === 0 ? of(undefined) : metricsStep$(machineId, metrics, progress);
        return metrics$.pipe(
          concatMap(() => (statuses.length === 0 ? of(undefined) : statusesStep$(machineId, statuses, progress))),
        );
      };

      const groupRun$ = (attemptId: string, ref: GroupRef, progress: Progress): Observable<void> =>
        withMark$(
          fresh$(attemptId).pipe(
            switchMap((drafts) => {
              const group = findGroup(drafts, ref);
              return group === null ? throwError(notFound) : machineOfGroup$(attemptId, group, progress);
            }),
            switchMap((group) => childrenOfGroup$(group, progress)),
          ),
          progress,
        );

      const pendingPage$ = (machineId: string, offset: number): Observable<MachineDraftsPage> =>
        rateLimited$(() =>
          store._quickDraftsApi.searchForMachine(
            machineId,
            {
              offset,
              limit: SEARCH_PAGE_SIZE,
              sort: { column: DraftSortColumn.ProposedAt, direction: SortDirection.Ascending },
              filter: { states: setFilter([DraftState.Pending]) },
            },
            options,
          ),
        );

      /** Reads every page of the pending drafts of the machine. The IDs of the attempts come oldest first. */
      const pendingAttemptIds$ = (machineId: string): Observable<readonly string[]> =>
        pendingPage$(machineId, 0).pipe(
          switchMap((first) => {
            const pageCount = Math.ceil(first.totalCount / SEARCH_PAGE_SIZE);
            const offsets = Array.from(
              { length: Math.max(pageCount - 1, 0) },
              (_, index) => (index + 1) * SEARCH_PAGE_SIZE,
            );
            return from(offsets).pipe(
              concatMap((offset) => pendingPage$(machineId, offset)),
              toArray(),
              map((rest) => [first, ...rest]),
            );
          }),
          map((pages) => [...new Set(pages.flatMap((page) => page.items.map((item) => item.attemptId)))]),
        );

      /** Applies the drafts of the machine in every attempt, one attempt after the other, oldest first. */
      const machineRun$ = (machineId: string, progress: Progress): Observable<void> =>
        pendingAttemptIds$(machineId).pipe(
          switchMap((attemptIds) =>
            from(attemptIds).pipe(
              concatMap((attemptId) => groupRun$(attemptId, { machineDraftId: null, machineId }, progress)),
              toArray(),
            ),
          ),
          map(() => undefined),
        );

      /** Applies the given drafts one after the other and stops at the first failure. Give them in apply order. */
      const draftsRun$ = (items: readonly DraftRef[], progress: Progress): Observable<void> =>
        from(items).pipe(
          concatMap((item) => draftRun$(item.attemptId, item.draftId, progress)),
          toArray(),
          map(() => undefined),
        );

      /** Applies every group of the attempt, one group after the other. */
      const attemptRun$ = (attemptId: string, progress: Progress): Observable<void> => {
        const entry = store.quickDrafts().find((candidate) => candidate.attemptId === attemptId);
        const refs: GroupRef[] = (entry?.drafts.groups ?? []).map((group) => ({
          machineDraftId: group.machineDraft?.id ?? null,
          machineId: group.machineId,
        }));
        return from(refs).pipe(
          concatMap((ref) => groupRun$(attemptId, ref, progress)),
          toArray(),
          map(() => undefined),
        );
      };

      const idsOf = (request: RunRequest): string[] => {
        if (request.kind === 'draft') {
          return [request.draftId];
        }
        if (request.kind === 'drafts') {
          return request.items.map((item) => item.draftId);
        }
        if (request.kind === 'attempt') {
          return store
            .quickDrafts()
            .filter((entry) => entry.attemptId === request.attemptId)
            .flatMap((entry) => entry.drafts.groups)
            .flatMap(pendingIds);
        }
        return store
          .quickDrafts()
          .flatMap((entry) =>
            request.kind === 'group' && entry.attemptId !== request.attemptId ? [] : entry.drafts.groups,
          )
          .filter((group) =>
            request.kind === 'machine'
              ? group.machineId === request.machineId
              : request.group.machineDraftId === null
                ? group.machineId === request.group.machineId
                : group.machineDraft?.id === request.group.machineDraftId,
          )
          .flatMap(pendingIds);
      };

      const toast = (severity: 'success' | 'error' | 'info', detail: string): void => {
        store._quickMessages.add({
          severity,
          summary: store._quickTranslate.instant(TOAST_SUMMARY_KEYS[severity]),
          detail,
        });
      };

      const run = rxMethod<RunRequest>(
        pipe(
          exhaustMap((request) => {
            const progress: Progress = { applied: 0, skipped: 0, appliedIds: [] };
            patchState(store, { applyingIds: idsOf(request) });
            const run$ =
              request.kind === 'draft'
                ? draftRun$(request.attemptId, request.draftId, progress)
                : request.kind === 'drafts'
                  ? draftsRun$(request.items, progress)
                  : request.kind === 'group'
                    ? groupRun$(request.attemptId, request.group, progress)
                    : request.kind === 'attempt'
                      ? attemptRun$(request.attemptId, progress)
                      : machineRun$(request.machineId, progress);
            return run$.pipe(
              tap(() => {
                toast(
                  'success',
                  store._quickTranslate.instant(
                    progress.skipped > 0 ? 'DRAFTS.QUICK.APPLIED_SKIPPED' : 'DRAFTS.QUICK.APPLIED',
                    { count: progress.applied, skipped: progress.skipped },
                  ),
                );
              }),
              catchError((error: unknown) => {
                toast(
                  'error',
                  store._quickTranslate.instant('DRAFTS.QUICK.FAILED', {
                    count: progress.applied,
                    reason: store._quickTranslate.instant(failureKey(error)),
                  }),
                );
                return EMPTY;
              }),
              finalize(() => {
                patchState(store, { applyingIds: [] });
                patchState(store, (state) => ({
                  quickRun: { count: state.quickRun.count + 1, hasApplied: progress.applied > 0 },
                }));
              }),
            );
          }),
        ),
      );

      return {
        /** Gives the loaded drafts of the attempts that the lists show. The reasons and the group runs read them. */
        setQuickDrafts(quickDrafts: readonly AttemptDraftsEntry[]): void {
          patchState(store, { quickDrafts });
        },
        applyDraft(attemptId: string, draftId: string): void {
          run({ kind: 'draft', attemptId, draftId });
        },
        applyGroup(attemptId: string, group: GroupRef): void {
          run({ kind: 'group', attemptId, group });
        },
        /** Applies the given drafts one after the other, in the given order, in one run. */
        applyDrafts(items: readonly DraftRef[]): void {
          run({ kind: 'drafts', items });
        },
        /** Applies every group of one attempt, one group after the other. */
        applyAttempt(attemptId: string): void {
          run({ kind: 'attempt', attemptId });
        },
        /** Applies the drafts of one machine in every attempt, on all pages of its pending drafts. */
        applyMachine(machineId: string): void {
          run({ kind: 'machine', machineId });
        },
      };
    }),
  );
}
