import { HttpStatusCode } from '@angular/common/http';
import { computed, DestroyRef, effect, inject, untracked } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, finalize, type Observable, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { withConfirm } from '@shared/confirm/with-confirm';
import { withStoreDevtools } from '@shared/devtools/with-store-devtools';
import { withDetailsMode } from '@shared/forms/with-details-mode';
import { HistoryStore } from '@shared/history/history.store';
import { formatCompose } from '@shared/history/history.store.utils';
import { httpErrorKey, skipErrorToast } from '@shared/http/http-error-key';
import { descriptionLinks } from '@shared/rich-text/file-image/description-links';
import { RichTextImageLinks } from '@shared/rich-text/file-image/rich-text-image-links';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';
import { withToasts } from '@shared/toasts/with-toasts';

import type { Machine } from '@machines/machine';
import { MachinesApi } from '@machines/machines-api';
import { MetricDefinitionsApi } from '@machines/metrics/metric-definitions-api';
import type { OperatingPhase } from '@machines/phases/operating-phase';
import { OperatingPhasesApi } from '@machines/phases/operating-phases-api';
import { nextPriority, toBody, toSourceOptions } from '@machines/phases/phase-form/phase-form.utils';
import type { PhaseFormValue } from '@machines/phases/phase-form/phase-form-schema';
import {
  movedOrder,
  PHASE_COMPOSE_NAME,
  sortByPriority,
  toPhaseEditor,
} from '@machines/phases/operating-phases-page-store/operating-phases-page.store.utils';
import { StatusDefinitionsApi } from '@machines/statuses/status-definitions-api';
import { INITIAL_GRID } from '@machines/statuses/list/status-definitions-grid.store.utils';
import { toSearchBody } from '@machines/statuses/list/status-definition-query';
import { CLOCK } from '@shared/time/clock';

/** One save of the dialog. `rxMethod` returns no result, so the caller passes `onSaved`. */
interface SaveRequest {
  readonly value: PhaseFormValue;
  readonly onSaved: () => void;
}

interface ScopeState {
  machineId: string | null;
  machine: Machine | null;
  isSaving: boolean;
}

/** The most statuses that the source select of a condition offers: the largest page that the API accepts. */
const STATUS_OPTION_LIMIT = 100;

