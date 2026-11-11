import { HttpContext, HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, effect, inject, linkedSignal, untracked } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import {
  patchState,
  signalStore,
  withComputed,
  withHooks,
  withLinkedState,
  withMethods,
  withProps,
  withState,
} from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { EMPTY, exhaustMap, type Observable, of, pipe, tap } from 'rxjs';
import { catchError, concatMap } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import { hasFormChanges } from '@shared/forms/form-changes';
import { httpErrorMessageKey } from '@shared/http/http-error-message';
import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';
import { AuthStore } from '@shared/security/auth.store';
import { RichTextImageLinks } from '@shared/rich-text/file-image/rich-text-image-links';
import { collectFileIds } from '@shared/rich-text/file-image/rich-text-images';
import { Permission } from '@shared/security/permissions';
import { CLOCK } from '@shared/time/clock';

import type { Machine } from '@machines/machine';
import { DraftState } from '@machines/drafts/draft';
import { DraftActionType } from '@machines/drafts/draft-actions';
import { DraftsApi } from '@machines/drafts/drafts-api';
import { MachinesApi } from '@machines/machines-api';
import { MetricDefinitionsApi } from '@machines/metrics/metric-definitions-api';
import { metricFormSchema, type MetricFormValue } from '@machines/metrics/metric-form/metric-form-schema';
import { MetricUnitsApi } from '@machines/metrics/metric-units-api';
import {
  appliedDraftIds,
  buildExamplePayload,
  EMPTY_METRIC_VALUE,
  firstItemId,
  hasNetChanges,
  isNewItem,
  itemsFromDefinitions,
  mapServerErrors,
  type MetricItem,
  netChanges,
  newLocalId,
  stageMetricDrafts,
  toApplyBody,
} from '@machines/metrics/metrics-page.store.utils';

const MACHINES_URL = '/machines';
const SUCCESS_SUMMARY_KEY = 'METRICS.TOASTS.SUCCESS';
const ERROR_SUMMARY_KEY = 'ERRORS.SUMMARY';

/** The open form: the item that it edits, or `null` for a new metric. */
export interface MetricEdit {
  readonly localId: string | null;
}

interface PageState {
  machineId: string | null;
  machine: Machine | null;
  newCount: number;
  formValue: MetricFormValue | null;
  isFormValid: boolean;
  isExampleOpen: boolean;
  /** Set while the page leaves after an apply or a confirmed discard: the leave guard does not ask. */
  isLeaving: boolean;
  isApplying: boolean;
  /** The attempt whose metric drafts the page stages, or `null` for a visit without drafts. */
  draftAttemptId: string | null;
}

