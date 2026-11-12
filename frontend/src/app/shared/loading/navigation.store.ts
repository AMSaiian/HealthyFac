import { inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationCancel, NavigationEnd, NavigationError, NavigationStart, Router } from '@angular/router';
import { signalStore, withProps } from '@ngrx/signals';
import { filter, map } from 'rxjs';

import { delayedVisibility } from '@shared/loading/delayed-visibility';
import { SPINNER_MIN_VISIBLE_MS, SPINNER_SHOW_AFTER_MS } from '@shared/loading/loading-constants';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

/** Tells the shell when a navigation is slow enough to show the spinner. */
export const NavigationStore = signalStore(
  { providedIn: 'root' },
  withProps(() => ({
    isSpinnerVisible: toSignal(
      inject(Router).events.pipe(
        filter(
          (event) =>
            event instanceof NavigationStart ||
            event instanceof NavigationEnd ||
            event instanceof NavigationCancel ||
            event instanceof NavigationError,
        ),
        map((event) => event instanceof NavigationStart),
        delayedVisibility(SPINNER_SHOW_AFTER_MS, SPINNER_MIN_VISIBLE_MS),
      ),
      { initialValue: false },
    ),
  })),
  withStoreDevtools('navigation'),
);
