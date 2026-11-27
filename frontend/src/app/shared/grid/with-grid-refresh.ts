import { signalStoreFeature, withMethods } from '@ngrx/signals';

import { withRefreshCooldown } from '@shared/refresh/with-refresh-cooldown';

/**
 * Adds the refresh button of a grid to a store: `refresh()` reloads the grid and starts the cooldown of the button.
 * Only the button calls `refresh()`. A real-time hint and an action result call the resource reload directly, so they
 * never start the cooldown. `TStore` names the members that `reload` reads. NgRx cannot type the store that a feature
 * joins, so the feature casts the store once, here (as `withRealtimeReload` does).
 */
export function withGridRefresh<TStore extends object = object>(reload: (store: TStore) => void) {
  return signalStoreFeature(
    withRefreshCooldown(),
    withMethods((store) => ({
      refresh(): void {
        if (store.isRefreshCoolingDown()) {
          return;
        }

        store.startRefreshCooldown();
        reload(store as unknown as TStore);
      },
    })),
  );
}
