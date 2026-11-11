import { type Signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';

import { delayedVisibility } from '@shared/loading/delayed-visibility';
import { SPINNER_MIN_VISIBLE_MS, SPINNER_SHOW_AFTER_MS } from '@shared/loading/loading-constants';

/**
 * Turns a loading signal into the signal that shows a skeleton: `true` only after the load lasted
 * `SPINNER_SHOW_AFTER_MS`, and then at least `SPINNER_MIN_VISIBLE_MS`. A fast load never flickers. Call it in an
 * injection context (a component field).
 */
export function delayedLoading(isLoading: Signal<boolean>): Signal<boolean> {
  return toSignal(toObservable(isLoading).pipe(delayedVisibility(SPINNER_SHOW_AFTER_MS, SPINNER_MIN_VISIBLE_MS)), {
    initialValue: false,
  });
}
