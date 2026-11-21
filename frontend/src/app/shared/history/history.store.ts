import { Location, PlatformNavigation } from '@angular/common';
import { inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  NavigationCancel,
  NavigationEnd,
  NavigationError,
  NavigationSkipped,
  NavigationStart,
  Router,
} from '@angular/router';
import { patchState, signalStore, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { filter, firstValueFrom } from 'rxjs';

import { COMPOSE_PARAM, parseCompose, withCompose, withoutCompose } from './history.store.utils';
import { HOME_RETURN_TARGET, type ReturnTarget } from './return-target';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';
import { AuthStore } from '@shared/security/auth-store/auth.store';

interface HistoryState {
  canGoBack: boolean;
  compose: string | null;
}

const initialState: HistoryState = { canGoBack: false, compose: null };

function isNavigationEnding(
  event: unknown,
): event is NavigationEnd | NavigationCancel | NavigationError | NavigationSkipped {
  return (
    event instanceof NavigationEnd ||
    event instanceof NavigationCancel ||
    event instanceof NavigationError ||
    event instanceof NavigationSkipped
  );
}

/**
 * Mirrors the browser history: whether the app has a previous entry, and the state that the Navigation API keeps on
 * the current entry. Without `window.navigation`, the app has no previous entry and no entry state.
 */
export const HistoryStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withProps(() => {
    const navigation = inject(PlatformNavigation, { optional: true }) as PlatformNavigation | null | undefined;
    return {
      _navigation: navigation ?? undefined,
      _location: inject(Location),
      _router: inject(Router),
      _auth: inject(AuthStore),
      /**
       * The state of the entry before the first navigation. The router's first navigation calls `history.replaceState`,
       * which clears the entry state.
       */
      _startState: { value: navigation?.currentEntry?.getState() as unknown },
      _knownNames: [] as string[],
      _pending: { back: null as Promise<void> | null },
      _capture: { hasStarted: false, id: null as number | null, state: undefined as unknown },
    };
  }),
  withMethods((store) => {
    function isPreviousEntryTheUrlWithoutCompose(): boolean {
      const navigation = store._navigation;
      const current = navigation?.currentEntry;
      if (navigation === undefined || current === null || current === undefined) {
        return false;
      }
      const previousUrl = navigation.entries()[current.index - 1]?.url;
      if (previousUrl === null || previousUrl === undefined) {
        return false;
      }
      const { pathname, search, hash } = new URL(previousUrl);
      const previous = store._router.serializeUrl(store._router.parseUrl(`${pathname}${search}${hash}`));
      return previous === store._router.serializeUrl(withoutCompose(store._router.parseUrl(store._router.url)));
    }

    async function goBack(target: ReturnTarget): Promise<void> {
      if (!store.canGoBack()) {
        await store._router.navigateByUrl(target.url);
        return;
      }
      const ended = firstValueFrom(store._router.events.pipe(filter(isNavigationEnding)));
      store._location.back();
      await ended;
    }

    /**
     * While a back is pending, `canGoBack` and the router URL still show the old entry, so a second call (a double
     * click, a cancel button and a dialog hide event) returns the pending back and goes back no further.
     */
    function back(target: ReturnTarget): Promise<void> {
      if (store._pending.back !== null) {
        return store._pending.back;
      }
      const pending = goBack(target).finally(() => {
        store._pending.back = null;
      });
      store._pending.back = pending;
      return pending;
    }

    function removeCompose(): Promise<boolean> {
      return store._router.navigateByUrl(withoutCompose(store._router.parseUrl(store._router.url)), {
        replaceUrl: true,
      });
    }

    return {
      canReturn(target: ReturnTarget): boolean {
        return store.canGoBack() || target.permission === undefined || store._auth.hasPermission(target.permission);
      },
      back,
      readEntryState(): unknown {
        return store._navigation?.currentEntry?.getState();
      },
      writeEntryState(state: unknown): void {
        store._navigation?.updateCurrentEntry({ state });
      },
      /** Returns the entry state of the start once, and `undefined` on each later call. */
      takeStartState(): unknown {
        const { value } = store._startState;
        store._startState.value = undefined;
        return value;
      },
      /**
       * The entry state at the start of a navigation. The router replaces the target entry of a browser back or forward
       * before it ends, and that clears the state, so the history store captures it at `NavigationStart`. A push has
       * no state. Returns `undefined` for another navigation.
       */
      startStateOf(navigationId: number): unknown {
        return store._capture.id === navigationId ? store._capture.state : undefined;
      },
      /** Pushes the current router URL with the compose value, so the dialog has its own history entry. */
      openCompose(value: string): Promise<boolean> {
        return store._router.navigateByUrl(withCompose(store._router.parseUrl(store._router.url), value));
      },
      /**
       * Closes the dialog. When the previous entry is the page without the compose value, it goes back one entry.
       * Otherwise it replaces the URL, so a loaded URL does not grow the history.
       */
      async closeCompose(): Promise<boolean> {
        if (store.compose() === null) {
          return false;
        }
        if (store._pending.back !== null) {
          await store._pending.back;
          return true;
        }
        if (!store.canGoBack() || !isPreviousEntryTheUrlWithoutCompose()) {
          return removeCompose();
        }
        await back(HOME_RETURN_TARGET);
        return true;
      },
      removeCompose,
      /** Adds names to the known compose names. An unknown name in the URL is removed. Returns the unregister function. */
      registerComposeNames(names: readonly string[]): () => void {
        store._knownNames.push(...names);
        return () => {
          for (const name of names) {
            const index = store._knownNames.indexOf(name);
            if (index !== -1) {
              store._knownNames.splice(index, 1);
            }
          }
        };
      },
    };
  }),
  withHooks({
    onInit(store) {
      store._router.events
        .pipe(
          filter((event) => event instanceof NavigationStart),
          takeUntilDestroyed(),
        )
        .subscribe((event) => {
          const isFirst = !store._capture.hasStarted;
          const isReplacing =
            event.navigationTrigger === 'popstate' || store._router.currentNavigation()?.extras.replaceUrl === true;
          store._capture.hasStarted = true;
          store._capture.id = event.id;
          store._capture.state = isFirst ? store.takeStartState() : isReplacing ? store.readEntryState() : undefined;
        });
      store._router.events
        .pipe(filter(isNavigationEnding), takeUntilDestroyed())
        .subscribe(() => patchState(store, { canGoBack: store._navigation?.canGoBack ?? false }));
      store._router.events
        .pipe(
          filter((event) => event instanceof NavigationEnd),
          takeUntilDestroyed(),
        )
        .subscribe((event) => {
          const compose = store._router.parseUrl(event.urlAfterRedirects).queryParamMap.get(COMPOSE_PARAM);
          patchState(store, { compose });
          if (compose !== null && !store._knownNames.includes(parseCompose(compose).name)) {
            void store.removeCompose();
          }
        });
    },
  }),
  withStoreDevtools('history'),
);
