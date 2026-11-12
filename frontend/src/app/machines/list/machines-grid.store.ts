import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { signalStore, withComputed, withMethods, withProps } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import { type Reloadable, withActionResultReload } from '@shared/actions/with-action-result-reload';
import { withConfirm } from '@shared/confirm/with-confirm';
import { withGridState } from '@shared/grid/with-grid-state';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';
import { CLOCK } from '@shared/time/clock';
import { withToasts } from '@shared/toasts/with-toasts';

import type { MachineColumn, MachineSortColumn } from '@machines/machine';
import { type Machine, type MachineFilter } from '@machines/machine';
import { MachineActionType } from '@machines/machine-actions';
import { DraftActionType } from '@machines/drafts/draft-actions';
import { toSearchBody } from '@machines/list/machine-query';
import { INITIAL_GRID } from '@machines/list/machines-grid.store.utils';
import { MachinesApi } from '@machines/machines-api';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

const MACHINES_PATH = '/machines';
const STATUSES_PATH = 'statuses';
const METRICS_PATH = 'metrics';
const FILES_PATH = 'files';

export const MachinesGridStore = signalStore(
  withGridState<MachineFilter, MachineSortColumn, MachineColumn>(INITIAL_GRID),
  withProps(() => ({
    _auth: inject(AuthStore),
    _clock: inject(CLOCK),
    _actions: inject(ActionStore),
    _api: inject(MachinesApi),
    _router: inject(Router),
  })),
  withToasts(),
  withConfirm(),
  withComputed((store) => ({
    canWrite: computed(() => store._auth.hasPermission(Permission.MachinesWrite)),
    canDelete: computed(() => store._auth.hasPermission(Permission.MachinesDelete)),
    canReadStatuses: computed(() => store._auth.hasPermission(Permission.StatusDefinitionsRead)),
    canReadFiles: computed(() => store._auth.hasPermission(Permission.FilesRead)),
    canReadMetrics: computed(() => store._auth.hasPermission(Permission.MetricDefinitionsRead)),
    canReadDrafts: computed(() => store._auth.hasPermission(Permission.DraftsRead)),
  })),
  withProps((store) => ({
    machines: rxResource({
      params: () =>
        toSearchBody(
          { offset: store.offset(), limit: store.limit(), sort: store.sort(), filter: store.filter() },
          store._clock.timeZoneId(),
        ),
      stream: ({ params }) => store._api.search(params),
    }),
  })),
  withMethods((store) => {
    const deleteMachine = rxMethod<Machine>(
      pipe(
        exhaustMap((machine) =>
          store._api.delete(machine.id).pipe(
            tap(() => {
              store._toastSuccess('MACHINES.DELETED');
              store.machines.reload();
            }),
            catchError(() => EMPTY),
          ),
        ),
      ),
    );

    return {
      requestCreate(): void {
        void store._actions.dispatch({ type: MachineActionType.Create, payload: {} });
      },
      requestUpdate(id: string): void {
        void store._actions.dispatch({ type: MachineActionType.Update, payload: { machineId: id } });
      },
      requestDrafts(id: string): void {
        void store._actions.dispatch({ type: MachineActionType.Drafts, payload: { machineId: id } });
      },
      openMetrics(id: string): void {
        void store._router.navigate([MACHINES_PATH, id, METRICS_PATH]);
      },
      openFiles(id: string): void {
        void store._router.navigate([MACHINES_PATH, id, FILES_PATH]);
      },
      openStatuses(id: string): void {
        void store._router.navigate([MACHINES_PATH, id, STATUSES_PATH]);
      },
      requestDelete(machine: Machine): void {
        store._confirmDestructive({
          headerKey: 'MACHINES.DELETE_HEADER',
          messageKey: 'MACHINES.DELETE_MESSAGE',
          params: { name: machine.name },
          acceptKey: 'ACTIONS.DELETE',
          onAccept: () => {
            deleteMachine(machine);
          },
        });
      },
    };
  }),
  withActionResultReload<{ machines: Reloadable }>([MachineActionType.Saved, DraftActionType.Changed], (store) => {
    store.machines.reload();
  }),
  withStoreDevtools('machines-grid'),
);
