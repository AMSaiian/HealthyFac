import { computed, effect, inject, untracked } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { signalStore, withComputed, withHooks, withMethods, withProps } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { EMPTY, exhaustMap, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import { withGridState } from '@shared/grid/with-grid-state';
import { AuthStore } from '@shared/security/auth.store';
import { Permission } from '@shared/security/permissions';
import { CLOCK } from '@shared/time/clock';

import type { MachineColumn, MachineSortColumn } from '@machines/machine';
import { type Machine, type MachineFilter } from '@machines/machine';
import { MachineActionType } from '@machines/machine-actions';
import { toSearchBody } from '@machines/list/machine-query';
import { INITIAL_GRID } from '@machines/list/machines-grid.store.utils';
import { MachinesApi } from '@machines/machines-api';

const MACHINES_PATH = '/machines';
const STATUSES_PATH = 'statuses';
const METRICS_PATH = 'metrics';

export const MachinesGridStore = signalStore(
  withGridState<MachineFilter, MachineSortColumn, MachineColumn>(INITIAL_GRID),
  withProps(() => ({
    _auth: inject(AuthStore),
    _clock: inject(CLOCK),
    _actions: inject(ActionStore),
    _api: inject(MachinesApi),
    _router: inject(Router),
    _confirmation: inject(ConfirmationService),
    _messages: inject(MessageService),
    _translate: inject(TranslateService),
  })),
  withComputed((store) => ({
    canWrite: computed(() => store._auth.hasPermission(Permission.MachinesWrite)),
    canDelete: computed(() => store._auth.hasPermission(Permission.MachinesDelete)),
    canReadStatuses: computed(() => store._auth.hasPermission(Permission.StatusDefinitionsRead)),
    canReadMetrics: computed(() => store._auth.hasPermission(Permission.MetricDefinitionsRead)),
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
              store._messages.add({
                severity: 'success',
                summary: store._translate.instant('MACHINES.TOASTS.SUCCESS'),
                detail: store._translate.instant('MACHINES.DELETED'),
              });
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
      openMetrics(id: string): void {
        void store._router.navigate([MACHINES_PATH, id, METRICS_PATH]);
      },
      openStatuses(id: string): void {
        void store._router.navigate([MACHINES_PATH, id, STATUSES_PATH]);
      },
      requestDelete(machine: Machine): void {
        store._confirmation.confirm({
          header: store._translate.instant('MACHINES.DELETE_HEADER'),
          message: store._translate.instant('MACHINES.DELETE_MESSAGE', { name: machine.name }),
          icon: 'pi pi-exclamation-triangle',
          acceptLabel: store._translate.instant('MACHINES.DELETE'),
          rejectLabel: store._translate.instant('ACTIONS.CANCEL'),
          acceptButtonProps: { severity: 'danger' },
          rejectButtonProps: { severity: 'secondary' },
          accept: () => {
            deleteMachine(machine);
          },
        });
      },
    };
  }),
  withHooks({
    onInit(store) {
      let seen = store._actions.lastResult()?.sequence ?? 0;
      effect(() => {
        const result = store._actions.lastResult();
        if (result !== null && result.type === MachineActionType.Saved && result.sequence > seen) {
          seen = result.sequence;
          untracked(() => store.machines.reload());
        }
      });
    },
  }),
);