export const MetricsPageStore = signalStore(
  withState<PageState>({
    machineId: null,
    machine: null,
    newCount: 0,
    formValue: null,
    isFormValid: false,
    isExampleOpen: false,
    isLeaving: false,
    isApplying: false,
    draftAttemptId: null,
  }),
  withProps(() => ({
    _auth: inject(AuthStore),
    _api: inject(MetricDefinitionsApi),
    _unitsApi: inject(MetricUnitsApi),
    _draftsApi: inject(DraftsApi),
    _actions: inject(ActionStore),
    _router: inject(Router),
    _machinesApi: inject(MachinesApi),
    _confirmation: inject(ConfirmationService),
    _messages: inject(MessageService),
    _translate: inject(TranslateService),
    _clock: inject(CLOCK),
    _imageLinks: inject(RichTextImageLinks),
  })),
  withProps((store) => ({
    definitions: rxResource({
      params: () => store.machineId() ?? undefined,
      stream: ({ params }) =>
        store._api.getDefinitions(params, { context: new HttpContext().set(SKIP_ERROR_TOAST, true) }),
    }),
    units: rxResource({
      params: () => store.machineId() ?? undefined,
      stream: () => store._unitsApi.getUnits(),
    }),
    drafts: rxResource({
      params: () => {
        const machineId = store.machineId();
        const attemptId = store.draftAttemptId();
        return machineId === null || attemptId === null ? undefined : attemptId;
      },
      stream: ({ params }) =>
        store._draftsApi.listForAttempt(params, { context: new HttpContext().set(SKIP_ERROR_TOAST, true) }),
    }),
  })),
  withComputed((store) => ({
    definitionsValue: computed(() => (store.definitions.hasValue() ? store.definitions.value() : undefined)),
  })),
  withLinkedState((store) => ({
    items: (): readonly MetricItem[] =>
      store.definitions.hasValue() ? itemsFromDefinitions(store.definitions.value().definitions) : [],
    selectedId: (): string | null =>
      store.definitions.hasValue() ? firstItemId(itemsFromDefinitions(store.definitions.value().definitions)) : null,
    edit: linkedSignal<unknown, MetricEdit | null>({ source: store.definitionsValue, computation: () => null }),
    itemErrors: linkedSignal<unknown, Readonly<Record<string, readonly string[]>>>({
      source: store.definitionsValue,
      computation: () => ({}),
    }),
  })),
  withComputed((store) => ({
    canRead: computed(() => store._auth.hasPermission(Permission.MetricDefinitionsRead)),
    canWrite: computed(() => store._auth.hasPermission(Permission.MetricDefinitionsWrite)),
    revision: computed(() => (store.definitions.hasValue() ? store.definitions.value().revision : 0)),
    hasChanges: computed(() => hasNetChanges(store.items())),
    examplePayload: computed((): string | null =>
      store.isExampleOpen() ? buildExamplePayload(store.items(), store._clock.now().toString()) : null,
    ),
    selected: computed(
      (): MetricItem | null => store.items().find((item) => item.localId === store.selectedId()) ?? null,
    ),
    isNotRegistered: computed(() => {
      const error = store.definitions.error();
      return error instanceof HttpErrorResponse && error.status === HttpStatusCode.NotFound;
    }),
  })),
  withProps((store) => ({
    imageLinks: rxResource({
      params: () => {
        const ids = collectFileIds(store.selected()?.value.description ?? null);
        return ids.length === 0 ? undefined : ids;
      },
      stream: ({ params }) => store._imageLinks.resolve(params),
    }),
  })),
  withComputed((store) => ({
    descriptionLinks: computed((): ReadonlyMap<string, string> =>
      store.imageLinks.hasValue() ? store.imageLinks.value() : new Map<string, string>(),
    ),
  })),
  withComputed((store) => ({
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
  })),
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
  withMethods((store) => {
    const showError = (detailKey: string): void => {
      store._messages.add({
        severity: 'error',
        summary: store._translate.instant(ERROR_SUMMARY_KEY),
        detail: store._translate.instant(detailKey),
      });
    };

    const confirmLoss = (messageKey: string, onAccept: () => void): void => {
      store._confirmation.confirm({
        header: store._translate.instant('METRICS.CONFIRM.HEADER'),
        message: store._translate.instant(messageKey),
        icon: 'pi pi-exclamation-triangle',
        acceptLabel: store._translate.instant('METRICS.CONFIRM.ACCEPT'),
        rejectLabel: store._translate.instant('ACTIONS.CANCEL'),
        acceptButtonProps: { severity: 'danger' },
        rejectButtonProps: { severity: 'secondary' },
        accept: onAccept,
      });
    };

    const guardForm = (action: () => void): void => {
      if (store.isFormChanged()) {
        confirmLoss('METRICS.CONFIRM.DRAFT_MESSAGE', action);
      } else {
        action();
      }
    };

    const closeForm = (): void => {
      patchState(store, { edit: null, formValue: null, isFormValid: false });
    };

    const replaceItem = (localId: string, change: (item: MetricItem) => MetricItem): void => {
      patchState(store, { items: store.items().map((item) => (item.localId === localId ? change(item) : item)) });
    };

    /** Marks the drafts of the applied items. A failed mark shows a toast and leaves those drafts pending. */
    const markApplied$ = (draftIds: readonly string[]): Observable<void> => {
      if (draftIds.length === 0) {
        return of(undefined);
      }
      return store._draftsApi.markApplied(draftIds, { context: new HttpContext().set(SKIP_ERROR_TOAST, true) }).pipe(
        tap(() => store._actions.complete(DraftActionType.Changed)),
        catchError(() => {
          showError('DRAFTS.METRICS.MARK_FAILED');
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

    const apply = rxMethod<void>(
      pipe(
        exhaustMap(() => {
          const changes = netChanges(store.items());
          const machineId = store.machineId() ?? '';
          patchState(store, { isApplying: true, itemErrors: {} });
          return store._api
            .apply(machineId, toApplyBody(store.revision(), changes), {
              context: new HttpContext().set(SKIP_ERROR_TOAST, true),
            })
            .pipe(
              concatMap(() => markApplied$(appliedDraftIds(changes))),
              tap(() => {
                store._messages.add({
                  severity: 'success',
                  summary: store._translate.instant(SUCCESS_SUMMARY_KEY),
                  detail: store._translate.instant('METRICS.TOASTS.APPLIED'),
                });
                patchState(store, { isApplying: false });
                store.definitions.reload();
                store.drafts.reload();
                leaveToReturnPath();
              }),
              catchError((error: unknown) => {
                const status = error instanceof HttpErrorResponse ? error.status : 0;
                patchState(store, { isApplying: false });
                if (status === HttpStatusCode.BadRequest) {
                  const body = (error as HttpErrorResponse).error as { errors?: Record<string, string[]> } | null;
                  patchState(store, { itemErrors: mapServerErrors(body?.errors ?? {}, changes) });
                  showError('METRICS.TOASTS.INVALID');
                } else {
                  showError(
                    status === HttpStatusCode.Conflict ? 'METRICS.TOASTS.CONFLICT' : httpErrorMessageKey(status),
                  );
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
        return store._machinesApi.get(machineId, { context: new HttpContext().set(SKIP_ERROR_TOAST, true) }).pipe(
          tap((machine) => {
            patchState(store, {
              machineId,
              machine,
              newCount: 0,
              formValue: null,
              isFormValid: false,
              isExampleOpen: false,
              isLeaving: false,
            });
          }),
        );
      },
      reload(): void {
        store.definitions.reload();
      },
      /** Sets the attempt whose pending metric drafts the page stages as proposed items. */
      loadDrafts(attemptId: string | null): void {
        patchState(store, { draftAttemptId: attemptId });
      },
      select(localId: string): void {
        if (localId !== store.selectedId()) {
          guardForm(() => {
            closeForm();
            patchState(store, { selectedId: localId });
          });
        }
      },
      startAdd(): void {
        guardForm(() => {
          patchState(store, { edit: { localId: null }, formValue: null, isFormValid: false, selectedId: null });
        });
      },
      startEdit(): void {
        const selectedId = store.selectedId();
        if (selectedId !== null) {
          patchState(store, { edit: { localId: selectedId }, formValue: null, isFormValid: false });
        }
      },
      cancelEdit(): void {
        closeForm();
      },
      setFormValue(formValue: MetricFormValue): void {
        patchState(store, { formValue });
      },
      setFormValidity(isFormValid: boolean): void {
        patchState(store, { isFormValid });
      },
      done(): void {
        const edit = store.edit();
        const current = store.formValue();
        if (edit === null || current === null || !store.canDone()) {
          return;
        }
        if (edit.localId === null) {
          const localId = newLocalId(store.newCount());
          const item: MetricItem = {
            localId,
            savedId: null,
            saved: null,
            value: current,
            isDeleted: false,
            draftId: null,
            sourceLocation: null,
          };
          patchState(store, { items: [...store.items(), item], newCount: store.newCount() + 1, selectedId: localId });
        } else {
          replaceItem(edit.localId, (item) => ({ ...item, value: current }));
          patchState(store, { itemErrors: { ...store.itemErrors(), [edit.localId]: [] } });
        }
        closeForm();
      },
      remove(localId: string): void {
        const item = store.items().find((candidate) => candidate.localId === localId);
        if (item === undefined) {
          return;
        }
        if (isNewItem(item)) {
          const rest = store.items().filter((candidate) => candidate.localId !== localId);
          patchState(store, {
            items: rest,
            selectedId: store.selectedId() === localId ? firstItemId(rest) : store.selectedId(),
          });
        } else {
          replaceItem(localId, (current) => ({ ...current, isDeleted: true }));
        }
      },
      restore(localId: string): void {
        replaceItem(localId, (item) => ({ ...item, isDeleted: false }));
      },
      requestDiscard(): void {
        confirmLoss('METRICS.CONFIRM.DISCARD_MESSAGE', () => {
          closeForm();
          patchState(store, { newCount: 0 });
          store.definitions.reload();
          leaveToReturnPath();
        });
      },
      /** The page **Back** button: returns to the dialog that opened the review, else to the machines. */
      back(): void {
        void store._router.navigateByUrl(store._actions.returnPath()?.url ?? MACHINES_URL);
      },
      apply(): void {
        if (store.canApply()) {
          apply();
        }
      },
      openExample(): void {
        patchState(store, { isExampleOpen: true });
      },
      closeExample(): void {
        patchState(store, { isExampleOpen: false });
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
          const staged = stageMetricDrafts(store.items(), store.newCount(), pending, store.units.value());
          patchState(store, { items: staged.items, newCount: staged.newCount });
        });
      });
    },
  }),
);
