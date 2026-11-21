import { computed, DestroyRef, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, type Observable, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { withConfirm } from '@shared/confirm/with-confirm';
import { withGridState } from '@shared/grid/with-grid-state';
import { HistoryStore } from '@shared/history/history.store';
import { formatCompose } from '@shared/history/history.store.utils';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';
import { skipErrorToast } from '@shared/http/http-error-key';
import { CLOCK } from '@shared/time/clock';
import { withToasts } from '@shared/toasts/with-toasts';

import type { Machine } from '@machines/machine';
import type {
  StatusDefinition,
  StatusDefinitionColumn,
  StatusDefinitionFilter,
  StatusDefinitionSortColumn,
} from '@machines/statuses/status-definition';
import { toSearchBody } from '@machines/statuses/list/status-definition-query';
import {
  INITIAL_GRID,
  STATUS_COMPOSE_NAME,
  toStatusEditor,
} from '@machines/statuses/list/status-definitions-grid.store.utils';
import { StatusDefinitionsApi } from '@machines/statuses/status-definitions-api';
import { MachinesApi } from '@machines/machines-api';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

interface ScopeState {
  machineId: string | null;
  machine: Machine | null;
}

export const StatusDefinitionsGridStore = signalStore(
  withGridState<StatusDefinitionFilter, StatusDefinitionSortColumn, StatusDefinitionColumn>(INITIAL_GRID),
  withState<ScopeState>({ machineId: null, machine: null }),
  withProps(() => ({
    _auth: inject(AuthStore),
    _clock: inject(CLOCK),
    _api: inject(StatusDefinitionsApi),
    _machinesApi: inject(MachinesApi),
    _history: inject(HistoryStore),
  })),
  withToasts(),
  withConfirm(),
  withComputed((store) => ({
    canWrite: computed(() => store._auth.hasPermission(Permission.StatusDefinitionsWrite)),
    canDelete: computed(() => store._auth.hasPermission(Permission.StatusDefinitionsDelete)),
    editor: computed(() => toStatusEditor(store._history.compose())),
  })),
  withProps((store) => ({
    statuses: rxResource({
      params: () => {
        const machineId = store.machineId();
        return machineId === null
          ? undefined
          : {
              machineId,
              body: toSearchBody(
                { offset: store.offset(), limit: store.limit(), sort: store.sort(), filter: store.filter() },
                store._clock.timeZoneId(),
              ),
            };
      },
      stream: ({ params }) => store._api.search(params.machineId, params.body),
    }),
  })),
  withMethods((store) => {
    const deleteErrorCode = rxMethod<StatusDefinition>(
      pipe(
        exhaustMap((statusDefinition) =>
          store._api.delete(store.machineId() ?? '', statusDefinition.id).pipe(
            tap(() => {
              store._toastSuccess('STATUS_DEFINITIONS.TOASTS.DELETED');
              store.statuses.reload();
            }),
            catchError(() => EMPTY),
          ),
        ),
      ),
    );

    return {
      /**
       * Starts a visit to the error codes of a machine: loads the machine, then resets the grid and closes the dialog.
       * The state changes only after a successful load, so a pending or failed load sends no search and keeps the
       * current machine. The load skips the error toast, so the caller decides what a failure means.
       */
      openMachine(machineId: string): Observable<Machine> {
        return store._machinesApi.get(machineId, skipErrorToast()).pipe(
          tap((machine) => {
            patchState(store, { machineId, machine });
            store.resetGrid();
          }),
        );
      },
      requestCreate(): void {
        void store._history.openCompose(formatCompose(STATUS_COMPOSE_NAME));
      },
      requestUpdate(id: string): void {
        void store._history.openCompose(formatCompose(STATUS_COMPOSE_NAME, id));
      },
      closeEditor(): void {
        void store._history.closeCompose();
      },
      onSaved(): void {
        void store._history.closeCompose();
        store.statuses.reload();
      },
      /** Loads the grid again and keeps the editor open: an update save leaves the dialog in view mode. */
      reloadStatuses(): void {
        store.statuses.reload();
      },
      requestDelete(statusDefinition: StatusDefinition): void {
        store._confirmDestructive({
          headerKey: 'STATUS_DEFINITIONS.DELETE_HEADER',
          messageKey: 'STATUS_DEFINITIONS.DELETE_MESSAGE',
          params: { code: statusDefinition.code },
          acceptKey: 'ACTIONS.DELETE',
          onAccept: () => {
            deleteErrorCode(statusDefinition);
          },
        });
      },
    };
  }),
  withHooks({
    onInit(store) {
      inject(DestroyRef).onDestroy(store._history.registerComposeNames([STATUS_COMPOSE_NAME]));
    },
  }),
  withStoreDevtools('status-definitions-grid'),
);
