import { effect, type Signal, untracked } from '@angular/core';
import { signalStoreFeature, withHooks } from '@ngrx/signals';

import type { ActionResult } from './app-action';

/** What the reload callback needs from a resource: any `ResourceRef` fits, whatever its value type. */
export interface Reloadable {
  reload(): boolean;
}

export interface ActionResultSource {
  readonly lastResult: Signal<ActionResult | null>;
}

/**
 * Starts an effect that calls `reload` with the result when the action result has a listed type and a higher sequence than the last
 * one that the caller saw. The result that exists at the call does not reload. Call it in an injection context
 * (for example in `onInit`). `withActionResultReload` wraps it as a store feature.
 */
export function reloadOnActionResult(
  source: ActionResultSource,
  types: readonly string[],
  reload: (result: ActionResult) => void,
): void {
  let seen = source.lastResult()?.sequence ?? 0;
  effect(() => {
    const result = source.lastResult();
    if (result !== null && types.includes(result.type) && result.sequence > seen) {
      seen = result.sequence;
      untracked(() => reload(result));
    }
  });
}

/**
 * Reloads the store when the action result has a listed type and a higher sequence than the last one that the store
 * saw. The store must have the prop `_actions`. `TStore` names the other members that `reload` reads (for example the
 * resource), so each caller gets a typed `store`. NgRx cannot type the store that a feature joins, and a feature that
 * lists `_actions` as input breaks the type of `signalStore`, so the feature casts the store once, here.
 */
export function withActionResultReload<TStore extends object = object>(
  types: readonly string[],
  reload: (store: TStore) => void,
) {
  return signalStoreFeature(
    withHooks({
      onInit(store) {
        const source = (store as unknown as { _actions: ActionResultSource })._actions;
        reloadOnActionResult(source, types, () => reload(store as unknown as TStore));
      },
    }),
  );
}
