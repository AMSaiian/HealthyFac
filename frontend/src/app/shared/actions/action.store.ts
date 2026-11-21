import { DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type ActivatedRouteSnapshot, NavigationEnd, NavigationStart, Router } from '@angular/router';
import { patchState, signalStore, withHooks, withMethods, withProps, withState } from '@ngrx/signals';

import { ACTION_HOSTS } from './action-registry';
import type { ActionResult, AppAction } from './app-action';
import { isEntryState, toAction } from './action.store.utils';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';
import { HistoryStore } from '@shared/history/history.store';
import { parseCompose } from '@shared/history/history.store.utils';

interface ActionState {
  active: AppAction | null;
  parent: AppAction | null;
  /** The open drawer came from the `compose` query param, so closing it also closes the history entry. */
  isComposed: boolean;
  lastResult: ActionResult | null;
}

const initialState: ActionState = { active: null, parent: null, isComposed: false, lastResult: null };

export const ActionStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withProps(() => ({
    _registrations: inject(ACTION_HOSTS),
    _router: inject(Router),
    _history: inject(HistoryStore),
    _flow: { isSkipped: false, endCount: 0, lastUrl: null as string | null, skippingId: null as number | null },
  })),
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

    function nextResult(type: string, detail: unknown): ActionResult {
      return { type, sequence: (store.lastResult()?.sequence ?? 0) + 1, detail };
    }

    /**
     * Keeps the open action in the entry state, so back, forward, and a refresh open it again. A drawer has its own
     * entry through `compose` and keeps no state. After `skipLocationChange`, the entry belongs to another page.
     */
    function writeEntryState(): void {
      if (store._flow.isSkipped) {
        return;
      }
      store._history.writeEntryState(
        store._history.compose() === null ? { active: store.active(), parent: store.parent() } : null,
      );
    }

    function setActions(active: AppAction | null, parent: AppAction | null, result?: ActionResult): void {
      patchState(store, { active, parent, ...(result === undefined ? {} : { lastResult: result }) });
      writeEntryState();
    }

    async function restoreAction(value: unknown): Promise<AppAction | null> {
      const action = toAction(value);
      return action !== null && (await isValid(action)) ? action : null;
    }

    function routeParams(): Record<string, string> {
      const params: Record<string, string> = {};
      for (let route: ActivatedRouteSnapshot | null = store._router.routerState.snapshot.root; route !== null;) {
        Object.assign(params, route.params);
        route = route.firstChild;
      }
      return params;
    }

    function encodeCompose(action: AppAction): string | null {
      return store._registrations.find((candidate) => candidate.type === action.type)?.compose?.encode(action) ?? null;
    }

    /** Opens the drawer that the `compose` value names. A value that makes no valid action leaves the URL. */
    async function restoreFromCompose(compose: string, endCount: number): Promise<void> {
      const { name, id } = parseCompose(compose);
      const codecs = store._registrations.flatMap((candidate) =>
        candidate.compose?.name === name ? [candidate.compose] : [],
      );
      if (codecs.length === 0) {
        return;
      }
      const params = routeParams();
      const decoded = [...new Set(codecs)].map((codec) => codec.decode(id, params)).find((action) => action !== null);
      const isOpened = decoded !== undefined && (await isValid(decoded));
      if (endCount !== store._flow.endCount) {
        return;
      }
      if (!isOpened) {
        void store._history.removeCompose();
        return;
      }
      patchState(store, { active: decoded, parent: null, isComposed: true });
      writeEntryState();
    }

    function closeComposedDrawer(): void {
      if (store.active() === null && store.isComposed()) {
        patchState(store, { isComposed: false });
        void store._history.closeCompose();
      }
    }

    async function restoreFromState(entryState: unknown, endCount: number): Promise<void> {
      patchState(store, { active: null, parent: null, isComposed: false });
      if (!isEntryState(entryState)) {
        writeEntryState();
        return;
      }
      const active = await restoreAction(entryState.active);
      const parent = active === null ? null : await restoreAction(entryState.parent);
      if (endCount !== store._flow.endCount) {
        return;
      }
      patchState(store, { active, parent });
      writeEntryState();
    }

    return {
      /**
       * Opens an action. With `returnTo`, the action that opened it comes back when this one completes or is cancelled,
       * so a drawer that starts a dialog returns with fresh data.
       */
      async dispatch(action: AppAction, returnTo: AppAction | null = null): Promise<void> {
        if (!(await isValid(action))) {
          return;
        }
        const compose = store._history.compose() === null && returnTo === null ? encodeCompose(action) : null;
        if (compose === null) {
          setActions(action, returnTo);
          return;
        }
        await store._history.openCompose(compose);
      },
      /** Opens an action on top of the open one. When the child ends, the open action opens again. */
      async dispatchChild(action: AppAction): Promise<void> {
        const parent = store.active();
        if (await isValid(action)) {
          setActions(action, parent);
        }
      },
      /** Tells the readers of the result that the action stored a change, and keeps the dialog open. */
      notify(resultType: string, detail?: unknown): void {
        patchState(store, { lastResult: nextResult(resultType, detail) });
      },
      complete(resultType: string, detail?: unknown): void {
        setActions(store.parent(), null, nextResult(resultType, detail));
        closeComposedDrawer();
      },
      cancel(): void {
        setActions(store.parent(), null);
        closeComposedDrawer();
      },
      _onNavigationStart(event: NavigationStart): void {
        store._flow.skippingId =
          store._router.currentNavigation()?.extras.skipLocationChange === true ? event.id : null;
      },
      _onNavigationEnd(event: NavigationEnd): void {
        store._flow.endCount += 1;
        const previousUrl = store._flow.lastUrl;
        store._flow.lastUrl = event.urlAfterRedirects;
        if (store._flow.skippingId === event.id) {
          store._flow.isSkipped = true;
          patchState(store, { active: null, parent: null, isComposed: false });
          return;
        }
        store._flow.isSkipped = false;
        if (event.urlAfterRedirects === previousUrl) {
          writeEntryState();
          return;
        }
        const compose = store._history.compose();
        if (compose !== null) {
          patchState(store, { active: null, parent: null, isComposed: false });
          void restoreFromCompose(compose, store._flow.endCount);
          return;
        }
        void restoreFromState(store._history.startStateOf(event.id), store._flow.endCount);
      },
    };
  }),
  withHooks({
    onInit(store) {
      const names = store._registrations.flatMap((candidate) =>
        candidate.compose === undefined ? [] : [candidate.compose.name],
      );
      const unregister = store._history.registerComposeNames([...new Set(names)]);
      inject(DestroyRef).onDestroy(unregister);
      store._router.events.pipe(takeUntilDestroyed()).subscribe((event) => {
        if (event instanceof NavigationStart) {
          store._onNavigationStart(event);
        } else if (event instanceof NavigationEnd) {
          store._onNavigationEnd(event);
        }
      });
    },
  }),
  withStoreDevtools('action'),
);
