import { inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { signalStoreFeature, withHooks } from '@ngrx/signals';
import { debounceTime, filter, merge } from 'rxjs';

import { type RealtimeHint, type RealtimeTopic } from '@shared/realtime/realtime';
import { RealtimeConnection } from '@shared/realtime/realtime-connection';

/** How long the reload waits for more hints, so a burst of hints gives one reload. */
export const REALTIME_RELOAD_DEBOUNCE_MILLISECONDS = 1000;

function matchesScope(hint: RealtimeHint, scope: string | null): boolean {
  return scope !== null && [hint.caseId, hint.attemptId, hint.machineId, hint.workOrderId].includes(scope);
}

/**
 * Reloads a store when a hint of one of the topics arrives (once, 1 second after the last hint of a burst), and once
 * after each reconnect of the real-time connection, because the pages missed the hints of the gap. With `scopeId`, a
 * hint counts only when its `caseId`, `attemptId`, `machineId`, or `workOrderId` equals the scope that the store shows now. The
 * subscription ends with the store. A hint never starts the refresh cooldown of the refresh button: `reload` is the
 * method that the button calls after its own checks, never `startRefreshCooldown`.
 *
 * `TStore` names the members that `reload` and `scopeId` read. NgRx cannot type the store that a feature joins, so the
 * feature casts the store once, here (as `withActionResultReload` does).
 */
export function withRealtimeReload<TStore extends object = object>(
  topics: readonly RealtimeTopic[],
  reload: (store: TStore) => void,
  scopeId?: (store: TStore) => string | null,
) {
  return signalStoreFeature(
    withHooks({
      onInit(store) {
        const connection = inject(RealtimeConnection);
        const typedStore = store as unknown as TStore;
        const hints$ = connection.hints$.pipe(
          filter((hint) => topics.includes(hint.topic)),
          filter((hint) => scopeId === undefined || matchesScope(hint, scopeId(typedStore))),
          debounceTime(REALTIME_RELOAD_DEBOUNCE_MILLISECONDS),
        );
        merge(hints$, connection.reconnected$)
          .pipe(takeUntilDestroyed())
          .subscribe(() => {
            reload(typedStore);
          });
      },
    }),
  );
}
