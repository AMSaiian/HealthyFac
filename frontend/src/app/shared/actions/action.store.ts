import { inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { patchState, signalStore, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { filter } from 'rxjs';

import { ACTION_HOSTS } from './action-registry';
import type { ActionResult, AppAction } from './app-action';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

/** The dialog to open again, the page it was open on, and the pages that the review flow itself may visit. */
export interface ReturnPath {
  readonly url: string;
  readonly action: AppAction;
  readonly targetPrefix: string;
}

interface ActionState {
  active: AppAction | null;
  parent: AppAction | null;
  returnPath: ReturnPath | null;
  lastResult: ActionResult | null;
}

const initialState: ActionState = { active: null, parent: null, returnPath: null, lastResult: null };

export const ActionStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withProps(() => ({ _registrations: inject(ACTION_HOSTS), _router: inject(Router) })),
  withMethods((store) => {
    async function isValid(action: AppAction): Promise<boolean> {
      const registration = store._registrations.find((candidate) => candidate.type === action.type);
      if (registration === undefined) {
        console.error(`No action host is registered for the action type "${action.type}".`);
        return false;
      }
      const { schema } = await registration.load();
      const result = await schema['~standard'].validate(action.payload);
      if (result.issues !== undefined) {
        console.error(`The payload of the action "${action.type}" is not valid.`, result.issues);
        return false;
      }
      return true;
    }

    return {
      /**
       * Opens an action. With `returnTo`, the action that opened it comes back when this one completes or is cancelled,
       * so a drawer that starts a dialog returns with fresh data.
       */
      async dispatch(action: AppAction, returnTo: AppAction | null = null): Promise<void> {
        if (await isValid(action)) {
          patchState(store, { active: action, parent: returnTo });
        }
      },
      /** Opens an action on top of the open one. When the child ends, the open action opens again. */
      async dispatchChild(action: AppAction): Promise<void> {
        const parent = store.active();
        if (await isValid(action)) {
          patchState(store, { active: action, parent });
        }
      },
      complete(resultType: string, detail?: unknown): void {
        const sequence = (store.lastResult()?.sequence ?? 0) + 1;
        patchState(store, {
          active: store.parent(),
          parent: null,
          lastResult: { type: resultType, sequence, detail },
        });
      },
      cancel(): void {
        patchState(store, { active: store.parent(), parent: null });
      },
      /** Keeps the dialog to open again when the router reaches `url`. Navigations under `targetPrefix` keep it. */
      rememberReturn(url: string, action: AppAction, targetPrefix: string): void {
        patchState(store, { returnPath: { url, action, targetPrefix } });
      },
    };
  }),
  withHooks({
    onInit(store) {
      store._router.events
        .pipe(
          filter((event) => event instanceof NavigationEnd),
          takeUntilDestroyed(),
        )
        .subscribe((event) => {
          const returnPath = store.returnPath();
          if (returnPath === null) {
            return;
          }
          if (event.urlAfterRedirects === returnPath.url) {
            patchState(store, { returnPath: null, active: returnPath.action, parent: null });
          } else if (!event.urlAfterRedirects.startsWith(returnPath.targetPrefix)) {
            patchState(store, { returnPath: null });
          }
        });
    },
  }),
  withStoreDevtools('action'),
);
