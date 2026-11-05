import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withProps, withState } from '@ngrx/signals';

import { ACTION_HOSTS } from './action-registry';
import type { ActionResult, AppAction } from './app-action';

interface ActionState {
  active: AppAction | null;
  lastResult: ActionResult | null;
}

const initialState: ActionState = { active: null, lastResult: null };

export const ActionStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withProps(() => ({ _registrations: inject(ACTION_HOSTS) })),
  withMethods((store) => ({
    async dispatch(action: AppAction): Promise<void> {
      const registration = store._registrations.find((candidate) => candidate.type === action.type);
      if (registration === undefined) {
        console.error(`No action host is registered for the action type "${action.type}".`);
        return;
      }
      const { schema } = await registration.load();
      const result = await schema['~standard'].validate(action.payload);
      if (result.issues !== undefined) {
        console.error(`The payload of the action "${action.type}" is not valid.`, result.issues);
        return;
      }
      patchState(store, { active: action });
    },
    complete(resultType: string): void {
      const sequence = (store.lastResult()?.sequence ?? 0) + 1;
      patchState(store, { active: null, lastResult: { type: resultType, sequence } });
    },
    cancel(): void {
      patchState(store, { active: null });
    },
  })),
);