export const OperatingPhasesPageStore = signalStore(
  withState<ScopeState>({ machineId: null, machine: null, isSaving: false }),
  withDetailsMode(),
  withProps(() => ({
    _auth: inject(AuthStore),
    _clock: inject(CLOCK),
    _api: inject(OperatingPhasesApi),
    _machinesApi: inject(MachinesApi),
    _metricsApi: inject(MetricDefinitionsApi),
    _statusesApi: inject(StatusDefinitionsApi),
    _history: inject(HistoryStore),
  })),
  withToasts(),
  withConfirm(),
  withComputed((store) => ({
    canWrite: computed(() => store._auth.hasPermission(Permission.OperatingPhasesWrite)),
    editor: computed(() => toPhaseEditor(store._history.compose())),
  })),
  withProps((store) => ({
    phases: rxResource({
      params: () => store.machineId() ?? undefined,
      stream: ({ params }) => store._api.list(params),
    }),
    metrics: rxResource({
      params: () => store.machineId() ?? undefined,
      stream: ({ params }) => store._metricsApi.getDefinitions(params, skipErrorToast()),
    }),
    statuses: rxResource({
      params: () => store.machineId() ?? undefined,
      stream: ({ params }) =>
        store._statusesApi.search(
          params,
          toSearchBody({ ...INITIAL_GRID, offset: 0, limit: STATUS_OPTION_LIMIT }, store._clock.timeZoneId()),
          skipErrorToast(),
        ),
    }),
  })),
  withComputed((store) => ({
    items: computed((): readonly OperatingPhase[] =>
      store.phases.hasValue() ? sortByPriority(store.phases.value().items) : [],
    ),
    sourceOptions: computed(() =>
      toSourceOptions(
        store.statuses.hasValue() ? store.statuses.value().items : [],
        store.metrics.hasValue() ? store.metrics.value().definitions : [],
      ),
    ),
    editedPhase: computed((): OperatingPhase | null => {
      const editor = store.editor();
      if (editor?.phaseId === null || editor === null || !store.phases.hasValue()) {
        return null;
      }
      return store.phases.value().items.find((phase) => phase.id === editor.phaseId) ?? null;
    }),
    isUpdate: computed(() => {
      const editor = store.editor();
      return editor !== null && editor.phaseId !== null;
    }),
  })),
  withProps((store) => ({
    descriptionLinks: descriptionLinks(() => store.editedPhase()?.description ?? null, inject(RichTextImageLinks))
      .links,
  })),
  withComputed((store) => ({
    statusIds: computed(
      (): ReadonlySet<string> =>
        new Set(store.statuses.hasValue() ? store.statuses.value().items.map((status) => status.id) : []),
    ),
    isFormDataLoading: computed(
      () => store.metrics.isLoading() || store.statuses.isLoading() || store.phases.isLoading(),
    ),
    /** Tells whether the pencil shows: the loaded phase in view mode, for a user who may change it. */
    isEditShown: computed(
      () => store.isViewMode() && store.isUpdate() && store.editedPhase() !== null && store.canWrite(),
    ),
  })),
  withMethods((store) => {
    const savePhase = rxMethod<SaveRequest>(
      pipe(
        tap(() => patchState(store, { isSaving: true })),
        exhaustMap(({ value, onSaved }) => {
          const machineId = store.machineId() ?? '';
          const phaseId = store.editor()?.phaseId ?? null;
          const edited = store.editedPhase();
          const options = skipErrorToast();
          const request$: Observable<unknown> =
            phaseId === null
              ? store._api.create(machineId, toBody(value, nextPriority(store.items()), store.statusIds()), options)
              : store._api.update(machineId, phaseId, toBody(value, edited?.priority ?? 0, store.statusIds()), options);
          return request$.pipe(
            tap(() => {
              store._toastSuccess(phaseId === null ? 'PHASES.TOASTS.CREATED' : 'PHASES.TOASTS.UPDATED');
              onSaved();
            }),
            catchError((error: unknown) => {
              store._toastError(httpErrorKey(error, { [HttpStatusCode.Conflict]: 'PHASES.TOASTS.CODE_TAKEN' }));
              return EMPTY;
            }),
            finalize(() => patchState(store, { isSaving: false })),
          );
        }),
      ),
    );
    const deletePhase = rxMethod<OperatingPhase>(
      pipe(
        exhaustMap((phase) =>
          store._api.delete(store.machineId() ?? '', phase.id).pipe(
            tap(() => {
              store._toastSuccess('PHASES.TOASTS.DELETED');
              store.phases.reload();
            }),
            catchError(() => EMPTY),
          ),
        ),
      ),
    );
    const sendOrder = rxMethod<readonly string[]>(
      pipe(
        exhaustMap((phaseIds) =>
          store._api.reorder(store.machineId() ?? '', { phaseIds }, skipErrorToast()).pipe(
            tap(() => store._toastSuccess('PHASES.TOASTS.REORDERED')),
            catchError((error: unknown) => {
              store._toastError(httpErrorKey(error));
              return EMPTY;
            }),
            finalize(() => store.phases.reload()),
          ),
        ),
      ),
    );

    return {
      /**
       * Starts a visit to the phases of a machine: loads the machine. The state changes only
       * after a successful load, so a failed load keeps the current machine. The load skips the error toast, so the
       * caller decides what a failure means.
       */
      openMachine(machineId: string): Observable<Machine> {
        return store._machinesApi
          .get(machineId, skipErrorToast())
          .pipe(tap((machine) => patchState(store, { machineId, machine })));
      },
      requestCreate(): void {
        void store._history.openCompose(formatCompose(PHASE_COMPOSE_NAME));
      },
      requestUpdate(phaseId: string): void {
        void store._history.openCompose(formatCompose(PHASE_COMPOSE_NAME, phaseId));
      },
      cancelEdit(): void {
        store.view();
      },
      closeEditor(): void {
        void store._history.closeCompose();
      },
      save: savePhase,
      onSaved(): void {
        if (store.editor()?.phaseId === null) {
          void store._history.closeCompose();
          store.phases.reload();
        } else {
          store.phases.reload();
          store.view();
        }
      },
      requestDelete(phase: OperatingPhase): void {
        store._confirmDestructive({
          headerKey: 'PHASES.DELETE_HEADER',
          messageKey: 'PHASES.DELETE_MESSAGE',
          params: { name: phase.name },
          acceptKey: 'ACTIONS.DELETE',
          onAccept: () => {
            deletePhase(phase);
          },
        });
      },
      /** Sends the new order after a row moved from `from` to `to`; the table reloads from the server after. */
      reorder(from: number, to: number): void {
        sendOrder(movedOrder(store.items(), from, to));
      },
    };
  }),
  withHooks({
    onInit(store) {
      inject(DestroyRef).onDestroy(store._history.registerComposeNames([PHASE_COMPOSE_NAME]));
      effect(() => {
        const editor = store.editor();
        if (editor !== null) {
          untracked(() => store._startMode(editor.phaseId !== null));
        }
      });
      effect(() => {
        const phaseId = store.editor()?.phaseId ?? null;
        if (
          phaseId !== null &&
          store.phases.hasValue() &&
          !store.phases.value().items.some((phase) => phase.id === phaseId)
        ) {
          void store._history.removeCompose();
        }
      });
    },
  }),
  withStoreDevtools('operating-phases-page'),
);
