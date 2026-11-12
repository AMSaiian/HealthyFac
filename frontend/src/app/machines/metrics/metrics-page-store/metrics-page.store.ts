import type { HttpErrorResponse } from '@angular/common/http';
import { HttpStatusCode } from '@angular/common/http';
import { computed, effect, inject, untracked } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, type Observable, of, pipe, tap } from 'rxjs';
import { catchError, concatMap } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import { withConfirm } from '@shared/confirm/with-confirm';
import { hasFormChanges } from '@shared/forms/form-changes';
import { httpErrorKey, isHttpStatus, skipErrorToast } from '@shared/http/http-error-key';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';
import { RichTextImageLinks } from '@shared/rich-text/file-image/rich-text-image-links';
import { collectFileIds } from '@shared/rich-text/file-image/rich-text-images';
import { CLOCK } from '@shared/time/clock';
import { withToasts } from '@shared/toasts/with-toasts';

import type { Machine } from '@machines/machine';
import { DraftState } from '@machines/drafts/draft';
import { DraftActionType } from '@machines/drafts/draft-actions';
import { MachinesApi } from '@machines/machines-api';
import { MetricDefinitionsApi } from '@machines/metrics/metric-definitions-api';
import { metricFormSchema, type MetricFormValue } from '@machines/metrics/metric-form/metric-form-schema';
import {
  appliedDraftIds,
  buildExamplePayload,
  EMPTY_METRIC_VALUE,
  hasNetChanges,
  mapServerErrors,
  type MetricItem,
  netChanges,
  stageMetricDrafts,
  toApplyBody,
} from '@machines/metrics/metrics-page-store/metrics-page.store.utils';
import { withMetricsPageForm } from './metrics-page.form.slice';
import { withMetricsPageItems } from './metrics-page.items.slice';
import { withMetricsPageMachine } from './metrics-page.machine.slice';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

const MACHINES_URL = '/machines';

/**
 * The metrics page of one machine. Each slice owns one state group and writes only that group. The store owns the
 * flags of the apply and leave workflows, and runs the workflows that change more than one group (open, select, done,
 * discard, apply).
 */
