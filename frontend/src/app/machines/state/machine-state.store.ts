import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, finalize, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';
import { withStoreDevtools } from '@shared/devtools/with-store-devtools';
import { httpErrorKey, skipErrorToast } from '@shared/http/http-error-key';
import { withToasts } from '@shared/toasts/with-toasts';
import type { MachineState } from '@machines/machine';
import { MachineActionType } from '@machines/machine-actions';
import { MachinesApi } from '@machines/machines-api';

import { machineIdOf, saveDisabledReason } from './machine-state.store.utils';

interface MachineStateStoreState {
  action: AppAction | null;
  selected: MachineState | null;
  isSaving: boolean;
}

export const MachineStateStore = signalStore(
  withState<MachineStateStoreState>({ action: null, selected: null, isSaving: false }),
  withProps(() => ({
    _api: inject(MachinesApi),
    _actions: inject(ActionStore),
  })),
  withToasts(),
  withComputed((store) => ({
    machineId: computed(() => machineIdOf(store.action())),
  })),
  withProps((store) => ({
    machine: rxResource({
      params: () => store.machineId(),
      stream: ({ params }) => store._api.get(params),
    }),
  })),
  withComputed((store) => ({
    currentState: computed((): MachineState | null => (store.machine.hasValue() ? store.machine.value().state : null)),
  })),
  withComputed((store) => ({
    selectedState: computed((): MachineState | null => store.selected() ?? store.currentState()),
  })),
  withComputed((store) => ({
    saveDisabledReason: computed(() => saveDisabledReason(store.currentState(), store.selectedState())),
  })),
  withMethods((store) => ({
    start(action: AppAction): void {
      patchState(store, { action, selected: null });
    },
    select(state: MachineState): void {
      patchState(store, { selected: state });
    },
    save: rxMethod<void>(
      pipe(
        exhaustMap(() => {
          const machineId = store.machineId();
          const state = store.selectedState();
          if (machineId === undefined || state === null || store.saveDisabledReason() !== null) {
            return EMPTY;
          }
          patchState(store, { isSaving: true });
          return store._api.changeState(machineId, { state }, skipErrorToast()).pipe(
            tap(() => {
              store._toastSuccess('MACHINES.TOASTS.STATE_CHANGED');
              store._actions.complete(MachineActionType.Saved);
            }),
            catchError((error: unknown) => {
              store._toastError(httpErrorKey(error));
              return EMPTY;
            }),
            finalize(() => patchState(store, { isSaving: false })),
          );
        }),
      ),
    ),
    cancel(): void {
      store._actions.cancel();
    },
  })),
  withStoreDevtools('machine-state'),
);
