import { HttpContext, HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, inject, linkedSignal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import {
  patchState,
  signalStore,
  withComputed,
  withLinkedState,
  withMethods,
  withProps,
  withState,
} from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { EMPTY, exhaustMap, type Observable, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { hasFormChanges } from '@shared/forms/form-changes';
import { httpErrorMessageKey } from '@shared/http/http-error-message';
import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';
import { AuthStore } from '@shared/security/auth.store';
import { Permission } from '@shared/security/permissions';
import { CLOCK } from '@shared/time/clock';

import type { Machine } from '@machines/machine';
import { MachinesApi } from '@machines/machines-api';
import { MetricDefinitionsApi } from '@machines/metrics/metric-definitions-api';
import { metricFormSchema, type MetricFormValue } from '@machines/metrics/metric-form/metric-form-schema';
import { MetricUnitsApi } from '@machines/metrics/metric-units-api';
import {
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
  toApplyBody,
} from '@machines/metrics/metrics-page.store.utils';

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
  draft: MetricFormValue | null;
  isDraftValid: boolean;
  isExampleOpen: boolean;
  isApplying: boolean;
}

export const MetricsPageStore = signalStore(
  withState<PageState>({
    machineId: null,
    machine: null,
    newCount: 0,
    draft: null,
    isDraftValid: false,
    isExampleOpen: false,
    isApplying: false,
  }),
  withProps(() => ({
    _auth: inject(AuthStore),
    _api: inject(MetricDefinitionsApi),
    _unitsApi: inject(MetricUnitsApi),
    _machinesApi: inject(MachinesApi),
    _confirmation: inject(ConfirmationService),
    _messages: inject(MessageService),
    _translate: inject(TranslateService),
    _clock: inject(CLOCK),
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
      store.isExampleOpen() && store.definitions.hasValue()
        ? buildExamplePayload(store.definitions.value().definitions, store._clock.now().toString())
        : null,
    ),
    selected: computed(
      (): MetricItem | null => store.items().find((item) => item.localId === store.selectedId()) ?? null,
    ),
    isNotRegistered: computed(() => {
      const error = store.definitions.error();
      return error instanceof HttpErrorResponse && error.status === HttpStatusCode.NotFound;
    }),
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
    isDraftChanged: computed(() => {
      const initial = store.editInitial();
      const draft = store.draft();
      return initial !== null && draft !== null && hasFormChanges(metricFormSchema, initial, draft);
    }),
  })),
  withComputed((store) => ({
    canDone: computed(() => store.isDraftValid() && store.isDraftChanged()),
    canApply: computed(() => store.hasChanges() && store.edit() === null && !store.isApplying()),
    hasPendingWork: computed(() => store.hasChanges() || store.isDraftChanged()),
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

    const guardDraft = (action: () => void): void => {
      if (store.isDraftChanged()) {
        confirmLoss('METRICS.CONFIRM.DRAFT_MESSAGE', action);
      } else {
        action();
      }
    };

    const closeForm = (): void => {
      patchState(store, { edit: null, draft: null, isDraftValid: false });
    };

    const replaceItem = (localId: string, change: (item: MetricItem) => MetricItem): void => {
      patchState(store, { items: store.items().map((item) => (item.localId === localId ? change(item) : item)) });
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
              tap(() => {
                store._messages.add({
                  severity: 'success',
                  summary: store._translate.instant(SUCCESS_SUMMARY_KEY),
                  detail: store._translate.instant('METRICS.TOASTS.APPLIED'),
                });
                patchState(store, { isApplying: false });
                store.definitions.reload();
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
              draft: null,
              isDraftValid: false,
              isExampleOpen: false,
            });
          }),
        );
      },
      reload(): void {
        store.definitions.reload();
      },
      select(localId: string): void {
        if (localId !== store.selectedId()) {
          guardDraft(() => {
            closeForm();
            patchState(store, { selectedId: localId });
          });
        }
      },
      startAdd(): void {
        guardDraft(() => {
          patchState(store, { edit: { localId: null }, draft: null, isDraftValid: false, selectedId: null });
        });
      },
      startEdit(): void {
        const selectedId = store.selectedId();
        if (selectedId !== null) {
          patchState(store, { edit: { localId: selectedId }, draft: null, isDraftValid: false });
        }
      },
      cancelEdit(): void {
        closeForm();
      },
      setDraft(draft: MetricFormValue): void {
        patchState(store, { draft });
      },
      setDraftValidity(isDraftValid: boolean): void {
        patchState(store, { isDraftValid });
      },
      done(): void {
        const edit = store.edit();
        const draft = store.draft();
        if (edit === null || draft === null || !store.canDone()) {
          return;
        }
        if (edit.localId === null) {
          const localId = newLocalId(store.newCount());
          const item: MetricItem = { localId, savedId: null, saved: null, value: draft, isDeleted: false };
          patchState(store, { items: [...store.items(), item], newCount: store.newCount() + 1, selectedId: localId });
        } else {
          replaceItem(edit.localId, (item) => ({ ...item, value: draft }));
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
        });
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
);