export const MetricsPageStore = signalStore(
  withToasts(),
  withConfirm(),
  withMetricsPageMachine(),
  withMetricsPageItems(),
  withMetricsPageForm(),
  withState({ isApplying: false, isLeaving: false }),
  withProps(() => ({
    _machinesApi: inject(MachinesApi),
    _api: inject(MetricDefinitionsApi),
    _actions: inject(ActionStore),
    _router: inject(Router),
  })),
  withComputed((store) => {
    const auth = inject(AuthStore);
    const clock = inject(CLOCK);
    return {
      canRead: computed(() => auth.hasPermission(Permission.MetricDefinitionsRead)),
      canWrite: computed(() => auth.hasPermission(Permission.MetricDefinitionsWrite)),
      hasChanges: computed(() => hasNetChanges(store.items())),
      examplePayload: computed((): string | null =>
        store.isExampleOpen() ? buildExamplePayload(store.items(), clock.now().toString()) : null,
      ),
      selected: computed(
        (): MetricItem | null => store.items().find((item) => item.localId === store.selectedId()) ?? null,
      ),
      editInitial: computed((): MetricFormValue | null => {
        const edit = store.edit();
        if (edit === null) {
          return null;
        }
        return edit.localId === null
          ? EMPTY_METRIC_VALUE
          : (store.items().find((item) => item.localId === edit.localId)?.value ?? null);
      }),
      takenKeys: computed((): readonly string[] => {
        const editedId = store.edit()?.localId ?? null;
        return store
          .items()
          .filter((item) => item.localId !== editedId)
          .map((item) => item.value.key);
      }),
    };
  }),
  withComputed((store) => ({
    isFormChanged: computed(() => {
      const initial = store.editInitial();
      const current = store.formValue();
      return initial !== null && current !== null && hasFormChanges(metricFormSchema, initial, current);
    }),
  })),
  withComputed((store) => ({
    canDone: computed(() => store.isFormValid() && store.isFormChanged()),
    canApply: computed(() => store.hasChanges() && store.edit() === null && !store.isApplying()),
    hasPendingWork: computed(() => store.hasChanges() || store.isFormChanged()),
  })),
  withProps((store) => {
    const imageLinksApi = inject(RichTextImageLinks);
    return {
      imageLinks: rxResource({
        params: () => {
          const ids = collectFileIds(store.selected()?.value.description ?? null);
          return ids.length === 0 ? undefined : ids;
        },
        stream: ({ params }) => imageLinksApi.resolve(params),
      }),
    };
  }),
  withComputed((store) => ({
    descriptionLinks: computed((): ReadonlyMap<string, string> =>
      store.imageLinks.hasValue() ? store.imageLinks.value() : new Map<string, string>(),
    ),
  })),
  withMethods((store) => {
    const confirmLoss = (messageKey: string, onAccept: () => void): void => {
      store._confirmDestructive({
        headerKey: 'METRICS.CONFIRM.HEADER',
        messageKey,
        acceptKey: 'METRICS.CONFIRM.ACCEPT',
        onAccept,
      });
    };

    const guardForm = (action: () => void): void => {
      if (store.isFormChanged()) {
        confirmLoss('METRICS.CONFIRM.DRAFT_MESSAGE', action);
      } else {
        action();
      }
    };

    /** Marks the drafts of the applied items. A failed mark shows a toast and leaves those drafts pending. */
    const markApplied$ = (draftIds: readonly string[]): Observable<void> => {
      if (draftIds.length === 0) {
        return of(undefined);
      }
      return store._draftsApi.markApplied(draftIds, skipErrorToast()).pipe(
        tap(() => store._actions.complete(DraftActionType.Changed)),
        catchError(() => {
          store._toastError('DRAFTS.METRICS.MARK_FAILED');
          return of(undefined);
        }),
      );
    };

    /**
     * Goes back to the page and the dialog that opened this review. The user has already decided (an apply or a
     * confirmed discard), so `isLeaving` lets the leave guard pass: the pending drafts are staged again after a
     * reload and would make it ask a second time. Does nothing without a return path.
     */
    const leaveToReturnPath = (): void => {
      const url = store._actions.returnPath()?.url;
      if (url !== undefined) {
        patchState(store, { isLeaving: true });
        void store._router.navigateByUrl(url).finally(() => {
          patchState(store, { isLeaving: false });
        });
      }
    };

    const applyRequest = rxMethod<void>(
      pipe(
        exhaustMap(() => {
          const changes = netChanges(store.items());
          const machineId = store.machineId() ?? '';
          patchState(store, { isApplying: true });
          store._setItemErrors({});
          return store._api.apply(machineId, toApplyBody(store.revision(), changes), skipErrorToast()).pipe(
            concatMap(() => markApplied$(appliedDraftIds(changes))),
            tap(() => {
              store._toastSuccess('METRICS.TOASTS.APPLIED');
              patchState(store, { isApplying: false });
              store.reload();
              store.drafts.reload();
              leaveToReturnPath();
            }),
            catchError((error: unknown) => {
              patchState(store, { isApplying: false });
              if (isHttpStatus(error, HttpStatusCode.BadRequest)) {
                const body = (error as HttpErrorResponse).error as { errors?: Record<string, string[]> } | null;
                store._setItemErrors(mapServerErrors(body?.errors ?? {}, changes));
                store._toastError('METRICS.TOASTS.INVALID');
              } else {
                store._toastError(httpErrorKey(error, { [HttpStatusCode.Conflict]: 'METRICS.TOASTS.CONFLICT' }));
              }
              return EMPTY;
            }),
          );
        }),
      ),
    );

    return {
      /**
       * Starts a visit to the metrics of a machine: loads the machine, then resets the page. The state changes only after
       * a successful load. The load skips the error toast, so the caller decides what a failure means.
       */
      openMachine(machineId: string): Observable<Machine> {
        return store._machinesApi.get(machineId, skipErrorToast()).pipe(
          tap((machine) => {
            store._setMachine(machineId, machine);
            store._resetNewCount();
            store.setFormValue(null);
            store.setFormValidity(false);
            store.closeExample();
            patchState(store, { isLeaving: false });
          }),
        );
      },
      select(localId: string): void {
        if (localId !== store.selectedId()) {
          guardForm(() => {
            store._closeForm();
            store._setSelectedId(localId);
          });
        }
      },
      startAdd(): void {
        guardForm(() => {
          store._openForm({ localId: null });
          store._setSelectedId(null);
        });
      },
      startEdit(): void {
        const selectedId = store.selectedId();
        if (selectedId !== null) {
          store._openForm({ localId: selectedId });
        }
      },
      cancelEdit(): void {
        store._closeForm();
      },
      done(): void {
        const edit = store.edit();
        const current = store.formValue();
        if (edit === null || current === null || !store.canDone()) {
          return;
        }
        if (edit.localId === null) {
          store._addNewItem(current);
        } else {
          store._replaceValue(edit.localId, current);
          store._clearItemError(edit.localId);
        }
        store._closeForm();
      },
      requestDiscard(): void {
        confirmLoss('METRICS.CONFIRM.DISCARD_MESSAGE', () => {
          store._closeForm();
          store._resetNewCount();
          store.reload();
          leaveToReturnPath();
        });
      },
      /** The page **Back** button: returns to the dialog that opened the review, else to the machines. */
      back(): void {
        void store._router.navigateByUrl(store._actions.returnPath()?.url ?? MACHINES_URL);
      },
      apply(): void {
        if (store.canApply()) {
          applyRequest();
        }
      },
    };
  }),
  withHooks({
    onInit(store) {
      let stagedDefinitions: unknown = null;
      let stagedDrafts: unknown = null;
      effect(() => {
        const definitions = store.definitionsValue();
        const attemptId = store.draftAttemptId();
        const machineId = store.machineId();
        const isSettled =
          store.definitions.status() === 'resolved' && store.drafts.status() === 'resolved' && store.units.hasValue();
        const drafts = isSettled ? store.drafts.value() : undefined;
        if (
          definitions === undefined ||
          drafts === undefined ||
          attemptId === null ||
          machineId === null ||
          (stagedDefinitions === definitions && stagedDrafts === drafts)
        ) {
          return;
        }
        stagedDefinitions = definitions;
        stagedDrafts = drafts;
        const pending = drafts.groups
          .filter((group) => group.machineId === machineId)
          .flatMap((group) => group.metrics)
          .filter((metric) => metric.state === DraftState.Pending);
        untracked(() => {
          store._setStaged(stageMetricDrafts(store.items(), store.newCount(), pending, store.units.value()));
        });
      });
    },
  }),
  withStoreDevtools('metrics-page'),
);
