import { computed } from '@angular/core';
import { patchState, signalStoreFeature, withComputed, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { interval, pipe, switchMap, take, tap } from 'rxjs';

/** How long a refresh button stays off after a click (Artur 2026-11-18). */
export const REFRESH_COOLDOWN_SECONDS = 5;

const SECOND_MS = 1000;

/**
 * Adds the cooldown of a refresh button to a store: `startRefreshCooldown` turns the button off, and
 * `refreshSecondsLeft` counts down to 0 once per second. The store checks `isRefreshCoolingDown` before it reloads.
 */
export function withRefreshCooldown() {
  return signalStoreFeature(
    withState({ refreshSecondsLeft: 0 }),
    withComputed((store) => ({
      isRefreshCoolingDown: computed(() => store.refreshSecondsLeft() > 0),
    })),
    withMethods((store) => {
      const countDown = rxMethod<void>(
        pipe(
          switchMap(() =>
            interval(SECOND_MS).pipe(
              take(REFRESH_COOLDOWN_SECONDS),
              tap((tick) => {
                patchState(store, { refreshSecondsLeft: REFRESH_COOLDOWN_SECONDS - 1 - tick });
              }),
            ),
          ),
        ),
      );
      return {
        startRefreshCooldown(): void {
          patchState(store, { refreshSecondsLeft: REFRESH_COOLDOWN_SECONDS });
          countDown();
        },
      };
    }),
  );
}
